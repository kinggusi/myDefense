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
        
        // 3. AlienUpgradeCost
        Sheet upgrade = workbook.createSheet("AlienUpgradeCost");
        Row h3 = upgrade.createRow(0);
        h3.createCell(0).setCellValue("currentLevel");
        h3.createCell(1).setCellValue("targetLevel");
        h3.createCell(2).setCellValue("requiredPieces");
        h3.createCell(3).setCellValue("requiredGold");
        h3.createCell(4).setCellValue("requiredGrowthCell");
        
        for (int i = 1; i < 50; i++) {
            Row r = upgrade.createRow(i);
            r.createCell(0).setCellValue(i);
            
            r.createCell(1).setCellValue(i + 1);
            r.createCell(2).setCellValue(i * 5);
            r.createCell(3).setCellValue(i * 100);
            r.createCell(4).setCellValue(i < 9 ? 0 : Math.min(50, ((i - 9) / 10 + 1) * 10));
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
        
        File dir = new File("../balance/source");
        if (!dir.exists()) dir.mkdirs();
        
        try (FileOutputStream fos = new FileOutputStream(new File(dir, "balance-data.xlsx"))) {
            workbook.write(fos);
        }
        workbook.close();
        System.out.println("Excel generated successfully.");
    }
}
