package com.denfense.server.service.balance;

import com.fasterxml.jackson.databind.ObjectMapper;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.core.io.Resource;
import org.springframework.core.io.ResourceLoader;
import org.springframework.stereotype.Component;

import com.denfense.server.balance.*;
import com.fasterxml.jackson.core.type.TypeReference;
import java.util.List;

import org.springframework.core.annotation.Order;

@Slf4j
@Component
@Order(1)
public class BalanceDataLoader implements ApplicationRunner {

    @Autowired
    public BalanceDataLoader(ResourceLoader resourceLoader, ObjectMapper mapper, BalanceDataValidator validator,
                               BalanceRegistry registry, AlienUpgradeBalanceRegistry upgrade,
                               MonsterBalanceRegistry monsters, WaveBalanceRegistry waves,
                               BattleRuleBalanceRegistry battleRules, MythicBreedingBalanceRegistry breeding,
                               PlanetBattleBalanceRegistry planetBattles, ResonanceBalanceRegistry resonance) {
        this.resourceLoader=resourceLoader; this.baseObjectMapper=mapper; this.validator=validator; this.registry=registry;
        this.alienUpgradeRegistry=upgrade; this.monsterBalanceRegistry=monsters; this.waveBalanceRegistry=waves;
        this.battleRuleBalanceRegistry=battleRules; this.mythicBreedingBalanceRegistry=breeding;
        this.planetBattleBalanceRegistry=planetBattles;
        this.resonanceBalanceRegistry=resonance;
    }

    public BalanceDataLoader(ResourceLoader resourceLoader, ObjectMapper mapper, BalanceDataValidator validator,
                              BalanceRegistry registry, AlienUpgradeBalanceRegistry upgrade,
                              MonsterBalanceRegistry monsters, WaveBalanceRegistry waves,
                              BattleRuleBalanceRegistry battleRules) {
        this(resourceLoader, mapper, validator, registry, upgrade, monsters, waves, battleRules,
                new MythicBreedingBalanceRegistry(), new PlanetBattleBalanceRegistry(), new ResonanceBalanceRegistry());
    }

    private final ResourceLoader resourceLoader;
    private final ObjectMapper baseObjectMapper;
    private final BalanceDataValidator validator;
    private final BalanceRegistry registry;
    private final AlienUpgradeBalanceRegistry alienUpgradeRegistry;
    private final MonsterBalanceRegistry monsterBalanceRegistry;
    private final WaveBalanceRegistry waveBalanceRegistry;
    private final BattleRuleBalanceRegistry battleRuleBalanceRegistry;
    private final MythicBreedingBalanceRegistry mythicBreedingBalanceRegistry;
    private final PlanetBattleBalanceRegistry planetBattleBalanceRegistry;
    private final ResonanceBalanceRegistry resonanceBalanceRegistry;

    @Value("${balance.reward.path:classpath:balance/generated/game-reward.json}")
    private String rewardFilePath;

    @Value("${balance.upgrade-cost.path:classpath:balance/generated/alien-upgrade-cost.json}")
    private String upgradeCostFilePath;

    @Value("${balance.level-stat.path:classpath:balance/generated/alien-level-stat.json}")
    private String levelStatFilePath;

    @Value("${balance.spec.path:classpath:balance/generated/alien-spec.json}")
    private String specFilePath;

    @Value("${balance.pool.path:classpath:balance/generated/gacha-pools.json}")
    private String poolFilePath;

    @Value("${balance.product.path:classpath:balance/generated/shop-products.json}")
    private String productFilePath;

    @Value("${balance.monster.path:classpath:balance/generated/monster-spec.json}")
    private String monsterFilePath;

    @Value("${balance.wave.path:classpath:balance/generated/wave-spec.json}")
    private String waveFilePath;

    @Value("${balance.wave-spawn.path:classpath:balance/generated/wave-spawn.json}")
    private String waveSpawnFilePath;

