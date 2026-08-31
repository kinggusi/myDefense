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
        addWaveSheets(workbook);
        addRows(workbook, "PlanetBattle",
                new String[]{"mapId", "order", "hpMultiplier", "speedMultiplier", "bossHpMultiplier", "enabled"},
                new Object[][]{
                        {"NEPTUNE", 1, 1.0, 1.0, 3.0, true},
                        {"URANUS", 2, 1.35, 1.03, 3.0, true},
                        {"SATURN", 3, 1.8, 1.06, 3.0, true},
                        {"JUPITER", 4, 2.4, 1.09, 3.0, true},
                        {"MARS", 5, 3.2, 1.12, 3.0, true},
                        {"EARTH", 6, 4.3, 1.15, 3.0, true},
                        {"VENUS", 7, 5.8, 1.18, 3.0, true},
                        {"MERCURY", 8, 7.8, 1.21, 3.0, true},
                        {"SUN", 9, 11.0, 1.25, 3.0, true}
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
                new String[]{"mutationType", "enabled", "injectorEnabled", "randomActivationEnabled", "weight", "attackMultiplier", "mpMultiplier", "attackSpeedMultiplier", "rangeMultiplier", "goldMultiplier", "mechanic", "splashRadius", "splashDamageMultiplier", "bossDamageMultiplier", "dotDamageMultiplier", "dotTickCount", "dotTickIntervalSeconds", "slowMultiplier", "slowDurationSeconds", "goldPerHit", "gambleSuccessChance", "gambleSuccessMultiplier", "gambleFailureMultiplier"},
                new Object[][]{
                        {"BERSERK", true, true, true, 1, 1.25, 1.0, 1.0, 1.0, 1.0, "BOSS_SINGLE", 0, 0, 2.0, 0, 0, 0, 1.0, 0, 0, 0, 1.0, 1.0},
                        {"GREEDY", true, true, true, 1, 1.0, 1.0, 1.0, 1.0, 1.25, "ECONOMY", 0, 0, 1.0, 0, 0, 0, 1.0, 0, 2, 0, 1.0, 1.0},
                        {"SWIFT", true, true, true, 1, 1.0, 1.0, 1.25, 1.0, 1.0, "ATTACK_SPEED", 0, 0, 1.0, 0, 0, 0, 1.0, 0, 0, 0, 1.0, 1.0},
                        {"GIANT", true, true, true, 1, 1.35, 1.1, 0.9, 1.1, 1.0, "SPLASH", 2.5, 0.65, 1.0, 0, 0, 0, 1.0, 0, 0, 0, 1.0, 1.0},
                        {"OBESE", true, true, true, 1, 0.8, 1.2, 0.8, 1.15, 1.0, "GAMBLE", 0, 0, 1.0, 0, 0, 0, 1.0, 0, 0, 0.25, 2.5, 0.5},
                        {"TOXIC", true, true, true, 1, 1.1, 1.0, 1.0, 1.0, 1.0, "DOT", 0, 0, 1.0, 0.2, 3, 1.0, 1.0, 0, 0, 0, 1.0, 1.0},
                        {"FROZEN", true, true, true, 1, 1.0, 1.1, 0.85, 1.0, 1.0, "SLOW", 0, 0, 1.0, 0, 0, 0, 0.7, 2.0, 0, 0, 1.0, 1.0},
                        {"BLANK", true, false, true, 1, 1.0, 1.0, 1.0, 1.0, 1.0, "NONE", 0, 0, 1.0, 0, 0, 0, 1.0, 0, 0, 0, 1.0, 1.0}
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
        addRows(workbook, "ResonanceBalance",
                new String[]{"track", "level", "requiredGold", "attackMultiplier", "attackSpeedMultiplier", "rangeMultiplier", "enabled"},
                new Object[][]{
                        {"NORMAL", 1, 400, 1.05, 1.01, 1.0, true},
                        {"NORMAL", 2, 800, 1.10, 1.02, 1.0, true},
                        {"NORMAL", 3, 1400, 1.15, 1.03, 1.0, true},
                        {"NORMAL", 4, 2200, 1.20, 1.04, 1.0, true},
                        {"NORMAL", 5, 3200, 1.25, 1.05, 1.0, true},
                        {"MYTHIC", 1, 800, 1.08, 1.01, 1.0, true},
                        {"MYTHIC", 2, 1600, 1.16, 1.02, 1.0, true},
                        {"MYTHIC", 3, 2800, 1.24, 1.03, 1.0, true},
                        {"MYTHIC", 4, 4400, 1.32, 1.04, 1.0, true},
                        {"MYTHIC", 5, 6500, 1.40, 1.05, 1.0, true}
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
        addBreedingSheets(workbook);
    }

    private static void addBreedingSheets(Workbook workbook) {
        if (workbook.getSheet("MythicBreedingConfig") != null) return;
        addRows(workbook, "MythicBreedingConfig",
                new String[]{"durationSeconds", "slotCount", "slot2UnlockLevel", "slot2GemPrice", "slot3GemPrice",
                        "duplicateRewardPieces", "accelerationUnitSeconds", "accelerationUnitDiamondCost", "enabled"},
                new Object[][]{{86400, 3, 30, 5000, 10000, 30, 600, 100, true}});
        Object[][] results = new Object[20][];
        for (int index = 0; index < 20; index++) {
            int mythicNo = index + 1;
            results[index] = new Object[]{mythicNo, 29 + index,
                    mythicNo <= 18 ? "STANDARD" : "BREEDING_EXCLUSIVE", mythicNo <= 18 ? 0 : 20, true};
        }
        addRows(workbook, "MythicBreedingResult",
                new String[]{"mythicNo", "alienId", "acquisitionType", "globalWeight", "enabled"}, results);
        java.util.List<Object[]> recipes = new java.util.ArrayList<>();
        for (int a = 29; a <= 48; a++) {
            for (int b = a + 1; b <= 48; b++) {
                recipes.add(new Object[]{"M" + a + "_M" + b, a - 28, a, b - 28, b,
                        29, 30, 31, 32, 33, 192, 20, 20, true});
            }
        }
        addRows(workbook, "MythicBreedingRecipe",
                new String[]{"recipeKey", "parentMythicNoA", "parentAlienIdA", "parentMythicNoB", "parentAlienIdB",
                        "candidate1AlienId", "candidate2AlienId", "candidate3AlienId", "candidate4AlienId", "candidate5AlienId",
                        "standardWeightEach", "exclusive19Weight", "exclusive20Weight", "enabled"},
                recipes.toArray(Object[][]::new));
    }

    private static void addWaveSheets(Workbook workbook) {
        Object[][] waves = new Object[80][];
        java.util.List<Object[]> spawns = new java.util.ArrayList<>();
        for (int wave = 1; wave <= 80; wave++) {
            boolean boss = wave % 10 == 0;
            String group = boss ? String.format("WAVE_%02d_BOSS", wave) : String.format("WAVE_%02d", wave);
            waves[wave - 1] = new Object[]{"COOP_STANDARD", wave, 1.0 + 0.1 * (wave - 1), 3, boss, boss ? 30 : 0, group, true};
            if (boss) {
                spawns.add(new Object[]{group, 1, "WAVE_BOSS", 1, 0, 1, "BOSS_SHARED"});
                continue;
            }
            int band = (wave - 1) / 10;
            int position = (wave - 1) % 10;
            int minimum = 12 + band * 6;
            int maximum = band == 7 ? 60 : minimum + 4;
            int total = Math.round(minimum + (maximum - minimum) * position / 8.0f);
            int elite = Math.round(total * (band * 0.05f));
            int order = 1;
            if (total - elite > 0)
                spawns.add(new Object[]{group, order++, "NORMAL_MONSTER", total - elite, 0, 1, "EACH_FIELD"});
            if (elite > 0)
                spawns.add(new Object[]{group, order, "ELITE_MONSTER", elite, 0, 1, "EACH_FIELD"});
        }
        addRows(workbook, "WaveSpec",
                new String[]{"modeId", "wave", "hpMultiplier", "interWaveDelaySeconds", "isBossWave", "bossTimeLimitSeconds", "spawnGroupId", "enabled"},
                waves);
        addRows(workbook, "WaveSpawn",
                new String[]{"spawnGroupId", "order", "monsterId", "spawnCountPerField", "startDelaySeconds", "spawnIntervalSeconds", "lanePolicy"},
                spawns.toArray(Object[][]::new));
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
