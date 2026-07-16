package com.denfense.server.service.balance;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import org.springframework.core.io.DefaultResourceLoader;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class BalanceManifestLoaderTest {

    @TempDir
    Path tempDir;

    private ObjectMapper mapper;

    @BeforeEach
    void setUp() throws Exception {
        mapper = new ObjectMapper();
        for (String name : BalanceManifestSupport.REQUIRED_FILES) {
            Files.writeString(tempDir.resolve(name), "{\"name\":\"" + name + "\"}");
        }
    }

    @Test
    void loadsValidManifestAndExposesVersionRegistry() throws Exception {
        BalanceVersionRegistry registry = new BalanceVersionRegistry();
        BalanceManifest manifest = validManifest();
        writeManifest(manifest);

        loader(registry).load();

        assertThat(registry.getSchemaVersion()).isEqualTo(1);
        assertThat(registry.getBalanceVersion()).isEqualTo(manifest.balanceVersion());
        assertThat(registry.getContentHash()).isEqualTo(manifest.contentHash());
        assertThat(registry.getFiles()).isEqualTo(manifest.files());
    }

    @Test
    void missingManifestAndUnsupportedSchemaFailFast() throws Exception {
        assertThatThrownBy(() -> loader(new BalanceVersionRegistry()).load()).hasMessageContaining("not found");

        BalanceManifest valid = validManifest();
        writeManifest(new BalanceManifest(99, valid.balanceVersion(), valid.contentHash(), valid.files()));
        assertThatThrownBy(() -> loader(new BalanceVersionRegistry()).load()).hasMessageContaining("schemaVersion");
    }

    @Test
    void missingFileShaAndSizeMismatchFailFast() throws Exception {
        BalanceManifest valid = validManifest();
        writeManifest(valid);
        Files.delete(tempDir.resolve(valid.files().get(0).name()));
        assertThatThrownBy(() -> loader(new BalanceVersionRegistry()).load()).hasMessageContaining("not found");

        restoreFiles();
        List<BalanceManifestFileEntry> shaEntries = new ArrayList<>(validManifest().files());
        BalanceManifestFileEntry first = shaEntries.get(0);
        shaEntries.set(0, new BalanceManifestFileEntry(first.name(), "0".repeat(64), first.size()));
        writeManifest(withEntries(shaEntries));
        assertThatThrownBy(() -> loader(new BalanceVersionRegistry()).load()).hasMessageContaining("SHA-256");

        List<BalanceManifestFileEntry> sizeEntries = new ArrayList<>(validManifest().files());
        first = sizeEntries.get(0);
        sizeEntries.set(0, new BalanceManifestFileEntry(first.name(), first.sha256(), first.size() + 1));
        writeManifest(withEntries(sizeEntries));
        assertThatThrownBy(() -> loader(new BalanceVersionRegistry()).load()).hasMessageContaining("size");
    }

    @Test
    void contentHashDuplicateOrderingAndTraversalFailFast() throws Exception {
        BalanceManifest valid = validManifest();
        writeManifest(new BalanceManifest(1, valid.balanceVersion(), "0".repeat(64), valid.files()));
        assertThatThrownBy(() -> loader(new BalanceVersionRegistry()).load()).hasMessageContaining("contentHash");

        List<BalanceManifestFileEntry> duplicate = new ArrayList<>(valid.files());
        duplicate.add(valid.files().get(0));
        writeManifest(new BalanceManifest(1, valid.balanceVersion(), valid.contentHash(), duplicate));
        assertThatThrownBy(() -> loader(new BalanceVersionRegistry()).load()).hasMessageContaining("Duplicate");

        List<BalanceManifestFileEntry> unsorted = new ArrayList<>(valid.files());
        java.util.Collections.swap(unsorted, 0, 1);
        writeManifest(new BalanceManifest(1, valid.balanceVersion(), valid.contentHash(), unsorted));
        assertThatThrownBy(() -> loader(new BalanceVersionRegistry()).load()).hasMessageContaining("sorted");

        List<BalanceManifestFileEntry> traversal = new ArrayList<>(valid.files());
        traversal.set(0, new BalanceManifestFileEntry("../escape.json", "0".repeat(64), 1));
        writeManifest(new BalanceManifest(1, valid.balanceVersion(), valid.contentHash(), traversal));
        assertThatThrownBy(() -> loader(new BalanceVersionRegistry()).load()).hasMessageContaining("Unsafe");
    }

    private BalanceManifestLoader loader(BalanceVersionRegistry registry) {
        BalanceManifestLoader loader = new BalanceManifestLoader(new DefaultResourceLoader(), mapper, registry);
        loader.setManifestPath(tempDir.resolve("balance-manifest.json").toUri().toString());
        loader.setGeneratedBasePath(tempDir.toUri().toString());
        return loader;
    }

    private BalanceManifest validManifest() throws Exception {
        List<BalanceManifestFileEntry> entries = new ArrayList<>();
        for (String name : BalanceManifestSupport.REQUIRED_FILES) {
            byte[] bytes = Files.readAllBytes(tempDir.resolve(name));
            entries.add(new BalanceManifestFileEntry(name, BalanceManifestSupport.sha256(bytes), bytes.length));
        }
        entries.sort(Comparator.comparing(BalanceManifestFileEntry::name));
        return withEntries(entries);
    }

    private BalanceManifest withEntries(List<BalanceManifestFileEntry> entries) {
        String hash = BalanceManifestSupport.contentHash(entries);
        return new BalanceManifest(1, BalanceManifestSupport.balanceVersion(hash), hash, entries);
    }

    private void writeManifest(BalanceManifest manifest) throws Exception {
        mapper.writeValue(tempDir.resolve("balance-manifest.json").toFile(), manifest);
    }

    private void restoreFiles() throws Exception {
        for (String name : BalanceManifestSupport.REQUIRED_FILES) {
            Files.writeString(tempDir.resolve(name), "{\"name\":\"" + name + "\"}");
        }
    }
}
