package com.denfense.server.balance;

import com.denfense.server.balance.tool.BalanceManifestGenerator;
import com.denfense.server.service.balance.BalanceManifest;
import com.denfense.server.service.balance.BalanceManifestFileEntry;
import com.denfense.server.service.balance.BalanceManifestSupport;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class BalanceManifestGeneratorTest {

    @TempDir
    Path tempDir;

    private final BalanceManifestGenerator generator = new BalanceManifestGenerator();

    @Test
    void generatesSortedDeterministicManifestWithoutVolatileFields() throws Exception {
        writeRequiredFiles();

        BalanceManifest first = generator.generate(tempDir);
        byte[] firstBytes = Files.readAllBytes(tempDir.resolve(BalanceManifestSupport.MANIFEST_FILE_NAME));
        BalanceManifest second = generator.generate(tempDir);
        byte[] secondBytes = Files.readAllBytes(tempDir.resolve(BalanceManifestSupport.MANIFEST_FILE_NAME));

        assertThat(first.files()).extracting(BalanceManifestFileEntry::name).isSorted();
        assertThat(first.files()).hasSize(BalanceManifestSupport.REQUIRED_FILES.size());
        assertThat(first).isEqualTo(second);
        assertThat(secondBytes).isEqualTo(firstBytes);
        assertThat(new String(firstBytes)).doesNotContain("timestamp", "generatedAt", "buildTime");
        assertThat(first.balanceVersion()).isEqualTo("1-" + first.contentHash().substring(0, 16));
    }

    @Test
    void fileContentNameAndSizeAffectContentHash() throws Exception {
        writeRequiredFiles();
        BalanceManifest initial = generator.generate(tempDir);

        Path reward = tempDir.resolve("game-reward.json");
        Files.writeString(reward, "{\"changed\":true}");
        BalanceManifest contentChanged = generator.generate(tempDir);
        assertThat(contentChanged.contentHash()).isNotEqualTo(initial.contentHash());
        assertThat(contentChanged.files().stream().filter(e -> e.name().equals("game-reward.json")).findFirst().orElseThrow().size())
                .isEqualTo(Files.size(reward));

        List<BalanceManifestFileEntry> withExtraName = new ArrayList<>(initial.files());
        withExtraName.add(new BalanceManifestFileEntry("extra-a.json", "0".repeat(64), 1));
        String firstExtraHash = generator.createManifest(withExtraName).contentHash();
        withExtraName.set(withExtraName.size() - 1,
                new BalanceManifestFileEntry("extra-b.json", "0".repeat(64), 1));
        assertThat(generator.createManifest(withExtraName).contentHash()).isNotEqualTo(firstExtraHash);
    }

    @Test
    void excludesManifestItselfFromFiles() throws Exception {
        writeRequiredFiles();
        Files.writeString(tempDir.resolve(BalanceManifestSupport.MANIFEST_FILE_NAME), "old");
        BalanceManifest manifest = generator.generate(tempDir);
        assertThat(manifest.files()).extracting(BalanceManifestFileEntry::name)
                .doesNotContain(BalanceManifestSupport.MANIFEST_FILE_NAME);
    }

    @Test
    void emptyDirectoryAndMissingRequiredJsonFailWithoutReplacingPreviousManifest() throws Exception {
        assertThatThrownBy(() -> generator.generate(tempDir)).isInstanceOf(IllegalStateException.class);

        writeRequiredFiles();
        Path manifestPath = tempDir.resolve(BalanceManifestSupport.MANIFEST_FILE_NAME);
        Files.writeString(manifestPath, "known-good");
        Files.delete(tempDir.resolve("alien-spec.json"));

        assertThatThrownBy(() -> generator.generate(tempDir)).isInstanceOf(IllegalStateException.class);
        assertThat(Files.readString(manifestPath)).isEqualTo("known-good");
    }

    @Test
    void duplicateAndUnsafeNamesAreRejected() {
        List<BalanceManifestFileEntry> entries = requiredEntries();
        entries.add(entries.get(0));
        assertThatThrownBy(() -> generator.createManifest(entries)).hasMessageContaining("Duplicate");

        List<BalanceManifestFileEntry> unsafe = requiredEntries();
        unsafe.add(new BalanceManifestFileEntry("../escape.json", "0".repeat(64), 1));
        assertThatThrownBy(() -> generator.createManifest(unsafe)).hasMessageContaining("Unsafe");
    }

    private void writeRequiredFiles() throws Exception {
        for (String name : BalanceManifestSupport.REQUIRED_FILES) {
            Files.writeString(tempDir.resolve(name), "{\"name\":\"" + name + "\"}");
        }
    }

    private List<BalanceManifestFileEntry> requiredEntries() {
        return new ArrayList<>(BalanceManifestSupport.REQUIRED_FILES.stream()
                .map(name -> new BalanceManifestFileEntry(name, "0".repeat(64), 1))
                .toList());
    }
}
