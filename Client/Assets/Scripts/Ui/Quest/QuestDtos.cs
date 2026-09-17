using System;

[Serializable]
public sealed class QuestBoardResponse
{
    public string username;
    public string serverTime;
    public QuestCycleDto daily;
    public QuestCycleDto weekly;
    public QuestAchievementDto[] achievements;
    public QuestWalletDto wallet;
}

[Serializable]
public sealed class QuestCycleDto
{
    public string cycleType;
    public string cycleKey;
    public string nextResetAt;
    public int activityPoints;
    public QuestItemDto[] quests;
    public QuestMilestoneDto[] milestones;
}

[Serializable]
public sealed class QuestItemDto
{
    public string questId;
    public string title;
    public string description;
    public long progress;
    public long targetAmount;
    public int activityPoints;
    public QuestRewardDto reward;
    public bool completed;
    public bool claimed;
}

[Serializable]
public sealed class QuestMilestoneDto
{
    public int requiredActivityPoints;
    public QuestRewardDto reward;
    public bool unlocked;
    public bool claimed;
}

[Serializable]
public sealed class QuestAchievementDto
{
    public string achievementId;
    public string category;
    public int tier;
    public string title;
    public string description;
    public long progress;
    public long targetAmount;
    public QuestRewardDto reward;
    public bool completed;
    public bool claimed;
}

[Serializable]
public sealed class QuestRewardDto
{
    public int gold;
    public int universalPiece;
    public int diamond;
}

[Serializable]
public sealed class QuestWalletDto
{
    public int gold;
    public int universalPiece;
    public int diamond;
}

[Serializable]
public sealed class QuestClaimRequest
{
    public string requestId;
    public string username;
    public string questId;
}

[Serializable]
public sealed class QuestMilestoneClaimRequest
{
    public string requestId;
    public string username;
    public string cycleType;
    public int requiredActivityPoints;
}

[Serializable]
public sealed class QuestAchievementClaimRequest
{
    public string requestId;
    public string username;
    public string achievementId;
}

[Serializable]
public sealed class QuestClaimResponse
{
    public string rewardKey;
    public QuestRewardDto reward;
    public QuestWalletDto wallet;
    public bool alreadyProcessed;
}
