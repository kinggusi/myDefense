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
