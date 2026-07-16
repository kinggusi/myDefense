package com.denfense.server.balance.battle;

import com.denfense.server.balance.tool.BalanceConversionException;
import com.denfense.server.balance.tool.battle.BattleBalanceExcelConverter;
import org.apache.poi.ss.usermodel.CellType;
import org.apache.poi.ss.usermodel.Row;
import org.apache.poi.ss.usermodel.Sheet;
import org.apache.poi.ss.usermodel.Workbook;
import org.apache.poi.ss.usermodel.WorkbookFactory;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

import java.io.InputStream;
import java.io.OutputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;
import java.util.function.Consumer;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.jupiter.api.Assertions.assertDoesNotThrow;
import static org.junit.jupiter.api.Assertions.assertThrows;

class BattleBalanceExcelConverterTest {
    private static final Path SOURCE = Path.of("..", "balance", "source", "battle-balance.xlsx").normalize();
    private static final List<String> OUTPUT_FILES = List.of(
            "wave-spec.json", "wave-spawn-spec.json", "boss-pattern-spec.json", "skill-spec.json",
            "alien-skill-links.json", "projectile-spec.json", "skill-effect-spec.json", "battle-balance-manifest.json");

    @TempDir
    Path tempDir;

    @Test
    void validWorkbookConvertsSevenDocumentsAndManifest() throws Exception {
        Path output = tempDir.resolve("output");
        BattleBalanceExcelConverter.convert(SOURCE, output);

        for (String file : OUTPUT_FILES) assertThat(output.resolve(file)).exists();
        assertThat(Files.readString(output.resolve("wave-spec.json"))).contains("\"roundNumber\":20");
        assertThat(Files.readString(output.resolve("skill-spec.json"))).contains("\"items\": []");
        assertThat(Files.readString(output.resolve("battle-balance-manifest.json")))
                .contains("\"balanceVersion\": \"battle-v1\"")
                .contains("Balance/Battle/wave-spec");
    }

    @Test
    void missingRequiredSheetFails() throws Exception {
        Path workbook = mutated(source -> source.removeSheetAt(source.getSheetIndex("SkillSpec")));
        assertThrows(BalanceConversionException.class, () -> convert(workbook));
    }

    @Test
    void headerTypoFails() throws Exception {
        Path workbook = mutated(source -> source.getSheet("WaveSpec").getRow(0).getCell(0).setCellValue("waveID"));
        assertThrows(BalanceConversionException.class, () -> convert(workbook));
    }

    @Test
    void missingRequiredValueFails() throws Exception {
        Path workbook = mutated(source -> source.getSheet("WaveSpec").getRow(1).getCell(0).setBlank());
        assertThrows(BalanceConversionException.class, () -> convert(workbook));
    }

    @Test
    void numericAndBooleanStringsFail() throws Exception {
        Path numeric = mutated(source -> source.getSheet("WaveSpec").getRow(1).getCell(1).setCellValue("1"));
        assertThrows(BalanceConversionException.class, () -> convert(numeric));

        Path bool = mutated(source -> source.getSheet("WaveSpec").getRow(1).getCell(5).setCellValue("true"));
        assertThrows(BalanceConversionException.class, () -> convert(bool));
    }

    @Test
    void invalidCaseSensitiveEnumFails() throws Exception {
        Path workbook = mutated(source -> source.getSheet("WaveSpec").getRow(1).getCell(2).setCellValue("regular"));
        assertThrows(BalanceConversionException.class, () -> convert(workbook));
    }

    @Test
    void duplicateWaveAndSpawnKeysFail() throws Exception {
        Path duplicateWave = mutated(source -> source.getSheet("WaveSpec").getRow(2).getCell(0).setCellValue("WAVE_001"));
        assertThrows(BalanceConversionException.class, () -> convert(duplicateWave));

        Path duplicateSpawn = mutated(source -> source.getSheet("WaveSpawnSpec").getRow(2).getCell(0).setCellValue("WAVE_001"));
        assertThrows(BalanceConversionException.class, () -> convert(duplicateSpawn));
    }

    @Test
    void bossSpawnCountOtherThanOneFails() throws Exception {
        Path workbook = mutated(source -> source.getSheet("WaveSpawnSpec").getRow(10).getCell(4).setCellValue(2));
        assertThrows(BalanceConversionException.class, () -> convert(workbook));
    }

    @Test
    void enabledRegularWithoutSpawnFails() throws Exception {
        Path workbook = mutated(source -> source.getSheet("WaveSpawnSpec").removeRow(source.getSheet("WaveSpawnSpec").getRow(1)));
        assertThrows(BalanceConversionException.class, () -> convert(workbook));
    }

    @Test
    void negativeMaxTargetCountFails() throws Exception {
        Path workbook = mutated(source -> addSkill(source.getSheet("SkillSpec"), -1));
        assertThrows(BalanceConversionException.class, () -> convert(workbook));
    }

