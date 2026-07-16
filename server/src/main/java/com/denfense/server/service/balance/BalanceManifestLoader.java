package com.denfense.server.service.balance;

import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.core.annotation.Order;
import org.springframework.core.io.Resource;
import org.springframework.core.io.ResourceLoader;
import org.springframework.stereotype.Component;

import java.io.IOException;
import java.io.InputStream;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

@Slf4j
@Component
@RequiredArgsConstructor
@Order(0)
public class BalanceManifestLoader implements ApplicationRunner {

    private final ResourceLoader resourceLoader;
    private final ObjectMapper objectMapper;
    private final BalanceVersionRegistry registry;

    @Value("${balance.manifest.path:classpath:balance/generated/balance-manifest.json}")
    private String manifestPath;

    @Value("${balance.generated.base-path:classpath:balance/generated/}")
    private String generatedBasePath;

    @Override
    public void run(ApplicationArguments args) throws Exception {
        load();
    }

    public void load() throws IOException {
        Resource manifestResource = resourceLoader.getResource(manifestPath);
        if (!manifestResource.exists()) {
            throw new IllegalStateException("Balance manifest not found: " + manifestPath);
        }

        ObjectMapper strictMapper = objectMapper.copy()
                .enable(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES);
        BalanceManifest manifest;
        try (InputStream input = manifestResource.getInputStream()) {
            manifest = strictMapper.readValue(input, BalanceManifest.class);
        }
        validateAndInitialize(manifest);
        log.info("Balance manifest verified. version={}, contentHash={}",
                manifest.balanceVersion(), manifest.contentHash());
    }

    void validateAndInitialize(BalanceManifest manifest) throws IOException {
        if (manifest.schemaVersion() != BalanceManifestSupport.SCHEMA_VERSION) {
            throw new IllegalStateException("Unsupported balance manifest schemaVersion: " + manifest.schemaVersion());
        }

        List<BalanceManifestFileEntry> actualEntries = new ArrayList<>();
        Set<String> names = new HashSet<>();
        String previousName = null;
        for (BalanceManifestFileEntry declared : manifest.files()) {
            String name = declared.name();
            if (!BalanceManifestSupport.isSafeFileName(name)) {
                throw new IllegalStateException("Unsafe balance manifest file name: " + name);
            }
            if (!names.add(name)) {
                throw new IllegalStateException("Duplicate balance manifest file entry: " + name);
            }
            if (previousName != null && previousName.compareTo(name) >= 0) {
                throw new IllegalStateException("Balance manifest files must be sorted by name.");
            }
            previousName = name;

            Resource resource = resourceLoader.getResource(generatedBasePath + name);
            if (!resource.exists()) {
                throw new IllegalStateException("Balance data file not found: " + name);
            }
            byte[] bytes;
            try (InputStream input = resource.getInputStream()) {
                bytes = input.readAllBytes();
            }
            String sha256 = BalanceManifestSupport.sha256(bytes);
            if (declared.size() != bytes.length) {
                throw new IllegalStateException("Balance data size mismatch: " + name);
            }
            if (!sha256.equals(declared.sha256())) {
                throw new IllegalStateException("Balance data SHA-256 mismatch: " + name);
            }
            actualEntries.add(new BalanceManifestFileEntry(name, sha256, bytes.length));
        }

        if (!names.containsAll(BalanceManifestSupport.REQUIRED_FILES)) {
            throw new IllegalStateException("Required balance manifest files are missing. expected="
                    + BalanceManifestSupport.REQUIRED_FILES + ", actual=" + names);
        }
        String contentHash = BalanceManifestSupport.contentHash(actualEntries);
        if (!contentHash.equals(manifest.contentHash())) {
            throw new IllegalStateException("Balance manifest contentHash mismatch.");
        }
        if (!BalanceManifestSupport.balanceVersion(contentHash).equals(manifest.balanceVersion())) {
            throw new IllegalStateException("Balance manifest balanceVersion mismatch.");
        }

        registry.init(new BalanceManifest(
                manifest.schemaVersion(), manifest.balanceVersion(), manifest.contentHash(), actualEntries));
    }

    public void setManifestPath(String manifestPath) {
        this.manifestPath = manifestPath;
    }

    public void setGeneratedBasePath(String generatedBasePath) {
        this.generatedBasePath = generatedBasePath.endsWith("/") ? generatedBasePath : generatedBasePath + "/";
    }
}
