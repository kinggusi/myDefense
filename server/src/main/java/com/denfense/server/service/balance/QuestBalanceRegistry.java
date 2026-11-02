package com.denfense.server.service.balance;

import com.denfense.server.balance.QuestBalance;
import com.denfense.server.balance.AchievementBalance;
import com.denfense.server.balance.QuestBalanceDocument;
import com.denfense.server.balance.QuestCycleType;
import com.denfense.server.balance.QuestMilestoneBalance;
import org.springframework.stereotype.Component;

import java.util.Comparator;
import java.util.List;

@Component
public class QuestBalanceRegistry {
    private List<QuestBalance> quests = List.of();
    private List<QuestMilestoneBalance> milestones = List.of();
    private List<AchievementBalance> achievements = List.of();

    public synchronized void init(QuestBalanceDocument document) {
        if (!quests.isEmpty() || !milestones.isEmpty() || !achievements.isEmpty()) {
            throw new IllegalStateException("Quest Balance is already initialized.");
        }
        quests = document.quests().stream()
                .filter(QuestBalance::enabled)
                .sorted(Comparator.comparing(QuestBalance::cycleType)
                        .thenComparingInt(QuestBalance::sortOrder))
                .toList();
        milestones = document.milestones().stream()
                .filter(QuestMilestoneBalance::enabled)
                .sorted(Comparator.comparing(QuestMilestoneBalance::cycleType)
                        .thenComparingInt(QuestMilestoneBalance::sortOrder))
                .toList();
        achievements = document.achievements().stream()
                .filter(AchievementBalance::enabled)
                .sorted(Comparator.comparingInt(AchievementBalance::sortOrder))
                .toList();
    }

    public List<QuestBalance> quests(QuestCycleType cycleType) {
        return quests.stream().filter(quest -> quest.cycleType() == cycleType).toList();
    }

    public List<QuestBalance> questsByCondition(String conditionId) {
        return quests.stream().filter(quest -> quest.conditionId().equals(conditionId)).toList();
    }

    public QuestBalance requireQuest(String questId) {
        return quests.stream().filter(quest -> quest.questId().equals(questId)).findFirst()
                .orElseThrow(() -> new IllegalArgumentException("Unknown Quest: " + questId));
    }

    public List<QuestMilestoneBalance> milestones(QuestCycleType cycleType) {
        return milestones.stream().filter(milestone -> milestone.cycleType() == cycleType).toList();
    }

    public QuestMilestoneBalance requireMilestone(QuestCycleType cycleType, int points) {
        return milestones(cycleType).stream()
                .filter(milestone -> milestone.requiredActivityPoints() == points)
                .findFirst()
                .orElseThrow(() -> new IllegalArgumentException("Unknown Quest milestone: " + cycleType + ":" + points));
    }

    public List<AchievementBalance> achievements() {
        return achievements;
    }

    public AchievementBalance requireAchievement(String achievementId) {
        return achievements.stream().filter(value -> value.achievementId().equals(achievementId)).findFirst()
                .orElseThrow(() -> new IllegalArgumentException("Unknown Achievement: " + achievementId));
    }
}