    @Value("${balance.planet-battle.path:classpath:balance/generated/planet-battle-balance.json}")
    private String planetBattleFilePath;

    @Value("${balance.field-limit.path:classpath:balance/generated/field-limit.json}")
    private String fieldLimitFilePath;

    @Value("${balance.summon.path:classpath:balance/generated/summon-balance.json}")
    private String summonFilePath;
    @Value("${balance.summon-pool.path:classpath:balance/generated/summon-pools.json}")
    private String summonPoolFilePath;

    @Value("${balance.merge-rule.path:classpath:balance/generated/merge-rules.json}")
    private String mergeRuleFilePath;

    @Value("${balance.mythic-choice.path:classpath:balance/generated/mythic-choice-balance.json}")
    private String mythicChoiceFilePath;

    @Value("${balance.mythic-breeding-config.path:classpath:balance/generated/mythic-breeding-config.json}")
    private String mythicBreedingConfigFilePath;
    @Value("${balance.mythic-breeding-results.path:classpath:balance/generated/mythic-breeding-results.json}")
    private String mythicBreedingResultsFilePath;

    @Value("${balance.battle-reward.path:classpath:balance/generated/battle-reward.json}")
    private String battleRewardFilePath;

    @Value("${balance.resonance.path:classpath:balance/generated/resonance-balance.json}")
    private String resonanceFilePath;

    public void setRewardFilePath(String rewardFilePath) {
        this.rewardFilePath = rewardFilePath;
    }

    public void setUpgradeFilePath(String upgradeFilePath) {
        this.upgradeCostFilePath = upgradeFilePath;
    }

    public void setLevelStatFilePath(String levelStatFilePath) {
        this.levelStatFilePath = levelStatFilePath;
    }

    public void setSpecFilePath(String specFilePath) {
        this.specFilePath = specFilePath;
    }

    public void setPoolFilePath(String poolFilePath) {
        this.poolFilePath = poolFilePath;
    }

    public void setProductFilePath(String productFilePath) {
        this.productFilePath = productFilePath;
    }

    public void setMonsterFilePath(String value) { this.monsterFilePath = value; }
    public void setWaveFilePath(String value) { this.waveFilePath = value; }
    public void setWaveSpawnFilePath(String value) { this.waveSpawnFilePath = value; }
    public void setPlanetBattleFilePath(String value) { this.planetBattleFilePath = value; }
    public void setFieldLimitFilePath(String value) { this.fieldLimitFilePath = value; }
    public void setSummonFilePath(String value) { this.summonFilePath = value; }
    public void setSummonPoolFilePath(String value) { this.summonPoolFilePath = value; }
    public void setMergeRuleFilePath(String value) { this.mergeRuleFilePath = value; }
    public void setMythicChoiceFilePath(String value) { this.mythicChoiceFilePath = value; }
    public void setBattleRewardFilePath(String value) { this.battleRewardFilePath = value; }
    public void setResonanceFilePath(String value) { this.resonanceFilePath = value; }

    @Override
    public void run(ApplicationArguments args) throws Exception {
        loadData();
    }

