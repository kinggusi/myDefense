package com.denfense.server.balance;

import com.denfense.server.balance.tool.BalanceConversionException;
import com.denfense.server.balance.tool.ExcelBalanceReader;
import com.denfense.server.service.balance.BalanceDataValidator;
import com.denfense.server.service.balance.GameRewardBalance;
import org.apache.poi.ss.usermodel.Cell;
import org.apache.poi.ss.usermodel.Row;
import org.apache.poi.ss.usermodel.Sheet;
import org.apache.poi.ss.usermodel.Workbook;
import org.apache.poi.xssf.usermodel.XSSFWorkbook;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.jupiter.api.Assertions.assertThrows;

class BalanceExcelConverterTest {

    private Workbook workbook;
    private File tempExcel;

    @BeforeEach
    void setUp() throws IOException {
        workbook = new XSSFWorkbook();
        tempExcel = File.createTempFile("test_balance", ".xlsx");
    }

    private void saveAndClose() throws IOException {
        try (FileOutputStream fos = new FileOutputStream(tempExcel)) {
            workbook.write(fos);
        }
        workbook.close();
    }

    private void createValidWorkbook() {
        Sheet reward = workbook.createSheet("GameReward");
        Row headerReward = reward.createRow(0);
        headerReward.createCell(0).setCellValue("baseRewardGold");
        headerReward.createCell(1).setCellValue("goldPerWave");
        headerReward.createCell(2).setCellValue("maxRewardGold");
        Row rowReward = reward.createRow(1);
        rowReward.createCell(0).setCellValue(100);
        rowReward.createCell(1).setCellValue(10);
        rowReward.createCell(2).setCellValue(1000);

        Sheet upgrade = workbook.createSheet("AlienUpgradeCost");
        Row headerUpgrade = upgrade.createRow(0);
        headerUpgrade.createCell(0).setCellValue("currentLevel");
        headerUpgrade.createCell(1).setCellValue("targetLevel");
        headerUpgrade.createCell(2).setCellValue("requiredPieces");
        headerUpgrade.createCell(3).setCellValue("requiredGold");
        headerUpgrade.createCell(4).setCellValue("requiredGrowthCell");
        for (int level = 1; level <= 49; level++) {
            Row row = upgrade.createRow(level);
            row.createCell(0).setCellValue(level);
            row.createCell(1).setCellValue(level + 1);
            row.createCell(2).setCellValue(level * 5);
            row.createCell(3).setCellValue(level * 100);
            row.createCell(4).setCellValue(level < 9 ? 0 : Math.min(50, ((level - 9) / 10 + 1) * 10));
        }

        Sheet levelStat = workbook.createSheet("AlienLevelStat");
        Row headerStat = levelStat.createRow(0);
        headerStat.createCell(0).setCellValue("level");
        headerStat.createCell(1).setCellValue("atkMultiplier");
        headerStat.createCell(2).setCellValue("mpMultiplier");
        headerStat.createCell(3).setCellValue("atkSpeedMultiplier");
        headerStat.createCell(4).setCellValue("rangeMultiplier");
        for (int level = 1; level <= 50; level++) {
            Row row = levelStat.createRow(level);
            row.createCell(0).setCellValue(level);
            row.createCell(1).setCellValue(1 + (level - 1) * 0.05);
            row.createCell(2).setCellValue(1 + (level - 1) * 0.03);
            row.createCell(3).setCellValue(1 + (level / 10) * 0.02);
            row.createCell(4).setCellValue(1.00);
        }

        Sheet spec = workbook.createSheet("AlienSpec");
        Row headerSpec = spec.createRow(0);
        headerSpec.createCell(0).setCellValue("alienId");
        headerSpec.createCell(1).setCellValue("name");
        headerSpec.createCell(2).setCellValue("description");
        headerSpec.createCell(3).setCellValue("grade");
        headerSpec.createCell(4).setCellValue("baseAttack");
        headerSpec.createCell(5).setCellValue("baseMp");
        headerSpec.createCell(6).setCellValue("attackSpeed");
        headerSpec.createCell(7).setCellValue("attackRange");
        headerSpec.createCell(8).setCellValue("evolutionTargetId");
        headerSpec.createCell(9).setCellValue("isLocked");
        Object[][] specs = {
            {1, "NORMAL"},
            {2, "EPIC"},
            {3, "UNIQUE"},
            {4, "LEGEND"},
            {29, "MYTHIC"},
            {30, "MYTHIC"},
            {31, "MYTHIC"},
            {32, "MYTHIC"}
        };
        int r = 1;
        for (Object[] sp : specs) {
            Row rowSpec = spec.createRow(r++);
            rowSpec.createCell(0).setCellValue((Integer) sp[0]);
            rowSpec.createCell(1).setCellValue("A");
            rowSpec.createCell(2).setCellValue("");
            rowSpec.createCell(3).setCellValue((String) sp[1]);
            rowSpec.createCell(4).setCellValue(10);
            rowSpec.createCell(5).setCellValue(10);
            rowSpec.createCell(6).setCellValue(1.0);
            rowSpec.createCell(7).setCellValue(1.0);
            rowSpec.createCell(9).setCellValue(false);
        }

        Sheet shop = workbook.createSheet("ShopProduct");
        Row hShop = shop.createRow(0);
        hShop.createCell(0).setCellValue("productId");
        hShop.createCell(1).setCellValue("name");
        hShop.createCell(2).setCellValue("currencyType");
        hShop.createCell(3).setCellValue("price");
        hShop.createCell(4).setCellValue("drawCount");
        hShop.createCell(5).setCellValue("gachaPoolId");
        hShop.createCell(6).setCellValue("active");

        Row rShop1 = shop.createRow(1);
        rShop1.createCell(0).setCellValue("ALIEN_GACHA_SINGLE");
        rShop1.createCell(1).setCellValue("1회 뽑기");
        rShop1.createCell(2).setCellValue("DIAMOND");
        rShop1.createCell(3).setCellValue(500);
        rShop1.createCell(4).setCellValue(1);
        rShop1.createCell(5).setCellValue("STANDARD_ALIEN_POOL");
        rShop1.createCell(6).setCellValue(true);

        Row rShop2 = shop.createRow(2);
        rShop2.createCell(0).setCellValue("ALIEN_GACHA_TEN");
        rShop2.createCell(1).setCellValue("10회 연속 뽑기");
        rShop2.createCell(2).setCellValue("DIAMOND");
        rShop2.createCell(3).setCellValue(5000);
        rShop2.createCell(4).setCellValue(10);
        rShop2.createCell(5).setCellValue("STANDARD_ALIEN_POOL");
        rShop2.createCell(6).setCellValue(true);

        Sheet pool = workbook.createSheet("GachaPool");
        Row hPool = pool.createRow(0);
        hPool.createCell(0).setCellValue("poolId");
        hPool.createCell(1).setCellValue("poolName");
        hPool.createCell(2).setCellValue("poolActive");
        hPool.createCell(3).setCellValue("grade");
        hPool.createCell(4).setCellValue("weight");
        hPool.createCell(5).setCellValue("alienIds");

        Row rPool1 = pool.createRow(1);
        rPool1.createCell(0).setCellValue("STANDARD_ALIEN_POOL");
        rPool1.createCell(1).setCellValue("Test Pool");
        rPool1.createCell(2).setCellValue(true);
        rPool1.createCell(3).setCellValue("NORMAL");
        rPool1.createCell(4).setCellValue(6000);
        rPool1.createCell(5).setCellValue("1");

        Row rPool2 = pool.createRow(2);
        rPool2.createCell(0).setCellValue("STANDARD_ALIEN_POOL");
        rPool2.createCell(1).setCellValue("Test Pool");
        rPool2.createCell(2).setCellValue(true);
        rPool2.createCell(3).setCellValue("EPIC");
        rPool2.createCell(4).setCellValue(2800);
        rPool2.createCell(5).setCellValue("2");

        Row rPool3 = pool.createRow(3);
        rPool3.createCell(0).setCellValue("STANDARD_ALIEN_POOL");
        rPool3.createCell(1).setCellValue("Test Pool");
        rPool3.createCell(2).setCellValue(true);
        rPool3.createCell(3).setCellValue("UNIQUE");
        rPool3.createCell(4).setCellValue(900);
        rPool3.createCell(5).setCellValue("3");

        Row rPool4 = pool.createRow(4);
        rPool4.createCell(0).setCellValue("STANDARD_ALIEN_POOL");
        rPool4.createCell(1).setCellValue("Test Pool");
        rPool4.createCell(2).setCellValue(true);
        rPool4.createCell(3).setCellValue("LEGEND");
        rPool4.createCell(4).setCellValue(250);
        rPool4.createCell(5).setCellValue("4");

        Row rPool5 = pool.createRow(5);
        rPool5.createCell(0).setCellValue("STANDARD_ALIEN_POOL");
        rPool5.createCell(1).setCellValue("Test Pool");
        rPool5.createCell(2).setCellValue(true);
        rPool5.createCell(3).setCellValue("MYTHIC");
        rPool5.createCell(4).setCellValue(50);
        rPool5.createCell(5).setCellValue("29,30,31,32");

        BattleBalanceTestWorkbook.addValidSheets(workbook);
    }

