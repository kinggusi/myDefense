package com.denfense.server.balance;

import com.denfense.server.balance.tool.BalanceExcelConverter;
import com.denfense.server.balance.tool.ExcelBalanceReader;
import com.denfense.server.service.balance.BalanceDataValidator;
import org.junit.jupiter.api.Test;

import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class BattleBalanceExcelContractTest {

    private final BalanceDataValidator validator = new BalanceDataValidator();

    @Test
    void canonicalWorkbookContainsAndValidatesAllBattleContracts() {
        ExcelBalanceReader.BalanceData data = readCanonical();
        BalanceExcelConverter.validate(data);

        assertThat(data.monsters()).hasSize(3);
        assertThat(data.waves()).hasSize(10);
        assertThat(data.waveSpawns()).hasSize(12);
        assertThat(data.fieldLimits()).singleElement().satisfies(limit -> {
            assertThat(limit.playerCount()).isEqualTo(2);
            assertThat(limit.maxAliveMonsterCountPerField()).isEqualTo(100);
            assertThat(limit.warningThreshold()).isEqualTo(80);
            assertThat(limit.dangerThreshold()).isEqualTo(90);
        });
        assertThat(data.summons()).singleElement().satisfies(summon -> {
            assertThat(summon.summonType()).isEqualTo("KIDNAP");
            assertThat(summon.baseCost()).isEqualTo(50);
            assertThat(summon.costIncreasePerUse()).isEqualTo(10);
            assertThat(summon.maxUses()).isEqualTo(-1);
        });
        assertThat(data.mergeRules()).hasSize(5).allSatisfy(rule -> {
            assertThat(rule.requiredCount()).isEqualTo(2);
            assertThat(rule.sameSpeciesRequired()).isTrue();
        });
        assertThat(data.mergeRules()).filteredOn(rule -> rule.sourceGrade().equals("LEGEND"))
                .singleElement().extracting(MergeRuleBalance::resultType).isEqualTo("MYTHIC_CHOICE");
        assertThat(data.mergeRules()).filteredOn(rule -> rule.sourceGrade().equals("MYTHIC"))
                .singleElement().extracting(MergeRuleBalance::resultType).isEqualTo("DISABLED");
        assertThat(data.mythicChoices()).singleElement().satisfies(choice -> {
            assertThat(choice.candidateCount()).isEqualTo(3);
            assertThat(choice.freeRerollCount()).isEqualTo(1);
            assertThat(choice.paidRerollLimit()).isEqualTo(1);
            assertThat(choice.paidRerollCost()).isEqualTo(100);
            assertThat(choice.excludePreviousCandidates()).isTrue();
            assertThat(choice.autoSelectPolicy()).isEqualTo("FIRST");
        });
    }

    @Test
    void sameGradeDifferentAlienIdsCannotMerge() {
        MergeRuleBalance normal = readCanonical().mergeRules().stream()
                .filter(rule -> rule.sourceGrade().equals("NORMAL"))
                .findFirst()
                .orElseThrow();

        assertThat(canMerge(normal, "NORMAL", 1L, "NORMAL", 2L)).isFalse();
        assertThat(canMerge(normal, "NORMAL", 1L, "NORMAL", 1L)).isTrue();
    }

    @Test
    void fieldLimitEliminatesAtOneHundredAndMatchFailsOnlyWhenAllPlayersAreEliminated() {
        FieldLimitBalance limit = readCanonical().fieldLimits().get(0);

        boolean playerAAt99 = 99 >= limit.maxAliveMonsterCountPerField();
        boolean playerAAt100 = 100 >= limit.maxAliveMonsterCountPerField();
        boolean playerBAt45 = 45 >= limit.maxAliveMonsterCountPerField();

        assertThat(playerAAt99).isFalse();
        assertThat(playerAAt100).isTrue();
        assertThat(playerAAt100 && playerBAt45).isFalse();
        assertThat(playerAAt100 && 100 >= limit.maxAliveMonsterCountPerField()).isTrue();
    }

    @Test
    void mythicPoolDerivesAllTwentyRowsWithoutUsingLockState() {
        ExcelBalanceReader.BalanceData data = readCanonical();
        List<AlienSpecBalance> mythics = data.alienSpecs().stream()
                .filter(spec -> "MYTHIC".equals(spec.grade()))
                .toList();

        assertThat(mythics).hasSize(20);
        assertThat(mythics).extracting(AlienSpecBalance::alienId).containsExactlyElementsOf(
                java.util.stream.LongStream.rangeClosed(29, 48).boxed().toList());
        assertThat(mythics).filteredOn(AlienSpecBalance::isLocked).hasSize(16);
        validator.validateMythicChoices(new MythicChoiceBalanceDocument(data.mythicChoices()), data.alienSpecs());
    }

    @Test
    void crossValidationRejectsMissingMonsterDuplicateSpawnAndModeMismatch() {
        ExcelBalanceReader.BalanceData data = readCanonical();
        WaveSpawnBalance first = data.waveSpawns().get(0);

        List<WaveSpawnBalance> missingMonster = new ArrayList<>(data.waveSpawns());
        missingMonster.set(0, new WaveSpawnBalance(first.spawnGroupId(), first.order(), "UNKNOWN", first.spawnCountPerField(),
                first.startDelaySeconds(), first.spawnIntervalSeconds(), first.lanePolicy()));
        assertThatThrownBy(() -> validator.validateWaveSpawns(
                new WaveSpawnBalanceDocument(missingMonster), new MonsterSpecBalanceDocument(data.monsters())))
                .hasMessageContaining("missing monsterId");

        List<WaveSpawnBalance> duplicate = new ArrayList<>(data.waveSpawns());
        duplicate.add(first);
        assertThatThrownBy(() -> validator.validateWaveSpawns(
                new WaveSpawnBalanceDocument(duplicate), new MonsterSpecBalanceDocument(data.monsters())))
                .hasMessageContaining("Duplicate");

        List<FieldLimitBalance> mismatched = List.of(new FieldLimitBalance("OTHER", 2, 100, 80, 90));
        assertThatThrownBy(() -> validator.validateBattleBalance(
                new MonsterSpecBalanceDocument(data.monsters()), new WaveSpecBalanceDocument(data.waves()),
                new WaveSpawnBalanceDocument(data.waveSpawns()), new FieldLimitBalanceDocument(mismatched),
                new SummonBalanceDocument(data.summons()), new MergeRuleBalanceDocument(data.mergeRules()),
                new MythicChoiceBalanceDocument(data.mythicChoices()), data.alienSpecs()))
                .hasMessageContaining("modeId sets");
    }

    @Test
    void validatorsRejectInvalidMergeMythicAndFieldLimits() {
        ExcelBalanceReader.BalanceData data = readCanonical();
        List<MergeRuleBalance> invalidMerge = new ArrayList<>(data.mergeRules());
        MergeRuleBalance normal = invalidMerge.get(0);
        invalidMerge.set(0, new MergeRuleBalance(normal.sourceGrade(), 2, false, normal.resultType(), normal.resultGrade(), true));
        assertThatThrownBy(() -> validator.validateMergeRules(new MergeRuleBalanceDocument(invalidMerge)))
                .hasMessageContaining("same grade and alienId");

        MythicChoiceBalance choice = data.mythicChoices().get(0);
        MythicChoiceBalance invalidChoice = new MythicChoiceBalance(choice.modeId(), 4, 0, 0, 0, false,
                choice.selectionTimeoutSeconds(), choice.autoSelectPolicy(), true, true);
        assertThatThrownBy(() -> validator.validateMythicChoices(
                new MythicChoiceBalanceDocument(List.of(invalidChoice)), data.alienSpecs()))
                .hasMessageContaining("Invalid MythicChoiceBalance");

        assertThatThrownBy(() -> validator.validateFieldLimits(new FieldLimitBalanceDocument(
                List.of(new FieldLimitBalance("COOP_STANDARD", 2, 100, 90, 80)))))
                .hasMessageContaining("Invalid field limit");
    }

    @Test
    void validatorsRejectInvalidTypeLaneReferenceAndNumericRanges() {
        ExcelBalanceReader.BalanceData data = readCanonical();
        MonsterSpecBalance firstMonster = data.monsters().get(0);
        List<MonsterSpecBalance> invalidTypes = new ArrayList<>(data.monsters());
        invalidTypes.set(0, new MonsterSpecBalance(firstMonster.monsterId(), firstMonster.name(), "INVALID",
                firstMonster.baseHp(), firstMonster.moveSpeed(), firstMonster.killGold(), true));
        assertThatThrownBy(() -> validator.validateMonsterSpecs(new MonsterSpecBalanceDocument(invalidTypes)))
                .hasMessageContaining("Unsupported monsterType");

        WaveSpawnBalance firstSpawn = data.waveSpawns().get(0);
        WaveSpawnBalance invalidLane = new WaveSpawnBalance(firstSpawn.spawnGroupId(), firstSpawn.order(),
                firstSpawn.monsterId(), firstSpawn.spawnCountPerField(), firstSpawn.startDelaySeconds(),
                firstSpawn.spawnIntervalSeconds(), "RANDOM_FIELD");
        assertThatThrownBy(() -> validator.validateWaveSpawns(
                new WaveSpawnBalanceDocument(List.of(invalidLane)), new MonsterSpecBalanceDocument(data.monsters())))
                .hasMessageContaining("Unsupported lanePolicy");

        SummonBalance invalidSummon = new SummonBalance("COOP_STANDARD", "KIDNAP", 50, 10, 0,
                "STANDARD_SUMMON_POOL", true);
        assertThatThrownBy(() -> validator.validateSummons(new SummonBalanceDocument(List.of(invalidSummon))))
                .hasMessageContaining("Invalid SummonBalance");

        WaveSpecBalance firstWave = data.waves().get(0);
        WaveSpecBalance invalidNormalWave = new WaveSpecBalance(firstWave.modeId(), firstWave.wave(),
                firstWave.hpMultiplier(), firstWave.interWaveDelaySeconds(), false,
                java.math.BigDecimal.ONE, firstWave.spawnGroupId(), true);
        List<WaveSpecBalance> invalidWaves = new ArrayList<>(data.waves());
        invalidWaves.set(0, invalidNormalWave);
        assertThatThrownBy(() -> validator.validateWaves(
                new WaveSpecBalanceDocument(invalidWaves), new WaveSpawnBalanceDocument(data.waveSpawns())))
                .hasMessageContaining("must be zero");
    }

    @Test
    void lanePoliciesEnforceBossSharedContract() {
        ExcelBalanceReader.BalanceData data = readCanonical();
        WaveSpawnBalance boss = data.waveSpawns().stream()
                .filter(spawn -> spawn.spawnGroupId().equals("WAVE_10_BOSS"))
                .findFirst()
                .orElseThrow();
        WaveSpawnBalance normal = data.waveSpawns().get(0);

        WaveSpawnBalance bossAsEachField = new WaveSpawnBalance(boss.spawnGroupId(), boss.order(), boss.monsterId(),
                boss.spawnCountPerField(), boss.startDelaySeconds(), boss.spawnIntervalSeconds(), "EACH_FIELD");
        assertThatThrownBy(() -> validator.validateWaveSpawns(
                new WaveSpawnBalanceDocument(List.of(bossAsEachField)), new MonsterSpecBalanceDocument(data.monsters())))
                .hasMessageContaining("EACH_FIELD cannot spawn WAVE_BOSS");

        WaveSpawnBalance normalAsBossShared = new WaveSpawnBalance(normal.spawnGroupId(), normal.order(), normal.monsterId(),
                normal.spawnCountPerField(), normal.startDelaySeconds(), normal.spawnIntervalSeconds(), "BOSS_SHARED");
        assertThatThrownBy(() -> validator.validateWaveSpawns(
                new WaveSpawnBalanceDocument(List.of(normalAsBossShared)), new MonsterSpecBalanceDocument(data.monsters())))
                .hasMessageContaining("BOSS_SHARED requires WAVE_BOSS");

        WaveSpawnBalance zeroBoss = new WaveSpawnBalance(boss.spawnGroupId(), boss.order(), boss.monsterId(), 0,
                boss.startDelaySeconds(), boss.spawnIntervalSeconds(), boss.lanePolicy());
        assertThatThrownBy(() -> validator.validateWaveSpawns(
                new WaveSpawnBalanceDocument(List.of(zeroBoss)), new MonsterSpecBalanceDocument(data.monsters())))
                .hasMessageContaining("Invalid WaveSpawn numeric value");

        WaveSpawnBalance twoBosses = new WaveSpawnBalance(boss.spawnGroupId(), boss.order(), boss.monsterId(), 2,
                boss.startDelaySeconds(), boss.spawnIntervalSeconds(), boss.lanePolicy());
        assertThatThrownBy(() -> validator.validateWaveSpawns(
                new WaveSpawnBalanceDocument(List.of(twoBosses)), new MonsterSpecBalanceDocument(data.monsters())))
                .hasMessageContaining("BOSS_SHARED spawn count must be exactly one");

        WaveSpawnBalance duplicateBoss = new WaveSpawnBalance(boss.spawnGroupId(), 2, boss.monsterId(), 1,
                boss.startDelaySeconds(), boss.spawnIntervalSeconds(), boss.lanePolicy());
        assertThatThrownBy(() -> validator.validateWaveSpawns(
                new WaveSpawnBalanceDocument(List.of(boss, duplicateBoss)), new MonsterSpecBalanceDocument(data.monsters())))
                .hasMessageContaining("Boss SpawnGroup must contain exactly one");

        WaveSpawnBalance mixedBoss = new WaveSpawnBalance(normal.spawnGroupId(), 3, boss.monsterId(), 1,
                boss.startDelaySeconds(), boss.spawnIntervalSeconds(), boss.lanePolicy());
        assertThatThrownBy(() -> validator.validateWaveSpawns(
                new WaveSpawnBalanceDocument(List.of(normal, mixedBoss)), new MonsterSpecBalanceDocument(data.monsters())))
                .hasMessageContaining("without mixed lanes");
    }

    @Test
    void documentCollectionsAreImmutable() {
        ExcelBalanceReader.BalanceData data = readCanonical();
        MonsterSpecBalanceDocument document = new MonsterSpecBalanceDocument(data.monsters());
        assertThatThrownBy(() -> document.monsters().add(data.monsters().get(0)))
                .isInstanceOf(UnsupportedOperationException.class);
    }

    private ExcelBalanceReader.BalanceData readCanonical() {
        return new ExcelBalanceReader(Path.of("..", "balance", "source", "balance-data.xlsx").toString()).read();
    }

    private boolean canMerge(
            MergeRuleBalance rule,
            String sourceGrade,
            long sourceAlienId,
            String targetGrade,
            long targetAlienId
    ) {
        return rule.enabled()
                && !"DISABLED".equals(rule.resultType())
                && sourceGrade.equals(targetGrade)
                && (!rule.sameSpeciesRequired() || sourceAlienId == targetAlienId);
    }
}