    public void loadData() throws Exception {
        log.info("Balance 데이터 로딩 시작...");

        try {
            ObjectMapper strictMapper = baseObjectMapper.copy()
                .enable(com.fasterxml.jackson.databind.DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES);

            Resource rewardRes = resourceLoader.getResource(rewardFilePath);
            if (!rewardRes.exists()) {
                throw new IllegalStateException("파일을 찾을 수 없습니다: " + rewardFilePath);
            }
            GameRewardBalance rewardBalance = strictMapper.readValue(rewardRes.getInputStream(), GameRewardBalance.class);
            validator.validateGameReward(rewardBalance);

            Resource upgradeRes = resourceLoader.getResource(upgradeCostFilePath);
            if (!upgradeRes.exists()) {
                throw new IllegalStateException("파일을 찾을 수 없습니다: " + upgradeCostFilePath);
            }
            List<AlienUpgradeCostBalance> upgradeCosts = strictMapper.readValue(
                    upgradeRes.getInputStream(), new TypeReference<List<AlienUpgradeCostBalance>>() {});

            Resource levelStatRes = resourceLoader.getResource(levelStatFilePath);
            if (!levelStatRes.exists()) {
                throw new IllegalStateException("파일을 찾을 수 없습니다: " + levelStatFilePath);
            }
            List<AlienLevelStatBalance> levelStats = strictMapper.readValue(
                    levelStatRes.getInputStream(), new TypeReference<List<AlienLevelStatBalance>>() {});
            validator.validateAlienLevelStats(levelStats);
            int maxLevel = levelStats.stream().mapToInt(AlienLevelStatBalance::level).max().orElseThrow();
            validator.validateAlienUpgradeCosts(upgradeCosts, maxLevel);

            Resource specRes = resourceLoader.getResource(specFilePath);
            if (!specRes.exists()) {
                throw new IllegalStateException("파일을 찾을 수 없습니다: " + specFilePath);
            }
            List<AlienSpecBalance> specs = strictMapper.readValue(specRes.getInputStream(), new TypeReference<List<AlienSpecBalance>>() {});
            validator.validateAlienSpec(specs);

            Resource poolRes = resourceLoader.getResource(poolFilePath);
            if (!poolRes.exists()) {
                throw new IllegalStateException("파일을 찾을 수 없습니다: " + poolFilePath);
            }
            com.denfense.server.balance.GachaPoolBalanceDocument poolDoc = strictMapper.readValue(poolRes.getInputStream(), com.denfense.server.balance.GachaPoolBalanceDocument.class);
            validator.validateGachaPool(poolDoc, specs);

            Resource productRes = resourceLoader.getResource(productFilePath);
            if (!productRes.exists()) {
                throw new IllegalStateException("파일을 찾을 수 없습니다: " + productFilePath);
            }
            com.denfense.server.balance.ShopProductBalanceDocument productDoc = strictMapper.readValue(productRes.getInputStream(), com.denfense.server.balance.ShopProductBalanceDocument.class);
            validator.validateShopProduct(productDoc, poolDoc);

            MonsterSpecBalanceDocument monsterDoc = readDocument(strictMapper, monsterFilePath, MonsterSpecBalanceDocument.class);
            WaveSpecBalanceDocument waveDoc = readDocument(strictMapper, waveFilePath, WaveSpecBalanceDocument.class);
            WaveSpawnBalanceDocument spawnDoc = readDocument(strictMapper, waveSpawnFilePath, WaveSpawnBalanceDocument.class);
            PlanetBattleBalanceDocument planetBattleDoc = readDocument(strictMapper, planetBattleFilePath, PlanetBattleBalanceDocument.class);
            FieldLimitBalanceDocument fieldLimitDoc = readDocument(strictMapper, fieldLimitFilePath, FieldLimitBalanceDocument.class);
            SummonBalanceDocument summonDoc = readDocument(strictMapper, summonFilePath, SummonBalanceDocument.class);
            SummonPoolBalanceDocument summonPoolDoc = readDocument(strictMapper, summonPoolFilePath, SummonPoolBalanceDocument.class);
            MergeRuleBalanceDocument mergeRuleDoc = readDocument(strictMapper, mergeRuleFilePath, MergeRuleBalanceDocument.class);
            MythicChoiceBalanceDocument mythicChoiceDoc = readDocument(strictMapper, mythicChoiceFilePath, MythicChoiceBalanceDocument.class);
            MythicBreedingConfigBalance breedingConfig = readDocument(strictMapper, mythicBreedingConfigFilePath, MythicBreedingConfigBalance.class);
            MythicBreedingResultDocument breedingResults = readDocument(strictMapper, mythicBreedingResultsFilePath, MythicBreedingResultDocument.class);
            BattleRewardBalance battleReward = readDocument(strictMapper, battleRewardFilePath, BattleRewardBalance.class);
            List<ResonanceBalance> resonanceBalances = readDocument(strictMapper, resonanceFilePath,
                    new TypeReference<List<ResonanceBalance>>() {});

            validator.validateBattleBalance(monsterDoc, waveDoc, spawnDoc, fieldLimitDoc, summonDoc,
                    mergeRuleDoc, mythicChoiceDoc, specs);
            validator.validatePlanetBattles(planetBattleDoc);
            validator.validateSummonPool(summonPoolDoc, specs);
            if (summonDoc.summons().stream().anyMatch(s -> summonPoolDoc.pools().stream().noneMatch(p -> p.poolId().equals(s.resultPoolId()))))
                throw new IllegalStateException("SummonBalance.resultPoolId must reference SummonPool.");
            validateBreedingBalance(breedingConfig, breedingResults, specs, poolDoc, mythicChoiceDoc);
            validator.validateBattleReward(battleReward);
            validator.validateResonanceBalance(resonanceBalances);

            alienUpgradeRegistry.init(upgradeCosts, levelStats);
            registry.init(rewardBalance, specs, productDoc.products(), poolDoc.pools());
            registry.initBattleReward(battleReward);
            monsterBalanceRegistry.init(monsterDoc.monsters());
            waveBalanceRegistry.init(waveDoc.waves(), spawnDoc.spawns());
            planetBattleBalanceRegistry.init(planetBattleDoc.planets());
            battleRuleBalanceRegistry.init(fieldLimitDoc.fieldLimits(), summonDoc.summons(),
                    mergeRuleDoc.mergeRules(), mythicChoiceDoc.mythicChoices(), summonPoolDoc.pools(), specs);
            mythicBreedingBalanceRegistry.init(breedingConfig, breedingResults.results(), breedingResults.recipes());
            resonanceBalanceRegistry.init(resonanceBalances);

            log.info("Balance 데이터 로딩 완료. MaxLevel: {}", maxLevel);
        } catch (Exception e) {
            log.error("Balance 데이터 로딩 실패! 서버 시작을 중단합니다.", e);
            throw e;
        }
    }