    @Test
    @DisplayName("1. 정상 변환 및 30. Validator 검증")
    void validConversion() throws IOException {
        createValidWorkbook();
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        ExcelBalanceReader.BalanceData data = reader.read();

        BalanceDataValidator validator = new BalanceDataValidator();
        validator.validateGameReward(data.gameReward());
        validator.validateAlienLevelStats(data.alienLevelStats());
        validator.validateAlienUpgradeCosts(data.alienUpgradeCosts(), 50);
        validator.validateAlienSpec(data.alienSpecs());
        com.denfense.server.balance.GachaPoolBalanceDocument poolDoc = new com.denfense.server.balance.GachaPoolBalanceDocument(data.gachaPools());
        validator.validateGachaPool(poolDoc, data.alienSpecs());
        com.denfense.server.balance.ShopProductBalanceDocument shopDoc = new com.denfense.server.balance.ShopProductBalanceDocument(data.shopProducts());
        validator.validateShopProduct(shopDoc, poolDoc);

        assertThat(data.gameReward().baseRewardGold()).isEqualTo(100);
        assertThat(data.alienLevelStats()).hasSize(50);
        assertThat(data.alienUpgradeCosts()).hasSize(49);

        Map<Integer, com.denfense.server.service.balance.AlienUpgradeCostBalance> costsByLevel =
                data.alienUpgradeCosts().stream().collect(Collectors.toMap(
                        com.denfense.server.service.balance.AlienUpgradeCostBalance::currentLevel,
                        Function.identity()));
        assertThat(costsByLevel.get(8).requiredGrowthCell()).isZero();
        assertThat(costsByLevel.get(9).requiredGrowthCell()).isEqualTo(10);
        assertThat(costsByLevel.get(18).requiredGrowthCell()).isEqualTo(10);
        assertThat(costsByLevel.get(19).requiredGrowthCell()).isEqualTo(20);
        assertThat(costsByLevel.get(28).requiredGrowthCell()).isEqualTo(20);
        assertThat(costsByLevel.get(29).requiredGrowthCell()).isEqualTo(30);
        assertThat(costsByLevel.get(38).requiredGrowthCell()).isEqualTo(30);
        assertThat(costsByLevel.get(39).requiredGrowthCell()).isEqualTo(40);
        assertThat(costsByLevel.get(48).requiredGrowthCell()).isEqualTo(40);
        assertThat(costsByLevel.get(49).requiredGrowthCell()).isEqualTo(50);

        Map<Integer, com.denfense.server.service.balance.AlienLevelStatBalance> statsByLevel =
                data.alienLevelStats().stream().collect(Collectors.toMap(
                        com.denfense.server.service.balance.AlienLevelStatBalance::level,
                        Function.identity()));
        assertStat(statsByLevel, 1, "1.00", "1.00", "1.00");
        assertStat(statsByLevel, 9, "1.40", "1.24", "1.00");
        assertStat(statsByLevel, 10, "1.45", "1.27", "1.02");
        assertStat(statsByLevel, 20, "1.95", "1.57", "1.04");
        assertStat(statsByLevel, 30, "2.45", "1.87", "1.06");
        assertStat(statsByLevel, 40, "2.95", "2.17", "1.08");
        assertStat(statsByLevel, 50, "3.45", "2.47", "1.10");
        
        assertThat(data.shopProducts()).hasSize(2);
        com.denfense.server.balance.ShopProductBalance shop1 = data.shopProducts().get(0);
        assertThat(shop1.productId()).isEqualTo("ALIEN_GACHA_SINGLE");
        assertThat(shop1.price()).isEqualTo(500);
        assertThat(shop1.drawCount()).isEqualTo(1);
        assertThat(shop1.gachaPoolId()).isEqualTo("STANDARD_ALIEN_POOL");
        
        com.denfense.server.balance.ShopProductBalance shop2 = data.shopProducts().get(1);
        assertThat(shop2.productId()).isEqualTo("ALIEN_GACHA_TEN");
        assertThat(shop2.price()).isEqualTo(5000);
        assertThat(shop2.drawCount()).isEqualTo(10);
        assertThat(shop2.gachaPoolId()).isEqualTo("STANDARD_ALIEN_POOL");
        
        assertThat(data.gachaPools()).hasSize(1);
        com.denfense.server.balance.GachaPoolBalance pool = data.gachaPools().get(0);
        assertThat(pool.poolId()).isEqualTo("STANDARD_ALIEN_POOL");
        assertThat(pool.gradeEntries()).hasSize(5);
        
        int weightSum = pool.gradeEntries().stream().mapToInt(com.denfense.server.balance.GachaGradeEntryBalance::weight).sum();
        assertThat(weightSum).isEqualTo(10000);
        
        com.denfense.server.balance.GachaGradeEntryBalance mythicEntry = pool.gradeEntries().stream()
            .filter(e -> "MYTHIC".equals(e.grade()))
            .findFirst().orElseThrow();
        assertThat(mythicEntry.weight()).isEqualTo(50);
        assertThat(mythicEntry.alienIds()).containsExactly(29L, 30L, 31L, 32L);
        assertThat(mythicEntry.alienIds()).noneMatch(id -> id >= 33 && id <= 48);
    }

