package com.denfense.server.service;

import com.denfense.server.balance.QuestCycleType;
import com.denfense.server.domain.QuestCycleProgress;
import com.denfense.server.domain.QuestProgress;
import com.denfense.server.domain.User;
import com.denfense.server.dto.QuestDtos;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.QuestCycleProgressRepository;
import com.denfense.server.repository.QuestRewardClaimRepository;
import com.denfense.server.repository.QuestProgressRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.QuestBalanceRegistry;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import static org.assertj.core.api.Assertions.*;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.Executors;

@SpringBootTest
class QuestServiceIntegrationTest {
    @Autowired QuestService service;
    @Autowired QuestTimeProvider time;
    @Autowired QuestBalanceRegistry balances;
    @Autowired QuestCycleProgressRepository progresses;
    @Autowired QuestRewardClaimRepository claims;
    @Autowired QuestProgressRepository permanentProgresses;
    @Autowired UserRepository users;

    @AfterEach
    void cleanup() {
        claims.deleteAllInBatch();
        progresses.deleteAllInBatch();
        permanentProgresses.deleteAllInBatch();
        users.findByUsername("quest-service-user").ifPresent(users::delete);
    }

    @Test
    void boardContainsDailyAndWeeklyDefinitionsAndKstReset() {
        User user = user();

        QuestDtos.BoardResponse response = service.getBoard(user.getUsername());

        assertThat(response.daily().quests()).hasSize(4);
        assertThat(response.weekly().quests()).hasSize(4);
        assertThat(response.daily().milestones()).extracting(QuestDtos.MilestoneItem::requiredActivityPoints)
                .containsExactly(25, 50, 75, 100);
        assertThat(response.weekly().milestones()).extracting(QuestDtos.MilestoneItem::requiredActivityPoints)
                .containsExactly(25, 50, 75, 100);
        assertThat(response.achievements()).hasSize(27);
        assertThat(response.achievements()).extracting(QuestDtos.AchievementItem::achievementId)
                .contains("ACH_BATTLE_001", "ACH_PLANET_SUN");
        assertThat(response.daily().nextResetAt()).isAfter(response.serverTime());
        assertThat(response.weekly().nextResetAt()).isAfter(response.serverTime());
    }

    @Test
    void completedQuestAndMilestoneRewardsAreEachGrantedOnce() {
        User user = user();
        var quest = balances.requireQuest("DAILY_PLAY_1");
        QuestCycleProgress progress = new QuestCycleProgress(
                user, quest.questId(), QuestCycleType.DAILY, time.cycleKey(QuestCycleType.DAILY));
        progress.add(1);
        progresses.saveAndFlush(progress);

        QuestDtos.ClaimResponse questClaim = service.claimQuest(
                new QuestDtos.QuestClaimRequest("quest-request", user.getUsername(), quest.questId()));
        QuestDtos.ClaimResponse retry = service.claimQuest(
                new QuestDtos.QuestClaimRequest("quest-request", user.getUsername(), quest.questId()));
        QuestDtos.ClaimResponse duplicate = service.claimQuest(
                new QuestDtos.QuestClaimRequest("quest-request-other", user.getUsername(), quest.questId()));
        QuestDtos.ClaimResponse milestone = service.claimMilestone(
                new QuestDtos.MilestoneClaimRequest("milestone-request", user.getUsername(), QuestCycleType.DAILY, 25));

        assertThat(questClaim.alreadyProcessed()).isFalse();
        assertThat(retry.alreadyProcessed()).isTrue();
        assertThat(duplicate.alreadyProcessed()).isTrue();
        assertThat(milestone.alreadyProcessed()).isFalse();
        assertThat(claims.count()).isEqualTo(2);
        assertThat(users.findById(user.getId()).orElseThrow().getGold()).isEqualTo(1000);
    }

    @Test
    void incompleteRewardAndReusedRequestForAnotherRewardAreRejected() {
        User user = user();
        assertThatThrownBy(() -> service.claimQuest(
                new QuestDtos.QuestClaimRequest("incomplete", user.getUsername(), "DAILY_WIN_1")))
                .isInstanceOfSatisfying(BusinessException.class,
                        exception -> assertThat(exception.getErrorCode()).isEqualTo(ErrorCode.QUEST_NOT_COMPLETED));

        var quest = balances.requireQuest("DAILY_PLAY_1");
        QuestCycleProgress progress = new QuestCycleProgress(
                user, quest.questId(), QuestCycleType.DAILY, time.cycleKey(QuestCycleType.DAILY));
        progress.add(1);
        progresses.saveAndFlush(progress);
        service.claimQuest(new QuestDtos.QuestClaimRequest("reused", user.getUsername(), quest.questId()));

        assertThatThrownBy(() -> service.claimMilestone(
                new QuestDtos.MilestoneClaimRequest("reused", user.getUsername(), QuestCycleType.DAILY, 25)))
                .isInstanceOfSatisfying(BusinessException.class,
                        exception -> assertThat(exception.getErrorCode()).isEqualTo(ErrorCode.QUEST_REQUEST_CONFLICT));
    }

