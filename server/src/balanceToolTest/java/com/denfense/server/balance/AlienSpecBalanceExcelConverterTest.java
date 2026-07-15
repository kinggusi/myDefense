package com.denfense.server.balance;

import com.denfense.server.balance.tool.BalanceConversionException;
import com.denfense.server.balance.tool.ExcelBalanceReader;
import com.denfense.server.service.balance.BalanceDataValidator;
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
import java.util.ArrayList;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.jupiter.api.Assertions.assertThrows;

public class AlienSpecBalanceExcelConverterTest {

    private Workbook workbook;
    private File tempExcel;
    private BalanceDataValidator validator;

    @BeforeEach
    void setUp() throws IOException {
        workbook = new XSSFWorkbook();
        tempExcel = File.createTempFile("test_balance", ".xlsx");
        validator = new BalanceDataValidator();
    }

    private void saveAndClose() throws IOException {
        try (FileOutputStream fos = new FileOutputStream(tempExcel)) {
            workbook.write(fos);
        }
        workbook.close();
    }

    private void createBaseSheets() {
        Sheet config = workbook.createSheet("Config");
        Row headerConfig = config.createRow(0);
        headerConfig.createCell(0).setCellValue("key");
        headerConfig.createCell(1).setCellValue("value");
        Row rowConfig = config.createRow(1);
        rowConfig.createCell(0).setCellValue("maxLevel");
        rowConfig.createCell(1).setCellValue(3);

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
        Row statHeader = levelStat.createRow(0);
        statHeader.createCell(0).setCellValue("level");
        statHeader.createCell(1).setCellValue("atkMultiplier");
        statHeader.createCell(2).setCellValue("mpMultiplier");
        statHeader.createCell(3).setCellValue("atkSpeedMultiplier");
        statHeader.createCell(4).setCellValue("rangeMultiplier");
        for (int level = 1; level <= 50; level++) {
            Row row = levelStat.createRow(level);
            row.createCell(0).setCellValue(level);
            row.createCell(1).setCellValue(1 + (level - 1) * 0.05);
            row.createCell(2).setCellValue(1 + (level - 1) * 0.03);
            row.createCell(3).setCellValue(1 + (level / 10) * 0.02);
            row.createCell(4).setCellValue(1.0);
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

        Row rShop = shop.createRow(1);
        rShop.createCell(0).setCellValue("TEST_PROD");
        rShop.createCell(1).setCellValue("Test Prod");
        rShop.createCell(2).setCellValue("DIAMOND");
        rShop.createCell(3).setCellValue(500);
        rShop.createCell(4).setCellValue(1);
        rShop.createCell(5).setCellValue("TEST_POOL");
        rShop.createCell(6).setCellValue(true);

        Sheet pool = workbook.createSheet("GachaPool");
        Row hPool = pool.createRow(0);
        hPool.createCell(0).setCellValue("poolId");
        hPool.createCell(1).setCellValue("poolName");
        hPool.createCell(2).setCellValue("poolActive");
        hPool.createCell(3).setCellValue("grade");
        hPool.createCell(4).setCellValue("weight");
        hPool.createCell(5).setCellValue("alienIds");

        Row rPool = pool.createRow(1);
        rPool.createCell(0).setCellValue("TEST_POOL");
        rPool.createCell(1).setCellValue("Test Pool");
        rPool.createCell(2).setCellValue(true);
        rPool.createCell(3).setCellValue("NORMAL");
        rPool.createCell(4).setCellValue(10000);
        rPool.createCell(5).setCellValue("1");
    }

    private Sheet createAlienSpecSheet() {
        Sheet spec = workbook.createSheet("AlienSpec");
        Row header = spec.createRow(0);
        header.createCell(0).setCellValue("alienId");
        header.createCell(1).setCellValue("name");
        header.createCell(2).setCellValue("description");
        header.createCell(3).setCellValue("grade");
        header.createCell(4).setCellValue("baseAttack");
        header.createCell(5).setCellValue("baseMp");
        header.createCell(6).setCellValue("attackSpeed");
        header.createCell(7).setCellValue("attackRange");
        header.createCell(8).setCellValue("evolutionTargetId");
        header.createCell(9).setCellValue("isLocked");
        return spec;
    }

    private void addSpecRow(Sheet spec, int rowNum, Object id, Object name, Object desc, Object grade, Object atk, Object mp, Object speed, Object range, Object target, Object locked) {
        Row row = spec.createRow(rowNum);
        if (id instanceof Number) row.createCell(0).setCellValue(((Number) id).doubleValue()); else if (id != null) row.createCell(0).setCellValue(id.toString());
        if (name != null) row.createCell(1).setCellValue(name.toString());
        if (desc != null) row.createCell(2).setCellValue(desc.toString());
        if (grade != null) row.createCell(3).setCellValue(grade.toString());
        if (atk instanceof Number) row.createCell(4).setCellValue(((Number) atk).doubleValue()); else if (atk != null) row.createCell(4).setCellValue(atk.toString());
        if (mp instanceof Number) row.createCell(5).setCellValue(((Number) mp).doubleValue()); else if (mp != null) row.createCell(5).setCellValue(mp.toString());
        if (speed instanceof Number) row.createCell(6).setCellValue(((Number) speed).doubleValue()); else if (speed != null) row.createCell(6).setCellValue(speed.toString());
        if (range instanceof Number) row.createCell(7).setCellValue(((Number) range).doubleValue()); else if (range != null) row.createCell(7).setCellValue(range.toString());
        
        if (target != null) {
            if (target instanceof Number) row.createCell(8).setCellValue(((Number) target).doubleValue());
            else row.createCell(8).setCellValue(target.toString());
        }
        
        if (locked instanceof Boolean) row.createCell(9).setCellValue((Boolean) locked);
        else if (locked != null) row.createCell(9).setCellValue(locked.toString());
    }

    private ExcelBalanceReader.BalanceData readData() throws IOException {
        saveAndClose();
        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        return reader.read();
    }

    @Test
    @DisplayName("1. 정상 AlienSpec 32건 변환 및 2. alienId 오름차순 검증")
    void valid32Specs() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        for (int i = 1; i <= 32; i++) {
            addSpecRow(spec, i, i, "Name" + i, "Desc" + i, "NORMAL", 10, 10, 1.0, 1.0, null, false);
        }
        
        ExcelBalanceReader.BalanceData data = readData();
        List<AlienSpecBalance> specs = data.alienSpecs();
        assertThat(specs).hasSize(32);
        
        validator.validateAlienSpec(specs);
        
        for (int i = 0; i < 32; i++) {
            assertThat(specs.get(i).alienId()).isEqualTo(i + 1);
        }
    }

