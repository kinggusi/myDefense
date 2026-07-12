package com.denfense.server.balance.tool;

import com.denfense.server.service.balance.BalanceDataValidator;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Collections;

public class BalanceExcelConverter {

    public static void main(String[] args) {
        if (args.length < 3) {
            System.err.println("Usage: convertBalance <excelPath> <rewardPath> <upgradePath>");
            System.exit(1);
        }

        String excelPath = args[0];
        Path rewardPath = Paths.get(args[1]);
        Path upgradePath = Paths.get(args[2]);

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

            // 3. Write temp files
            BalanceJsonWriter writer = new BalanceJsonWriter();
            Path rewardTemp = writer.writeTempJson(rewardPath, data.gameReward());
            Path upgradeTemp = writer.writeTempJson(upgradePath, data.alienUpgrade());

            // 4. Atomic move
            // 참고: 첫 번째 파일 교체 후 두 번째 파일 교체 실패 시 부분 갱신 위험이 존재합니다.
            // MVP에서는 두 파일을 먼저 임시 생성 및 검증한 뒤 순차 교체합니다.
            try {
                writer.replaceFile(rewardTemp, rewardPath);
                writer.replaceFile(upgradeTemp, upgradePath);
            } catch (Exception e) {
                // 백업 복구는 MVP 범위를 넘어서므로 로깅만 수행
                System.err.println("WARNING: 부분 갱신 실패 가능성 존재. 파일을 수동으로 확인하십시오.");
                throw e;
            }

            System.out.println("Conversion successful.");
            System.out.println("Reward JSON: " + rewardPath.toAbsolutePath());
            System.out.println("Upgrade JSON: " + upgradePath.toAbsolutePath());

        } catch (Exception e) {
            System.err.println("Conversion failed:");
            e.printStackTrace();
            System.exit(1);
        }
    }
}
