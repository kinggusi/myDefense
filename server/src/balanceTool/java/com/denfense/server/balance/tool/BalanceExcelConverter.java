package com.denfense.server.balance.tool;

import com.denfense.server.service.balance.BalanceDataValidator;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Collections;

public class BalanceExcelConverter {

    public static void main(String[] args) {
        if (args.length < 6) {
            System.err.println("Usage: convertBalance <excelPath> <rewardPath> <upgradePath> <specPath> <shopPath> <poolPath>");
            System.exit(1);
        }

        String excelPath = args[0];
        Path rewardPath = Paths.get(args[1]);
        Path upgradePath = Paths.get(args[2]);
        Path specPath = Paths.get(args[3]);
        Path shopPath = Paths.get(args[4]);
        Path poolPath = Paths.get(args[5]);

        try {
            System.out.println("Starting Excel conversion...");
            System.out.println("Input: " + excelPath);
            
            // 1. Read Excel
            ExcelBalanceReader reader = new ExcelBalanceReader(excelPath);
            ExcelBalanceReader.BalanceData data = reader.read();

            // 2. Validate
            BalanceDataValidator validator = new BalanceDataValidator();
            validator.validateGameReward(data.gameReward());
            validator.validateAlienUpgrade(data.alienUpgrade());
            validator.validateAlienSpec(data.alienSpecs());
            com.denfense.server.balance.GachaPoolBalanceDocument poolDoc = new com.denfense.server.balance.GachaPoolBalanceDocument(data.gachaPools());
            validator.validateGachaPool(poolDoc, data.alienSpecs());
            com.denfense.server.balance.ShopProductBalanceDocument shopDoc = new com.denfense.server.balance.ShopProductBalanceDocument(data.shopProducts());
            validator.validateShopProduct(shopDoc, poolDoc);

            // 3. Write temp files
            BalanceJsonWriter writer = new BalanceJsonWriter();
            Path rewardTemp = writer.writeTempJson(rewardPath, data.gameReward());
            Path upgradeTemp = writer.writeTempJson(upgradePath, data.alienUpgrade());
            Path specTemp = writer.writeTempJson(specPath, data.alienSpecs());
            Path shopTemp = writer.writeTempJson(shopPath, shopDoc);
            Path poolTemp = writer.writeTempJson(poolPath, poolDoc);

            // 4. Atomic move
            try {
                writer.replaceFile(rewardTemp, rewardPath);
                writer.replaceFile(upgradeTemp, upgradePath);
                writer.replaceFile(specTemp, specPath);
                writer.replaceFile(shopTemp, shopPath);
                writer.replaceFile(poolTemp, poolPath);
            } catch (Exception e) {
                System.err.println("WARNING: 부분 갱신 실패 가능성 존재. 파일을 수동으로 확인하십시오.");
                throw e;
            }

            System.out.println("Conversion successful.");
            System.out.println("Reward JSON: " + rewardPath.toAbsolutePath());
            System.out.println("Upgrade JSON: " + upgradePath.toAbsolutePath());
            System.out.println("Spec JSON: " + specPath.toAbsolutePath());
            System.out.println("Shop JSON: " + shopPath.toAbsolutePath());
            System.out.println("Pool JSON: " + poolPath.toAbsolutePath());

        } catch (Exception e) {
            System.err.println("Conversion failed:");
            e.printStackTrace();
            System.exit(1);
        }
    }
}
