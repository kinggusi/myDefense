package com.denfense.server.balance.tool.battle;

import com.denfense.server.balance.tool.BalanceConversionException;
import org.apache.poi.ss.usermodel.Cell;
import org.apache.poi.ss.usermodel.CellType;
import org.apache.poi.ss.usermodel.DateUtil;
import org.apache.poi.ss.usermodel.Row;
import org.apache.poi.ss.usermodel.Sheet;
import org.apache.poi.ss.usermodel.Workbook;
import org.apache.poi.ss.usermodel.WorkbookFactory;

import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

import static com.denfense.server.balance.tool.battle.BattleBalanceData.*;

public final class BattleExcelReader {
    public static final List<String> SHEET_NAMES = List.of(
            "WaveSpec", "WaveSpawnSpec", "BossPatternSpec", "SkillSpec",
            "AlienSkillLink", "ProjectileSpec", "SkillEffectSpec");

    public static final List<String> WAVE_HEADERS = List.of(
            "waveId", "roundNumber", "waveType", "nextWaveDelaySeconds", "bossTimeLimitSeconds", "enabled");
    public static final List<String> SPAWN_HEADERS = List.of(
            "waveId", "spawnOrder", "lanePolicy", "monsterId", "spawnCount", "spawnDelaySeconds",
            "spawnIntervalSeconds", "hpMultiplier", "moveSpeedMultiplier");
    public static final List<String> BOSS_PATTERN_HEADERS = List.of(
            "waveId", "patternOrder", "patternType", "triggerType", "triggerValue", "cooldownSeconds",
            "skillId", "parameterKey", "parameterValue", "enabled");
    public static final List<String> SKILL_HEADERS = List.of(
            "skillId", "nameKey", "descriptionKey", "skillType", "triggerType", "cooldownSeconds", "mpCost",
            "castRange", "targetPolicy", "maxTargetCount", "projectileId", "animationKey", "vfxKey", "sfxKey", "enabled");
    public static final List<String> ALIEN_SKILL_HEADERS = List.of(
            "alienId", "skillId", "slotIndex", "castPriority", "enabled");
    public static final List<String> PROJECTILE_HEADERS = List.of(
            "projectileId", "prefabKey", "moveType", "speed", "lifetimeSeconds", "hitRadius", "pierceCount",
            "destroyOnHit", "lostTargetPolicy", "enabled");
    public static final List<String> SKILL_EFFECT_HEADERS = List.of(
            "skillId", "executionOrder", "triggerPhase", "effectType", "magnitudeSource", "baseMagnitude",
            "coefficient", "chance", "durationSeconds", "tickIntervalSeconds", "radius", "maxStacks",
            "stackPolicy", "bossMultiplier");

    private final Path excelPath;

    public BattleExcelReader(Path excelPath) {
        this.excelPath = excelPath;
    }

    public Data read() {
        try (InputStream input = Files.newInputStream(excelPath);
             Workbook workbook = WorkbookFactory.create(input)) {
            validateSheets(workbook);
            return new Data(
                    readWaves(workbook),
                    readSpawns(workbook),
                    readBossPatterns(workbook),
                    readSkills(workbook),
                    readAlienSkillLinks(workbook),
                    readProjectiles(workbook),
                    readSkillEffects(workbook));
        } catch (IOException exception) {
            throw new BalanceConversionException("Battle Excel 파일을 읽을 수 없습니다: " + excelPath, exception);
        }
    }

    private static void validateSheets(Workbook workbook) {
        Set<String> actual = new LinkedHashSet<>();
        for (int index = 0; index < workbook.getNumberOfSheets(); index++) {
            Sheet sheet = workbook.getSheetAt(index);
            actual.add(sheet.getSheetName());
            if (sheet.getNumMergedRegions() > 0)
                throw new BalanceConversionException("[" + sheet.getSheetName() + "] 병합 셀은 허용되지 않습니다.");
        }
        if (!actual.equals(new LinkedHashSet<>(SHEET_NAMES)))
            throw new BalanceConversionException("Battle Excel 시트가 정확히 일치해야 합니다. expected=" + SHEET_NAMES + ", actual=" + actual);
    }

