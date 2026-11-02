using System;
using System.Collections.Generic;

public static class QuestClientContract
{
    public const string BoardPath = "/quests?username=";
    public const string QuestClaimPath = "/quests/claims";
    public const string MilestoneClaimPath = "/quests/milestone-claims";
    public const string AchievementClaimPath = "/quests/achievement-claims";

    public static QuestCycleDto SelectCycle(QuestBoardResponse board, bool weekly)
        => weekly ? board?.weekly : board?.daily;

    public static int CountClaimable(QuestCycleDto cycle)
    {
        if (cycle == null) return 0;
        int count = 0;
        if (cycle.quests != null)
            foreach (QuestItemDto quest in cycle.quests)
                if (quest != null && quest.completed && !quest.claimed) count++;
        if (cycle.milestones != null)
            foreach (QuestMilestoneDto milestone in cycle.milestones)
                if (milestone != null && milestone.unlocked && !milestone.claimed) count++;
        return count;
    }

    public static int CountClaimable(QuestAchievementDto[] achievements)
    {
        if (achievements == null) return 0;
        int count = 0;
        foreach (QuestAchievementDto achievement in achievements)
            if (achievement != null && achievement.completed && !achievement.claimed) count++;
        return count;
    }

    public static string FormatReward(QuestRewardDto reward)
    {
        if (reward == null) return "보상 없음";
        var parts = new List<string>(3);
        if (reward.gold > 0) parts.Add("Gold " + reward.gold.ToString("N0"));
        if (reward.universalPiece > 0) parts.Add("왹져 DNA " + reward.universalPiece.ToString("N0"));
        if (reward.diamond > 0) parts.Add("Gem " + reward.diamond.ToString("N0"));
        return parts.Count == 0 ? "보상 없음" : string.Join(" / ", parts);
    }

    public static string FormatProgress(long progress, long target)
        => Math.Min(Math.Max(0L, progress), Math.Max(0L, target)).ToString("N0") + " / " + Math.Max(0L, target).ToString("N0");

    public static string FormatReset(string resetIso, string serverTimeIso)
    {
        if (!DateTimeOffset.TryParse(resetIso, out DateTimeOffset reset)
            || !DateTimeOffset.TryParse(serverTimeIso, out DateTimeOffset serverTime))
            return "서버 기준";

        TimeSpan remaining = reset - serverTime;
        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
        return remaining.TotalDays >= 1
            ? ((int)remaining.TotalDays) + "일 " + remaining.Hours + "시간 후"
            : remaining.Hours + "시간 " + remaining.Minutes + "분 후";
    }

    public static string NewRequestId() => Guid.NewGuid().ToString("N");
}
