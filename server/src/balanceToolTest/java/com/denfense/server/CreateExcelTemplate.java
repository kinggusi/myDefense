package com.denfense.server;

import org.apache.poi.ss.usermodel.*;
import org.apache.poi.xssf.usermodel.XSSFWorkbook;

import java.io.File;
import java.io.FileOutputStream;

public class CreateExcelTemplate {
    public static void main(String[] args) throws Exception {
        Workbook workbook = new XSSFWorkbook();
        
        // 1. GameReward
        Sheet reward = workbook.createSheet("GameReward");
        Row h2 = reward.createRow(0);
        h2.createCell(0).setCellValue("baseRewardGold");
        h2.createCell(1).setCellValue("goldPerWave");
        h2.createCell(2).setCellValue("maxRewardGold");
        Row r2 = reward.createRow(1);
        r2.createCell(0).setCellValue(100);
        r2.createCell(1).setCellValue(10);
        r2.createCell(2).setCellValue(1000);

        Sheet battleReward = workbook.createSheet("BattleReward");
        String[] battleRewardHeaders = {"rewardType", "mapId", "wave", "gold", "universalPiece", "diamond",
                "failureRewardBaseGold", "failureRewardCapPercent", "minimumRewardWave", "enabled"};
        Row battleHeader = battleReward.createRow(0);
        for (int i = 0; i < battleRewardHeaders.length; i++) battleHeader.createCell(i).setCellValue(battleRewardHeaders[i]);
        Object[][] battleRows = {
                {"CONFIG", "ALL", 0, 0, 0, 0, 10000, 80, 10, true},
                {"CHECKPOINT", "ALL", 10, 500, 10, 0, 0, 0, 0, true},
                {"CHECKPOINT", "ALL", 20, 750, 15, 0, 0, 0, 0, true},
                {"CHECKPOINT", "ALL", 30, 1000, 20, 0, 0, 0, 0, true},
                {"CHECKPOINT", "ALL", 40, 1500, 25, 0, 0, 0, 0, true},
                {"CHECKPOINT", "ALL", 50, 2000, 30, 0, 0, 0, 0, true},
                {"CHECKPOINT", "ALL", 60, 2500, 35, 0, 0, 0, 0, true},
                {"CHECKPOINT", "ALL", 70, 3000, 40, 0, 0, 0, 0, true},
                {"CHECKPOINT", "ALL", 80, 4000, 50, 0, 0, 0, 0, true},
        };
        for (int rowIndex = 0; rowIndex < battleRows.length; rowIndex++) {
            Row row = battleReward.createRow(rowIndex + 1);
            for (int col = 0; col < battleRows[rowIndex].length; col++) {
                Object value = battleRows[rowIndex][col];
                if (value instanceof Number n) row.createCell(col).setCellValue(n.doubleValue());
                else if (value instanceof Boolean b) row.createCell(col).setCellValue(b);
                else row.createCell(col).setCellValue(String.valueOf(value));
            }
        }
        
        // 2. AlienUpgradeCost
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
