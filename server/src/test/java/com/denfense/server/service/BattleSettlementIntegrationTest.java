package com.denfense.server.service;

import com.denfense.server.domain.User;
import com.denfense.server.dto.battle.BattleSettlementDtos;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.BattlePlayerSettlementRepository;
import com.denfense.server.repository.BattleRewardClaimRepository;
import com.denfense.server.repository.BattleSettlementRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.repository.UserPlanetUnlockRepository;
import com.denfense.server.repository.BattleEntryReservationRepository;
import com.denfense.server.domain.UserPlanetUnlock;
import com.denfense.server.domain.PlanetUnlockSource;
import com.denfense.server.service.balance.BalanceVersionRegistry;
import com.denfense.server.service.balance.MonsterBalanceRegistry;
import com.denfense.server.service.balance.WaveBalanceRegistry;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.Callable;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

@SpringBootTest
class BattleSettlementIntegrationTest {
    @Autowired BattleSettlementService service;
    @Autowired UserRepository users;
    @Autowired BattleSettlementRepository settlements;
    @Autowired BattlePlayerSettlementRepository playerSettlements;
    @Autowired BattleRewardClaimRepository rewardClaims;
    @Autowired BalanceVersionRegistry versions;
    @Autowired BattleSessionRosterRegistry rosters;
    @Autowired WaveBalanceRegistry waves;
    @Autowired MonsterBalanceRegistry monsters;
    @Autowired UserPlanetUnlockRepository planetUnlocks;
    @Autowired BattlePlanetEntryService battleEntries;
    @Autowired BattleEntryReservationRepository entryReservations;
    @Autowired BattleSettlementWriter settlementWriter;

    @BeforeEach
    void cleanup() {
        rewardClaims.deleteAll();
        playerSettlements.deleteAll();
        settlements.deleteAll();
        entryReservations.deleteAll();
        planetUnlocks.deleteAll();
        users.deleteAll();
        rosters.clearForTest();
    }

    @Test
    void normalSettlementPersistsTwoPlayersAndEmptyRewards() {
        User a = user("battle-a"), b = user("battle-b");
        var out = service.settle(valid("req-1", "session-1", "hash-1", a, b));
        assertThat(out.status()).isEqualTo("ACCEPTED");
        assertThat(out.alreadyProcessed()).isFalse();
        assertThat(out.rewards()).isEmpty();
        assertThat(settlements.count()).isEqualTo(1);
        assertThat(playerSettlements.count()).isEqualTo(2);
    }

