using System;

namespace GachaShop.Core
{
    [Serializable]
    public sealed class GachaPurchaseResponse
    {
        public string productId;
        public string currencyType;
        public int price;
        public int remainingDiamond;
        public int drawCount;
        public GachaDraw[] draws;
        public GachaReward[] rewards;
    }

    [Serializable]
    public sealed class GachaDraw
    {
        public int order;
        public long alienId;
        public string grade;
    }

    [Serializable]
    public sealed class GachaReward
    {
        public long alienId;
        public string grade;
        public int occurrenceCount;
        public bool newlyUnlocked;
        public int piecesAdded;
        public int currentLevel;
        public int currentPieces;
    }

    public enum GachaPurchaseFailureKind
    {
        Fatal,
        Retryable
    }

    public sealed class GachaPurchaseViewModel
    {
        public int RemainingDiamond { get; }
        public string Summary { get; }
        public string Details { get; }

        public GachaPurchaseViewModel(int remainingDiamond, string summary, string details)
        {
            RemainingDiamond = remainingDiamond;
            Summary = summary;
            Details = details;
        }
    }
}
