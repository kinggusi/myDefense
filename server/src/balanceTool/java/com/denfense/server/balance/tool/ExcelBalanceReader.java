package com.denfense.server.balance.tool;

import com.denfense.server.service.balance.AlienUpgradeCostBalance;
import com.denfense.server.service.balance.GameRewardBalance;
import org.apache.poi.ss.usermodel.*;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.*;

public class ExcelBalanceReader {

    private final String filePath;
    private FormulaEvaluator evaluator;

    public ExcelBalanceReader(String filePath) {
        this.filePath = filePath;
    }

    public BalanceData read() {
        try (FileInputStream fis = new FileInputStream(new File(filePath));
             Workbook workbook = WorkbookFactory.create(fis)) {
             
            this.evaluator = workbook.getCreationHelper().createFormulaEvaluator();
            
            checkMergedRegions(workbook);

            GameRewardBalance reward = readGameRewardSheet(workbook);
            com.denfense.server.balance.BattleRewardBalance battleReward = readBattleRewardSheet(workbook);
            List<AlienUpgradeCostBalance> upgradeCosts = readAlienUpgradeCostSheet(workbook);
            List<com.denfense.server.service.balance.AlienLevelStatBalance> levelStats = readAlienLevelStatSheet(workbook);
            
            upgradeCosts.sort(Comparator.comparingInt(AlienUpgradeCostBalance::currentLevel));
            
            List<com.denfense.server.balance.AlienSpecBalance> alienSpecs = readAlienSpecSheet(workbook);
            List<com.denfense.server.balance.ShopProductBalance> shopProducts = readShopProductSheet(workbook);
            List<com.denfense.server.balance.GachaPoolBalance> gachaPools = readGachaPoolSheet(workbook);
            List<com.denfense.server.balance.SummonPoolBalance> summonPools = readSummonPoolSheet(workbook);
            List<com.denfense.server.balance.MonsterSpecBalance> monsters = readMonsterSpecSheet(workbook);
            List<com.denfense.server.balance.WaveSpecBalance> waves = readWaveSpecSheet(workbook);
            List<com.denfense.server.balance.WaveSpawnBalance> waveSpawns = readWaveSpawnSheet(workbook);
            List<com.denfense.server.balance.PlanetBattleBalance> planetBattles = readPlanetBattleSheet(workbook);
            List<com.denfense.server.balance.FieldLimitBalance> fieldLimits = readFieldLimitSheet(workbook);
            List<com.denfense.server.balance.SummonBalance> summons = readSummonBalanceSheet(workbook);
            List<com.denfense.server.balance.MergeRuleBalance> mergeRules = readMergeRuleSheet(workbook);
            List<com.denfense.server.balance.MythicChoiceBalance> mythicChoices = readMythicChoiceSheet(workbook);
            List<com.denfense.server.balance.MutationSpecBalance> mutationSpecs = readMutationSpecSheet(workbook);
            com.denfense.server.balance.MutationConfigBalance mutationConfig = readMutationConfigSheet(workbook);
            List<com.denfense.server.balance.InjectorPoolBalance> injectorPools = readInjectorPoolSheet(workbook);
            List<com.denfense.server.balance.ResonanceBalance> resonanceBalances = readResonanceBalanceSheet(workbook);
            com.denfense.server.balance.MythicBreedingConfigBalance breedingConfig = readMythicBreedingConfigSheet(workbook);
            List<com.denfense.server.balance.MythicBreedingResultBalance> breedingResults = readMythicBreedingResultSheet(workbook);
            List<com.denfense.server.balance.MythicBreedingRecipeBalance> breedingRecipes = readMythicBreedingRecipeSheet(workbook);

            return new BalanceData(reward, battleReward, upgradeCosts, levelStats, alienSpecs, shopProducts, gachaPools, summonPools,
                    monsters, waves, waveSpawns, planetBattles, fieldLimits, summons, mergeRules, mythicChoices,
                    mutationSpecs, mutationConfig, injectorPools, resonanceBalances,
                    breedingConfig, breedingResults, breedingRecipes);

        } catch (IOException e) {
            throw new BalanceConversionException("파일을 읽는 중 오류가 발생했습니다: " + filePath, e);
        }
    }

    private void checkMergedRegions(Workbook workbook) {
        for (int i = 0; i < workbook.getNumberOfSheets(); i++) {
            Sheet sheet = workbook.getSheetAt(i);
            if (sheet.getNumMergedRegions() > 0) {
                // Find first merged region to report
                org.apache.poi.ss.util.CellRangeAddress region = sheet.getMergedRegion(0);
                throw new BalanceConversionException(
                        String.format("[%s] 병합된 셀이 존재합니다: %s", sheet.getSheetName(), region.formatAsString())
                );
            }
        }
    }

