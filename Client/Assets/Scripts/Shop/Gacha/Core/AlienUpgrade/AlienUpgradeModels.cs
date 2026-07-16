using System;

namespace AlienUpgrade.Core
{
    [Serializable]
    public sealed class AlienUpgradeStatusDto
    {
        public long alienId;
        public string alienName;
        public string grade;
        public bool owned;
        public bool specLocked;
        public int currentLevel;
        public int currentPieces;
        public int universalPiece;
        public int gold;
        public int growthCell;
        public int maxLevel;
        public bool maxLevelReached;
        public bool canUpgrade;
        public string cannotUpgradeReason;
        public int requiredPieces;
        public int requiredUniversalPiece;
        public int requiredGold;
        public int requiredGrowthCell;
        public int baseAtk;
        public int baseMp;
        public double atkSpeed;
        public double range;
        public double currentAtk;
        public double currentMp;
        public double currentAtkSpeed;
        public double currentRange;
    }

    [Serializable]
    public sealed class AlienUpgradeResponseDto
    {
        public long alienId;
        public string alienName;
        public int beforeLevel;
        public int afterLevel;
        public int requiredPieces;
        public int usedPieces;
        public int remainingPieces;
        public int usedUniversalPiece;
        public int remainingUniversalPiece;
        public int usedGold;
        public int remainingGold;
        public int usedGrowthCell;
        public int remainingGrowthCell;
        public bool maxLevelReached;
        public int maxLevel;
        public bool canUpgrade;
        public string cannotUpgradeReason;
        public int nextRequiredPieces;
        public int nextRequiredUniversalPiece;
        public int nextRequiredGold;
        public int nextRequiredGrowthCell;
        public double currentAtk;
        public double currentMp;
        public double currentAtkSpeed;
        public double currentRange;
    }

    public sealed class AlienUpgradeViewModel
    {
        public long AlienId { get; internal set; }
        public string AlienName { get; internal set; }
        public string Grade { get; internal set; }
        public int Level { get; internal set; }
        public int MaxLevel { get; internal set; }
        public int CurrentPieces { get; internal set; }
        public int UniversalPiece { get; internal set; }
        public int Gold { get; internal set; }
        public int GrowthCell { get; internal set; }
        public int RequiredPieces { get; internal set; }
        public int RequiredUniversalPiece { get; internal set; }
        public int RequiredGold { get; internal set; }
        public int RequiredGrowthCell { get; internal set; }
        public double Attack { get; internal set; }
        public double Mp { get; internal set; }
        public double AttackSpeed { get; internal set; }
        public double Range { get; internal set; }
        public bool MaxLevelReached { get; internal set; }
        public bool CanUpgrade { get; internal set; }
        public string CannotUpgradeReason { get; internal set; }
    }
}