    @Test
    void missingParameterValueFails() throws Exception {
        Path workbook = mutated(source -> {
            Sheet sheet = source.getSheet("BossPatternSpec");
            Row row = sheet.createRow(1);
            row.createCell(0).setCellValue("WAVE_010");
            row.createCell(1).setCellValue(1);
            row.createCell(2).setCellValue("WAIT");
            row.createCell(3).setCellValue("TIME");
            row.createCell(4).setCellValue(1.0);
            row.createCell(5).setCellValue(0.0);
            row.createCell(6).setCellValue("");
            row.createCell(7).setCellValue("");
            row.createCell(9).setCellValue(true);
        });
        assertThrows(BalanceConversionException.class, () -> convert(workbook));
    }

    @Test
    void instantSpeedZeroPasses() throws Exception {
        Path workbook = mutated(source -> addProjectile(source.getSheet("ProjectileSpec"), "INSTANT", 0.0));
        assertDoesNotThrow(() -> convert(workbook));
    }

    @Test
    void nonInstantSpeedZeroFails() throws Exception {
        Path workbook = mutated(source -> addProjectile(source.getSheet("ProjectileSpec"), "HOMING", 0.0));
        assertThrows(BalanceConversionException.class, () -> convert(workbook));
    }

    @Test
    void maxStacksZeroFails() throws Exception {
        Path workbook = mutated(source -> {
            addSkill(source.getSheet("SkillSpec"), 0);
            Sheet sheet = source.getSheet("SkillEffectSpec");
            Row row = sheet.createRow(1);
            row.createCell(0).setCellValue("SKILL_TEST");
            row.createCell(1).setCellValue(1);
            row.createCell(2).setCellValue("ON_HIT");
            row.createCell(3).setCellValue("DAMAGE");
            row.createCell(4).setCellValue("FLAT");
            row.createCell(5).setCellValue(0.0);
            row.createCell(6).setCellValue(1.0);
            row.createCell(7).setCellValue(1.0);
            row.createCell(8).setCellValue(0.0);
            row.createCell(9).setCellValue(0.0);
            row.createCell(10).setCellValue(0.0);
            row.createCell(11).setCellValue(0);
            row.createCell(12).setCellValue("NONE");
            row.createCell(13).setCellValue(1.0);
        });
        assertThrows(BalanceConversionException.class, () -> convert(workbook));
    }

    @Test
    void convertingTwiceProducesIdenticalBytes() throws Exception {
        Path first = tempDir.resolve("first");
        Path second = tempDir.resolve("second");
        BattleBalanceExcelConverter.convert(SOURCE, first);
        BattleBalanceExcelConverter.convert(SOURCE, second);

        for (String file : OUTPUT_FILES)
            assertThat(Files.readAllBytes(first.resolve(file))).containsExactly(Files.readAllBytes(second.resolve(file)));
    }

    private Path mutated(Consumer<Workbook> mutation) throws Exception {
        Path target = Files.createTempFile(tempDir, "battle-balance-", ".xlsx");
        try (InputStream input = Files.newInputStream(SOURCE);
             Workbook workbook = WorkbookFactory.create(input)) {
            mutation.accept(workbook);
            try (OutputStream output = Files.newOutputStream(target)) {
                workbook.write(output);
            }
        }
        return target;
    }

    private void convert(Path workbook) {
        BattleBalanceExcelConverter.convert(workbook, tempDir.resolve("out-" + System.nanoTime()));
    }

    private static void addSkill(Sheet sheet, int maxTargetCount) {
        Row row = sheet.createRow(1);
        row.createCell(0).setCellValue("SKILL_TEST");
        row.createCell(1).setCellValue("skill.test.name");
        row.createCell(2).setCellValue("");
        row.createCell(3).setCellValue("ACTIVE");
        row.createCell(4).setCellValue("AUTO_COOLDOWN");
        row.createCell(5).setCellValue(1.0);
        row.createCell(6).setCellValue(0.0);
        row.createCell(7).setCellValue(5.0);
        row.createCell(8).setCellValue("DEFAULT_PROGRESS");
        row.createCell(9).setCellValue(maxTargetCount);
        row.createCell(10).setCellValue("");
        row.createCell(11).setCellValue("");
        row.createCell(12).setCellValue("");
        row.createCell(13).setCellValue("");
        row.createCell(14).setCellValue(true);
    }

    private static void addProjectile(Sheet sheet, String moveType, double speed) {
        Row row = sheet.createRow(1);
        row.createCell(0).setCellValue("PROJECTILE_TEST");
        row.createCell(1).setCellValue("Bullet");
        row.createCell(2).setCellValue(moveType);
        row.createCell(3).setCellValue(speed);
        row.createCell(4).setCellValue(5.0);
        row.createCell(5).setCellValue(0.0);
        row.createCell(6).setCellValue(0);
        row.createCell(7).setCellValue(true);
        row.createCell(8).setCellValue("DESTROY");
        row.createCell(9).setCellValue(true);
    }
}