    private GameRewardBalance readGameRewardSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "GameReward");
        
        Row headerRow = sheet.getRow(0);
        if (headerRow == null) {
            throw new BalanceConversionException("[GameReward] 헤더가 없습니다.");
        }
        
        List<String> headers = readHeaders(sheet.getSheetName(), headerRow, Arrays.asList("baseRewardGold", "goldPerWave", "maxRewardGold"));
        
        int baseIndex = headers.indexOf("baseRewardGold");
        int waveIndex = headers.indexOf("goldPerWave");
        int maxIndex = headers.indexOf("maxRewardGold");
        
        int dataRowCount = 0;
        GameRewardBalance reward = null;
        
        for (int i = 1; i <= sheet.getLastRowNum(); i++) {
            Row row = sheet.getRow(i);
            if (row == null) continue;
            
            // 데이터 행 확인
            boolean hasData = false;
            for (int col = 0; col < headers.size(); col++) {
                Cell cell = row.getCell(col);
                if (cell != null && cell.getCellType() != CellType.BLANK) {
                    hasData = true;
                    break;
                }
            }
            if (!hasData) continue; // 빈 행 무시
            
            dataRowCount++;
            if (dataRowCount > 1) {
                throw new BalanceConversionException("[GameReward] 데이터 행이 1개여야 합니다.");
            }
            
            int base = readIntCell(sheet.getSheetName(), i, "baseRewardGold", row.getCell(baseIndex));
            int wave = readIntCell(sheet.getSheetName(), i, "goldPerWave", row.getCell(waveIndex));
            int max = readIntCell(sheet.getSheetName(), i, "maxRewardGold", row.getCell(maxIndex));
            
            reward = new GameRewardBalance(base, wave, max);
        }
        
        if (reward == null) {
            throw new BalanceConversionException("[GameReward] 데이터 행이 없습니다.");
        }
        
        return reward;
    }

    private com.denfense.server.balance.BattleRewardBalance readBattleRewardSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "BattleReward");
        List<String> headers = requiredHeaders(sheet, "rewardType", "mapId", "wave", "gold", "universalPiece", "diamond",
                "failureRewardBaseGold", "failureRewardCapPercent", "minimumRewardWave", "enabled");
        int type = headers.indexOf("rewardType"), map = headers.indexOf("mapId"), wave = headers.indexOf("wave");
        int gold = headers.indexOf("gold"), universal = headers.indexOf("universalPiece"), diamond = headers.indexOf("diamond");
        int failureBase = headers.indexOf("failureRewardBaseGold"), failureCap = headers.indexOf("failureRewardCapPercent");
        int minimum = headers.indexOf("minimumRewardWave"), enabled = headers.indexOf("enabled");
        int maxWave = 0, minimumRewardWave = 0, failureRewardBaseGold = 0, failureRewardCapPercent = 0;
        List<com.denfense.server.balance.BattleRewardBalance.Checkpoint> checkpoints = new ArrayList<>();
        List<com.denfense.server.balance.BattleRewardBalance.MapFirstClear> mapFirstClears = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size()) || !readBooleanCell(sheet.getSheetName(), rowIndex, "enabled", row.getCell(enabled))) continue;
            String rewardType = readStringCell(sheet.getSheetName(), rowIndex, "rewardType", row.getCell(type));
            int rowWave = readIntCell(sheet.getSheetName(), rowIndex, "wave", row.getCell(wave));
            if ("CONFIG".equals(rewardType)) {
                maxWave = rowWave == 0 ? 80 : rowWave;
                failureRewardBaseGold = readIntCell(sheet.getSheetName(), rowIndex, "failureRewardBaseGold", row.getCell(failureBase));
                failureRewardCapPercent = readIntCell(sheet.getSheetName(), rowIndex, "failureRewardCapPercent", row.getCell(failureCap));
                minimumRewardWave = readIntCell(sheet.getSheetName(), rowIndex, "minimumRewardWave", row.getCell(minimum));
            } else if ("CHECKPOINT".equals(rewardType)) {
                checkpoints.add(new com.denfense.server.balance.BattleRewardBalance.Checkpoint(rowWave,
                        readIntCell(sheet.getSheetName(), rowIndex, "gold", row.getCell(gold)),
                        readIntCell(sheet.getSheetName(), rowIndex, "universalPiece", row.getCell(universal))));
            } else if ("MAP_FIRST_CLEAR".equals(rewardType)) {
                mapFirstClears.add(new com.denfense.server.balance.BattleRewardBalance.MapFirstClear(
                        readStringCell(sheet.getSheetName(), rowIndex, "mapId", row.getCell(map)), rowWave,
                        readIntCell(sheet.getSheetName(), rowIndex, "diamond", row.getCell(diamond))));
            } else {
                throw new BalanceConversionException("[BattleReward] unknown rewardType: " + rewardType);
            }
        }
        if (maxWave == 0) maxWave = 80;
        checkpoints.sort(Comparator.comparingInt(com.denfense.server.balance.BattleRewardBalance.Checkpoint::wave));
        mapFirstClears.sort(Comparator.comparing(com.denfense.server.balance.BattleRewardBalance.MapFirstClear::mapId));
        return new com.denfense.server.balance.BattleRewardBalance(maxWave, minimumRewardWave, failureRewardBaseGold,
                failureRewardCapPercent, checkpoints, mapFirstClears);
    }

    private List<AlienUpgradeCostBalance> readAlienUpgradeCostSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "AlienUpgradeCost");
        Row headerRow = sheet.getRow(0);
        if (headerRow == null) {
            throw new BalanceConversionException("[AlienUpgradeCost] 헤더가 없습니다.");
        }
        List<String> headers = readHeaders(sheet.getSheetName(), headerRow,
                Arrays.asList("currentLevel", "targetLevel", "requiredPieces", "requiredGold", "requiredGrowthCell"));
        int currentIndex = headers.indexOf("currentLevel");
        int targetIndex = headers.indexOf("targetLevel");
        int piecesIndex = headers.indexOf("requiredPieces");
        int goldIndex = headers.indexOf("requiredGold");
        int growthIndex = headers.indexOf("requiredGrowthCell");
        List<AlienUpgradeCostBalance> costs = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            costs.add(new AlienUpgradeCostBalance(
                    readIntCell(sheet.getSheetName(), rowIndex, "currentLevel", row.getCell(currentIndex)),
                    readIntCell(sheet.getSheetName(), rowIndex, "targetLevel", row.getCell(targetIndex)),
                    readIntCell(sheet.getSheetName(), rowIndex, "requiredPieces", row.getCell(piecesIndex)),
                    readIntCell(sheet.getSheetName(), rowIndex, "requiredGold", row.getCell(goldIndex)),
                    readIntCell(sheet.getSheetName(), rowIndex, "requiredGrowthCell", row.getCell(growthIndex))
            ));
        }
        costs.sort(Comparator.comparingInt(AlienUpgradeCostBalance::currentLevel));
        return costs;
    }

    private List<com.denfense.server.service.balance.AlienLevelStatBalance> readAlienLevelStatSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "AlienLevelStat");
        Row headerRow = sheet.getRow(0);
        if (headerRow == null) {
            throw new BalanceConversionException("[AlienLevelStat] 헤더가 없습니다.");
        }
        List<String> headers = readHeaders(sheet.getSheetName(), headerRow,
                Arrays.asList("level", "atkMultiplier", "mpMultiplier", "atkSpeedMultiplier", "rangeMultiplier"));
        int levelIndex = headers.indexOf("level");
        int atkIndex = headers.indexOf("atkMultiplier");
        int mpIndex = headers.indexOf("mpMultiplier");
        int speedIndex = headers.indexOf("atkSpeedMultiplier");
        int rangeIndex = headers.indexOf("rangeMultiplier");
        List<com.denfense.server.service.balance.AlienLevelStatBalance> stats = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            stats.add(new com.denfense.server.service.balance.AlienLevelStatBalance(
                    readIntCell(sheet.getSheetName(), rowIndex, "level", row.getCell(levelIndex)),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "atkMultiplier", row.getCell(atkIndex), 3),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "mpMultiplier", row.getCell(mpIndex), 3),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "atkSpeedMultiplier", row.getCell(speedIndex), 3),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "rangeMultiplier", row.getCell(rangeIndex), 3)
            ));
        }
        stats.sort(Comparator.comparingInt(com.denfense.server.service.balance.AlienLevelStatBalance::level));
        return stats;
    }

    private boolean isBlankRow(Row row, int columnCount) {
        for (int column = 0; column < columnCount; column++) {
            Cell cell = row.getCell(column);
            if (cell != null && cell.getCellType() != CellType.BLANK) return false;
        }
        return true;
    }

    private Sheet getSheetOrThrow(Workbook workbook, String sheetName) {
        Sheet sheet = workbook.getSheet(sheetName);
        if (sheet == null) {
            throw new BalanceConversionException("필수 시트가 없습니다: " + sheetName);
        }
        return sheet;
    }

    private List<String> readHeaders(String sheetName, Row headerRow, List<String> expectedHeaders) {
        List<String> headers = new ArrayList<>();
        Set<String> seenHeaders = new HashSet<>();
        
        for (int i = 0; i < headerRow.getLastCellNum(); i++) {
            Cell cell = headerRow.getCell(i);
            if (cell == null || cell.getCellType() == CellType.BLANK) {
                if (i < expectedHeaders.size()) {
                    throw new BalanceConversionException(String.format("[%s] %d열: 빈 헤더입니다.", sheetName, i + 1));
                }
                break;
            }
            if (cell.getCellType() != CellType.STRING) {
                throw new BalanceConversionException(String.format("[%s] %d열: 헤더는 문자열이어야 합니다.", sheetName, i + 1));
            }
            String h = cell.getStringCellValue().trim();
            if (h.isEmpty()) {
                throw new BalanceConversionException(String.format("[%s] %d열: 빈 헤더입니다.", sheetName, i + 1));
            }
            if (!seenHeaders.add(h)) {
                throw new BalanceConversionException(String.format("[%s] %d열: %s - 중복된 헤더입니다.", sheetName, i + 1, h));
            }
            headers.add(h);
        }
        
        for (String expected : expectedHeaders) {
            if (!headers.contains(expected)) {
                throw new BalanceConversionException(String.format("[%s] 필수 헤더 누락: %s", sheetName, expected));
            }
        }
        
        for (String h : headers) {
            if (!expectedHeaders.contains(h)) {
                throw new BalanceConversionException(String.format("[%s] 알 수 없는 헤더입니다: %s", sheetName, h));
            }
        }
        
        return headers;
    }

    private String readStringCell(String sheetName, int rowIdx, String colName, Cell cell) {
        if (cell == null || cell.getCellType() == CellType.BLANK) {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 빈 셀입니다.", sheetName, rowIdx + 1, colName));
        }
        
        CellType type = cell.getCellType();
        if (type == CellType.FORMULA) {
            CellValue cv = evaluator.evaluate(cell);
            type = cv.getCellType();
            if (type == CellType.STRING) {
                return cv.getStringValue().trim();
            } else {
                throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 수식 결과가 문자열이 아닙니다.", sheetName, rowIdx + 1, colName));
            }
        }
        
        if (type != CellType.STRING) {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 문자열이어야 합니다.", sheetName, rowIdx + 1, colName));
        }
        String val = cell.getStringCellValue().trim();
        if (val.isEmpty()) {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 빈 문자열입니다.", sheetName, rowIdx + 1, colName));
        }
        return val;
    }

    private int readIntCell(String sheetName, int rowIdx, String colName, Cell cell) {
        if (cell == null || cell.getCellType() == CellType.BLANK) {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 빈 셀입니다.", sheetName, rowIdx + 1, colName));
        }

        CellType type = cell.getCellType();
        double value;
        String rawString = "";

        if (type == CellType.FORMULA) {
            try {
                CellValue cv = evaluator.evaluate(cell);
                type = cv.getCellType();
                if (type == CellType.NUMERIC) {
                    value = cv.getNumberValue();
                    rawString = String.valueOf(value);
                } else if (type == CellType.ERROR) {
                    throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 수식 오류입니다.", sheetName, rowIdx + 1, colName));
                } else {
                    throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 수식 결과가 숫자가 아닙니다.", sheetName, rowIdx + 1, colName));
                }
            } catch (Exception e) {
                 if (e instanceof BalanceConversionException) throw e;
                 throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 수식 계산 실패", sheetName, rowIdx + 1, colName), e);
            }
        } else if (type == CellType.NUMERIC) {
            value = cell.getNumericCellValue();
            rawString = String.valueOf(value);
            if (DateUtil.isCellDateFormatted(cell)) {
                throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: %s - 날짜 타입은 허용되지 않습니다.", sheetName, rowIdx + 1, colName, cell.getLocalDateTimeCellValue()));
            }
        } else if (type == CellType.STRING) {
            rawString = cell.getStringCellValue();
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: %s - 문자열 형태의 숫자는 허용되지 않습니다.", sheetName, rowIdx + 1, colName, rawString));
        } else if (type == CellType.BOOLEAN) {
            rawString = String.valueOf(cell.getBooleanCellValue());
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: %s - Boolean 타입은 허용되지 않습니다.", sheetName, rowIdx + 1, colName, rawString));
        } else {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 지원하지 않는 셀 타입입니다.", sheetName, rowIdx + 1, colName));
        }

        if (!Double.isFinite(value)) {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: %s - NaN/Infinity는 허용되지 않습니다.", sheetName, rowIdx + 1, colName, rawString));
        }
        if (value != Math.rint(value)) {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: %s - 소수는 허용되지 않습니다.", sheetName, rowIdx + 1, colName, rawString));
        }
        if (value < Integer.MIN_VALUE || value > Integer.MAX_VALUE) {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: %s - int 범위를 초과했습니다.", sheetName, rowIdx + 1, colName, rawString));
        }

        return (int) value;
    }

    private List<com.denfense.server.balance.AlienSpecBalance> readAlienSpecSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "AlienSpec");

        Row headerRow = sheet.getRow(0);
        if (headerRow == null) {
            throw new BalanceConversionException("[AlienSpec] 헤더가 없습니다.");
        }

        List<String> expectedHeaders = Arrays.asList("alienId", "name", "description", "grade", "baseAttack", "baseMp", "attackSpeed", "attackRange", "evolutionTargetId", "isLocked");
        List<String> headers = readHeaders(sheet.getSheetName(), headerRow, expectedHeaders);

        int idIdx = headers.indexOf("alienId");
        int nameIdx = headers.indexOf("name");
        int descIdx = headers.indexOf("description");
        int gradeIdx = headers.indexOf("grade");
        int atkIdx = headers.indexOf("baseAttack");
        int mpIdx = headers.indexOf("baseMp");
        int speedIdx = headers.indexOf("attackSpeed");
        int rangeIdx = headers.indexOf("attackRange");
        int targetIdx = headers.indexOf("evolutionTargetId");
        int lockIdx = headers.indexOf("isLocked");

        List<com.denfense.server.balance.AlienSpecBalance> specs = new ArrayList<>();

        for (int i = 1; i <= sheet.getLastRowNum(); i++) {
            Row row = sheet.getRow(i);
            if (row == null) continue;

            boolean hasData = false;
            for (int col = 0; col < headers.size(); col++) {
                Cell cell = row.getCell(col);
                if (cell != null && cell.getCellType() != CellType.BLANK) {
                    hasData = true;
                    break;
                }
            }
            if (!hasData) continue;

            long id = readLongCell(sheet.getSheetName(), i, "alienId", row.getCell(idIdx));
            String name = readStringCell(sheet.getSheetName(), i, "name", row.getCell(nameIdx));
            String desc = readStringCellOrDefault(sheet.getSheetName(), i, "description", row.getCell(descIdx), "");
            String grade = readStringCell(sheet.getSheetName(), i, "grade", row.getCell(gradeIdx));
            int atk = readIntCell(sheet.getSheetName(), i, "baseAttack", row.getCell(atkIdx));
            int mp = readIntCell(sheet.getSheetName(), i, "baseMp", row.getCell(mpIdx));
            double speed = readDoubleCell(sheet.getSheetName(), i, "attackSpeed", row.getCell(speedIdx));
            double range = readDoubleCell(sheet.getSheetName(), i, "attackRange", row.getCell(rangeIdx));
            Long target = readLongCellNullable(sheet.getSheetName(), i, "evolutionTargetId", row.getCell(targetIdx));
            boolean locked = readBooleanCell(sheet.getSheetName(), i, "isLocked", row.getCell(lockIdx));

            specs.add(new com.denfense.server.balance.AlienSpecBalance(id, name, desc, grade, atk, mp, speed, range, target, locked));
        }

        return specs;
    }

    private String readStringCellOrDefault(String sheetName, int rowIdx, String colName, Cell cell, String defaultValue) {
        if (cell == null || cell.getCellType() == CellType.BLANK) {
            return defaultValue;
        }
        if (cell.getCellType() == CellType.STRING) {
            String val = cell.getStringCellValue().trim();
            if (val.isEmpty()) return defaultValue;
        }
        return readStringCell(sheetName, rowIdx, colName, cell);
    }

    private long readLongCell(String sheetName, int rowIdx, String colName, Cell cell) {
        return (long) readIntCell(sheetName, rowIdx, colName, cell);
    }

    private Long readLongCellNullable(String sheetName, int rowIdx, String colName, Cell cell) {
        if (cell == null || cell.getCellType() == CellType.BLANK) {
            return null;
        }
        return readLongCell(sheetName, rowIdx, colName, cell);
    }

    private double readDoubleCell(String sheetName, int rowIdx, String colName, Cell cell) {
        if (cell == null || cell.getCellType() == CellType.BLANK) {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 빈 셀입니다.", sheetName, rowIdx + 1, colName));
        }

        CellType type = cell.getCellType();
        double value;
        String rawString = "";

        if (type == CellType.FORMULA) {
            try {
                CellValue cv = evaluator.evaluate(cell);
                type = cv.getCellType();
                if (type == CellType.NUMERIC) {
                    value = cv.getNumberValue();
                    rawString = String.valueOf(value);
                } else {
                    throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 수식 결과가 숫자가 아닙니다.", sheetName, rowIdx + 1, colName));
                }
            } catch (Exception e) {
                 if (e instanceof BalanceConversionException) throw e;
                 throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 수식 계산 실패", sheetName, rowIdx + 1, colName), e);
            }
        } else if (type == CellType.NUMERIC) {
            value = cell.getNumericCellValue();
            rawString = String.valueOf(value);
            if (DateUtil.isCellDateFormatted(cell)) {
                throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: %s - 날짜 타입은 허용되지 않습니다.", sheetName, rowIdx + 1, colName, cell.getLocalDateTimeCellValue()));
            }
        } else if (type == CellType.STRING) {
            rawString = cell.getStringCellValue();
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: %s - 문자열 형태의 숫자는 허용되지 않습니다.", sheetName, rowIdx + 1, colName, rawString));
        } else if (type == CellType.BOOLEAN) {
            rawString = String.valueOf(cell.getBooleanCellValue());
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: %s - Boolean 타입은 허용되지 않습니다.", sheetName, rowIdx + 1, colName, rawString));
        } else {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 지원하지 않는 셀 타입입니다.", sheetName, rowIdx + 1, colName));
        }

        if (!Double.isFinite(value)) {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: %s - NaN/Infinity는 허용되지 않습니다.", sheetName, rowIdx + 1, colName, rawString));
        }

        return value;
    }

    private BigDecimal readDecimalCell(String sheetName, int rowIdx, String colName, Cell cell) {
        return readDecimalCell(sheetName, rowIdx, colName, cell, 2);
    }

    private BigDecimal readDecimalCell(String sheetName, int rowIdx, String colName, Cell cell, int scale) {
        if (cell == null || cell.getCellType() == CellType.BLANK) {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 값이 비어 있습니다.", sheetName, rowIdx + 1, colName));
        }
        CellValue evaluated = evaluator.evaluate(cell);
        if (evaluated == null || evaluated.getCellType() != CellType.NUMERIC) {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 값은 숫자여야 합니다.", sheetName, rowIdx + 1, colName));
        }
        double value = evaluated.getNumberValue();
        if (!Double.isFinite(value)) {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 값이 유한한 숫자가 아닙니다.", sheetName, rowIdx + 1, colName));
        }
        return BigDecimal.valueOf(value).setScale(scale, RoundingMode.HALF_UP);
    }

    private boolean readBooleanCell(String sheetName, int rowIdx, String colName, Cell cell) {
        if (cell == null || cell.getCellType() == CellType.BLANK) {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 빈 셀입니다.", sheetName, rowIdx + 1, colName));
        }

        CellType type = cell.getCellType();
        if (type == CellType.FORMULA) {
            CellValue cv = evaluator.evaluate(cell);
            type = cv.getCellType();
            if (type == CellType.BOOLEAN) {
                return cv.getBooleanValue();
            } else {
                throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: 수식 결과가 Boolean이 아닙니다.", sheetName, rowIdx + 1, colName));
            }
        } else if (type == CellType.BOOLEAN) {
            return cell.getBooleanCellValue();
        } else {
            throw new BalanceConversionException(String.format("[%s] %d행 '%s' 열: Boolean이어야 합니다.", sheetName, rowIdx + 1, colName));
        }
    }

    private List<com.denfense.server.balance.ShopProductBalance> readShopProductSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "ShopProduct");

        Row headerRow = sheet.getRow(0);
        if (headerRow == null) {
            throw new BalanceConversionException("[ShopProduct] 헤더가 없습니다.");
        }

        List<String> expectedHeaders = Arrays.asList("productId", "name", "currencyType", "price", "drawCount", "gachaPoolId", "active");
        List<String> headers = readHeaders(sheet.getSheetName(), headerRow, expectedHeaders);

        int idIdx = headers.indexOf("productId");
        int nameIdx = headers.indexOf("name");
        int currIdx = headers.indexOf("currencyType");
        int priceIdx = headers.indexOf("price");
        int countIdx = headers.indexOf("drawCount");
        int poolIdx = headers.indexOf("gachaPoolId");
        int activeIdx = headers.indexOf("active");

        List<com.denfense.server.balance.ShopProductBalance> products = new ArrayList<>();
        Set<String> seenIds = new HashSet<>();

        for (int i = 1; i <= sheet.getLastRowNum(); i++) {
            Row row = sheet.getRow(i);
            if (row == null) continue;

            boolean hasData = false;
            for (int col = 0; col < headers.size(); col++) {
                Cell cell = row.getCell(col);
                if (cell != null && cell.getCellType() != CellType.BLANK) {
                    hasData = true;
                    break;
                }
            }
            if (!hasData) continue;

            String productId = readStringCell(sheet.getSheetName(), i, "productId", row.getCell(idIdx));
            if (!seenIds.add(productId)) {
                throw new BalanceConversionException(String.format("[%s] %d행 'productId' 열: %s - 중복된 상품 ID입니다.", sheet.getSheetName(), i + 1, productId));
            }

            String name = readStringCell(sheet.getSheetName(), i, "name", row.getCell(nameIdx));
            String currencyType = readStringCell(sheet.getSheetName(), i, "currencyType", row.getCell(currIdx));
            int price = readIntCell(sheet.getSheetName(), i, "price", row.getCell(priceIdx));
            if (price <= 0) {
                throw new BalanceConversionException(String.format("[%s] %d행 'price' 열: %d - 가격은 양수여야 합니다.", sheet.getSheetName(), i + 1, price));
            }
            int drawCount = readIntCell(sheet.getSheetName(), i, "drawCount", row.getCell(countIdx));
            if (drawCount <= 0) {
                throw new BalanceConversionException(String.format("[%s] %d행 'drawCount' 열: %d - 뽑기 횟수는 양수여야 합니다.", sheet.getSheetName(), i + 1, drawCount));
            }
            String gachaPoolId = readStringCell(sheet.getSheetName(), i, "gachaPoolId", row.getCell(poolIdx));
            boolean active = readBooleanCell(sheet.getSheetName(), i, "active", row.getCell(activeIdx));

            products.add(new com.denfense.server.balance.ShopProductBalance(productId, name, currencyType, price, drawCount, gachaPoolId, active));
        }
        
        products.sort(Comparator.comparing(com.denfense.server.balance.ShopProductBalance::productId));
        return products;
    }

    private List<com.denfense.server.balance.GachaPoolBalance> readGachaPoolSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "GachaPool");

        Row headerRow = sheet.getRow(0);
        if (headerRow == null) {
            throw new BalanceConversionException("[GachaPool] 헤더가 없습니다.");
        }

        List<String> expectedHeaders = Arrays.asList("poolId", "poolName", "poolActive", "grade", "weight", "alienIds");
        List<String> headers = readHeaders(sheet.getSheetName(), headerRow, expectedHeaders);

        int poolIdIdx = headers.indexOf("poolId");
        int nameIdx = headers.indexOf("poolName");
        int activeIdx = headers.indexOf("poolActive");
        int gradeIdx = headers.indexOf("grade");
        int weightIdx = headers.indexOf("weight");
        int alienIdsIdx = headers.indexOf("alienIds");

        class PoolBuilder {
            String poolId;
            String poolName;
            boolean poolActive;
            List<com.denfense.server.balance.GachaGradeEntryBalance> entries = new ArrayList<>();
        }
        Map<String, PoolBuilder> builderMap = new LinkedHashMap<>();

        for (int i = 1; i <= sheet.getLastRowNum(); i++) {
            Row row = sheet.getRow(i);
            if (row == null) continue;

            boolean hasData = false;
            for (int col = 0; col < headers.size(); col++) {
                Cell cell = row.getCell(col);
                if (cell != null && cell.getCellType() != CellType.BLANK) {
                    hasData = true;
                    break;
                }
            }
            if (!hasData) continue;

            String poolId = readStringCell(sheet.getSheetName(), i, "poolId", row.getCell(poolIdIdx));
            String poolName = readStringCell(sheet.getSheetName(), i, "poolName", row.getCell(nameIdx));
            boolean poolActive = readBooleanCell(sheet.getSheetName(), i, "poolActive", row.getCell(activeIdx));
            String grade = readStringCell(sheet.getSheetName(), i, "grade", row.getCell(gradeIdx));
            int weight = readIntCell(sheet.getSheetName(), i, "weight", row.getCell(weightIdx));
            if (weight <= 0) {
                throw new BalanceConversionException(String.format("[%s] %d행 'weight' 열: 가중치는 0보다 커야 합니다.", sheet.getSheetName(), i + 1));
            }
            
            String alienIdsStr = readStringCell(sheet.getSheetName(), i, "alienIds", row.getCell(alienIdsIdx));
            String[] alienIdTokens = alienIdsStr.split(",");
            List<Long> alienIds = new ArrayList<>();
            for (String token : alienIdTokens) {
                token = token.trim();
                if (token.isEmpty()) {
                    throw new BalanceConversionException(String.format("[%s] %d행 'alienIds' 열: 쉼표 사이에 빈 값이 있습니다.", sheet.getSheetName(), i + 1));
                }
                try {
                    alienIds.add(Long.parseLong(token));
                } catch (NumberFormatException e) {
                    throw new BalanceConversionException(String.format("[%s] %d행 'alienIds' 열: %s - 숫자가 아닙니다.", sheet.getSheetName(), i + 1, token));
                }
            }
            if (alienIds.isEmpty()) {
                throw new BalanceConversionException(String.format("[%s] %d행 'alienIds' 열: 비어 있습니다.", sheet.getSheetName(), i + 1));
            }
            alienIds.sort(Long::compareTo);

            PoolBuilder builder = builderMap.computeIfAbsent(poolId, id -> {
                PoolBuilder b = new PoolBuilder();
                b.poolId = id;
                b.poolName = poolName;
                b.poolActive = poolActive;
                return b;
            });
            builder.entries.add(new com.denfense.server.balance.GachaGradeEntryBalance(grade, weight, alienIds));
        }
        
        List<com.denfense.server.balance.GachaPoolBalance> pools = new ArrayList<>();
        for (PoolBuilder b : builderMap.values()) {
            pools.add(new com.denfense.server.balance.GachaPoolBalance(b.poolId, b.poolName, b.poolActive, b.entries));
        }
        pools.sort(Comparator.comparing(com.denfense.server.balance.GachaPoolBalance::poolId));
        return pools;
    }

    private List<com.denfense.server.balance.SummonPoolBalance> readSummonPoolSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "SummonPool");
        List<String> headers = requiredHeaders(sheet, "poolId", "poolName", "poolActive", "grade", "weight", "alienIds");
        Map<String, List<com.denfense.server.balance.SummonPoolEntryBalance>> entries = new LinkedHashMap<>();
        Map<String, String> names = new LinkedHashMap<>();
        Map<String, Boolean> active = new LinkedHashMap<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            String poolId = readStringCell(sheet.getSheetName(), rowIndex, "poolId", row.getCell(headers.indexOf("poolId")));
            String poolName = readStringCell(sheet.getSheetName(), rowIndex, "poolName", row.getCell(headers.indexOf("poolName")));
            boolean poolActive = readBooleanCell(sheet.getSheetName(), rowIndex, "poolActive", row.getCell(headers.indexOf("poolActive")));
            String grade = readStringCell(sheet.getSheetName(), rowIndex, "grade", row.getCell(headers.indexOf("grade")));
            int weight = readIntCell(sheet.getSheetName(), rowIndex, "weight", row.getCell(headers.indexOf("weight")));
            String ids = readStringCell(sheet.getSheetName(), rowIndex, "alienIds", row.getCell(headers.indexOf("alienIds")));
            List<Long> alienIds = Arrays.stream(ids.split(",")).map(String::trim).filter(s -> !s.isEmpty()).map(Long::parseLong).sorted().toList();
            if (alienIds.isEmpty() || weight <= 0) throw new BalanceConversionException("[SummonPool] invalid row " + (rowIndex + 1));
            names.putIfAbsent(poolId, poolName);
            active.putIfAbsent(poolId, poolActive);
            entries.computeIfAbsent(poolId, ignored -> new ArrayList<>())
                    .add(new com.denfense.server.balance.SummonPoolEntryBalance(grade, weight, alienIds));
        }
        return entries.keySet().stream().sorted()
                .map(id -> new com.denfense.server.balance.SummonPoolBalance(id, names.get(id), active.get(id), entries.get(id)))
                .toList();
    }

    private List<com.denfense.server.balance.MonsterSpecBalance> readMonsterSpecSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "MonsterSpec");
        List<String> headers = requiredHeaders(sheet,
                "monsterId", "name", "monsterType", "baseHp", "moveSpeed", "killGold", "enabled");
        List<com.denfense.server.balance.MonsterSpecBalance> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            result.add(new com.denfense.server.balance.MonsterSpecBalance(
                    readStringCell(sheet.getSheetName(), rowIndex, "monsterId", row.getCell(headers.indexOf("monsterId"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "name", row.getCell(headers.indexOf("name"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "monsterType", row.getCell(headers.indexOf("monsterType"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "baseHp", row.getCell(headers.indexOf("baseHp"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "moveSpeed", row.getCell(headers.indexOf("moveSpeed"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "killGold", row.getCell(headers.indexOf("killGold"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "enabled", row.getCell(headers.indexOf("enabled")))
            ));
        }
        result.sort(Comparator.comparing(com.denfense.server.balance.MonsterSpecBalance::monsterId));
        return result;
    }

    private List<com.denfense.server.balance.WaveSpecBalance> readWaveSpecSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "WaveSpec");
        List<String> headers = requiredHeaders(sheet, "modeId", "wave", "hpMultiplier", "interWaveDelaySeconds",
                "isBossWave", "bossTimeLimitSeconds", "spawnGroupId", "enabled");
        List<com.denfense.server.balance.WaveSpecBalance> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            result.add(new com.denfense.server.balance.WaveSpecBalance(
                    readStringCell(sheet.getSheetName(), rowIndex, "modeId", row.getCell(headers.indexOf("modeId"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "wave", row.getCell(headers.indexOf("wave"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "hpMultiplier", row.getCell(headers.indexOf("hpMultiplier"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "interWaveDelaySeconds", row.getCell(headers.indexOf("interWaveDelaySeconds"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "isBossWave", row.getCell(headers.indexOf("isBossWave"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "bossTimeLimitSeconds", row.getCell(headers.indexOf("bossTimeLimitSeconds"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "spawnGroupId", row.getCell(headers.indexOf("spawnGroupId"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "enabled", row.getCell(headers.indexOf("enabled")))
            ));
        }
        result.sort(Comparator.comparing(com.denfense.server.balance.WaveSpecBalance::modeId)
                .thenComparingInt(com.denfense.server.balance.WaveSpecBalance::wave));
        return result;
    }

    private List<com.denfense.server.balance.WaveSpawnBalance> readWaveSpawnSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "WaveSpawn");
        List<String> headers = requiredHeaders(sheet, "spawnGroupId", "order", "monsterId", "spawnCountPerField",
                "startDelaySeconds", "spawnIntervalSeconds", "lanePolicy");
        List<com.denfense.server.balance.WaveSpawnBalance> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            result.add(new com.denfense.server.balance.WaveSpawnBalance(
                    readStringCell(sheet.getSheetName(), rowIndex, "spawnGroupId", row.getCell(headers.indexOf("spawnGroupId"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "order", row.getCell(headers.indexOf("order"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "monsterId", row.getCell(headers.indexOf("monsterId"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "spawnCountPerField", row.getCell(headers.indexOf("spawnCountPerField"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "startDelaySeconds", row.getCell(headers.indexOf("startDelaySeconds"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "spawnIntervalSeconds", row.getCell(headers.indexOf("spawnIntervalSeconds"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "lanePolicy", row.getCell(headers.indexOf("lanePolicy")))
            ));
        }
        result.sort(Comparator.comparing(com.denfense.server.balance.WaveSpawnBalance::spawnGroupId)
                .thenComparingInt(com.denfense.server.balance.WaveSpawnBalance::order));
        return result;
    }

    private List<com.denfense.server.balance.PlanetBattleBalance> readPlanetBattleSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "PlanetBattle");
        List<String> headers = requiredHeaders(sheet, "mapId", "order", "hpMultiplier", "speedMultiplier",
                "bossHpMultiplier", "enabled");
        List<com.denfense.server.balance.PlanetBattleBalance> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            result.add(new com.denfense.server.balance.PlanetBattleBalance(
                    readStringCell(sheet.getSheetName(), rowIndex, "mapId", row.getCell(headers.indexOf("mapId"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "order", row.getCell(headers.indexOf("order"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "hpMultiplier", row.getCell(headers.indexOf("hpMultiplier"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "speedMultiplier", row.getCell(headers.indexOf("speedMultiplier"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "bossHpMultiplier", row.getCell(headers.indexOf("bossHpMultiplier"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "enabled", row.getCell(headers.indexOf("enabled")))
            ));
        }
        result.sort(Comparator.comparingInt(com.denfense.server.balance.PlanetBattleBalance::order));
        return result;
    }

    private List<com.denfense.server.balance.FieldLimitBalance> readFieldLimitSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "FieldLimitBalance");
        List<String> headers = requiredHeaders(sheet, "modeId", "playerCount", "maxAliveMonsterCountPerField",
                "warningThreshold", "dangerThreshold");
        List<com.denfense.server.balance.FieldLimitBalance> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            result.add(new com.denfense.server.balance.FieldLimitBalance(
                    readStringCell(sheet.getSheetName(), rowIndex, "modeId", row.getCell(headers.indexOf("modeId"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "playerCount", row.getCell(headers.indexOf("playerCount"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "maxAliveMonsterCountPerField", row.getCell(headers.indexOf("maxAliveMonsterCountPerField"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "warningThreshold", row.getCell(headers.indexOf("warningThreshold"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "dangerThreshold", row.getCell(headers.indexOf("dangerThreshold")))
            ));
        }
        result.sort(Comparator.comparing(com.denfense.server.balance.FieldLimitBalance::modeId));
        return result;
    }

    private List<com.denfense.server.balance.SummonBalance> readSummonBalanceSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "SummonBalance");
        List<String> headers = requiredHeaders(sheet, "modeId", "summonType", "baseCost", "costIncreasePerUse",
                "maxUses", "resultPoolId", "enabled");
        List<com.denfense.server.balance.SummonBalance> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            result.add(new com.denfense.server.balance.SummonBalance(
                    readStringCell(sheet.getSheetName(), rowIndex, "modeId", row.getCell(headers.indexOf("modeId"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "summonType", row.getCell(headers.indexOf("summonType"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "baseCost", row.getCell(headers.indexOf("baseCost"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "costIncreasePerUse", row.getCell(headers.indexOf("costIncreasePerUse"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "maxUses", row.getCell(headers.indexOf("maxUses"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "resultPoolId", row.getCell(headers.indexOf("resultPoolId"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "enabled", row.getCell(headers.indexOf("enabled")))
            ));
        }
        result.sort(Comparator.comparing(com.denfense.server.balance.SummonBalance::modeId)
                .thenComparing(com.denfense.server.balance.SummonBalance::summonType));
        return result;
    }

    private List<com.denfense.server.balance.MergeRuleBalance> readMergeRuleSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "MergeRule");
        List<String> headers = requiredHeaders(sheet, "sourceGrade", "requiredCount", "sameSpeciesRequired",
                "resultType", "resultGrade", "enabled");
        List<com.denfense.server.balance.MergeRuleBalance> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            result.add(new com.denfense.server.balance.MergeRuleBalance(
                    readStringCell(sheet.getSheetName(), rowIndex, "sourceGrade", row.getCell(headers.indexOf("sourceGrade"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "requiredCount", row.getCell(headers.indexOf("requiredCount"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "sameSpeciesRequired", row.getCell(headers.indexOf("sameSpeciesRequired"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "resultType", row.getCell(headers.indexOf("resultType"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "resultGrade", row.getCell(headers.indexOf("resultGrade"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "enabled", row.getCell(headers.indexOf("enabled")))
            ));
        }
        List<String> gradeOrder = List.of("NORMAL", "EPIC", "UNIQUE", "LEGEND", "MYTHIC");
        result.sort(Comparator.comparingInt(rule -> gradeOrder.indexOf(rule.sourceGrade())));
        return result;
    }

    private List<com.denfense.server.balance.MythicChoiceBalance> readMythicChoiceSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "MythicChoiceBalance");
        List<String> headers = requiredHeaders(sheet, "modeId", "candidateCount", "freeRerollCount", "paidRerollLimit",
                "paidRerollCost", "excludePreviousCandidates", "selectionTimeoutSeconds", "autoSelectPolicy",
                "battleContinuesDuringSelection", "enabled");
        List<com.denfense.server.balance.MythicChoiceBalance> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            result.add(new com.denfense.server.balance.MythicChoiceBalance(
                    readStringCell(sheet.getSheetName(), rowIndex, "modeId", row.getCell(headers.indexOf("modeId"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "candidateCount", row.getCell(headers.indexOf("candidateCount"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "freeRerollCount", row.getCell(headers.indexOf("freeRerollCount"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "paidRerollLimit", row.getCell(headers.indexOf("paidRerollLimit"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "paidRerollCost", row.getCell(headers.indexOf("paidRerollCost"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "excludePreviousCandidates", row.getCell(headers.indexOf("excludePreviousCandidates"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "selectionTimeoutSeconds", row.getCell(headers.indexOf("selectionTimeoutSeconds"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "autoSelectPolicy", row.getCell(headers.indexOf("autoSelectPolicy"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "battleContinuesDuringSelection", row.getCell(headers.indexOf("battleContinuesDuringSelection"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "enabled", row.getCell(headers.indexOf("enabled")))
            ));
        }
        result.sort(Comparator.comparing(com.denfense.server.balance.MythicChoiceBalance::modeId));
        return result;
    }

    private List<com.denfense.server.balance.MutationSpecBalance> readMutationSpecSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "MutationSpec");
        List<String> headers = requiredHeaders(sheet, "mutationType", "enabled", "injectorEnabled", "randomActivationEnabled",
                "weight", "attackMultiplier", "mpMultiplier", "attackSpeedMultiplier", "rangeMultiplier", "goldMultiplier",
                "mechanic", "splashRadius", "splashDamageMultiplier", "bossDamageMultiplier", "dotDamageMultiplier",
                "dotTickCount", "dotTickIntervalSeconds", "slowMultiplier", "slowDurationSeconds", "goldPerHit",
                "gambleSuccessChance", "gambleSuccessMultiplier", "gambleFailureMultiplier");
        List<com.denfense.server.balance.MutationSpecBalance> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            result.add(new com.denfense.server.balance.MutationSpecBalance(
                    readStringCell(sheet.getSheetName(), rowIndex, "mutationType", row.getCell(headers.indexOf("mutationType"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "enabled", row.getCell(headers.indexOf("enabled"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "injectorEnabled", row.getCell(headers.indexOf("injectorEnabled"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "randomActivationEnabled", row.getCell(headers.indexOf("randomActivationEnabled"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "weight", row.getCell(headers.indexOf("weight"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "attackMultiplier", row.getCell(headers.indexOf("attackMultiplier"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "mpMultiplier", row.getCell(headers.indexOf("mpMultiplier"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "attackSpeedMultiplier", row.getCell(headers.indexOf("attackSpeedMultiplier"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "rangeMultiplier", row.getCell(headers.indexOf("rangeMultiplier"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "goldMultiplier", row.getCell(headers.indexOf("goldMultiplier"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "mechanic", row.getCell(headers.indexOf("mechanic"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "splashRadius", row.getCell(headers.indexOf("splashRadius"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "splashDamageMultiplier", row.getCell(headers.indexOf("splashDamageMultiplier"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "bossDamageMultiplier", row.getCell(headers.indexOf("bossDamageMultiplier"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "dotDamageMultiplier", row.getCell(headers.indexOf("dotDamageMultiplier"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "dotTickCount", row.getCell(headers.indexOf("dotTickCount"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "dotTickIntervalSeconds", row.getCell(headers.indexOf("dotTickIntervalSeconds"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "slowMultiplier", row.getCell(headers.indexOf("slowMultiplier"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "slowDurationSeconds", row.getCell(headers.indexOf("slowDurationSeconds"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "goldPerHit", row.getCell(headers.indexOf("goldPerHit"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "gambleSuccessChance", row.getCell(headers.indexOf("gambleSuccessChance"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "gambleSuccessMultiplier", row.getCell(headers.indexOf("gambleSuccessMultiplier"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "gambleFailureMultiplier", row.getCell(headers.indexOf("gambleFailureMultiplier")))
            ));
        }
        result.sort(Comparator.comparing(com.denfense.server.balance.MutationSpecBalance::mutationType));
        return result;
    }

    private com.denfense.server.balance.MutationConfigBalance readMutationConfigSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "MutationConfig");
        List<String> headers = requiredHeaders(sheet, "modeId", "initialActivationCost", "rerollCost1", "rerollCost2", "rerollCost3", "rerollCost4", "rerollCostAfterMax", "injectorReplaceCost");
        com.denfense.server.balance.MutationConfigBalance result = null;
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            if (result != null) throw new BalanceConversionException("[MutationConfig] exactly one data row is required.");
            result = new com.denfense.server.balance.MutationConfigBalance(
                    readStringCell(sheet.getSheetName(), rowIndex, "modeId", row.getCell(headers.indexOf("modeId"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "initialActivationCost", row.getCell(headers.indexOf("initialActivationCost"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "rerollCost1", row.getCell(headers.indexOf("rerollCost1"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "rerollCost2", row.getCell(headers.indexOf("rerollCost2"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "rerollCost3", row.getCell(headers.indexOf("rerollCost3"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "rerollCost4", row.getCell(headers.indexOf("rerollCost4"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "rerollCostAfterMax", row.getCell(headers.indexOf("rerollCostAfterMax"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "injectorReplaceCost", row.getCell(headers.indexOf("injectorReplaceCost")))
            );
        }
        if (result == null) throw new BalanceConversionException("[MutationConfig] data row is missing.");
        return result;
    }

    private List<com.denfense.server.balance.InjectorPoolBalance> readInjectorPoolSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "InjectorPool");
        List<String> headers = requiredHeaders(sheet, "poolId", "poolName", "poolActive", "mutationType", "weight", "resultType");
        List<com.denfense.server.balance.InjectorPoolBalance> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            result.add(new com.denfense.server.balance.InjectorPoolBalance(
                    readStringCell(sheet.getSheetName(), rowIndex, "poolId", row.getCell(headers.indexOf("poolId"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "poolName", row.getCell(headers.indexOf("poolName"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "poolActive", row.getCell(headers.indexOf("poolActive"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "mutationType", row.getCell(headers.indexOf("mutationType"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "weight", row.getCell(headers.indexOf("weight"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "resultType", row.getCell(headers.indexOf("resultType")))
            ));
        }
        result.sort(Comparator.comparing(com.denfense.server.balance.InjectorPoolBalance::mutationType));
        return result;
    }

    private List<com.denfense.server.balance.ResonanceBalance> readResonanceBalanceSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "ResonanceBalance");
        List<String> headers = requiredHeaders(sheet, "track", "level", "requiredGold", "attackMultiplier",
                "attackSpeedMultiplier", "rangeMultiplier", "enabled");
        List<com.denfense.server.balance.ResonanceBalance> result = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            result.add(new com.denfense.server.balance.ResonanceBalance(
                    readStringCell(sheet.getSheetName(), rowIndex, "track", row.getCell(headers.indexOf("track"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "level", row.getCell(headers.indexOf("level"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "requiredGold", row.getCell(headers.indexOf("requiredGold"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "attackMultiplier", row.getCell(headers.indexOf("attackMultiplier"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "attackSpeedMultiplier", row.getCell(headers.indexOf("attackSpeedMultiplier"))),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "rangeMultiplier", row.getCell(headers.indexOf("rangeMultiplier"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "enabled", row.getCell(headers.indexOf("enabled")))
            ));
        }
        result.sort(Comparator.comparing(com.denfense.server.balance.ResonanceBalance::track)
                .thenComparingInt(com.denfense.server.balance.ResonanceBalance::level));
        return result;
    }

    private com.denfense.server.balance.MythicBreedingConfigBalance readMythicBreedingConfigSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "MythicBreedingConfig");
        List<String> headers = requiredHeaders(sheet, "durationSeconds", "slotCount", "slot2UnlockLevel",
                "slot2GemPrice", "slot3GemPrice", "duplicateRewardPieces", "accelerationUnitSeconds",
                "accelerationUnitDiamondCost", "enabled");
        Row row = sheet.getRow(1);
        if (row == null) throw new BalanceConversionException("[MythicBreedingConfig] data row is missing.");
        return new com.denfense.server.balance.MythicBreedingConfigBalance(
                readIntCell(sheet.getSheetName(), 1, "durationSeconds", row.getCell(headers.indexOf("durationSeconds"))),
                readIntCell(sheet.getSheetName(), 1, "slotCount", row.getCell(headers.indexOf("slotCount"))),
                readIntCell(sheet.getSheetName(), 1, "slot2UnlockLevel", row.getCell(headers.indexOf("slot2UnlockLevel"))),
                readIntCell(sheet.getSheetName(), 1, "slot2GemPrice", row.getCell(headers.indexOf("slot2GemPrice"))),
                readIntCell(sheet.getSheetName(), 1, "slot3GemPrice", row.getCell(headers.indexOf("slot3GemPrice"))),
                readIntCell(sheet.getSheetName(), 1, "duplicateRewardPieces", row.getCell(headers.indexOf("duplicateRewardPieces"))),
                readIntCell(sheet.getSheetName(), 1, "accelerationUnitSeconds", row.getCell(headers.indexOf("accelerationUnitSeconds"))),
                readIntCell(sheet.getSheetName(), 1, "accelerationUnitDiamondCost", row.getCell(headers.indexOf("accelerationUnitDiamondCost"))),
                readBooleanCell(sheet.getSheetName(), 1, "enabled", row.getCell(headers.indexOf("enabled"))));
    }

    private List<com.denfense.server.balance.MythicBreedingResultBalance> readMythicBreedingResultSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "MythicBreedingResult");
        List<String> headers = requiredHeaders(sheet, "mythicNo", "alienId", "acquisitionType", "globalWeight", "enabled");
        List<com.denfense.server.balance.MythicBreedingResultBalance> results = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            results.add(new com.denfense.server.balance.MythicBreedingResultBalance(
                    readLongCell(sheet.getSheetName(), rowIndex, "alienId", row.getCell(headers.indexOf("alienId"))),
                    readStringCell(sheet.getSheetName(), rowIndex, "acquisitionType", row.getCell(headers.indexOf("acquisitionType"))),
                    readIntCell(sheet.getSheetName(), rowIndex, "globalWeight", row.getCell(headers.indexOf("globalWeight"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "enabled", row.getCell(headers.indexOf("enabled")))));
        }
        return results;
    }

    private List<com.denfense.server.balance.MythicBreedingRecipeBalance> readMythicBreedingRecipeSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "MythicBreedingRecipe");
        List<String> headers = requiredHeaders(sheet, "recipeKey", "parentMythicNoA", "parentAlienIdA",
                "parentMythicNoB", "parentAlienIdB", "candidate1AlienId", "candidate2AlienId", "candidate3AlienId",
                "candidate4AlienId", "candidate5AlienId", "standardWeightEach", "exclusive19Weight",
                "exclusive20Weight", "enabled");
        List<com.denfense.server.balance.MythicBreedingRecipeBalance> recipes = new ArrayList<>();
        for (int rowIndex = 1; rowIndex <= sheet.getLastRowNum(); rowIndex++) {
            Row row = sheet.getRow(rowIndex);
            if (row == null || isBlankRow(row, headers.size())) continue;
            recipes.add(new com.denfense.server.balance.MythicBreedingRecipeBalance(
                    readStringCell(sheet.getSheetName(), rowIndex, "recipeKey", row.getCell(headers.indexOf("recipeKey"))),
                    readLongCell(sheet.getSheetName(), rowIndex, "parentAlienIdA", row.getCell(headers.indexOf("parentAlienIdA"))),
                    readLongCell(sheet.getSheetName(), rowIndex, "parentAlienIdB", row.getCell(headers.indexOf("parentAlienIdB"))),
                    List.of(
                            readLongCell(sheet.getSheetName(), rowIndex, "candidate1AlienId", row.getCell(headers.indexOf("candidate1AlienId"))),
                            readLongCell(sheet.getSheetName(), rowIndex, "candidate2AlienId", row.getCell(headers.indexOf("candidate2AlienId"))),
                            readLongCell(sheet.getSheetName(), rowIndex, "candidate3AlienId", row.getCell(headers.indexOf("candidate3AlienId"))),
                            readLongCell(sheet.getSheetName(), rowIndex, "candidate4AlienId", row.getCell(headers.indexOf("candidate4AlienId"))),
                            readLongCell(sheet.getSheetName(), rowIndex, "candidate5AlienId", row.getCell(headers.indexOf("candidate5AlienId")))),
                    readIntCell(sheet.getSheetName(), rowIndex, "standardWeightEach", row.getCell(headers.indexOf("standardWeightEach"))),
                    47L,
                    readIntCell(sheet.getSheetName(), rowIndex, "exclusive19Weight", row.getCell(headers.indexOf("exclusive19Weight"))),
                    48L,
                    readIntCell(sheet.getSheetName(), rowIndex, "exclusive20Weight", row.getCell(headers.indexOf("exclusive20Weight"))),
                    readBooleanCell(sheet.getSheetName(), rowIndex, "enabled", row.getCell(headers.indexOf("enabled")))));
        }
        return recipes;
    }

    private List<String> requiredHeaders(Sheet sheet, String... expected) {
        Row header = sheet.getRow(0);
        if (header == null) {
            throw new BalanceConversionException("[" + sheet.getSheetName() + "] header is missing.");
        }
        return readHeaders(sheet.getSheetName(), header, Arrays.asList(expected));
    }

    public static record BalanceData(
        GameRewardBalance gameReward,
        com.denfense.server.balance.BattleRewardBalance battleReward,
        List<AlienUpgradeCostBalance> alienUpgradeCosts,
        List<com.denfense.server.service.balance.AlienLevelStatBalance> alienLevelStats,
        List<com.denfense.server.balance.AlienSpecBalance> alienSpecs,
        List<com.denfense.server.balance.ShopProductBalance> shopProducts,
        List<com.denfense.server.balance.GachaPoolBalance> gachaPools,
        List<com.denfense.server.balance.SummonPoolBalance> summonPools,
        List<com.denfense.server.balance.MonsterSpecBalance> monsters,
        List<com.denfense.server.balance.WaveSpecBalance> waves,
        List<com.denfense.server.balance.WaveSpawnBalance> waveSpawns,
        List<com.denfense.server.balance.PlanetBattleBalance> planetBattles,
        List<com.denfense.server.balance.FieldLimitBalance> fieldLimits,
        List<com.denfense.server.balance.SummonBalance> summons,
        List<com.denfense.server.balance.MergeRuleBalance> mergeRules,
        List<com.denfense.server.balance.MythicChoiceBalance> mythicChoices
        , List<com.denfense.server.balance.MutationSpecBalance> mutationSpecs,
        com.denfense.server.balance.MutationConfigBalance mutationConfig,
        List<com.denfense.server.balance.InjectorPoolBalance> injectorPools,
        List<com.denfense.server.balance.ResonanceBalance> resonanceBalances,
        com.denfense.server.balance.MythicBreedingConfigBalance mythicBreedingConfig,
        List<com.denfense.server.balance.MythicBreedingResultBalance> mythicBreedingResults,
        List<com.denfense.server.balance.MythicBreedingRecipeBalance> mythicBreedingRecipes
    ) {}
}