    @Test
    void concurrentClaimsGrantRewardOnlyOnce() throws Exception {
        User user = user();
        var quest = balances.requireQuest("DAILY_PLAY_1");
        QuestCycleProgress progress = new QuestCycleProgress(
                user, quest.questId(), QuestCycleType.DAILY, time.cycleKey(QuestCycleType.DAILY));
        progress.add(1);
        progresses.saveAndFlush(progress);
        CountDownLatch ready = new CountDownLatch(2);
        CountDownLatch start = new CountDownLatch(1);
        var executor = Executors.newFixedThreadPool(2);
        try {
            var first = executor.submit(() -> claimWhenReleased(user.getUsername(), "concurrent-a", ready, start));
            var second = executor.submit(() -> claimWhenReleased(user.getUsername(), "concurrent-b", ready, start));
            ready.await();
            start.countDown();
            assertThat(first.get().rewardKey()).isEqualTo(second.get().rewardKey());
        } finally {
            executor.shutdownNow();
        }
        assertThat(claims.count()).isEqualTo(1);
        assertThat(users.findById(user.getId()).orElseThrow().getGold()).isEqualTo(500);
    }

    @Test
    void permanentAchievementProgressUnlocksEveryReachedTierAndRewardIsGrantedOnce() {
        User user = user();
        QuestProgress progress = new QuestProgress(user, QuestSettlementProcessor.MATCH_PARTICIPATION);
        progress.add(10);
        permanentProgresses.saveAndFlush(progress);

        QuestDtos.BoardResponse board = service.getBoard(user.getUsername());
        assertThat(board.achievements().stream()
                .filter(value -> value.achievementId().startsWith("ACH_BATTLE_")).toList())
                .extracting(QuestDtos.AchievementItem::completed)
                .containsExactly(true, true, false);

        QuestDtos.ClaimResponse first = service.claimAchievement(new QuestDtos.AchievementClaimRequest(
                "achievement-request", user.getUsername(), "ACH_BATTLE_001"));
        QuestDtos.ClaimResponse retry = service.claimAchievement(new QuestDtos.AchievementClaimRequest(
                "achievement-request", user.getUsername(), "ACH_BATTLE_001"));
        QuestDtos.ClaimResponse duplicate = service.claimAchievement(new QuestDtos.AchievementClaimRequest(
                "achievement-request-2", user.getUsername(), "ACH_BATTLE_001"));

        assertThat(first.alreadyProcessed()).isFalse();
        assertThat(retry.alreadyProcessed()).isTrue();
        assertThat(duplicate.alreadyProcessed()).isTrue();
        assertThat(claims.count()).isEqualTo(1);
        assertThat(users.findById(user.getId()).orElseThrow().getGold()).isEqualTo(1000);
    }

    @Test
    void incompleteAchievementAndUnknownAchievementAreRejected() {
        User user = user();
        assertThatThrownBy(() -> service.claimAchievement(new QuestDtos.AchievementClaimRequest(
                "achievement-incomplete", user.getUsername(), "ACH_WIN_001")))
                .isInstanceOfSatisfying(BusinessException.class,
                        exception -> assertThat(exception.getErrorCode())
                                .isEqualTo(ErrorCode.ACHIEVEMENT_NOT_COMPLETED));
        assertThatThrownBy(() -> service.claimAchievement(new QuestDtos.AchievementClaimRequest(
                "achievement-unknown", user.getUsername(), "UNKNOWN")))
                .isInstanceOfSatisfying(BusinessException.class,
                        exception -> assertThat(exception.getErrorCode()).isEqualTo(ErrorCode.ACHIEVEMENT_NOT_FOUND));
    }

    private QuestDtos.ClaimResponse claimWhenReleased(String username, String requestId,
                                                       CountDownLatch ready, CountDownLatch start) throws Exception {
        ready.countDown();
        start.await();
        return service.claimQuest(new QuestDtos.QuestClaimRequest(requestId, username, "DAILY_PLAY_1"));
    }

    private User user() {
        return users.saveAndFlush(new User("quest-service-user", "pw"));
    }
}