    private static List<WaveSpec> readWaves(Workbook workbook) {
        Sheet sheet = checkedSheet(workbook, "WaveSpec", WAVE_HEADERS);
        List<WaveSpec> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (!hasData(row, WAVE_HEADERS.size())) continue;
            result.add(new WaveSpec(
                    stringCell(sheet, row, rowIndex, 0, "waveId", true),
                    intCell(sheet, row, rowIndex, 1, "roundNumber"),
                    stringCell(sheet, row, rowIndex, 2, "waveType", true),
                    decimalCell(sheet, row, rowIndex, 3, "nextWaveDelaySeconds"),
                    decimalCell(sheet, row, rowIndex, 4, "bossTimeLimitSeconds"),
                    booleanCell(sheet, row, rowIndex, 5, "enabled")));
        }
        return result;
    }

    private static List<WaveSpawnSpec> readSpawns(Workbook workbook) {
        Sheet sheet = checkedSheet(workbook, "WaveSpawnSpec", SPAWN_HEADERS);
        List<WaveSpawnSpec> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (!hasData(row, SPAWN_HEADERS.size())) continue;
            result.add(new WaveSpawnSpec(
                    stringCell(sheet, row, rowIndex, 0, "waveId", true),
                    intCell(sheet, row, rowIndex, 1, "spawnOrder"),
                    stringCell(sheet, row, rowIndex, 2, "lanePolicy", true),
                    stringCell(sheet, row, rowIndex, 3, "monsterId", true),
                    intCell(sheet, row, rowIndex, 4, "spawnCount"),
                    decimalCell(sheet, row, rowIndex, 5, "spawnDelaySeconds"),
                    decimalCell(sheet, row, rowIndex, 6, "spawnIntervalSeconds"),
                    decimalCell(sheet, row, rowIndex, 7, "hpMultiplier"),
                    decimalCell(sheet, row, rowIndex, 8, "moveSpeedMultiplier")));
        }
        return result;
    }

    private static List<BossPatternSpec> readBossPatterns(Workbook workbook) {
        Sheet sheet = checkedSheet(workbook, "BossPatternSpec", BOSS_PATTERN_HEADERS);
        List<BossPatternSpec> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (!hasData(row, BOSS_PATTERN_HEADERS.size())) continue;
            result.add(new BossPatternSpec(
                    stringCell(sheet, row, rowIndex, 0, "waveId", true),
                    intCell(sheet, row, rowIndex, 1, "patternOrder"),
                    stringCell(sheet, row, rowIndex, 2, "patternType", true),
                    stringCell(sheet, row, rowIndex, 3, "triggerType", true),
                    decimalCell(sheet, row, rowIndex, 4, "triggerValue"),
                    decimalCell(sheet, row, rowIndex, 5, "cooldownSeconds"),
                    stringCell(sheet, row, rowIndex, 6, "skillId", false),
                    stringCell(sheet, row, rowIndex, 7, "parameterKey", false),
                    decimalCell(sheet, row, rowIndex, 8, "parameterValue"),
                    booleanCell(sheet, row, rowIndex, 9, "enabled")));
        }
        return result;
    }

    private static List<SkillSpec> readSkills(Workbook workbook) {
        Sheet sheet = checkedSheet(workbook, "SkillSpec", SKILL_HEADERS);
        List<SkillSpec> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (!hasData(row, SKILL_HEADERS.size())) continue;
            result.add(new SkillSpec(
                    stringCell(sheet, row, rowIndex, 0, "skillId", true),
                    stringCell(sheet, row, rowIndex, 1, "nameKey", true),
                    stringCell(sheet, row, rowIndex, 2, "descriptionKey", false),
                    stringCell(sheet, row, rowIndex, 3, "skillType", true),
                    stringCell(sheet, row, rowIndex, 4, "triggerType", true),
                    decimalCell(sheet, row, rowIndex, 5, "cooldownSeconds"),
                    decimalCell(sheet, row, rowIndex, 6, "mpCost"),
                    decimalCell(sheet, row, rowIndex, 7, "castRange"),
                    stringCell(sheet, row, rowIndex, 8, "targetPolicy", true),
                    intCell(sheet, row, rowIndex, 9, "maxTargetCount"),
                    stringCell(sheet, row, rowIndex, 10, "projectileId", false),
                    stringCell(sheet, row, rowIndex, 11, "animationKey", false),
                    stringCell(sheet, row, rowIndex, 12, "vfxKey", false),
                    stringCell(sheet, row, rowIndex, 13, "sfxKey", false),
                    booleanCell(sheet, row, rowIndex, 14, "enabled")));
        }
        return result;
    }

    private static List<AlienSkillLink> readAlienSkillLinks(Workbook workbook) {
        Sheet sheet = checkedSheet(workbook, "AlienSkillLink", ALIEN_SKILL_HEADERS);
        List<AlienSkillLink> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (!hasData(row, ALIEN_SKILL_HEADERS.size())) continue;
            result.add(new AlienSkillLink(
                    longCell(sheet, row, rowIndex, 0, "alienId"),
                    stringCell(sheet, row, rowIndex, 1, "skillId", true),
                    intCell(sheet, row, rowIndex, 2, "slotIndex"),
                    intCell(sheet, row, rowIndex, 3, "castPriority"),
                    booleanCell(sheet, row, rowIndex, 4, "enabled")));
        }
        return result;
    }

    private static List<ProjectileSpec> readProjectiles(Workbook workbook) {
        Sheet sheet = checkedSheet(workbook, "ProjectileSpec", PROJECTILE_HEADERS);
        List<ProjectileSpec> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (!hasData(row, PROJECTILE_HEADERS.size())) continue;
            result.add(new ProjectileSpec(
                    stringCell(sheet, row, rowIndex, 0, "projectileId", true),
                    stringCell(sheet, row, rowIndex, 1, "prefabKey", true),
                    stringCell(sheet, row, rowIndex, 2, "moveType", true),
                    decimalCell(sheet, row, rowIndex, 3, "speed"),
                    decimalCell(sheet, row, rowIndex, 4, "lifetimeSeconds"),
                    decimalCell(sheet, row, rowIndex, 5, "hitRadius"),
                    intCell(sheet, row, rowIndex, 6, "pierceCount"),
                    booleanCell(sheet, row, rowIndex, 7, "destroyOnHit"),
                    stringCell(sheet, row, rowIndex, 8, "lostTargetPolicy", true),
                    booleanCell(sheet, row, rowIndex, 9, "enabled")));
        }
        return result;
    }

    private static List<SkillEffectSpec> readSkillEffects(Workbook workbook) {
        Sheet sheet = checkedSheet(workbook, "SkillEffectSpec", SKILL_EFFECT_HEADERS);
        List<SkillEffectSpec> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (!hasData(row, SKILL_EFFECT_HEADERS.size())) continue;
            result.add(new SkillEffectSpec(
                    stringCell(sheet, row, rowIndex, 0, "skillId", true),
                    intCell(sheet, row, rowIndex, 1, "executionOrder"),
                    stringCell(sheet, row, rowIndex, 2, "triggerPhase", true),
                    stringCell(sheet, row, rowIndex, 3, "effectType", true),
                    stringCell(sheet, row, rowIndex, 4, "magnitudeSource", true),
                    decimalCell(sheet, row, rowIndex, 5, "baseMagnitude"),
                    decimalCell(sheet, row, rowIndex, 6, "coefficient"),
                    decimalCell(sheet, row, rowIndex, 7, "chance"),
                    decimalCell(sheet, row, rowIndex, 8, "durationSeconds"),
                    decimalCell(sheet, row, rowIndex, 9, "tickIntervalSeconds"),
                    decimalCell(sheet, row, rowIndex, 10, "radius"),
                    intCell(sheet, row, rowIndex, 11, "maxStacks"),
                    stringCell(sheet, row, rowIndex, 12, "stackPolicy", true),
                    decimalCell(sheet, row, rowIndex, 13, "bossMultiplier")));
        }
        return result;
    }

    private static Sheet checkedSheet(Workbook workbook, String name, List<String> expectedHeaders) {
        Sheet sheet = workbook.getSheet(name);
        if (sheet == null) throw new BalanceConversionException("필수 Battle 시트가 없습니다: " + name);
        Row header = sheet.getRow(0);
        if (header == null) throw new BalanceConversionException("[" + name + "] 헤더가 없습니다.");
        if (header.getLastCellNum() != expectedHeaders.size())
            throw new BalanceConversionException("[" + name + "] Header 개수가 정확하지 않습니다.");
        for (int index = 0; index < expectedHeaders.size(); index++) {
            Cell cell = header.getCell(index);
            if (cell == null || cell.getCellType() != CellType.STRING
                    || !expectedHeaders.get(index).equals(cell.getStringCellValue()))
                throw new BalanceConversionException("[" + name + "] " + (index + 1) + "열 Header는 '" + expectedHeaders.get(index) + "'이어야 합니다.");
        }
        return sheet;
    }

    private static boolean hasData(Row row, int columnCount) {
        if (row == null) return false;
        for (int index = 0; index < columnCount; index++) {
            Cell cell = row.getCell(index);
            if (cell != null && cell.getCellType() != CellType.BLANK) return true;
        }
        return false;
    }

    private static String stringCell(Sheet sheet, Row row, int rowIndex, int column, String name, boolean required) {
        Cell cell = row == null ? null : row.getCell(column);
        if (cell == null || cell.getCellType() == CellType.BLANK) {
            if (required) throw cellError(sheet, rowIndex, name, "필수 문자열 셀이 비어 있습니다.");
            return "";
        }
        if (cell.getCellType() != CellType.STRING)
            throw cellError(sheet, rowIndex, name, "STRING 셀이어야 합니다.");
        String value = cell.getStringCellValue().trim();
        if (required && value.isEmpty()) throw cellError(sheet, rowIndex, name, "필수 문자열이 공백입니다.");
        return value;
    }

    private static int intCell(Sheet sheet, Row row, int rowIndex, int column, String name) {
        double value = numericCell(sheet, row, rowIndex, column, name);
        if (value != Math.rint(value) || value < Integer.MIN_VALUE || value > Integer.MAX_VALUE)
            throw cellError(sheet, rowIndex, name, "INT32 숫자 셀이어야 합니다.");
        return (int) value;
    }

    private static long longCell(Sheet sheet, Row row, int rowIndex, int column, String name) {
        double value = numericCell(sheet, row, rowIndex, column, name);
        if (value != Math.rint(value) || value < Long.MIN_VALUE || value > Long.MAX_VALUE)
            throw cellError(sheet, rowIndex, name, "정수 숫자 셀이어야 합니다.");
        return (long) value;
    }

    private static double decimalCell(Sheet sheet, Row row, int rowIndex, int column, String name) {
        return numericCell(sheet, row, rowIndex, column, name);
    }

    private static double numericCell(Sheet sheet, Row row, int rowIndex, int column, String name) {
        Cell cell = row == null ? null : row.getCell(column);
        if (cell == null || cell.getCellType() == CellType.BLANK)
            throw cellError(sheet, rowIndex, name, "필수 숫자 셀이 비어 있습니다.");
        if (cell.getCellType() != CellType.NUMERIC || DateUtil.isCellDateFormatted(cell))
            throw cellError(sheet, rowIndex, name, "문자열이 아닌 NUMERIC 셀이어야 합니다.");
        double value = cell.getNumericCellValue();
        if (!Double.isFinite(value)) throw cellError(sheet, rowIndex, name, "NaN/Infinity는 허용되지 않습니다.");
        return value;
    }

    private static boolean booleanCell(Sheet sheet, Row row, int rowIndex, int column, String name) {
        Cell cell = row == null ? null : row.getCell(column);
        if (cell == null || cell.getCellType() == CellType.BLANK)
            throw cellError(sheet, rowIndex, name, "필수 Boolean 셀이 비어 있습니다.");
        if (cell.getCellType() != CellType.BOOLEAN)
            throw cellError(sheet, rowIndex, name, "문자열이 아닌 BOOLEAN 셀이어야 합니다.");
        return cell.getBooleanCellValue();
    }

    private static BalanceConversionException cellError(Sheet sheet, int rowIndex, String name, String reason) {
        return new BalanceConversionException("[" + sheet.getSheetName() + "] " + (rowIndex + 1) + "행 '" + name + "': " + reason);
    }
}
