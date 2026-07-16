package com.denfense.server.balance;

import com.denfense.server.balance.tool.BalanceConversionException;
import com.denfense.server.balance.tool.BalanceJsonWriter;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.LinkedHashMap;
import java.util.Map;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class BalanceJsonWriterAtomicTest {
    @TempDir Path tempDir;

    @Test
    void replacementFailureRestoresEveryPreviousGeneratedFile() throws Exception {
        Path firstTarget = tempDir.resolve("first.json");
        Path secondTarget = tempDir.resolve("second.json");
        Files.writeString(firstTarget, "old-first");
        Files.writeString(secondTarget, "old-second");

        Path firstStaged = tempDir.resolve("first.staged");
        Files.writeString(firstStaged, "new-first");
        Path missingSecondStaged = tempDir.resolve("missing-second.staged");
        Map<Path, Path> replacements = new LinkedHashMap<>();
        replacements.put(firstStaged, firstTarget);
        replacements.put(missingSecondStaged, secondTarget);

        assertThatThrownBy(() -> new BalanceJsonWriter().replaceFilesAtomically(replacements))
                .isInstanceOf(BalanceConversionException.class);
        assertThat(Files.readString(firstTarget)).isEqualTo("old-first");
        assertThat(Files.readString(secondTarget)).isEqualTo("old-second");
    }
}
