package com.denfense.server.balance;

import com.denfense.server.balance.tool.BalanceConversionException;
import com.denfense.server.balance.tool.ExcelBalanceReader;
import com.denfense.server.service.balance.AlienUpgradeBalanceFile;
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

        Sheet upgrade = workbook.createSheet("AlienUpgrade");
        Row headerUpgrade = upgrade.createRow(0);
        headerUpgrade.createCell(0).setCellValue("currentLevel");
        headerUpgrade.createCell(1).setCellValue("requiredPieces");
        headerUpgrade.createCell(2).setCellValue("requiredGold");
        headerUpgrade.createCell(3).setCellValue("requiredGrowthCell");

        Row rowUpgrade1 = upgrade.createRow(1);
        rowUpgrade1.createCell(0).setCellValue(1);
        rowUpgrade1.createCell(1).setCellValue(5);
        rowUpgrade1.createCell(2).setCellValue(100);
        rowUpgrade1.createCell(3).setCellValue(0);

        Row rowUpgrade2 = upgrade.createRow(2);
        rowUpgrade2.createCell(0).setCellValue(2);
        rowUpgrade2.createCell(1).setCellValue(10);
        rowUpgrade2.createCell(2).setCellValue(200);
        rowUpgrade2.createCell(3).setCellValue(0);
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
        validator.validateAlienUpgrade(data.alienUpgrade());

        assertThat(data.gameReward().baseRewardGold()).isEqualTo(100);
        assertThat(data.alienUpgrade().maxLevel()).isEqualTo(3);
        assertThat(data.alienUpgrade().costs()).hasSize(2);
    }

    @Test
    @DisplayName("4. AlienUpgrade 시트 없음")
    void missingSheet() throws IOException {
        createValidWorkbook();
        workbook.removeSheetAt(2); // Remove AlienUpgrade
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        BalanceConversionException ex = assertThrows(BalanceConversionException.class, reader::read);
        assertThat(ex).hasMessageContaining("필수 시트가 없습니다: AlienUpgrade");
    }

    @Test
    @DisplayName("10. 문자열 숫자 거부")
    void stringNumeric() throws IOException {
        createValidWorkbook();
        workbook.getSheet("Config").getRow(1).getCell(1).setCellValue("3"); // String instead of numeric
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
        workbook.getSheet("AlienUpgrade").getRow(1).setZeroHeight(true);
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        ExcelBalanceReader.BalanceData data = reader.read();
        assertThat(data.alienUpgrade().costs()).hasSize(2);
    }

    @Test
    @DisplayName("18. 병합 셀 거부")
    void mergedRegion() throws IOException {
        createValidWorkbook();
        workbook.getSheet("Config").addMergedRegion(new org.apache.poi.ss.util.CellRangeAddress(0, 0, 0, 1));
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        BalanceConversionException ex = assertThrows(BalanceConversionException.class, reader::read);
        assertThat(ex).hasMessageContaining("병합된 셀이 존재합니다");
    }

    @Test
    @DisplayName("22. 중복 레벨 거부 (Validator에서 수행)")
    void duplicateLevel() throws IOException {
        createValidWorkbook();
        workbook.getSheet("AlienUpgrade").getRow(2).getCell(0).setCellValue(1); // Dup level 1
        saveAndClose();

        ExcelBalanceReader reader = new ExcelBalanceReader(tempExcel.getAbsolutePath());
        ExcelBalanceReader.BalanceData data = reader.read();
        BalanceDataValidator validator = new BalanceDataValidator();
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgrade(data.alienUpgrade()));
    }
}