    private void assertStat(
            Map<Integer, com.denfense.server.service.balance.AlienLevelStatBalance> statsByLevel,
            int level,
            String atk,
            String mp,
            String speed
    ) {
        com.denfense.server.service.balance.AlienLevelStatBalance stat = statsByLevel.get(level);
        assertThat(stat.atkMultiplier()).isEqualByComparingTo(atk);
        assertThat(stat.mpMultiplier()).isEqualByComparingTo(mp);
        assertThat(stat.atkSpeedMultiplier()).isEqualByComparingTo(speed);
        assertThat(stat.rangeMultiplier()).isEqualByComparingTo("1.00");
    }

    @Test
    @DisplayName("4. AlienUpgradeCost 시트 없음")
    void missingSheet() throws IOException {
        createValidWorkbook();
        workbook.removeSheetAt(workbook.getSheetIndex("AlienUpgradeCost"));
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        BalanceConversionException ex = assertThrows(BalanceConversionException.class, reader::read);
        assertThat(ex).hasMessageContaining("AlienUpgradeCost");
    }

    @Test
    @DisplayName("10. 문자열 숫자 거부")
    void stringNumeric() throws IOException {
        createValidWorkbook();
        workbook.getSheet("AlienUpgradeCost").getRow(1).getCell(0).setCellValue("1");
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        BalanceConversionException ex = assertThrows(BalanceConversionException.class, reader::read);
        assertThat(ex).hasMessageContaining("문자열 형태의 숫자는 허용되지 않습니다");
    }

