package com.denfense.server.balance.tool;

import com.denfense.server.service.balance.AlienUpgradeBalanceFile;
import com.denfense.server.service.balance.AlienUpgradeCostBalance;
import com.denfense.server.service.balance.GameRewardBalance;
import org.apache.poi.ss.usermodel.*;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
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

            int maxLevel = readConfigSheet(workbook);
            GameRewardBalance reward = readGameRewardSheet(workbook);
            List<AlienUpgradeCostBalance> upgradeCosts = readAlienUpgradeSheet(workbook, maxLevel);
            
            upgradeCosts.sort(Comparator.comparingInt(AlienUpgradeCostBalance::currentLevel));
            
            List<com.denfense.server.balance.AlienSpecBalance> alienSpecs = readAlienSpecSheet(workbook);

            return new BalanceData(reward, new AlienUpgradeBalanceFile(maxLevel, upgradeCosts), alienSpecs);

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

    private int readConfigSheet(Workbook workbook) {
        Sheet sheet = getSheetOrThrow(workbook, "Config");
        
        // Config header check
        Row headerRow = sheet.getRow(0);
        if (headerRow == null) {
            throw new BalanceConversionException("[Config] 헤더가 없습니다.");
        }
        
        List<String> headers = readHeaders(sheet.getSheetName(), headerRow, Arrays.asList("key", "value"));
        
        int keyIndex = headers.indexOf("key");
        int valueIndex = headers.indexOf("value");
        
        Integer maxLevel = null;
        Set<String> seenKeys = new HashSet<>();
        
        for (int i = 1; i <= sheet.getLastRowNum(); i++) {
            Row row = sheet.getRow(i);
            if (row == null) continue;
            
            Cell keyCell = row.getCell(keyIndex);
            if (keyCell == null || keyCell.getCellType() == CellType.BLANK) continue; // 빈 행 무시(Key가 없으면)? 아니면 빈 셀 거부? "빈 셀 즉시 실패"
            
            String key = readStringCell(sheet.getSheetName(), i, "key", keyCell);
            if (!seenKeys.add(key)) {
                throw new BalanceConversionException(String.format("[%s] %d행 'key' 열: %s - 중복된 키입니다.", sheet.getSheetName(), i + 1, key));
            }
            
            Cell valueCell = row.getCell(valueIndex);
            
            if ("maxLevel".equals(key)) {
                maxLevel = readIntCell(sheet.getSheetName(), i, "value", valueCell);
            } else {
                throw new BalanceConversionException(String.format("[%s] %d행 'key' 열: %s - 알 수 없는 키입니다.", sheet.getSheetName(), i + 1, key));
            }
        }
        
        if (maxLevel == null) {
            throw new BalanceConversionException("[Config] 필수 키 'maxLevel'이 누락되었습니다.");
        }
        
        return maxLevel;
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

    private List<AlienUpgradeCostBalance> readAlienUpgradeSheet(Workbook workbook, int maxLevel) {
        Sheet sheet = getSheetOrThrow(workbook, "AlienUpgrade");
        
        Row headerRow = sheet.getRow(0);
        if (headerRow == null) {
            throw new BalanceConversionException("[AlienUpgrade] 헤더가 없습니다.");
        }
        
        List<String> expectedHeaders = Arrays.asList("currentLevel", "requiredPieces", "requiredGold", "requiredGrowthCell");
        List<String> headers = readHeaders(sheet.getSheetName(), headerRow, expectedHeaders);
        
        int levelIndex = headers.indexOf("currentLevel");
        int piecesIndex = headers.indexOf("requiredPieces");
        int goldIndex = headers.indexOf("requiredGold");
        int cellIndex = headers.indexOf("requiredGrowthCell");
        
        List<AlienUpgradeCostBalance> costs = new ArrayList<>();
        
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
            
            int clvl = readIntCell(sheet.getSheetName(), i, "currentLevel", row.getCell(levelIndex));
            int pieces = readIntCell(sheet.getSheetName(), i, "requiredPieces", row.getCell(piecesIndex));
            int gold = readIntCell(sheet.getSheetName(), i, "requiredGold", row.getCell(goldIndex));
            int gcell = readIntCell(sheet.getSheetName(), i, "requiredGrowthCell", row.getCell(cellIndex));
            
            costs.add(new AlienUpgradeCostBalance(clvl, pieces, gold, gcell));
        }
        
        return costs;
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

    public static record BalanceData(GameRewardBalance gameReward, AlienUpgradeBalanceFile alienUpgrade, List<com.denfense.server.balance.AlienSpecBalance> alienSpecs) {}
}
