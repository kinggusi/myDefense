package com.denfense.server.balance.tool;

import com.denfense.server.balance.*;
import org.apache.poi.ss.usermodel.*;
import java.nio.file.*;
import java.util.*;

/** Strict authoring reader: numeric cells must be integers; no silent coercion or formulas. */
public final class ShopExcelReader {
    public ShopCatalogBalance read(String path) {
        try (var in = Files.newInputStream(Path.of(path)); var book = WorkbookFactory.create(in)) {
            var offers = rows(book, "ShopOffer", List.of("offerId","category","currencyType","price","rewardType",
                    "rewardAmount","grade","gachaProductId","limitPeriod","limitCount","limitGroup","active","displayOrder"));
            var policy = rows(book, "ShopPolicy", List.of("policyId","resetZone","luckyWeight","luckyTotalWeight",
                    "dailyAdRerolls","discountOfferId","discountPrice","weeklyDiscountCount","adChallengeTtlSeconds"));
            if (policy.size() != 1) throw new IllegalStateException("ShopPolicy requires one row.");
            var p = policy.get(0);
            return new ShopCatalogBalance(offers.stream().map(r -> new ShopOfferBalance(s(r,0),s(r,1),s(r,2),n(r,3),s(r,4),
                    n(r,5),s(r,6),s(r,7),s(r,8),n(r,9),s(r,10),b(r,11),n(r,12))).toList(),
                    new ShopPolicyBalance(s(p,0),s(p,1),n(p,2),n(p,3),n(p,4),s(p,5),n(p,6),n(p,7),n(p,8)));
        } catch (Exception e) { throw new BalanceConversionException("Invalid ShopOffer/ShopPolicy workbook.", e); }
    }
    private List<Row> rows(Workbook book, String name, List<String> headers) {
        var sheet = book.getSheet(name);
        if (sheet == null || sheet.getNumMergedRegions() != 0 || sheet.getRow(0) == null
                || sheet.getRow(0).getLastCellNum() != headers.size()) throw new IllegalStateException(name + " headers missing.");
        for (int i=0; i<headers.size(); i++) if (!headers.get(i).equals(s(sheet.getRow(0), i))) throw new IllegalStateException(name + " header mismatch.");
        var result = new ArrayList<Row>();
        for (int i=1; i<=sheet.getLastRowNum(); i++) {
            Row r=sheet.getRow(i); if(r==null) continue;
            boolean any=false;
            for(Cell c:r) if(c.getCellType()!=CellType.BLANK && !(c.getCellType()==CellType.STRING && c.getStringCellValue().isBlank())) any=true;
            if(any) { if(r.getLastCellNum()>headers.size()) throw new IllegalStateException("Unexpected columns."); result.add(r); }
        }
        return result;
    }
    private String s(Row r,int i) { Cell c=r.getCell(i); if(c==null||c.getCellType()==CellType.BLANK)return "";
        if(c.getCellType()!=CellType.STRING)throw new IllegalStateException("Text required."); return c.getStringCellValue().trim(); }
    private int n(Row r,int i) { Cell c=r.getCell(i); if(c==null||c.getCellType()!=CellType.NUMERIC)throw new IllegalStateException("Integer required.");
        double v=c.getNumericCellValue(); if(!Double.isFinite(v)||v!=(int)v)throw new IllegalStateException("Integer out of range."); return (int)v; }
    private boolean b(Row r,int i) { Cell c=r.getCell(i); if(c!=null&&c.getCellType()==CellType.BOOLEAN)return c.getBooleanCellValue();
        String v=s(r,i); if(!Set.of("true","false").contains(v))throw new IllegalStateException("Boolean required."); return Boolean.parseBoolean(v); }
}
