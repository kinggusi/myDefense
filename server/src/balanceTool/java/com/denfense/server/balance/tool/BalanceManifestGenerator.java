package com.denfense.server.balance.tool;

import com.denfense.server.service.balance.BalanceManifest;
import com.denfense.server.service.balance.BalanceManifestFileEntry;
import com.denfense.server.service.balance.BalanceManifestSupport;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;

import java.io.IOException;
import java.nio.file.AtomicMoveNotSupportedException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

public class BalanceManifestGenerator {

    private final ObjectMapper objectMapper;

    public BalanceManifestGenerator() {
        objectMapper = new ObjectMapper().enable(SerializationFeature.INDENT_OUTPUT);
    }

    public BalanceManifest generate(Path generatedDirectory) throws IOException {
        if (!Files.isDirectory(generatedDirectory)) {
            throw new IllegalStateException("Generated balance directory not found: " + generatedDirectory);
        }

        List<Path> files;
        try (var stream = Files.list(generatedDirectory)) {
            files = stream
                    .filter(Files::isRegularFile)
                    .filter(path -> path.getFileName().toString().endsWith(".json"))
                    .filter(path -> !BalanceManifestSupport.MANIFEST_FILE_NAME.equals(path.getFileName().toString()))
                    .sorted(Comparator.comparing(path -> path.getFileName().toString()))
                    .toList();
        }

        List<BalanceManifestFileEntry> entries = new ArrayList<>();
        for (Path file : files) {
            byte[] bytes = Files.readAllBytes(file);
            entries.add(new BalanceManifestFileEntry(
                    file.getFileName().toString(), BalanceManifestSupport.sha256(bytes), bytes.length));
        }
        BalanceManifest manifest = createManifest(entries);
        writeAtomically(generatedDirectory.resolve(BalanceManifestSupport.MANIFEST_FILE_NAME), manifest);
        return manifest;
    }

    public BalanceManifest createManifest(List<BalanceManifestFileEntry> sourceEntries) {
        if (sourceEntries.isEmpty()) {
            throw new IllegalStateException("Generated balance directory contains no JSON files.");
        }
        List<BalanceManifestFileEntry> entries = sourceEntries.stream()
                .sorted(Comparator.comparing(BalanceManifestFileEntry::name))
                .toList();
        Set<String> names = new HashSet<>();
        for (BalanceManifestFileEntry entry : entries) {
            if (!BalanceManifestSupport.isSafeFileName(entry.name())) {
                throw new IllegalStateException("Unsafe generated balance file name: " + entry.name());
            }
            if (!names.add(entry.name())) {
                throw new IllegalStateException("Duplicate generated balance file name: " + entry.name());
            }
        }
        if (!names.containsAll(BalanceManifestSupport.REQUIRED_FILES)) {
            throw new IllegalStateException("Required generated balance JSON is missing: "
                    + BalanceManifestSupport.REQUIRED_FILES.stream().filter(name -> !names.contains(name)).sorted().toList());
        }
        String contentHash = BalanceManifestSupport.contentHash(entries);
        return new BalanceManifest(
                BalanceManifestSupport.SCHEMA_VERSION,
                BalanceManifestSupport.balanceVersion(contentHash),
                contentHash,
                entries);
    }

    private void writeAtomically(Path manifestPath, BalanceManifest manifest) throws IOException {
        Files.createDirectories(manifestPath.getParent());
        Path temp = Files.createTempFile(manifestPath.getParent(), "balance-manifest-", ".tmp");
        try {
            objectMapper.writeValue(temp.toFile(), manifest);
            try {
                Files.move(temp, manifestPath,
                        StandardCopyOption.ATOMIC_MOVE, StandardCopyOption.REPLACE_EXISTING);
            } catch (AtomicMoveNotSupportedException e) {
                Files.move(temp, manifestPath, StandardCopyOption.REPLACE_EXISTING);
            }
        } finally {
            Files.deleteIfExists(temp);
        }
    }
}
