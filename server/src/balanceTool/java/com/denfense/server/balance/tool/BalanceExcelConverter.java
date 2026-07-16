package com.denfense.server.balance.tool;

import com.denfense.server.balance.*;
import com.denfense.server.service.balance.BalanceDataValidator;
import com.denfense.server.service.balance.BalanceManifestSupport;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.LinkedHashMap;
import java.util.Map;

public class BalanceExcelConverter {

    private static final int ARGUMENT_COUNT = 14;

    public static void main(String[] args) {
        if (args.length < ARGUMENT_COUNT) {
            System.err.println("Usage: convertBalance <excelPath> <rewardPath> <upgradeCostPath> <levelStatPath> "
                    + "<specPath> <shopPath> <poolPath> <monsterPath> <wavePath> <waveSpawnPath> "
                    + "<fieldLimitPath> <summonPath> <mergeRulePath> <mythicChoicePath>");
            System.exit(1);
        }

        String excelPath = args[0];
        Path[] targets = new Path[ARGUMENT_COUNT - 1];
        for (int index = 0; index < targets.length; index++) {
            targets[index] = Paths.get(args[index + 1]);
        }

        Path generatedDirectory = requireSharedGeneratedDirectory(targets);
        Path stagingDirectory = null;
        try {
            ExcelBalanceReader.BalanceData data = new ExcelBalanceReader(excelPath).read();
            validate(data);

            stagingDirectory = Files.createTempDirectory(generatedDirectory.getParent(), "balance-generation-");
            BalanceJsonWriter writer = new BalanceJsonWriter();
            Map<Path, Object> stagedDocuments = documents(stagingDirectory, data);
            for (Map.Entry<Path, Object> entry : stagedDocuments.entrySet()) {
                Path temp = writer.writeTempJson(entry.getKey(), entry.getValue());
                writer.replaceFile(temp, entry.getKey());
            }

            new BalanceManifestGenerator().generate(stagingDirectory);

            Map<Path, Path> stagedToTarget = new LinkedHashMap<>();
            for (Path target : targets) {
                stagedToTarget.put(stagingDirectory.resolve(target.getFileName()), target);
            }
            stagedToTarget.put(
                    stagingDirectory.resolve(BalanceManifestSupport.MANIFEST_FILE_NAME),
                    generatedDirectory.resolve(BalanceManifestSupport.MANIFEST_FILE_NAME));
            writer.replaceFilesAtomically(stagedToTarget);

            System.out.println("Conversion successful. Generated files: " + stagedToTarget.size());
            System.out.println("Manifest JSON: " + generatedDirectory.resolve(BalanceManifestSupport.MANIFEST_FILE_NAME).toAbsolutePath());
        } catch (Exception e) {
            System.err.println("Conversion failed:");
            e.printStackTrace();
            System.exit(1);
        } finally {
            deleteDirectoryQuietly(stagingDirectory);
        }
    }

    public static void validate(ExcelBalanceReader.BalanceData data) {
        BalanceDataValidator validator = new BalanceDataValidator();
        validator.validateGameReward(data.gameReward());
        validator.validateAlienLevelStats(data.alienLevelStats());
        int maxLevel = data.alienLevelStats().stream()
                .mapToInt(com.denfense.server.service.balance.AlienLevelStatBalance::level)
                .max()
                .orElseThrow();
        validator.validateAlienUpgradeCosts(data.alienUpgradeCosts(), maxLevel);
        validator.validateAlienSpec(data.alienSpecs());

        GachaPoolBalanceDocument poolDocument = new GachaPoolBalanceDocument(data.gachaPools());
        ShopProductBalanceDocument productDocument = new ShopProductBalanceDocument(data.shopProducts());
        validator.validateGachaPool(poolDocument, data.alienSpecs());
        validator.validateShopProduct(productDocument, poolDocument);
        validator.validateBattleBalance(
                new MonsterSpecBalanceDocument(data.monsters()),
                new WaveSpecBalanceDocument(data.waves()),
                new WaveSpawnBalanceDocument(data.waveSpawns()),
                new FieldLimitBalanceDocument(data.fieldLimits()),
                new SummonBalanceDocument(data.summons()),
                new MergeRuleBalanceDocument(data.mergeRules()),
                new MythicChoiceBalanceDocument(data.mythicChoices()),
                data.alienSpecs());
    }

    private static Map<Path, Object> documents(Path directory, ExcelBalanceReader.BalanceData data) {
        Map<Path, Object> documents = new LinkedHashMap<>();
        documents.put(directory.resolve("game-reward.json"), data.gameReward());
        documents.put(directory.resolve("alien-upgrade-cost.json"), data.alienUpgradeCosts());
        documents.put(directory.resolve("alien-level-stat.json"), data.alienLevelStats());
        documents.put(directory.resolve("alien-spec.json"), data.alienSpecs());
        documents.put(directory.resolve("shop-products.json"), new ShopProductBalanceDocument(data.shopProducts()));
        documents.put(directory.resolve("gacha-pools.json"), new GachaPoolBalanceDocument(data.gachaPools()));
        documents.put(directory.resolve("monster-spec.json"), new MonsterSpecBalanceDocument(data.monsters()));
        documents.put(directory.resolve("wave-spec.json"), new WaveSpecBalanceDocument(data.waves()));
        documents.put(directory.resolve("wave-spawn.json"), new WaveSpawnBalanceDocument(data.waveSpawns()));
        documents.put(directory.resolve("field-limit.json"), new FieldLimitBalanceDocument(data.fieldLimits()));
        documents.put(directory.resolve("summon-balance.json"), new SummonBalanceDocument(data.summons()));
        documents.put(directory.resolve("merge-rules.json"), new MergeRuleBalanceDocument(data.mergeRules()));
        documents.put(directory.resolve("mythic-choice-balance.json"), new MythicChoiceBalanceDocument(data.mythicChoices()));
        return documents;
    }

    private static Path requireSharedGeneratedDirectory(Path... paths) {
        Path directory = paths[0].toAbsolutePath().normalize().getParent();
        for (Path path : paths) {
            if (!directory.equals(path.toAbsolutePath().normalize().getParent())) {
                throw new IllegalArgumentException("All generated balance JSON files must share one directory.");
            }
        }
        return directory;
    }

    private static void deleteDirectoryQuietly(Path directory) {
        if (directory == null || !Files.exists(directory)) return;
        try (var paths = Files.walk(directory)) {
            paths.sorted(java.util.Comparator.reverseOrder()).forEach(path -> {
                try {
                    Files.deleteIfExists(path);
                } catch (IOException ignored) {
                }
            });
        } catch (IOException ignored) {
        }
    }
}