    @Test
    @DisplayName("11. 소수 거부")
    void decimalNumeric() throws IOException {
        createValidWorkbook();
        workbook.getSheet("GameReward").getRow(1).getCell(0).setCellValue(100.5); 
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        BalanceConversionException ex = assertThrows(BalanceConversionException.class, reader::read);
        assertThat(ex).hasMessageContaining("소수는 허용되지 않습니다");
    }

    @Test
    @DisplayName("16. 수식 정상 반영")
    void formulaCell() throws IOException {
        createValidWorkbook();
        workbook.getSheet("GameReward").getRow(1).getCell(0).setCellFormula("50*2"); 
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        ExcelBalanceReader.BalanceData data = reader.read();
        assertThat(data.gameReward().baseRewardGold()).isEqualTo(100);
    }

    @Test
    @DisplayName("19. 숨김 행 처리됨")
    void hiddenRow() throws IOException {
        createValidWorkbook();
        workbook.getSheet("AlienUpgradeCost").getRow(1).setZeroHeight(true);
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        ExcelBalanceReader.BalanceData data = reader.read();
        assertThat(data.alienUpgradeCosts()).hasSize(49);
    }

    @Test
    @DisplayName("18. 병합 셀 거부")
    void mergedRegion() throws IOException {
        createValidWorkbook();
        workbook.getSheet("GameReward").addMergedRegion(new org.apache.poi.ss.util.CellRangeAddress(0, 0, 0, 1));
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        BalanceConversionException ex = assertThrows(BalanceConversionException.class, reader::read);
        assertThat(ex).hasMessageContaining("병합된 셀이 존재합니다");
    }

