package com.denfense.server.balance;

import com.denfense.server.balance.tool.BalanceExcelConverter;
import com.denfense.server.balance.tool.ExcelBalanceReader;
import org.apache.poi.ss.usermodel.Row;
import org.apache.poi.ss.usermodel.Sheet;
import org.apache.poi.ss.usermodel.Cell;
import org.apache.poi.xssf.usermodel.XSSFWorkbook;
import org.junit.jupiter.api.Test;

import java.io.InputStream;
import java.io.OutputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThatThrownBy;

class MythicBreedingBalanceContractTest {

    @Test void recipeCount189FailsFast() throws Exception {
        Path workbook=mutate(wb->{Sheet sheet=wb.getSheet("MythicBreedingRecipe");sheet.removeRow(sheet.getRow(sheet.getLastRowNum()));});
        assertInvalid(read(workbook));
    }

    @Test void recipeCount191FailsFast() throws Exception {
        Path workbook=mutate(wb->{Sheet sheet=wb.getSheet("MythicBreedingRecipe");Row source=sheet.getRow(1), target=sheet.createRow(sheet.getLastRowNum()+1);for(int i=0;i<source.getLastCellNum();i++)copy(source.getCell(i),target.createCell(i));});
        assertInvalid(read(workbook));
    }

    @Test void parentOutsideResultMythicTwentyFailsFast() throws Exception {
        Path workbook=mutate(wb->wb.getSheet("MythicBreedingRecipe").getRow(1).getCell(2).setCellValue(1));
        assertInvalid(read(workbook));
    }

    @Test void recipeProbabilityChangeFailsFast() throws Exception {
        Path workbook=mutate(wb->wb.getSheet("MythicBreedingRecipe").getRow(1).getCell(10).setCellValue(191));
        assertInvalid(read(workbook));
    }

    @Test void breedingExclusiveInGachaFailsFast() throws Exception {
        Path workbook=mutate(wb->{Sheet sheet=wb.getSheet("GachaPool");for(Row row:sheet){if(row.getRowNum()>0&&"MYTHIC".equals(row.getCell(3).getStringCellValue())){row.getCell(5).setCellValue(row.getCell(5).getStringCellValue()+",47");break;}}});
        assertInvalid(read(workbook));
    }

    @Test void breedingExclusiveMissingFromMythicChoiceExclusionFailsFast() throws Exception {
        ExcelBalanceReader.BalanceData data=read(Path.of("..","balance","source","balance-data.xlsx"));
        GachaPoolBalanceDocument pools=new GachaPoolBalanceDocument(data.gachaPools());
        MythicChoiceBalanceDocument choices=new MythicChoiceBalanceDocument(data.mythicChoices(), List.of(47L));
        assertThatThrownBy(()->BalanceExcelConverter.validateBreedingBalance(data,pools,choices))
                .isInstanceOf(IllegalStateException.class);
    }

    @Test void duplicateMythicChoiceExclusionFailsFast() throws Exception {
        ExcelBalanceReader.BalanceData data=read(Path.of("..","balance","source","balance-data.xlsx"));
        GachaPoolBalanceDocument pools=new GachaPoolBalanceDocument(data.gachaPools());
        MythicChoiceBalanceDocument choices=new MythicChoiceBalanceDocument(data.mythicChoices(), List.of(47L,48L,48L));
        assertThatThrownBy(()->BalanceExcelConverter.validateBreedingBalance(data,pools,choices))
                .isInstanceOf(IllegalStateException.class);
    }

    private void assertInvalid(ExcelBalanceReader.BalanceData data) {
        GachaPoolBalanceDocument pools=new GachaPoolBalanceDocument(data.gachaPools());
        MythicChoiceBalanceDocument choices=new MythicChoiceBalanceDocument(data.mythicChoices(),List.of(47L,48L));
        assertThatThrownBy(()->BalanceExcelConverter.validateBreedingBalance(data,pools,choices))
                .isInstanceOf(IllegalStateException.class);
    }

    private ExcelBalanceReader.BalanceData read(Path path){return new ExcelBalanceReader(path.toAbsolutePath().toString()).read();}

    private Path mutate(WorkbookMutation mutation) throws Exception {
        Path target=Files.createTempFile("mythic-breeding-contract-",".xlsx");
        try(InputStream input=Files.newInputStream(Path.of("..","balance","source","balance-data.xlsx"));
            XSSFWorkbook workbook=new XSSFWorkbook(input)) {
            mutation.apply(workbook);
            try(OutputStream output=Files.newOutputStream(target)){workbook.write(output);}
        }
        return target;
    }

    private static void copy(Cell source, Cell target) {
        switch (source.getCellType()) {
            case STRING -> target.setCellValue(source.getStringCellValue());
            case NUMERIC -> target.setCellValue(source.getNumericCellValue());
            case BOOLEAN -> target.setCellValue(source.getBooleanCellValue());
            default -> target.setBlank();
        }
    }

    @FunctionalInterface private interface WorkbookMutation { void apply(XSSFWorkbook workbook) throws Exception; }
}
