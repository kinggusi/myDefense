package com.denfense.server.service;

import com.denfense.server.domain.*;
import com.denfense.server.repository.*;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.Executors;

import static org.assertj.core.api.Assertions.assertThat;

@SpringBootTest
class QuestSettlementProcessorIntegrationTest {
    private final List<Long> createdUserIds = new ArrayList<>();
    @Autowired QuestSettlementProcessor processor;
    @Autowired QuestProgressRepository progresses;
    @Autowired QuestSettlementApplicationRepository applications;
    @Autowired BattlePlayerSettlementRepository playerSettlements;
    @Autowired BattleSettlementRepository settlements;
    @Autowired UserRepository users;

    @AfterEach
    void cleanup() {
        applications.deleteAllInBatch();
        progresses.deleteAllInBatch();
        playerSettlements.deleteAllInBatch();
        settlements.deleteAllInBatch();
        users.deleteAllByIdInBatch(createdUserIds);
        createdUserIds.clear();
    }

    @Test
    void productionFailedSettlementAppliesPartialTotalsExactlyOnce() {
        User first = saveUser("quest-first");
        User second = saveUser("quest-second");
        BattleSettlement settlement = settlement("quest-production", SessionSource.PRODUCTION, BattleResult.DEFEAT, 2);
        settlements.saveAndFlush(settlement);
        playerSettlements.save(new BattlePlayerSettlement(
                settlement, first, 1, false, null, 7, 1, 0,
                100, 20, 10, 110, false));
        playerSettlements.saveAndFlush(new BattlePlayerSettlement(
                settlement, second, 2, true, 3, 5, 2, 0,
                100, 20, 10, 110, false));

        QuestSettlementProcessor.ProcessResult firstResult = processor.process(settlement.getId());
        QuestSettlementProcessor.ProcessResult retryResult = processor.process(settlement.getId());

        assertThat(firstResult.excludedBySource()).isFalse();
        assertThat(firstResult.applicationCount()).isEqualTo(8);
        assertThat(retryResult.applicationCount()).isZero();
        assertThat(applications.count()).isEqualTo(8);
        assertProgress(first, QuestSettlementProcessor.MATCH_PARTICIPATION, 1);
        assertProgress(first, QuestSettlementProcessor.WAVE_CLEARED, 2);
        assertProgress(first, QuestSettlementProcessor.MONSTER_KILL, 7);
        assertProgress(first, QuestSettlementProcessor.SUPPORT_KILL, 1);
        assertProgress(second, QuestSettlementProcessor.MONSTER_KILL, 5);
        assertProgress(second, QuestSettlementProcessor.SUPPORT_KILL, 2);
    }

    @Test
    void localAndValidationSettlementsNeverMutatePermanentQuestProgress() {
        assertExcluded("quest-local", SessionSource.LOCAL_DEVELOPMENT);
        assertExcluded("quest-fixture", SessionSource.VALIDATION_FIXTURE);

        assertThat(progresses.count()).isZero();
        assertThat(applications.count()).isZero();
    }

    @Test
    void concurrentProductionProcessingStillAppliesEachConditionOnce() throws Exception {
        User first = saveUser("quest-race-first");
        User second = saveUser("quest-race-second");
        BattleSettlement settlement = settlement("quest-race", SessionSource.PRODUCTION, BattleResult.DEFEAT, 2);
        settlements.saveAndFlush(settlement);
        playerSettlements.save(new BattlePlayerSettlement(
                settlement, first, 1, false, null, 7, 1, 0,
                100, 20, 10, 110, false));
        playerSettlements.saveAndFlush(new BattlePlayerSettlement(
                settlement, second, 2, false, null, 5, 2, 0,
                100, 20, 10, 110, false));

        CountDownLatch ready = new CountDownLatch(2);
        CountDownLatch start = new CountDownLatch(1);
        var executor = Executors.newFixedThreadPool(2);
        try {
            var calls = java.util.List.of(
                    executor.submit(() -> processWhenReleased(settlement.getId(), ready, start)),
                    executor.submit(() -> processWhenReleased(settlement.getId(), ready, start)));
            ready.await();
            start.countDown();
            int applied = 0;
            for (var call : calls) applied += call.get().applicationCount();
            assertThat(applied).isEqualTo(8);
        } finally {
            executor.shutdownNow();
        }

        assertThat(applications.count()).isEqualTo(8);
        assertProgress(first, QuestSettlementProcessor.MONSTER_KILL, 7);
        assertProgress(second, QuestSettlementProcessor.SUPPORT_KILL, 2);
    }

    @Test
    void abandonedProductionParticipantIsExcludedFromQuestProgress() {
        User abandoned = saveUser("quest-abandoned");
        User eligible = saveUser("quest-eligible");
        BattleSettlement settlement = settlement("quest-abandon", SessionSource.PRODUCTION, BattleResult.DEFEAT, 2);
        settlements.saveAndFlush(settlement);
        playerSettlements.save(new BattlePlayerSettlement(
                settlement, abandoned, 1, true, 2, 7, 1, 0,
                100, 20, 10, 110, true));
        playerSettlements.saveAndFlush(new BattlePlayerSettlement(
                settlement, eligible, 2, false, null, 5, 2, 0,
                100, 20, 10, 110, false));

        QuestSettlementProcessor.ProcessResult result = processor.process(settlement.getId());

        assertThat(result.applicationCount()).isEqualTo(4);
        assertThat(progresses.findByUserIdAndQuestConditionId(
                abandoned.getId(), QuestSettlementProcessor.MATCH_PARTICIPATION)).isEmpty();
        assertProgress(eligible, QuestSettlementProcessor.MATCH_PARTICIPATION, 1);
    }

    private QuestSettlementProcessor.ProcessResult processWhenReleased(
            Long settlementId, CountDownLatch ready, CountDownLatch start) throws InterruptedException {
        ready.countDown();
        start.await();
        return processor.process(settlementId);
    }

    private void assertExcluded(String key, SessionSource source) {
        User first = saveUser(key + "-first");
        User second = saveUser(key + "-second");
        BattleSettlement settlement = settlement(key, source, BattleResult.DEFEAT, 2);
        settlements.saveAndFlush(settlement);
        playerSettlements.save(new BattlePlayerSettlement(
                settlement, first, 1, false, null, 7, 1, 0,
                100, 20, 10, 110, false));
        playerSettlements.saveAndFlush(new BattlePlayerSettlement(
                settlement, second, 2, false, null, 5, 2, 0,
                100, 20, 10, 110, false));

        assertThat(processor.process(settlement.getId()).excludedBySource()).isTrue();
    }

    private BattleSettlement settlement(String key, SessionSource source, BattleResult result, int finalWave) {
        LocalDateTime startedAt = LocalDateTime.of(2026, 9, 2, 10, 0);
        return new BattleSettlement(
                key + "-session", key + "-request", key + "-hash",
                "balance", "content", result, finalWave, "NEPTUNE", source,
                startedAt, startedAt.plusMinutes(5));
    }

    private User saveUser(String username) {
        User user = users.saveAndFlush(new User(username, "pw"));
        createdUserIds.add(user.getId());
        return user;
    }

    private void assertProgress(User user, String conditionId, long expected) {
        assertThat(progresses.findByUserIdAndQuestConditionId(user.getId(), conditionId))
                .get().extracting(QuestProgress::getProgress).isEqualTo(expected);
    }
}
