package com.denfense.server;

import org.apache.poi.ss.usermodel.*;
import org.apache.poi.xssf.usermodel.XSSFWorkbook;

import java.io.File;
import java.io.FileOutputStream;

public class CreateExcelTemplate {
    public static void main(String[] args) throws Exception {
        Workbook workbook = new XSSFWorkbook();
        
        // 1. Config
        Sheet config = workbook.createSheet("Config");
        Row h1 = config.createRow(0);
        h1.createCell(0).setCellValue("key");
        h1.createCell(1).setCellValue("value");
        Row r1 = config.createRow(1);
        r1.createCell(0).setCellValue("maxLevel");
        r1.createCell(1).setCellValue(50);
        
        // 2. GameReward
        Sheet reward = workbook.createSheet("GameReward");
        Row h2 = reward.createRow(0);
        h2.createCell(0).setCellValue("baseRewardGold");
        h2.createCell(1).setCellValue("goldPerWave");
        h2.createCell(2).setCellValue("maxRewardGold");
        Row r2 = reward.createRow(1);
        r2.createCell(0).setCellValue(100);
        r2.createCell(1).setCellValue(10);
        r2.createCell(2).setCellValue(1000);
        
        // 3. AlienUpgrade
        Sheet upgrade = workbook.createSheet("AlienUpgrade");
        Row h3 = upgrade.createRow(0);
        h3.createCell(0).setCellValue("currentLevel");
        h3.createCell(1).setCellValue("requiredPieces");
        h3.createCell(2).setCellValue("requiredGold");
        h3.createCell(3).setCellValue("requiredGrowthCell");
        
        for (int i = 1; i < 50; i++) {
            Row r = upgrade.createRow(i);
            r.createCell(0).setCellValue(i);
            
            // From UpgradeCostPolicy (or json)
            // 1~9: pieces=5, gold=i*10
            // 10~19: pieces=10, gold=i*15
            // 20~29: pieces=15, gold=i*20, cell=1
            // 30~39: pieces=20, gold=i*30, cell=2
            // 40~49: pieces=30, gold=i*40, cell=3
            
            int p, g, c;
            if (i < 10) { p=5; g=i*10; c=0; }
            else if (i < 20) { p=10; g=i*15; c=0; }
            else if (i < 30) { p=15; g=i*20; c=1; }
            else if (i < 40) { p=20; g=i*30; c=2; }
            else { p=30; g=i*40; c=3; }
            
            r.createCell(1).setCellValue(p);
            r.createCell(2).setCellValue(g);
            r.createCell(3).setCellValue(c);
        }
        
        File dir = new File("../balance/source");
        if (!dir.exists()) dir.mkdirs();
        
        try (FileOutputStream fos = new FileOutputStream(new File(dir, "balance-data.xlsx"))) {
            workbook.write(fos);
        }
        workbook.close();
        System.out.println("Excel generated successfully.");
    }
}
