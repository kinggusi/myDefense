package com.denfense.server.balance;

import org.apache.poi.ss.usermodel.Row;
import org.apache.poi.ss.usermodel.Sheet;
import org.apache.poi.ss.usermodel.Workbook;

final class BattleBalanceTestWorkbook {
    private BattleBalanceTestWorkbook() {
    }

    static void addValidSheets(Workbook workbook) {
        addRows(workbook, "MonsterSpec",
                new String[]{"monsterId", "name", "monsterType", "baseHp", "moveSpeed", "killGold", "enabled"},
                new Object[][]{
                        {"NORMAL_MONSTER", "Normal", "NORMAL", 30, 5, 20, true},
                        {"ELITE_MONSTER", "Elite", "ELITE", 60, 4, 40, true},
                        {"WAVE_BOSS", "Boss", "WAVE_BOSS", 300, 2, 200, true}
                });
        addRows(workbook, "WaveSpec",
                new String[]{"modeId", "wave", "hpMultiplier", "interWaveDelaySeconds", "isBossWave", "bossTimeLimitSeconds", "spawnGroupId", "enabled"},
                new Object[][]{
                        {"COOP_STANDARD", 1, 1.0, 3, false, 0, "WAVE_01", true},
                        {"COOP_STANDARD", 2, 1.1, 3, true, 30, "WAVE_02_BOSS", true}
                });
        addRows(workbook, "WaveSpawn",
                new String[]{"spawnGroupId", "order", "monsterId", "spawnCountPerField", "startDelaySeconds", "spawnIntervalSeconds", "lanePolicy"},
                new Object[][]{
                        {"WAVE_01", 1, "NORMAL_MONSTER", 10, 0, 1, "EACH_FIELD"},
                        {"WAVE_02_BOSS", 1, "WAVE_BOSS", 1, 0, 1, "BOSS_SHARED"}
                });
        addRows(workbook, "FieldLimitBalance",
                new String[]{"modeId", "playerCount", "maxAliveMonsterCountPerField", "warningThreshold", "dangerThreshold"},
                new Object[][]{{"COOP_STANDARD", 2, 100, 80, 90}});
        addRows(workbook, "SummonBalance",
                new String[]{"modeId", "summonType", "baseCost", "costIncreasePerUse", "maxUses", "resultPoolId", "enabled"},
                new Object[][]{{"COOP_STANDARD", "KIDNAP", 50, 10, -1, "STANDARD_SUMMON_POOL", true}});
        addRows(workbook, "SummonPool",
                new String[]{"poolId", "poolName", "poolActive", "grade", "weight", "alienIds"},
                new Object[][]{{"STANDARD_SUMMON_POOL", "Battle Kidnap NORMAL", true, "NORMAL", 10000, "1"}});
        addRows(workbook, "MergeRule",
                new String[]{"sourceGrade", "requiredCount", "sameSpeciesRequired", "resultType", "resultGrade", "enabled"},
                new Object[][]{
                        {"NORMAL", 2, true, "RANDOM_NEXT_GRADE", "EPIC", true},
                        {"EPIC", 2, true, "RANDOM_NEXT_GRADE", "UNIQUE", true},
                        {"UNIQUE", 2, true, "RANDOM_NEXT_GRADE", "LEGEND", true},
                        {"LEGEND", 2, true, "MYTHIC_CHOICE", "MYTHIC", true},
                        {"MYTHIC", 2, true, "DISABLED", "MYTHIC", true}
                });
        addRows(workbook, "MythicChoiceBalance",
                new String[]{"modeId", "candidateCount", "freeRerollCount", "paidRerollLimit", "paidRerollCost", "excludePreviousCandidates", "selectionTimeoutSeconds", "autoSelectPolicy", "battleContinuesDuringSelection", "enabled"},
                new Object[][]{{"COOP_STANDARD", 3, 1, 1, 100, true, 8, "FIRST", true, true}});
        addRows(workbook, "MutationSpec",
                new String[]{"mutationType", "enabled", "injectorEnabled", "randomActivationEnabled", "weight", "attackMultiplier", "mpMultiplier", "attackSpeedMultiplier", "rangeMultiplier", "goldMultiplier"},
                new Object[][]{
                        {"BERSERK", true, true, true, 1, 1.25, 1.0, 1.0, 1.0, 1.0},
                        {"GREEDY", true, true, true, 1, 1.0, 1.0, 1.0, 1.0, 1.25},
                        {"SWIFT", true, true, true, 1, 1.0, 1.0, 1.25, 1.0, 1.0},
                        {"GIANT", true, true, true, 1, 1.35, 1.1, 0.9, 1.1, 1.0},
                        {"OBESE", true, true, true, 1, 0.8, 1.2, 0.8, 1.15, 1.0},
                        {"TOXIC", true, true, true, 1, 1.1, 1.0, 1.0, 1.0, 1.0},
                        {"FROZEN", true, true, true, 1, 1.0, 1.1, 0.85, 1.0, 1.0},
                        {"BLANK", true, false, true, 1, 1.0, 1.0, 1.0, 1.0, 1.0}
                });
        addRows(workbook, "MutationConfig",
                new String[]{"modeId", "initialActivationCost", "rerollCost1", "rerollCost2", "rerollCost3", "rerollCost4", "rerollCostAfterMax", "injectorReplaceCost"},
                new Object[][]{{"COOP_STANDARD", 300, 600, 1200, 2400, 4800, 4800, 0}});
        addRows(workbook, "InjectorPool",
                new String[]{"poolId", "poolName", "poolActive", "mutationType", "weight", "resultType"},
                new Object[][]{
                        {"BATTLE_INJECTOR_POOL", "Battle Kidnap Injector", true, "BERSERK", 1, "MUTATION_INJECTOR"},
                        {"BATTLE_INJECTOR_POOL", "Battle Kidnap Injector", true, "GREEDY", 1, "MUTATION_INJECTOR"},
                        {"BATTLE_INJECTOR_POOL", "Battle Kidnap Injector", true, "SWIFT", 1, "MUTATION_INJECTOR"},
                        {"BATTLE_INJECTOR_POOL", "Battle Kidnap Injector", true, "GIANT", 1, "MUTATION_INJECTOR"},
                        {"BATTLE_INJECTOR_POOL", "Battle Kidnap Injector", true, "OBESE", 1, "MUTATION_INJECTOR"},
                        {"BATTLE_INJECTOR_POOL", "Battle Kidnap Injector", true, "TOXIC", 1, "MUTATION_INJECTOR"},
                        {"BATTLE_INJECTOR_POOL", "Battle Kidnap Injector", true, "FROZEN", 1, "MUTATION_INJECTOR"}
                });
        if (workbook.getSheet("BattleReward") == null) {
            addRows(workbook, "BattleReward",
                    new String[]{"rewardType", "mapId", "wave", "gold", "universalPiece", "diamond", "failureRewardBaseGold", "failureRewardCapPercent", "minimumRewardWave", "enabled"},
                    new Object[][]{
                            {"CONFIG", "ALL", 0, 0, 0, 0, 10000, 80, 10, true},
                            {"CHECKPOINT", "ALL", 10, 500, 10, 0, 0, 0, 0, true},
                            {"CHECKPOINT", "ALL", 20, 750, 15, 0, 0, 0, 0, true},
                            {"CHECKPOINT", "ALL", 30, 1000, 20, 0, 0, 0, 0, true},
                            {"CHECKPOINT", "ALL", 40, 1500, 25, 0, 0, 0, 0, true},
                            {"CHECKPOINT", "ALL", 50, 2000, 30, 0, 0, 0, 0, true},
                            {"CHECKPOINT", "ALL", 60, 2500, 35, 0, 0, 0, 0, true},
                            {"CHECKPOINT", "ALL", 70, 3000, 40, 0, 0, 0, 0, true},
                            {"CHECKPOINT", "ALL", 80, 4000, 50, 0, 0, 0, 0, true},
                            {"MAP_FIRST_CLEAR", "TEST_MAP", 80, 0, 0, 3000, 0, 0, 0, true}
                    });
        }
    }

    private static void addRows(Workbook workbook, String name, String[] headers, Object[][] values) {
        Sheet sheet = workbook.createSheet(name);
        Row header = sheet.createRow(0);
        for (int column = 0; column < headers.length; column++) header.createCell(column).setCellValue(headers[column]);
        for (int rowIndex = 0; rowIndex < values.length; rowIndex++) {
            Row row = sheet.createRow(rowIndex + 1);
            for (int column = 0; column < values[rowIndex].length; column++) {
                Object value = values[rowIndex][column];
                if (value instanceof String string) row.createCell(column).setCellValue(string);
                else if (value instanceof Boolean bool) row.createCell(column).setCellValue(bool);
                else if (value instanceof Number number) row.createCell(column).setCellValue(number.doubleValue());
            }
        }
    }
}
