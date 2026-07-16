using System;
using System.Linq;

namespace GachaShop.Core
{
    public sealed class GachaRevealState
    {
        public int TotalCards { get; private set; }
        public int RevealedCards { get; private set; }
        public bool IsRevealing { get; private set; }
        public bool IsResultVisible { get; private set; }
        public bool CanStartPurchase => !IsRevealing && !IsResultVisible;

        public void Begin(int totalCards)
        {
            if (totalCards <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCards));
            }

            TotalCards = totalCards;
            RevealedCards = 0;
            IsRevealing = true;
            IsResultVisible = true;
        }

        public void RevealNext()
        {
            if (!IsRevealing)
            {
                return;
            }

            RevealedCards = Math.Min(RevealedCards + 1, TotalCards);
            if (RevealedCards == TotalCards)
            {
                IsRevealing = false;
            }
        }

        public void Skip()
        {
            if (!IsResultVisible)
            {
                return;
            }

            RevealedCards = TotalCards;
            IsRevealing = false;
        }

        public void Close()
        {
            TotalCards = 0;
            RevealedCards = 0;
            IsRevealing = false;
            IsResultVisible = false;
        }

        public static GachaDraw[] OrderDraws(GachaDraw[] draws)
        {
            return (draws ?? Array.Empty<GachaDraw>())
                .OrderBy(draw => draw.order)
                .ToArray();
        }
    }
}
