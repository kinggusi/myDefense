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
            List<AlienUpgradeCostBalance> upgradeCosts = readAlienUpgradeCostSheet(workbook);
            List<com.denfense.server.service.balance.AlienLevelStatBalance> levelStats = readAlienLevelStatSheet(workbook);
            
            upgradeCosts.sort(Comparator.comparingInt(AlienUpgradeCostBalance::currentLevel));
            
            List<com.denfense.server.balance.AlienSpecBalance> alienSpecs = readAlienSpecSheet(workbook);
            List<com.denfense.server.balance.ShopProductBalance> shopProducts = readShopProductSheet(workbook);
            List<com.denfense.server.balance.GachaPoolBalance> gachaPools = readGachaPoolSheet(workbook);

            return new BalanceData(reward, upgradeCosts, levelStats, alienSpecs, shopProducts, gachaPools);

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
                    readDecimalCell(sheet.getSheetName(), rowIndex, "atkMultiplier", row.getCell(atkIndex)),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "mpMultiplier", row.getCell(mpIndex)),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "atkSpeedMultiplier", row.getCell(speedIndex)),
                    readDecimalCell(sheet.getSheetName(), rowIndex, "rangeMultiplier", row.getCell(rangeIndex))
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
        return BigDecimal.valueOf(value).setScale(2, RoundingMode.HALF_UP);
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

    public static record BalanceData(
        GameRewardBalance gameReward, 
        List<AlienUpgradeCostBalance> alienUpgradeCosts,
        List<com.denfense.server.service.balance.AlienLevelStatBalance> alienLevelStats,
        List<com.denfense.server.balance.AlienSpecBalance> alienSpecs,
        List<com.denfense.server.balance.ShopProductBalance> shopProducts,
        List<com.denfense.server.balance.GachaPoolBalance> gachaPools
    ) {}
}
