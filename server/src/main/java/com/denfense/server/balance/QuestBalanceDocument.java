package com.denfense.server.balance;

import java.util.List;

public record QuestBalanceDocument(
        List<QuestBalance> quests,
        List<QuestMilestoneBalance> milestones,
        List<AchievementBalance> achievements
) {
}
