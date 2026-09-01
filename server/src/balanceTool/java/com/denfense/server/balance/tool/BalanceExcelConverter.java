package com.denfense.server.balance.tool;

import com.denfense.server.balance.*;
import com.denfense.server.service.balance.BalanceDataValidator;
import com.denfense.server.service.balance.BalanceManifestSupport;
import com.fasterxml.jackson.databind.ObjectMapper;

import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public class BalanceExcelConverter {

    private static final int ARGUMENT_COUNT = 20;

    public static void main(String[] args) {
        if (args.length < ARGUMENT_COUNT) {
            System.err.println("Usage: convertBalance <excelPath> <rewardPath> <upgradeCostPath> <levelStatPath> "
                    + "<specPath> <shopPath> <poolPath> <monsterPath> <wavePath> <waveSpawnPath> <planetBattlePath> "
                    + "<fieldLimitPath> <summonPath> <summonPoolPath> <mergeRulePath> <mythicChoicePath>"
                    + " <mutationSpecPath> <mutationConfigPath> <injectorPoolPath> <resonancePath>");
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
            List<Long> excludedAlienIds = readCanonicalMythicChoiceExcludedAlienIds();

            stagingDirectory = Files.createTempDirectory(generatedDirectory.getParent(), "balance-generation-");
            BalanceJsonWriter writer = new BalanceJsonWriter();
            Map<Path, Object> stagedDocuments = documents(stagingDirectory, data, excludedAlienIds);
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
            stagedToTarget.put(
                    stagingDirectory.resolve("battle-reward.json"),
                    generatedDirectory.resolve("battle-reward.json"));
            stagedToTarget.put(
                    stagingDirectory.resolve("mythic-breeding-config.json"),
                    generatedDirectory.resolve("mythic-breeding-config.json"));
            stagedToTarget.put(
                    stagingDirectory.resolve("mythic-breeding-results.json"),
                    generatedDirectory.resolve("mythic-breeding-results.json"));
            stagedToTarget.put(
                    stagingDirectory.resolve("daily-content.json"),
                    generatedDirectory.resolve("daily-content.json"));
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
        validator.validateBattleReward(data.battleReward());
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
            validator.validateSummonPool(new SummonPoolBalanceDocument(data.summonPools()), data.alienSpecs());
            if (data.summons().stream().anyMatch(s -> !data.summonPools().stream().anyMatch(p -> p.poolId().equals(s.resultPoolId()))))
                throw new IllegalStateException("SummonBalance.resultPoolId must reference SummonPool.");
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
        validator.validatePlanetBattles(new PlanetBattleBalanceDocument(data.planetBattles()));
        validator.validateDailyContents(new DailyContentBalanceDocument(data.dailyContents()));
        validator.validateResonanceBalance(data.resonanceBalances());
        validateMutationBalance(data);
        validateBreedingBalance(data, poolDocument, new MythicChoiceBalanceDocument(data.mythicChoices(),
                readCanonicalMythicChoiceExcludedAlienIdsUnchecked()));
    }

    private static Map<Path, Object> documents(
            Path directory,
            ExcelBalanceReader.BalanceData data,
            List<Long> excludedAlienIds
    ) {
        Map<Path, Object> documents = new LinkedHashMap<>();
        documents.put(directory.resolve("game-reward.json"), data.gameReward());
        documents.put(directory.resolve("battle-reward.json"), data.battleReward());
        documents.put(directory.resolve("alien-upgrade-cost.json"), data.alienUpgradeCosts());
        documents.put(directory.resolve("alien-level-stat.json"), data.alienLevelStats());
        documents.put(directory.resolve("alien-spec.json"), data.alienSpecs());
        documents.put(directory.resolve("shop-products.json"), new ShopProductBalanceDocument(data.shopProducts()));
        documents.put(directory.resolve("gacha-pools.json"), new GachaPoolBalanceDocument(data.gachaPools()));
        documents.put(directory.resolve("summon-pools.json"), new SummonPoolBalanceDocument(data.summonPools()));
        documents.put(directory.resolve("monster-spec.json"), new MonsterSpecBalanceDocument(data.monsters()));
        documents.put(directory.resolve("wave-spec.json"), new WaveSpecBalanceDocument(data.waves()));
        documents.put(directory.resolve("wave-spawn.json"), new WaveSpawnBalanceDocument(data.waveSpawns()));
        documents.put(directory.resolve("planet-battle-balance.json"), new PlanetBattleBalanceDocument(data.planetBattles()));
        documents.put(directory.resolve("field-limit.json"), new FieldLimitBalanceDocument(data.fieldLimits()));
        documents.put(directory.resolve("summon-balance.json"), new SummonBalanceDocument(data.summons()));
        documents.put(directory.resolve("merge-rules.json"), new MergeRuleBalanceDocument(data.mergeRules()));
        documents.put(directory.resolve("mythic-choice-balance.json"),
                new MythicChoiceBalanceDocument(data.mythicChoices(), excludedAlienIds));
        documents.put(directory.resolve("mutation-spec.json"), data.mutationSpecs());
        documents.put(directory.resolve("mutation-config.json"), data.mutationConfig());
        documents.put(directory.resolve("injector-pool.json"), data.injectorPools());
        documents.put(directory.resolve("resonance-balance.json"), data.resonanceBalances());
        documents.put(directory.resolve("mythic-breeding-config.json"), data.mythicBreedingConfig());
        documents.put(directory.resolve("mythic-breeding-results.json"),
                new MythicBreedingResultDocument(data.mythicBreedingResults(), data.mythicBreedingRecipes()));
        documents.put(directory.resolve("daily-content.json"), new DailyContentBalanceDocument(data.dailyContents()));
        return documents;
    }

    public static void validateBreedingBalance(ExcelBalanceReader.BalanceData data,
                                               GachaPoolBalanceDocument pools,
                                               MythicChoiceBalanceDocument choices) {
        MythicBreedingConfigBalance config = data.mythicBreedingConfig();
        if (config == null || !config.enabled() || config.durationSeconds() != 86400 || config.slotCount() != 3
                || config.slot2UnlockLevel() != 30 || config.slot2GemPrice() != 5000 || config.slot3GemPrice() != 10000
                || config.duplicateRewardPieces() != 30 || config.accelerationUnitSeconds() != 600
                || config.accelerationUnitDiamondCost() != 100)
            throw new IllegalStateException("Invalid MythicBreedingConfig");
        var results = data.mythicBreedingResults();
        if (results == null || results.size() != 20 || results.stream().map(MythicBreedingResultBalance::alienId).distinct().count() != 20
                || results.stream().anyMatch(r -> !r.enabled() || r.weight() < 0))
            throw new IllegalStateException("MythicBreedingResult must define 20 unique enabled Mythics");
        var specs = data.alienSpecs().stream().collect(java.util.stream.Collectors.toMap(AlienSpecBalance::alienId, java.util.function.Function.identity()));
        if (results.stream().anyMatch(r -> specs.get(r.alienId()) == null || !"MYTHIC".equals(specs.get(r.alienId()).grade())))
            throw new IllegalStateException("Breeding results must reference MYTHIC AlienSpec");
        var standard = results.stream().filter(r -> "STANDARD".equals(r.acquisitionType())).map(MythicBreedingResultBalance::alienId).collect(java.util.stream.Collectors.toSet());
        var exclusive = results.stream().filter(r -> "BREEDING_EXCLUSIVE".equals(r.acquisitionType())).map(MythicBreedingResultBalance::alienId).collect(java.util.stream.Collectors.toSet());
        if (standard.size() != 18 || exclusive.size() != 2)
            throw new IllegalStateException("Breeding results must contain STANDARD 18 and BREEDING_EXCLUSIVE 2");
        var gachaIds = pools.pools().stream().flatMap(p -> p.gradeEntries().stream()).flatMap(e -> e.alienIds().stream()).collect(java.util.stream.Collectors.toSet());
        if (exclusive.stream().anyMatch(gachaIds::contains)
                || choices.excludedAlienIds().size() != new java.util.HashSet<>(choices.excludedAlienIds()).size()
                || !exclusive.equals(new java.util.HashSet<>(choices.excludedAlienIds())))
            throw new IllegalStateException("Breeding-exclusive Mythics must be excluded from Gacha and Battle Mythic Choice");
        var recipes = data.mythicBreedingRecipes();
        if (recipes == null || recipes.size() != 190) throw new IllegalStateException("All 190 breeding recipes are required");
        java.util.Set<Long> resultIds = results.stream().map(MythicBreedingResultBalance::alienId).collect(java.util.stream.Collectors.toSet());
        java.util.Set<String> pairs = new java.util.HashSet<>();
        java.util.List<Long> exclusiveIds = exclusive.stream().sorted().toList();
        for (var recipe : recipes) {
            String pair = recipe.parentAlienIdA() + ":" + recipe.parentAlienIdB();
            if (!recipe.enabled() || recipe.parentAlienIdA() >= recipe.parentAlienIdB() || !pairs.add(pair)
                    || !resultIds.contains(recipe.parentAlienIdA()) || !resultIds.contains(recipe.parentAlienIdB())
                    || recipe.standardResultAlienIds().size() != 5 || recipe.standardResultAlienIds().stream().distinct().count() != 5
                    || !standard.containsAll(recipe.standardResultAlienIds()) || recipe.standardWeightEach() != 192
                    || recipe.exclusive19AlienId() != exclusiveIds.get(0) || recipe.exclusive20AlienId() != exclusiveIds.get(1)
                    || recipe.exclusive19Weight() != 20 || recipe.exclusive20Weight() != 20)
                throw new IllegalStateException("Invalid breeding recipe: " + recipe.recipeKey());
        }
    }

    private static List<Long> readCanonicalMythicChoiceExcludedAlienIdsUnchecked() {
        try { return readCanonicalMythicChoiceExcludedAlienIds(); }
        catch (IOException e) { throw new IllegalStateException("Failed to read Mythic Choice exclusions", e); }
    }

    private static void validateMutationBalance(ExcelBalanceReader.BalanceData data) {
        if (data.mutationSpecs() == null || data.mutationSpecs().size() != 8)
            throw new IllegalStateException("MutationSpec must define exactly 8 mutation types.");
        java.util.Set<String> types = new java.util.HashSet<>();
        for (MutationSpecBalance spec : data.mutationSpecs()) {
            if (!types.add(spec.mutationType()) || spec.weight() <= 0
                    || spec.attackMultiplier().signum() <= 0 || spec.mpMultiplier().signum() <= 0
                    || spec.attackSpeedMultiplier().signum() <= 0 || spec.rangeMultiplier().signum() <= 0
                    || spec.goldMultiplier().signum() <= 0 || spec.bossDamageMultiplier().signum() <= 0
                    || spec.slowMultiplier().signum() <= 0 || spec.gambleSuccessMultiplier().signum() <= 0
                    || spec.gambleFailureMultiplier().signum() <= 0
                    || spec.splashRadius().signum() < 0 || spec.splashDamageMultiplier().signum() < 0
                    || spec.dotDamageMultiplier().signum() < 0 || spec.dotTickCount() < 0
                    || spec.dotTickIntervalSeconds().signum() < 0 || spec.slowDurationSeconds().signum() < 0
                    || spec.goldPerHit() < 0 || spec.gambleSuccessChance().signum() < 0
                    || spec.gambleSuccessChance().compareTo(java.math.BigDecimal.ONE) > 0)
                throw new IllegalStateException("Invalid MutationSpec: " + spec.mutationType());
            validateMutationMechanic(spec);
        }
        if (!types.containsAll(java.util.Set.of("BERSERK", "GREEDY", "SWIFT", "GIANT", "OBESE", "TOXIC", "FROZEN", "BLANK")))
            throw new IllegalStateException("MutationSpec is missing a required mutation type.");
        MutationSpecBalance blank = data.mutationSpecs().stream().filter(s -> "BLANK".equals(s.mutationType())).findFirst().orElseThrow();
        if (blank.injectorEnabled()) throw new IllegalStateException("BLANK must not be injector-enabled.");
        MutationConfigBalance config = data.mutationConfig();
        if (config == null || config.initialActivationCost() < 0 || config.rerollCost1() <= 0 || config.rerollCost2() <= 0
                || config.rerollCost3() <= 0 || config.rerollCost4() <= 0 || config.rerollCostAfterMax() <= 0 || config.injectorReplaceCost() < 0)
            throw new IllegalStateException("Invalid MutationConfig.");
        if (data.injectorPools() == null || data.injectorPools().isEmpty()) throw new IllegalStateException("InjectorPool must not be empty.");
        java.util.Set<String> poolTypes = new java.util.HashSet<>();
        java.util.Set<String> injectorEnabledTypes = data.mutationSpecs().stream()
                .filter(MutationSpecBalance::injectorEnabled)
                .map(MutationSpecBalance::mutationType)
                .collect(java.util.stream.Collectors.toSet());
        int totalWeight = 0;
        String poolId = null;
        String poolName = null;
        Boolean poolActive = null;
        for (InjectorPoolBalance pool : data.injectorPools()) {
            if (!poolTypes.add(pool.mutationType()) || pool.weight() <= 0 || !"MUTATION_INJECTOR".equals(pool.resultType())
                    || !types.contains(pool.mutationType()) || "BLANK".equals(pool.mutationType()))
                throw new IllegalStateException("Invalid InjectorPool row: " + pool.mutationType());
            if (poolId == null) {
                poolId = pool.poolId();
                poolName = pool.poolName();
                poolActive = pool.poolActive();
            } else if (!java.util.Objects.equals(poolId, pool.poolId())
                    || !java.util.Objects.equals(poolName, pool.poolName())
                    || !java.util.Objects.equals(poolActive, pool.poolActive())) {
                throw new IllegalStateException("InjectorPool rows must share pool identity and active flag.");
            }
            totalWeight += pool.weight();
        }
        if (totalWeight <= 0 || !Boolean.TRUE.equals(poolActive))
            throw new IllegalStateException("InjectorPool must be active with positive total weight.");
        if (!injectorEnabledTypes.equals(poolTypes))
            throw new IllegalStateException("MutationSpec injectorEnabled types must exactly match InjectorPool types.");
    }

    private static void validateMutationMechanic(MutationSpecBalance spec) {
        String expected = switch (spec.mutationType()) {
            case "GIANT" -> "SPLASH";
            case "BERSERK" -> "BOSS_SINGLE";
            case "SWIFT" -> "ATTACK_SPEED";
            case "TOXIC" -> "DOT";
            case "GREEDY" -> "ECONOMY";
            case "OBESE" -> "GAMBLE";
            case "FROZEN" -> "SLOW";
            case "BLANK" -> "NONE";
            default -> null;
        };
        if (!java.util.Objects.equals(expected, spec.mechanic()))
            throw new IllegalStateException("Mutation mechanic mismatch: " + spec.mutationType());
        if ("SPLASH".equals(expected) && (spec.splashRadius().signum() <= 0 || spec.splashDamageMultiplier().signum() <= 0))
            throw new IllegalStateException("GIANT requires splash values.");
        if ("BOSS_SINGLE".equals(expected) && spec.bossDamageMultiplier().compareTo(java.math.BigDecimal.ONE) <= 0)
            throw new IllegalStateException("BERSERK requires a boss multiplier.");
        if ("DOT".equals(expected) && (spec.dotDamageMultiplier().signum() <= 0 || spec.dotTickCount() <= 0 || spec.dotTickIntervalSeconds().signum() <= 0))
            throw new IllegalStateException("TOXIC requires DoT values.");
        if ("SLOW".equals(expected) && (spec.slowMultiplier().compareTo(java.math.BigDecimal.ONE) >= 0 || spec.slowDurationSeconds().signum() <= 0))
            throw new IllegalStateException("FROZEN requires slow values.");
        if ("ECONOMY".equals(expected) && spec.goldPerHit() <= 0)
            throw new IllegalStateException("GREEDY requires goldPerHit.");
        if ("GAMBLE".equals(expected) && (spec.gambleSuccessChance().signum() <= 0
                || spec.gambleSuccessChance().compareTo(java.math.BigDecimal.ONE) >= 0
                || spec.gambleSuccessMultiplier().compareTo(java.math.BigDecimal.ONE) <= 0
                || spec.gambleFailureMultiplier().compareTo(java.math.BigDecimal.ONE) >= 0))
            throw new IllegalStateException("OBESE requires gamble values.");
    }

    private static List<Long> readCanonicalMythicChoiceExcludedAlienIds() throws IOException {
        try (InputStream resource = BalanceExcelConverter.class
                .getResourceAsStream("/balance/generated/mythic-choice-balance.json")) {
            if (resource == null) {
                throw new IllegalStateException("Canonical balance resource is missing: mythic-choice-balance.json");
            }
            MythicChoiceBalanceDocument document = new ObjectMapper()
                    .readValue(resource, MythicChoiceBalanceDocument.class);
            return document.excludedAlienIds();
        }
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