    @Test
    @DisplayName("3. 중복 alienId 거절")
    void duplicateId() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 1, "A", "", "NORMAL", 10, 10, 1.0, 1.0, null, false);
        addSpecRow(spec, 2, 1, "B", "", "NORMAL", 10, 10, 1.0, 1.0, null, false);
        
        ExcelBalanceReader.BalanceData data = readData();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienSpec(data.alienSpecs()));
    }

    @Test
    @DisplayName("4. alienId 0 거절 및 5. alienId 음수 거절")
    void negativeOrZeroId() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 0, "A", "", "NORMAL", 10, 10, 1.0, 1.0, null, false);
        addSpecRow(spec, 2, -1, "B", "", "NORMAL", 10, 10, 1.0, 1.0, null, false);
        
        ExcelBalanceReader.BalanceData data = readData();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienSpec(data.alienSpecs()));
    }

    @Test
    @DisplayName("6. 빈 name 거절")
    void emptyName() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 1, "", "", "NORMAL", 10, 10, 1.0, 1.0, null, false);
        
        assertThrows(BalanceConversionException.class, this::readData);
    }

    @Test
    @DisplayName("7. 잘못된 grade 거절")
    void invalidGrade() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 1, "A", "", "INVALID_GRADE", 10, 10, 1.0, 1.0, null, false);
        
        ExcelBalanceReader.BalanceData data = readData();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienSpec(data.alienSpecs()));
    }

    @Test
    @DisplayName("8,9,10,11,12. 스탯 거절 검증")
    void invalidStats() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 1, "A", "", "NORMAL", -1, 10, 1.0, 1.0, null, false); // baseAttack < 0
        ExcelBalanceReader.BalanceData data1 = readData();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienSpec(List.of(data1.alienSpecs().get(0))));
        
        setUp();
        createBaseSheets();
        spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 2, "B", "", "NORMAL", 10, -1, 1.0, 1.0, null, false); // baseMp < 0
        ExcelBalanceReader.BalanceData data2 = readData();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienSpec(List.of(data2.alienSpecs().get(0))));

        setUp();
        createBaseSheets();
        spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 3, "C", "", "NORMAL", 10, 10, 0, 1.0, null, false); // attackSpeed 0
        ExcelBalanceReader.BalanceData data3 = readData();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienSpec(List.of(data3.alienSpecs().get(0))));

        setUp();
        createBaseSheets();
        spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 4, "D", "", "NORMAL", 10, 10, -1.0, 1.0, null, false); // attackSpeed 음수
        ExcelBalanceReader.BalanceData data4 = readData();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienSpec(List.of(data4.alienSpecs().get(0))));

        setUp();
        createBaseSheets();
        spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 5, "E", "", "NORMAL", 10, 10, 1.0, -1.0, null, false); // attackRange 음수
        ExcelBalanceReader.BalanceData data5 = readData();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienSpec(List.of(data5.alienSpecs().get(0))));
    }

    @Test
    @DisplayName("13. 정상 소수값 허용")
    void allowDecimals() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 1, "A", "", "NORMAL", 10, 10, 1.5, 3.5, null, false);
        
        ExcelBalanceReader.BalanceData data = readData();
        validator.validateAlienSpec(data.alienSpecs());
        assertThat(data.alienSpecs().get(0).attackSpeed()).isEqualTo(1.5);
        assertThat(data.alienSpecs().get(0).attackRange()).isEqualTo(3.5);
    }

    @Test
    @DisplayName("14. 문자열 숫자 거절 및 15. isLocked 문자열 true 거절")
    void stringNumericAndBoolean() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        addSpecRow(spec, 1, "1", "A", "", "NORMAL", 10, 10, 1.0, 1.0, null, "true"); // string ID and String locked
        
        assertThrows(BalanceConversionException.class, this::readData);
    }

    @Test
    @DisplayName("16. isLocked BOOLEAN 허용 및 17. description 빈 셀을 \"\"로 정규화")
    void validBooleanAndEmptyDescription() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 1, "A", null, "NORMAL", 10, 10, 1.0, 1.0, null, true); // boolean true
        
        ExcelBalanceReader.BalanceData data = readData();
        assertThat(data.alienSpecs().get(0).isLocked()).isTrue();
        assertThat(data.alienSpecs().get(0).description()).isEqualTo("");
    }

    @Test
    @DisplayName("18. 없는 evolutionTargetId 거절")
    void invalidTargetId() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 1, "A", "", "NORMAL", 10, 10, 1.0, 1.0, 99, false); // target 99 doesn't exist
        
        ExcelBalanceReader.BalanceData data = readData();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienSpec(data.alienSpecs()));
    }

    @Test
    @DisplayName("19. 자기 참조 거절 (1노드 사이클)")
    void selfReference() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 1, "A", "", "NORMAL", 10, 10, 1.0, 1.0, 1, false); 
        
        ExcelBalanceReader.BalanceData data = readData();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienSpec(data.alienSpecs()));
    }

    @Test
    @DisplayName("20. 2노드 cycle 거절")
    void twoNodeCycle() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 1, "A", "", "NORMAL", 10, 10, 1.0, 1.0, 2, false); 
        addSpecRow(spec, 2, 2, "B", "", "NORMAL", 10, 10, 1.0, 1.0, 1, false); 
        
        ExcelBalanceReader.BalanceData data = readData();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienSpec(data.alienSpecs()));
    }

    @Test
    @DisplayName("21. 3노드 cycle 거절")
    void threeNodeCycle() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 1, "A", "", "NORMAL", 10, 10, 1.0, 1.0, 2, false); 
        addSpecRow(spec, 2, 2, "B", "", "NORMAL", 10, 10, 1.0, 1.0, 3, false); 
        addSpecRow(spec, 3, 3, "C", "", "NORMAL", 10, 10, 1.0, 1.0, 1, false); 
        
        ExcelBalanceReader.BalanceData data = readData();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienSpec(data.alienSpecs()));
    }

    @Test
    @DisplayName("합류 구조(1->2, 3->2) 정상 통과")
    void joinStructure() throws IOException {
        createBaseSheets();
        Sheet spec = createAlienSpecSheet();
        addSpecRow(spec, 1, 1, "A", "", "NORMAL", 10, 10, 1.0, 1.0, 2, false); 
        addSpecRow(spec, 2, 2, "B", "", "NORMAL", 10, 10, 1.0, 1.0, null, false); 
        addSpecRow(spec, 3, 3, "C", "", "NORMAL", 10, 10, 1.0, 1.0, 2, false); 
        
        ExcelBalanceReader.BalanceData data = readData();
        validator.validateAlienSpec(data.alienSpecs()); // Should not throw
    }

    @Test
    @DisplayName("23. alien-spec.json 파일 누락 fail-fast")
    void missingSpecSheet() throws IOException {
        createBaseSheets(); // without AlienSpec
        saveAndClose();
        
        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        assertThrows(BalanceConversionException.class, reader::read);
    }
}