    private void validateBreedingBalance(MythicBreedingConfigBalance config, MythicBreedingResultDocument document,
                                         List<AlienSpecBalance> specs, GachaPoolBalanceDocument pools,
                                         MythicChoiceBalanceDocument choices) {
        if (!config.enabled() || config.durationSeconds() != 86400 || config.slotCount() != 3
                || config.slot2UnlockLevel() != 30 || config.slot2GemPrice() != 5000 || config.slot3GemPrice() != 10000
                || config.duplicateRewardPieces() != 30 || config.accelerationUnitSeconds() != 600
                || config.accelerationUnitDiamondCost() != 100)
            throw new IllegalStateException("Invalid mythic breeding config");
        if (document.results().size() != 20 || document.results().stream().map(MythicBreedingResultBalance::alienId).distinct().count() != 20)
            throw new IllegalStateException("Mythic breeding must define 20 unique results");
        var specById = specs.stream().collect(java.util.stream.Collectors.toMap(AlienSpecBalance::alienId, java.util.function.Function.identity()));
        long standard = document.results().stream().filter(r -> "STANDARD".equals(r.acquisitionType())).count();
        long exclusiveCount = document.results().stream().filter(r -> "BREEDING_EXCLUSIVE".equals(r.acquisitionType())).count();
        if (standard != 18 || exclusiveCount != 2 || document.results().stream().anyMatch(r -> r.weight() < 0 || !r.enabled() || specById.get(r.alienId()) == null || !"MYTHIC".equals(specById.get(r.alienId()).grade())))
            throw new IllegalStateException("Invalid mythic breeding result contract");
        var gachaIds = pools.pools().stream().flatMap(p -> p.gradeEntries().stream()).flatMap(e -> e.alienIds().stream()).collect(java.util.stream.Collectors.toSet());
        if (document.results().stream().filter(r -> "BREEDING_EXCLUSIVE".equals(r.acquisitionType())).anyMatch(r -> gachaIds.contains(r.alienId())))
            throw new IllegalStateException("Breeding-exclusive Mythics cannot be in gacha pool");
        var exclusiveIds = document.results().stream().filter(r -> "BREEDING_EXCLUSIVE".equals(r.acquisitionType())).map(MythicBreedingResultBalance::alienId).collect(java.util.stream.Collectors.toSet());
        if (choices.excludedAlienIds().size() != new java.util.HashSet<>(choices.excludedAlienIds()).size()
                || !exclusiveIds.equals(new java.util.HashSet<>(choices.excludedAlienIds()))
                || choices.excludedAlienIds().stream().anyMatch(id -> !specById.containsKey(id)))
            throw new IllegalStateException("Battle Mythic Choice pool must explicitly exclude breeding-exclusive results");
        validateBreedingRecipes(document, specById, exclusiveIds);
    }

