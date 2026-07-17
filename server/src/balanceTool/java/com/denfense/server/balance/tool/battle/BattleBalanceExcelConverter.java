package com.denfense.server.balance.tool.battle;

import java.nio.file.Path;

public final class BattleBalanceExcelConverter {
    private BattleBalanceExcelConverter() {}

    public static void main(String[] args) {
        if (args.length != 2)
            throw new IllegalArgumentException("Usage: convertBattleBalance <battleExcelPath> <unityBattleResourcesDirectory>");
        convert(Path.of(args[0]), Path.of(args[1]));
    }

    public static BattleBalanceJsonWriter.WriteResult convert(Path excelPath, Path outputDirectory) {
        BattleBalanceData.Data data = new BattleExcelReader(excelPath).read();
        new BattleBalanceValidator().validate(data);
        BattleBalanceJsonWriter.WriteResult result = new BattleBalanceJsonWriter().writeAll(data, outputDirectory);
        System.out.println("Battle balance conversion successful.");
        System.out.println("Input: " + excelPath.toAbsolutePath());
        System.out.println("Output: " + outputDirectory.toAbsolutePath());
        System.out.println("Bundle hash: " + result.bundleHash());
        return result;
    }
}