    @Test
    void requestIdHasPriorityAndIsIdempotent() {
        User a = user("idem-a"), b = user("idem-b");
        var request = valid("same", "s1", "h1", a, b);
        service.settle(request);
        assertThat(service.settle(request).alreadyProcessed()).isTrue();
        assertThatThrownBy(() -> service.settle(valid("same", "s2", "h1", a, b)))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_REQUEST_CONFLICT));
    }

    @Test
    void writerRecoversCompletedReservationAsExistingSettlement() {
        User a = user("completed-race-a"), b = user("completed-race-b");
        var request = valid("completed-race-request", "completed-race-session", "ignored", a, b);

        var first = settlementWriter.create(request, com.denfense.server.domain.SessionSource.LOCAL_DEVELOPMENT);
        battleEntries.completeIfReserved(request.battleSessionId());
        var retry = settlementWriter.create(request, com.denfense.server.domain.SessionSource.LOCAL_DEVELOPMENT);

        assertThat(first.created()).isTrue();
        assertThat(retry.created()).isFalse();
        assertThat(retry.settlement().getId()).isEqualTo(first.settlement().getId());
        assertThat(settlements.findAll()).hasSize(1);
        assertThat(playerSettlements.findAll()).hasSize(2);
    }

    @Test
    void storedSettlementRemainsIdempotentAfterTrustedRosterExpires() {
        User a = user("expired-idem-a"), b = user("expired-idem-b");
        var request = valid("expired-idem", "expired-session", "expired-hash", a, b);
        service.settle(request);
        rosters.clearForTest();

        assertThat(service.settle(request).alreadyProcessed()).isTrue();
        assertThat(settlements.count()).isEqualTo(1);
        assertThat(playerSettlements.count()).isEqualTo(2);
    }

    @Test
    void unregisteredBattleSessionCannotGrantRewards() {
        User a = user("unregistered-a"), b = user("unregistered-b");
        var request = request("unregistered", "unregistered-session", "unregistered-hash", a, b,
                "VICTORY", 80, "EARTH", false, false, false);
        assertThatThrownBy(() -> service.settle(request))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_PARTICIPANT_MISMATCH));
        assertThat(rewardClaims.count()).isZero();
    }

    @Test
    void incompleteCanonicalWaveProgressionCannotGrantRewards() {
        User a = user("progress-a"), b = user("progress-b");
        register("progress-session", a, b, "EARTH");
        var now = LocalDateTime.now();
        var invalid = new BattleSettlementDtos.Request(
                "progress", "progress-session", versions.getBalanceVersion(), versions.getContentHash(),
                "VICTORY", 80, "EARTH", now.minusMinutes(20), now,
                List.of(player(a, 1, 1, 0, false), player(b, 2, 1, 0, false)),
                List.of(new BattleSettlementDtos.Monster("NORMAL_MONSTER", 2, 0, 40)),
                List.of(), List.of(), "progress-hash");
        assertThatThrownBy(() -> service.settle(invalid))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));
        assertThat(rewardClaims.count()).isZero();
    }

    @Test
    void invalidGoldAndParticipantsAreRejected() {
        User a = user("invalid-a"), b = user("invalid-b");
        var valid = valid("bad", "sbad", "h", a, b);
        var first = valid.players().get(0);
        var invalidPlayer = new BattleSettlementDtos.Player(first.playerId(), first.playerSlot(), first.eliminated(),
                first.eliminatedWave(), first.kills(), first.supportKills(), first.bossKills(),
                first.initialInGameGold(), first.inGameGoldEarned(), first.inGameGoldSpent(), 99, first.abandoned());
        var invalid = new BattleSettlementDtos.Request(valid.requestId(), valid.battleSessionId(), valid.balanceVersion(),
                valid.contentHash(), valid.result(), valid.finalWave(), valid.mapId(), valid.startedAt(), valid.finishedAt(),
                List.of(invalidPlayer, valid.players().get(1)), valid.monsterKills(),
                valid.waveSpawnFacts(), valid.partialWaveKills(),
                valid.summaryHash());
        assertThatThrownBy(() -> service.settle(invalid)).isInstanceOf(BusinessException.class);
    }

    @Test
    void invalidResultIsRejectedAsBusinessError() {
        User a = user("result-a"), b = user("result-b");
        var valid = valid("bad-result", "result-session", "result-hash", a, b);
        var invalid = new BattleSettlementDtos.Request(valid.requestId(), valid.battleSessionId(), valid.balanceVersion(),
                valid.contentHash(), "CLEARED", valid.finalWave(), valid.mapId(), valid.startedAt(), valid.finishedAt(),
                valid.players(), valid.monsterKills(), valid.waveSpawnFacts(), valid.partialWaveKills(),
                valid.summaryHash());
        assertThatThrownBy(() -> service.settle(invalid))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));
    }

    @Test
    void finalWaveAboveCanonicalMaximumIsRejected() {
        User a = user("wave-a"), b = user("wave-b");
        var invalid = request("wave-invalid", "wave-session", "wave-hash", a, b,
                "DEFEAT", 81, "EARTH", false, false, true);
        assertThatThrownBy(() -> service.settle(invalid))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));
    }

    @Test
    void unknownMapIdIsRejectedBeforeRewardClaim() {
        User a = user("map-a"), b = user("map-b");
        var invalid = request("map-invalid", "map-session", "map-hash", a, b,
                "DEFEAT", 70, "UNKNOWN_PLANET", false, false, false);
        rosters.register("map-session", 1, a.getUsername(), "UNKNOWN_PLANET",
                versions.getBalanceVersion(), versions.getContentHash());
        rosters.register("map-session", 2, b.getUsername(), "UNKNOWN_PLANET",
                versions.getBalanceVersion(), versions.getContentHash());
        assertThatThrownBy(() -> service.settle(invalid))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));
    }

    @Test
    void concurrentSameRequestCreatesOneSettlement() throws Exception {
        User a = user("conc-a"), b = user("conc-b");
        var request = valid("conc", "sc", "hc", a, b);
        ExecutorService executor = Executors.newFixedThreadPool(2);
        CountDownLatch gate = new CountDownLatch(1);
        Callable<BattleSettlementDtos.Response> task = () -> { gate.await(); return service.settle(request); };
        Future<BattleSettlementDtos.Response> x = executor.submit(task), y = executor.submit(task);
        gate.countDown();
        var first = x.get();
        var second = y.get();
        executor.shutdown();
        assertThat(settlements.count()).isEqualTo(1);
        assertThat(playerSettlements.count()).isEqualTo(2);
        assertThat(first.alreadyProcessed() ^ second.alreadyProcessed()).isTrue();
    }

    @Test
    void wave70FailureAwardsReachedCheckpointsOnlyOnce() {
        User a = user("reward-a"), b = user("reward-b");
        var request = valid("reward-70", "reward-session-70", "reward-hash-70", a, b,
                "DEFEAT", 70, "EARTH");
        var first = service.settle(request);
        var second = service.settle(request);
        assertThat(first.rewards()).extracting(BattleSettlementDtos.Reward::rewardType)
                .contains("SETTLEMENT", "CHECKPOINT");
        assertThat(second.alreadyProcessed()).isTrue();
        assertThat(rewardClaims.findByBattleSessionIdOrderByIdAsc("reward-session-70")).hasSize(16);
        assertThat(users.findByUsername("reward-a").orElseThrow().getDiamond()).isZero();
    }

    @Test
    void firstVictoryAtWave80AwardsPlanetDiamondAndReclearDoesNotDuplicate() {
        User a = user("first-a"), b = user("first-b");
        var request = valid("first-clear", "first-session", "first-hash", a, b,
                "VICTORY", 80, "EARTH");
        var first = service.settle(request);
        assertThat(first.rewards()).extracting(BattleSettlementDtos.Reward::rewardType).contains("MAP_FIRST_CLEAR");
        int claimsAfterFirst = rewardClaims.findByBattleSessionIdOrderByIdAsc("first-session").size();
        var second = service.settle(request);
        assertThat(second.alreadyProcessed()).isTrue();
        assertThat(rewardClaims.findByBattleSessionIdOrderByIdAsc("first-session")).hasSize(claimsAfterFirst);
        assertThat(users.findByUsername("first-a").orElseThrow().getDiamond()).isEqualTo(8000);
    }

    @Test
    void abandonedPlayerReceivesNoPermanentReward() {
        User a = user("active-player"), b = user("abandoned-player");
        var request = valid("abandoned", "abandoned-session", "abandoned-hash", a, b,
                "VICTORY", 80, "EARTH", false, true);
        service.settle(request);
        assertThat(rewardClaims.findByBattleSessionIdOrderByIdAsc("abandoned-session"))
                .allMatch(claim -> claim.getUser().getId().equals(a.getId()));
        assertThat(planetUnlocks.findByUserIdAndMapId(a.getId(), "VENUS")).isPresent();
        assertThat(planetUnlocks.findByUserIdAndMapId(b.getId(), "VENUS")).isEmpty();
    }

    @Test
    void failedMatchAcceptsCanonicalPartialWaveKillAndEliminationAtNextWave() {
        User a = user("partial-a"), b = user("partial-b");
        register("partial-session", a, b, "EARTH");
        LocalDateTime now = LocalDateTime.now().withNano(0);
        var fact = waveSpawnFact("18446744073709551615", 1, 1, 1);
        var partial = new BattleSettlementDtos.PartialWaveKill(
                "18446744073709551615", 1, "WAVE_01", "NORMAL_MONSTER", "EACH_FIELD", 1,
                1, 1, 1, 2);
        var unsigned = new BattleSettlementDtos.Request(
                "partial-request", "partial-session", versions.getBalanceVersion(), versions.getContentHash(),
                "DEFEAT", 0, "EARTH", now.minusMinutes(1), now,
                List.of(
                        new BattleSettlementDtos.Player(a.getUsername(), 1, true, 1, 1, 0, 0,
                                100, 20, 0, 120, false),
                        new BattleSettlementDtos.Player(b.getUsername(), 2, false, null, 0, 1, 0,
                                100, 0, 0, 100, false)),
                List.of(new BattleSettlementDtos.Monster("NORMAL_MONSTER", 1, 0, 20)),
                List.of(fact), List.of(partial), "");

        var response = service.settle(signed(unsigned));

        assertThat(response.status()).isEqualTo("ACCEPTED");
        assertThat(settlements.count()).isEqualTo(1);
        assertThat(playerSettlements.count()).isEqualTo(2);
    }

    @Test
    void victoryRejectsPartialWaveLedger() {
        User a = user("victory-partial-a"), b = user("victory-partial-b");
        var valid = valid("victory-partial", "victory-partial-session", "ignored", a, b,
                "VICTORY", 80, "EARTH");
        var fact = new BattleSettlementDtos.WaveSpawnFact(
                "1", 80, "WAVE_80_BOSS", "WAVE_BOSS", "BOSS_SHARED", null, 1, 1);
        var partial = new BattleSettlementDtos.PartialWaveKill(
                "1", 80, "WAVE_80_BOSS", "WAVE_BOSS", "BOSS_SHARED", null,
                1, 1, 1, null);
        var invalid = signed(new BattleSettlementDtos.Request(
                valid.requestId(), valid.battleSessionId(), valid.balanceVersion(), valid.contentHash(),
                valid.result(), valid.finalWave(), valid.mapId(), valid.startedAt(), valid.finishedAt(),
                valid.players(), valid.monsterKills(), List.of(fact), List.of(partial), ""));

        assertThatThrownBy(() -> service.settle(invalid))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));
    }

    @Test
    void duplicateRuntimeMonsterIdAndSpawnPositionAreRejected() {
        User a = user("duplicate-partial-a"), b = user("duplicate-partial-b");
        register("duplicate-partial-session", a, b, "EARTH");
        LocalDateTime now = LocalDateTime.now().withNano(0);
        var firstFact = waveSpawnFact("1", 1, 1, 1);
        var duplicateFact = waveSpawnFact("1", 1, 1, 1);
        var first = new BattleSettlementDtos.PartialWaveKill(
                "1", 1, "WAVE_01", "NORMAL_MONSTER", "EACH_FIELD", 1,
                1, 1, 1, null);
        var duplicate = new BattleSettlementDtos.PartialWaveKill(
                "1", 1, "WAVE_01", "NORMAL_MONSTER", "EACH_FIELD", 1,
                1, 1, 2, null);
        var invalid = signed(new BattleSettlementDtos.Request(
                "duplicate-partial", "duplicate-partial-session", versions.getBalanceVersion(), versions.getContentHash(),
                "DEFEAT", 0, "EARTH", now.minusMinutes(1), now,
                List.of(player(a, 1, 1, 0, false), player(b, 2, 1, 0, false)),
                List.of(new BattleSettlementDtos.Monster("NORMAL_MONSTER", 2, 0, 40)),
                List.of(firstFact, duplicateFact), List.of(first, duplicate), ""));

        assertThatThrownBy(() -> service.settle(invalid))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));
    }

    @Test
    void changedPayloadWithOldSummaryHashIsRejected() {
        User a = user("hash-a"), b = user("hash-b");
        var valid = valid("hash-request", "hash-session", "ignored", a, b);
        var first = valid.players().get(0);
        var changed = new BattleSettlementDtos.Player(
                first.playerId(), first.playerSlot(), first.eliminated(), first.eliminatedWave(),
                first.kills(), first.supportKills() + 1, first.bossKills(), first.initialInGameGold(),
                first.inGameGoldEarned(), first.inGameGoldSpent(), first.finalInGameGold(), first.abandoned());
        var tampered = new BattleSettlementDtos.Request(
                valid.requestId(), valid.battleSessionId(), valid.balanceVersion(), valid.contentHash(),
                valid.result(), valid.finalWave(), valid.mapId(), valid.startedAt(), valid.finishedAt(),
                List.of(changed, valid.players().get(1)), valid.monsterKills(),
                valid.waveSpawnFacts(), valid.partialWaveKills(),
                valid.summaryHash());

        assertThatThrownBy(() -> service.settle(tampered))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));
    }

    @Test
    void completedWaveExpectationExcludesLaneAfterEliminationWave() {
        User a = user("lane-a"), b = user("lane-b");
        register("lane-session", a, b, "EARTH");
        LocalDateTime now = LocalDateTime.now().withNano(0);
        var unsigned = new BattleSettlementDtos.Request(
                "lane-request", "lane-session", versions.getBalanceVersion(), versions.getContentHash(),
                "DEFEAT", 2, "EARTH", now.minusMinutes(2), now,
                List.of(
                        new BattleSettlementDtos.Player(a.getUsername(), 1, true, 1, 20, 0, 0,
                                100, 0, 0, 100, false),
                        new BattleSettlementDtos.Player(b.getUsername(), 2, false, null, 17, 0, 0,
                                100, 0, 0, 100, false)),
                List.of(new BattleSettlementDtos.Monster("NORMAL_MONSTER", 37, 0, 740)),
                List.of(), List.of(), "");

        assertThat(service.settle(signed(unsigned)).status()).isEqualTo("ACCEPTED");
    }

    @Test
    void partialWaveRuntimeIdsMustBeStrictlyUnsignedAscending() {
        User a = user("order-a"), b = user("order-b");
        register("order-session", a, b, "EARTH");
        LocalDateTime now = LocalDateTime.now().withNano(0);
        var firstFact = waveSpawnFact("1", 1, 1, 2);
        var secondFact = waveSpawnFact("2", 1, 1, 1);
        var second = new BattleSettlementDtos.PartialWaveKill(
                "2", 1, "WAVE_01", "NORMAL_MONSTER", "EACH_FIELD", 1,
                1, 1, 1, null);
        var first = new BattleSettlementDtos.PartialWaveKill(
                "1", 1, "WAVE_01", "NORMAL_MONSTER", "EACH_FIELD", 1,
                1, 2, 2, null);
        var invalid = signed(new BattleSettlementDtos.Request(
                "order-request", "order-session", versions.getBalanceVersion(), versions.getContentHash(),
                "DEFEAT", 0, "EARTH", now.minusMinutes(1), now,
                List.of(player(a, 1, 1, 0, false), player(b, 2, 1, 0, false)),
                List.of(new BattleSettlementDtos.Monster("NORMAL_MONSTER", 2, 0, 40)),
                List.of(firstFact, secondFact), List.of(second, first), ""));

        assertThatThrownBy(() -> service.settle(invalid))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));
    }

    @Test
    void partialWaveAttributionMustBeIncludedInPlayerAggregates() {
        User a = user("attribution-a"), b = user("attribution-b");
        register("attribution-session", a, b, "EARTH");
        LocalDateTime now = LocalDateTime.now().withNano(0);
        var fact = waveSpawnFact("1", 1, 1, 1);
        var partial = new BattleSettlementDtos.PartialWaveKill(
                "1", 1, "WAVE_01", "NORMAL_MONSTER", "EACH_FIELD", 1,
                1, 1, 1, 2);
        var invalid = signed(new BattleSettlementDtos.Request(
                "attribution-request", "attribution-session", versions.getBalanceVersion(), versions.getContentHash(),
                "DEFEAT", 0, "EARTH", now.minusMinutes(1), now,
                List.of(player(a, 1, 0, 0, false), player(b, 2, 1, 0, false)),
                List.of(new BattleSettlementDtos.Monster("NORMAL_MONSTER", 1, 0, 20)),
                List.of(fact), List.of(partial), ""));

        assertThatThrownBy(() -> service.settle(invalid))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));
    }

    @Test
    void partialWaveKillWithoutMatchingSpawnFactIsRejected() {
        User a = user("missing-fact-a"), b = user("missing-fact-b");
        register("missing-fact-session", a, b, "EARTH");
        LocalDateTime now = LocalDateTime.now().withNano(0);
        var partial = new BattleSettlementDtos.PartialWaveKill(
                "1", 1, "WAVE_01", "NORMAL_MONSTER", "EACH_FIELD", 1,
                1, 1, 1, null);
        var invalid = signed(new BattleSettlementDtos.Request(
                "missing-fact", "missing-fact-session", versions.getBalanceVersion(), versions.getContentHash(),
                "DEFEAT", 0, "EARTH", now.minusMinutes(1), now,
                List.of(player(a, 1, 1, 0, false), player(b, 2, 0, 0, false)),
                List.of(new BattleSettlementDtos.Monster("NORMAL_MONSTER", 1, 0, 20)),
                List.of(), List.of(partial), ""));

        assertThatThrownBy(() -> service.settle(invalid))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));
    }

    @Test
    void partialWaveKillMustMatchCanonicalSpawnIdentity() {
        User a = user("identity-a"), b = user("identity-b");
        register("identity-session", a, b, "EARTH");
        LocalDateTime now = LocalDateTime.now().withNano(0);
        var fact = waveSpawnFact("1", 1, 1, 1);
        var mismatchedKill = new BattleSettlementDtos.PartialWaveKill(
                "1", 1, "WAVE_01", "NORMAL_MONSTER", "EACH_FIELD", 2,
                1, 1, 1, null);
        var invalid = signed(new BattleSettlementDtos.Request(
                "identity", "identity-session", versions.getBalanceVersion(), versions.getContentHash(),
                "DEFEAT", 0, "EARTH", now.minusMinutes(1), now,
                List.of(player(a, 1, 1, 0, false), player(b, 2, 0, 0, false)),
                List.of(new BattleSettlementDtos.Monster("NORMAL_MONSTER", 1, 0, 20)),
                List.of(fact), List.of(mismatchedKill), ""));

        assertThatThrownBy(() -> service.settle(invalid))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));
    }

    @Test
    void spawnedButNotKilledPartialWaveFactIsAccepted() {
        User a = user("fact-only-a"), b = user("fact-only-b");
        register("fact-only-session", a, b, "EARTH");
        LocalDateTime now = LocalDateTime.now().withNano(0);
        var request = signed(new BattleSettlementDtos.Request(
                "fact-only", "fact-only-session", versions.getBalanceVersion(), versions.getContentHash(),
                "DEFEAT", 0, "EARTH", now.minusMinutes(1), now,
                List.of(player(a, 1, 0, 0, false), player(b, 2, 0, 0, false)),
                List.of(), List.of(waveSpawnFact("1", 1, 1, 1)), List.of(), ""));

        assertThat(service.settle(request).status()).isEqualTo("ACCEPTED");
    }

    @Test
    void waveSpawnFactsMustBeStrictlyUnsignedAscending() {
        User a = user("fact-order-a"), b = user("fact-order-b");
        register("fact-order-session", a, b, "EARTH");
        LocalDateTime now = LocalDateTime.now().withNano(0);
        var request = signed(new BattleSettlementDtos.Request(
                "fact-order", "fact-order-session", versions.getBalanceVersion(), versions.getContentHash(),
                "DEFEAT", 0, "EARTH", now.minusMinutes(1), now,
                List.of(player(a, 1, 0, 0, false), player(b, 2, 0, 0, false)),
                List.of(),
                List.of(waveSpawnFact("2", 1, 1, 1), waveSpawnFact("1", 1, 1, 2)),
                List.of(), ""));

        assertThatThrownBy(() -> service.settle(request))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));
    }

    @Test
    void playerEliminatedBeforePartialWaveCannotClaimKillOrSupportAttribution() {
        User a = user("inactive-a"), b = user("inactive-b");
        register("inactive-killer-session", a, b, "EARTH");
        LocalDateTime now = LocalDateTime.now().withNano(0);
        var fact = waveSpawnFact("25", 2, 2, 1, 1);
        var inactiveKiller = new BattleSettlementDtos.PartialWaveKill(
                "25", 2, "WAVE_02", "NORMAL_MONSTER", "EACH_FIELD", 2,
                1, 1, 1, null);
        var killerRequest = signed(new BattleSettlementDtos.Request(
                "inactive-killer", "inactive-killer-session", versions.getBalanceVersion(), versions.getContentHash(),
                "DEFEAT", 1, "EARTH", now.minusMinutes(2), now,
                List.of(
                        new BattleSettlementDtos.Player(a.getUsername(), 1, true, 1, 13, 0, 0,
                                100, 0, 0, 100, false),
                        player(b, 2, 12, 0, false)),
                List.of(new BattleSettlementDtos.Monster("NORMAL_MONSTER", 25, 0, 500)),
                List.of(fact), List.of(inactiveKiller), ""));

        assertThatThrownBy(() -> service.settle(killerRequest))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));

        rosters.clearForTest();
        register("inactive-support-session", a, b, "EARTH");
        var inactiveSupport = new BattleSettlementDtos.PartialWaveKill(
                "25", 2, "WAVE_02", "NORMAL_MONSTER", "EACH_FIELD", 2,
                1, 1, 2, 1);
        var supportRequest = signed(new BattleSettlementDtos.Request(
                "inactive-support", "inactive-support-session", versions.getBalanceVersion(), versions.getContentHash(),
                "DEFEAT", 1, "EARTH", now.minusMinutes(2), now,
                List.of(
                        new BattleSettlementDtos.Player(a.getUsername(), 1, true, 1, 12, 1, 0,
                                100, 0, 0, 100, false),
                        player(b, 2, 13, 0, false)),
                List.of(new BattleSettlementDtos.Monster("NORMAL_MONSTER", 25, 0, 500)),
                List.of(fact), List.of(inactiveSupport), ""));

        assertThatThrownBy(() -> service.settle(supportRequest))
                .isInstanceOfSatisfying(BusinessException.class,
                        error -> assertThat(error.getErrorCode()).isEqualTo(ErrorCode.BATTLE_SUMMARY_INVALID));
    }

    private BattleSettlementDtos.Request valid(String requestId, String sessionId, String hash, User a, User b) {
        return valid(requestId, sessionId, hash, a, b, "DEFEAT", 1, "EARTH");
    }

    private BattleSettlementDtos.Request valid(String requestId, String sessionId, String hash, User a, User b,
                                                String result, int wave, String map) {
        return valid(requestId, sessionId, hash, a, b, result, wave, map, false, false);
    }

    private BattleSettlementDtos.Request valid(String requestId, String sessionId, String hash, User a, User b,
                                                String result, int wave, String map,
                                                boolean abandonedA, boolean abandonedB) {
        return request(requestId, sessionId, hash, a, b, result, wave, map, abandonedA, abandonedB, true);
    }

    private BattleSettlementDtos.Request request(String requestId, String sessionId, String hash, User a, User b,
                                                 String result, int wave, String map,
                                                 boolean abandonedA, boolean abandonedB, boolean register) {
        if (register) register(sessionId, a, b, map);
        Map<String, Integer> counts = expectedCounts(wave);
        List<BattleSettlementDtos.Monster> monsterSummaries = counts.entrySet().stream()
                .sorted(Map.Entry.comparingByKey())
                .map(entry -> new BattleSettlementDtos.Monster(
                        entry.getKey(), entry.getValue(),
                        entry.getKey().equals("WAVE_BOSS") ? entry.getValue() : 0,
                        monsters.getById(entry.getKey()).killGold() * entry.getValue()))
                .toList();
        int totalKills = counts.values().stream().mapToInt(Integer::intValue).sum();
        int bossKills = counts.getOrDefault("WAVE_BOSS", 0);
        int firstKills = totalKills / 2 + totalKills % 2;
        int secondKills = totalKills / 2;
        int firstBossKills = Math.min(firstKills, bossKills);
        int secondBossKills = bossKills - firstBossKills;
        LocalDateTime now = LocalDateTime.now().withNano(0);
        return signed(new BattleSettlementDtos.Request(
                requestId, sessionId, versions.getBalanceVersion(), versions.getContentHash(), result, wave, map,
                now.minusMinutes(20), now,
                List.of(player(a, 1, firstKills, firstBossKills, abandonedA),
                        player(b, 2, secondKills, secondBossKills, abandonedB)),
                monsterSummaries, List.of(), List.of(), ""));
    }

    private BattleSettlementDtos.Request signed(BattleSettlementDtos.Request request) {
        String hash = BattleSettlementSummaryHasher.compute(request);
        return new BattleSettlementDtos.Request(
                request.requestId(), request.battleSessionId(), request.balanceVersion(), request.contentHash(),
                request.result(), request.finalWave(), request.mapId(), request.startedAt(), request.finishedAt(),
                request.players(), request.monsterKills(), request.waveSpawnFacts(), request.partialWaveKills(), hash);
    }

    private Map<String, Integer> expectedCounts(int finalWave) {
        Map<String, Integer> counts = new HashMap<>();
        for (int wave = 1; wave <= Math.min(finalWave, 80); wave++) {
            var waveSpec = waves.getWave("COOP_STANDARD", wave);
            for (var spawn : waves.getSpawns(waveSpec.spawnGroupId())) {
                int lanes = spawn.lanePolicy().equals("EACH_FIELD") ? 2 : 1;
                counts.merge(spawn.monsterId(), spawn.spawnCountPerField() * lanes, Integer::sum);
            }
        }
        return counts;
    }

    private void register(String sessionId, User a, User b, String map) {
        unlockForTest(a, map);
        unlockForTest(b, map);
        battleEntries.reserve(sessionId, map, a.getId(), b.getId());
        rosters.register(sessionId, 1, a.getUsername(), map, versions.getBalanceVersion(), versions.getContentHash());
        rosters.register(sessionId, 2, b.getUsername(), map, versions.getBalanceVersion(), versions.getContentHash());
    }

    private void unlockForTest(User user, String map) {
        if (!planetUnlocks.existsByUserIdAndMapId(user.getId(), map)) {
            planetUnlocks.saveAndFlush(new UserPlanetUnlock(
                    user, map, PlanetUnlockSource.PREVIOUS_PLANET_CLEAR, "TEST_FIXTURE"));
        }
    }

    private BattleSettlementDtos.Player player(User user, int slot, int kills, int bossKills, boolean abandoned) {
        return new BattleSettlementDtos.Player(
                user.getUsername(), slot, false, null, kills, 0, bossKills,
                100, 0, 0, 100, abandoned);
    }

    private BattleSettlementDtos.WaveSpawnFact waveSpawnFact(
            String runtimeMonsterId, int fieldOwnerPlayerSlot, int spawnOrder, int spawnOrdinal) {
        return waveSpawnFact(runtimeMonsterId, 1, fieldOwnerPlayerSlot, spawnOrder, spawnOrdinal);
    }

    private BattleSettlementDtos.WaveSpawnFact waveSpawnFact(
            String runtimeMonsterId, int spawnWave, int fieldOwnerPlayerSlot, int spawnOrder, int spawnOrdinal) {
        return new BattleSettlementDtos.WaveSpawnFact(
                runtimeMonsterId, spawnWave, String.format("WAVE_%02d", spawnWave),
                "NORMAL_MONSTER", "EACH_FIELD",
                fieldOwnerPlayerSlot, spawnOrder, spawnOrdinal);
    }

    private User user(String name) {
        return users.save(new User(name, "pw"));
    }
}