    private void validateBreedingRecipes(MythicBreedingResultDocument document,
                                         java.util.Map<Long, AlienSpecBalance> specById,
                                         java.util.Set<Long> exclusiveIds) {
        if (document.recipes() == null || document.recipes().size() != 190)
            throw new IllegalStateException("Mythic breeding must define all 190 parent combinations");
        java.util.Set<String> pairs = new java.util.HashSet<>();
        java.util.Set<Long> resultIds = document.results().stream().map(MythicBreedingResultBalance::alienId)
                .collect(java.util.stream.Collectors.toSet());
        java.util.Set<Long> standardIds = document.results().stream()
                .filter(r -> "STANDARD".equals(r.acquisitionType())).map(MythicBreedingResultBalance::alienId)
                .collect(java.util.stream.Collectors.toSet());
        java.util.List<Long> exclusive = exclusiveIds.stream().sorted().toList();
        for (var recipe : document.recipes()) {
            if (!recipe.enabled() || recipe.parentAlienIdA() >= recipe.parentAlienIdB()
                    || !resultIds.contains(recipe.parentAlienIdA()) || !resultIds.contains(recipe.parentAlienIdB()))
                throw new IllegalStateException("Invalid breeding parent pair: " + recipe.recipeKey());
            String pair = recipe.parentAlienIdA() + ":" + recipe.parentAlienIdB();
            if (!pairs.add(pair) || recipe.standardResultAlienIds() == null
                    || recipe.standardResultAlienIds().size() != 5
                    || recipe.standardResultAlienIds().stream().distinct().count() != 5
                    || !standardIds.containsAll(recipe.standardResultAlienIds())
                    || recipe.standardWeightEach() != 192
                    || recipe.exclusive19Weight() != 20 || recipe.exclusive20Weight() != 20
                    || recipe.exclusive19AlienId() != exclusive.get(0)
                    || recipe.exclusive20AlienId() != exclusive.get(1)
                    || recipe.standardWeightEach() * 5 + recipe.exclusive19Weight() + recipe.exclusive20Weight() != 1000)
                throw new IllegalStateException("Invalid breeding recipe: " + recipe.recipeKey());
        }
        if (pairs.size() != 190 || specById.values().stream().filter(s -> "MYTHIC".equals(s.grade())).count() != 20)
            throw new IllegalStateException("Breeding recipe coverage mismatch");
    }

    private <T> T readDocument(ObjectMapper mapper, String path, Class<T> type) throws Exception {
        Resource resource = resourceLoader.getResource(path);
        if (!resource.exists()) {
            throw new IllegalStateException("Balance file not found: " + path);
        }
        try (var input = resource.getInputStream()) {
            return mapper.readValue(input, type);
        }
    }

    private <T> T readDocument(ObjectMapper mapper, String path, TypeReference<T> type) throws Exception {
        Resource resource = resourceLoader.getResource(path);
        if (!resource.exists()) {
            throw new IllegalStateException("Balance file not found: " + path);
        }
        try (var input = resource.getInputStream()) {
            return mapper.readValue(input, type);
        }
    }
}
