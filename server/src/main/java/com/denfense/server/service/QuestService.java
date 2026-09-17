package com.denfense.server.service;

import com.denfense.server.balance.*;
import com.denfense.server.domain.*;
import com.denfense.server.dto.QuestDtos;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.*;
import com.denfense.server.service.balance.QuestBalanceRegistry;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Instant;
import java.util.*;
import java.util.function.Function;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class QuestService {
    private final UserRepository users;
    private final QuestCycleProgressRepository progresses;
    private final QuestProgressRepository permanentProgresses;
    private final QuestRewardClaimRepository claims;
    private final QuestBalanceRegistry balances;
    private final QuestTimeProvider time;

    @Transactional(readOnly = true)
    public QuestDtos.BoardResponse getBoard(String username) {
        User user = requireUser(username);
        Instant serverTime = time.now().toInstant();
        return new QuestDtos.BoardResponse(user.getUsername(), serverTime,
                cycle(user, QuestCycleType.DAILY), cycle(user, QuestCycleType.WEEKLY),
                achievements(user), wallet(user));
    }

    @Transactional
    public QuestDtos.ClaimResponse claimQuest(QuestDtos.QuestClaimRequest request) {
        User user = lockUser(request.username());
        QuestBalance quest = requireQuest(request.questId());
        String cycleKey = time.cycleKey(quest.cycleType());
        String rewardKey = questRewardKey(quest, cycleKey);
        QuestRewardClaim existing = resolveExistingRequest(user, request.requestId(), rewardKey);
        if (existing != null) return response(existing, user, true);
        QuestRewardClaim existingReward = claims.findByUserIdAndRewardKey(user.getId(), rewardKey).orElse(null);
        if (existingReward != null) return response(existingReward, user, true);

        long progress = progresses.findByUserIdAndQuestIdAndCycleKey(user.getId(), quest.questId(), cycleKey)
                .map(QuestCycleProgress::getProgress).orElse(0L);
        if (progress < quest.targetAmount()) throw new BusinessException(ErrorCode.QUEST_NOT_COMPLETED);
        return grant(user, rewardKey, request.requestId(), reward(quest), false);
    }

    @Transactional
    public QuestDtos.ClaimResponse claimMilestone(QuestDtos.MilestoneClaimRequest request) {
        User user = lockUser(request.username());
        QuestMilestoneBalance milestone = requireMilestone(request.cycleType(), request.requiredActivityPoints());
        String cycleKey = time.cycleKey(request.cycleType());
        String rewardKey = milestoneRewardKey(request.cycleType(), cycleKey, milestone.requiredActivityPoints());
        QuestRewardClaim existing = resolveExistingRequest(user, request.requestId(), rewardKey);
        if (existing != null) return response(existing, user, true);
        QuestRewardClaim existingReward = claims.findByUserIdAndRewardKey(user.getId(), rewardKey).orElse(null);
        if (existingReward != null) return response(existingReward, user, true);
        if (activityPoints(user, request.cycleType(), cycleKey) < milestone.requiredActivityPoints()) {
            throw new BusinessException(ErrorCode.QUEST_MILESTONE_LOCKED);
        }
        return grant(user, rewardKey, request.requestId(), reward(milestone), false);
    }

    @Transactional
    public QuestDtos.ClaimResponse claimAchievement(QuestDtos.AchievementClaimRequest request) {
        User user = lockUser(request.username());
        AchievementBalance achievement = requireAchievement(request.achievementId());
        String rewardKey = achievementRewardKey(achievement.achievementId());
        QuestRewardClaim existing = resolveExistingRequest(user, request.requestId(), rewardKey);
        if (existing != null) return response(existing, user, true);
        QuestRewardClaim existingReward = claims.findByUserIdAndRewardKey(user.getId(), rewardKey).orElse(null);
        if (existingReward != null) return response(existingReward, user, true);

        long progress = permanentProgresses
                .findByUserIdAndQuestConditionId(user.getId(), achievement.conditionId())
                .map(QuestProgress::getProgress).orElse(0L);
        if (progress < achievement.targetAmount()) {
            throw new BusinessException(ErrorCode.ACHIEVEMENT_NOT_COMPLETED);
        }
        return grant(user, rewardKey, request.requestId(), reward(achievement), false);
    }

    private QuestDtos.CycleResponse cycle(User user, QuestCycleType type) {
        String cycleKey = time.cycleKey(type);
        Map<String, QuestCycleProgress> progressByQuest = progresses.findAllByUserIdAndCycleKey(user.getId(), cycleKey)
                .stream().collect(Collectors.toMap(QuestCycleProgress::getQuestId, Function.identity()));
        Set<String> claimedKeys = claims.findAllByUserIdAndRewardKeyStartingWith(user.getId(), rewardPrefix(type, cycleKey))
                .stream().map(QuestRewardClaim::getRewardKey).collect(Collectors.toSet());
        List<QuestDtos.QuestItem> questItems = balances.quests(type).stream().map(quest -> {
            long progress = Optional.ofNullable(progressByQuest.get(quest.questId()))
                    .map(QuestCycleProgress::getProgress).orElse(0L);
            return new QuestDtos.QuestItem(quest.questId(), quest.title(), quest.description(),
                    Math.min(progress, quest.targetAmount()), quest.targetAmount(), quest.activityPoints(), reward(quest),
                    progress >= quest.targetAmount(), claimedKeys.contains(questRewardKey(quest, cycleKey)));
        }).toList();
        int activity = questItems.stream().filter(QuestDtos.QuestItem::completed)
                .mapToInt(QuestDtos.QuestItem::activityPoints).sum();
        List<QuestDtos.MilestoneItem> milestones = balances.milestones(type).stream()
                .map(value -> new QuestDtos.MilestoneItem(value.requiredActivityPoints(), reward(value),
                        activity >= value.requiredActivityPoints(),
                        claimedKeys.contains(milestoneRewardKey(type, cycleKey, value.requiredActivityPoints()))))
                .toList();
        return new QuestDtos.CycleResponse(type, cycleKey, time.nextResetAt(type), activity, questItems, milestones);
    }

    private int activityPoints(User user, QuestCycleType type, String cycleKey) {
        Map<String, Long> progress = progresses.findAllByUserIdAndCycleKey(user.getId(), cycleKey).stream()
                .collect(Collectors.toMap(QuestCycleProgress::getQuestId, QuestCycleProgress::getProgress));
        return balances.quests(type).stream()
                .filter(quest -> progress.getOrDefault(quest.questId(), 0L) >= quest.targetAmount())
                .mapToInt(QuestBalance::activityPoints).sum();
    }

    private List<QuestDtos.AchievementItem> achievements(User user) {
        Map<String, Long> progressByCondition = permanentProgresses.findAllByUserId(user.getId()).stream()
                .collect(Collectors.toMap(QuestProgress::getQuestConditionId, QuestProgress::getProgress));
        Set<String> claimedKeys = claims.findAllByUserIdAndRewardKeyStartingWith(user.getId(), "ACHIEVEMENT:")
                .stream().map(QuestRewardClaim::getRewardKey).collect(Collectors.toSet());
        return balances.achievements().stream().map(achievement -> {
            long progress = progressByCondition.getOrDefault(achievement.conditionId(), 0L);
            return new QuestDtos.AchievementItem(achievement.achievementId(), achievement.category(), achievement.tier(),
                    achievement.title(), achievement.description(), Math.min(progress, achievement.targetAmount()),
                    achievement.targetAmount(), reward(achievement), progress >= achievement.targetAmount(),
                    claimedKeys.contains(achievementRewardKey(achievement.achievementId())));
        }).toList();
    }

    private QuestDtos.ClaimResponse grant(User user, String rewardKey, String requestId,
                                           QuestDtos.Reward reward, boolean alreadyProcessed) {
        user.earnGold(reward.gold());
        user.earnUniversalPiece(reward.universalPiece());
        user.earnDiamond(reward.diamond());
        QuestRewardClaim claim = claims.saveAndFlush(new QuestRewardClaim(user, rewardKey, requestId.trim(),
                reward.gold(), reward.universalPiece(), reward.diamond(), time.now().toLocalDateTime()));
        return response(claim, user, alreadyProcessed);
    }

    private QuestRewardClaim resolveExistingRequest(User user, String requestId, String rewardKey) {
        QuestRewardClaim claim = claims.findByUserIdAndRequestId(user.getId(), requestId.trim()).orElse(null);
        if (claim != null && !claim.getRewardKey().equals(rewardKey)) {
            throw new BusinessException(ErrorCode.QUEST_REQUEST_CONFLICT);
        }
        return claim;
    }

    private QuestDtos.ClaimResponse response(QuestRewardClaim claim, User user, boolean alreadyProcessed) {
        return new QuestDtos.ClaimResponse(claim.getRewardKey(),
                new QuestDtos.Reward(claim.getRewardGold(), claim.getRewardUniversalPiece(), claim.getRewardDiamond()),
                wallet(user), alreadyProcessed);
    }

    private QuestDtos.Wallet wallet(User user) {
        return new QuestDtos.Wallet(user.getGold(), user.getUniversalPiece(), user.getDiamond());
    }

    private QuestDtos.Reward reward(QuestBalance quest) {
        return new QuestDtos.Reward(quest.rewardGold(), quest.rewardUniversalPiece(), quest.rewardDiamond());
    }

    private QuestDtos.Reward reward(QuestMilestoneBalance milestone) {
        return new QuestDtos.Reward(milestone.rewardGold(), milestone.rewardUniversalPiece(), milestone.rewardDiamond());
    }

    private QuestDtos.Reward reward(AchievementBalance achievement) {
        return new QuestDtos.Reward(achievement.rewardGold(), achievement.rewardUniversalPiece(),
                achievement.rewardDiamond());
    }

    private QuestBalance requireQuest(String questId) {
        try {
            return balances.requireQuest(questId.trim());
        } catch (IllegalArgumentException exception) {
            throw new BusinessException(ErrorCode.QUEST_NOT_FOUND);
        }
    }

    private QuestMilestoneBalance requireMilestone(QuestCycleType type, int points) {
        try {
            return balances.requireMilestone(type, points);
        } catch (IllegalArgumentException exception) {
            throw new BusinessException(ErrorCode.QUEST_NOT_FOUND);
        }
    }

    private AchievementBalance requireAchievement(String achievementId) {
        try {
            return balances.requireAchievement(achievementId.trim());
        } catch (IllegalArgumentException exception) {
            throw new BusinessException(ErrorCode.ACHIEVEMENT_NOT_FOUND);
        }
    }

    private User requireUser(String username) {
        return users.findByUsername(username.trim()).orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND));
    }

    private User lockUser(String username) {
        return users.findByUsernameForUpdate(username.trim()).orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND));
    }

    private static String rewardPrefix(QuestCycleType type, String cycleKey) {
        return type.name() + ":" + cycleKey + ":";
    }

    private static String questRewardKey(QuestBalance quest, String cycleKey) {
        return rewardPrefix(quest.cycleType(), cycleKey) + "QUEST:" + quest.questId();
    }

    private static String milestoneRewardKey(QuestCycleType type, String cycleKey, int points) {
        return rewardPrefix(type, cycleKey) + "MILESTONE:" + points;
    }

    private static String achievementRewardKey(String achievementId) {
        return "ACHIEVEMENT:" + achievementId;
    }
}