    @Test
    @DisplayName("22. 중복 레벨 거부 (Validator에서 수행)")
    void duplicateLevel() throws IOException {
        createValidWorkbook();
        workbook.getSheet("AlienUpgradeCost").getRow(2).getCell(0).setCellValue(1);
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        ExcelBalanceReader.BalanceData data = reader.read();
        BalanceDataValidator validator = new BalanceDataValidator();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgradeCosts(data.alienUpgradeCosts(), 50));
    }

    @Test
    @DisplayName("duplicateShopProductId_fails")
    void duplicateShopProductId_fails() throws IOException {
        createValidWorkbook();
        workbook.getSheet("ShopProduct").getRow(2).getCell(0).setCellValue("ALIEN_GACHA_SINGLE");
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        BalanceConversionException ex = assertThrows(BalanceConversionException.class, reader::read);
        assertThat(ex).hasMessageContaining("중복된 상품 ID입니다");
    }

    @Test
    @DisplayName("nonPositiveShopProductPrice_fails")
    void nonPositiveShopProductPrice_fails() throws IOException {
        createValidWorkbook();
        workbook.getSheet("ShopProduct").getRow(1).getCell(3).setCellValue(0);
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        BalanceConversionException ex = assertThrows(BalanceConversionException.class, reader::read);
        assertThat(ex).hasMessageContaining("가격은 양수여야 합니다");
    }

    @Test
    @DisplayName("nonPositiveDrawCount_fails")
    void nonPositiveDrawCount_fails() throws IOException {
        createValidWorkbook();
        workbook.getSheet("ShopProduct").getRow(1).getCell(4).setCellValue(0);
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        BalanceConversionException ex = assertThrows(BalanceConversionException.class, reader::read);
        assertThat(ex).hasMessageContaining("뽑기 횟수는 양수여야 합니다");
    }

    @Test
    @DisplayName("duplicateGradeInSamePool_fails")
    void duplicateGradeInSamePool_fails() throws IOException {
        createValidWorkbook();
        workbook.getSheet("GachaPool").getRow(2).getCell(3).setCellValue("NORMAL");
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        ExcelBalanceReader.BalanceData data = reader.read();
        BalanceDataValidator validator = new BalanceDataValidator();
        com.denfense.server.balance.GachaPoolBalanceDocument poolDoc = new com.denfense.server.balance.GachaPoolBalanceDocument(data.gachaPools());
        IllegalStateException ex = assertThrows(IllegalStateException.class, () -> validator.validateGachaPool(poolDoc, data.alienSpecs()));
        assertThat(ex).hasMessageContaining("중복된 grade가 존재합니다");
    }

    @Test
    @DisplayName("invalidPoolWeightSum_fails")
    void invalidPoolWeightSum_fails() throws IOException {
        createValidWorkbook();
        workbook.getSheet("GachaPool").getRow(1).getCell(4).setCellValue(6001);
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        ExcelBalanceReader.BalanceData data = reader.read();
        BalanceDataValidator validator = new BalanceDataValidator();
        com.denfense.server.balance.GachaPoolBalanceDocument poolDoc = new com.denfense.server.balance.GachaPoolBalanceDocument(data.gachaPools());
        IllegalStateException ex = assertThrows(IllegalStateException.class, () -> validator.validateGachaPool(poolDoc, data.alienSpecs()));
        assertThat(ex).hasMessageContaining("총합은 10000이어야 합니다");
    }

    @Test
    @DisplayName("invalidAlienIds_fails")
    void invalidAlienIds_fails() throws IOException {
        createValidWorkbook();
        workbook.getSheet("GachaPool").getRow(1).getCell(5).setCellValue("1,abc,2");
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        BalanceConversionException ex = assertThrows(BalanceConversionException.class, reader::read);
        assertThat(ex).hasMessageContaining("숫자가 아닙니다");
    }

    @Test
    @DisplayName("duplicateAlienIdInSamePool_fails")
    void duplicateAlienIdInSamePool_fails() throws IOException {
        createValidWorkbook();
        workbook.getSheet("GachaPool").getRow(2).getCell(5).setCellValue("1"); // Row 1 already has "1"
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        ExcelBalanceReader.BalanceData data = reader.read();
        BalanceDataValidator validator = new BalanceDataValidator();
        com.denfense.server.balance.GachaPoolBalanceDocument poolDoc = new com.denfense.server.balance.GachaPoolBalanceDocument(data.gachaPools());
        IllegalStateException ex = assertThrows(IllegalStateException.class, () -> validator.validateGachaPool(poolDoc, data.alienSpecs()));
        assertThat(ex).hasMessageContaining("중복된 alienId가 존재합니다");
    }

    @Test
    void upgradeCostTargetLevelMismatchFails() throws IOException {
        createValidWorkbook();
        workbook.getSheet("AlienUpgradeCost").getRow(1).getCell(1).setCellValue(3);
        assertUpgradeValidationFails();
    }

    @Test
    void upgradeCostNonPositiveValuesFail() throws IOException {
        createValidWorkbook();
        workbook.getSheet("AlienUpgradeCost").getRow(1).getCell(2).setCellValue(0);
        assertUpgradeValidationFails();
    }

    @Test
    void upgradeCostNegativeGrowthCellFails() throws IOException {
        createValidWorkbook();
        workbook.getSheet("AlienUpgradeCost").getRow(1).getCell(4).setCellValue(-1);
        assertUpgradeValidationFails();
    }

    @Test
    void upgradeCostLevel50RowFails() throws IOException {
        createValidWorkbook();
        Row row = workbook.getSheet("AlienUpgradeCost").createRow(50);
        row.createCell(0).setCellValue(50);
        row.createCell(1).setCellValue(51);
        row.createCell(2).setCellValue(250);
        row.createCell(3).setCellValue(5000);
        row.createCell(4).setCellValue(50);
        assertUpgradeValidationFails();
    }

    @Test
    void alienLevelStatMissingAndDuplicateLevelFail() throws IOException {
        createValidWorkbook();
        workbook.getSheet("AlienLevelStat").getRow(50).getCell(0).setCellValue(49);
        assertLevelStatValidationFails();
    }

    @Test
    void alienLevelStatNonPositiveMultiplierFails() throws IOException {
        createValidWorkbook();
        workbook.getSheet("AlienLevelStat").getRow(10).getCell(1).setCellValue(0);
        assertLevelStatValidationFails();
    }

    @Test
    void alienLevelStatLevelOneRuleFails() throws IOException {
        createValidWorkbook();
        workbook.getSheet("AlienLevelStat").getRow(1).getCell(1).setCellValue(1.05);
        assertLevelStatValidationFails();
    }

    @Test
    void alienLevelStatRangeRuleFails() throws IOException {
        createValidWorkbook();
        workbook.getSheet("AlienLevelStat").getRow(2).getCell(4).setCellValue(1.01);
        assertLevelStatValidationFails();
    }

    @Test
    void alienLevelStatMalformedDecimalFailsDuringRead() throws IOException {
        createValidWorkbook();
        workbook.getSheet("AlienLevelStat").getRow(10).getCell(1).setCellValue("1.45");
        saveAndClose();
        assertThrows(BalanceConversionException.class,
                () -> new ExcelBalanceReader(tempExcel.getAbsolutePath()).read());
    }

    private void assertUpgradeValidationFails() throws IOException {
        saveAndClose();
        ExcelBalanceReader.BalanceData data = new ExcelBalanceReader(tempExcel.getAbsolutePath()).read();
        BalanceDataValidator validator = new BalanceDataValidator();
        assertThrows(IllegalStateException.class,
                () -> validator.validateAlienUpgradeCosts(data.alienUpgradeCosts(), 50));
    }

    private void assertLevelStatValidationFails() throws IOException {
        saveAndClose();
        ExcelBalanceReader.BalanceData data = new ExcelBalanceReader(tempExcel.getAbsolutePath()).read();
        BalanceDataValidator validator = new BalanceDataValidator();
        assertThrows(IllegalStateException.class,
                () -> validator.validateAlienLevelStats(data.alienLevelStats()));
    }

    @Test
    @DisplayName("invalidBoolean_fails")
    void invalidBoolean_fails() throws IOException {
        createValidWorkbook();
        workbook.getSheet("ShopProduct").getRow(1).getCell(6).setCellValue("invalid_boolean");
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        BalanceConversionException ex = assertThrows(BalanceConversionException.class, reader::read);
        assertThat(ex).hasMessageContaining("Boolean이어야 합니다");
    }
}
