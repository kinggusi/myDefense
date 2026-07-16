using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienUpgrade.Core
{
    public sealed class AlienCollectionItem
    {
        public long AlienId { get; set; }
        public string Grade { get; set; }
        public int Level { get; set; }
        public int Pieces { get; set; }
        public bool Owned { get; set; }
    }

    public static class AlienCollectionOrdering
    {
        private static readonly IReadOnlyDictionary<string, int> GradeOrder =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["NORMAL"] = 0,
                ["EPIC"] = 1,
                ["UNIQUE"] = 2,
                ["LEGEND"] = 3,
                ["MYTHIC"] = 4
            };

        public static IReadOnlyList<long> OwnedAlienIds(IEnumerable<AlienCollectionItem> items)
        {
            return Order(items.Where(item => item.Owned))
                .Select(item => item.AlienId)
                .ToArray();
        }

        public static IReadOnlyList<long> LockedMythicAlienIds(IEnumerable<AlienCollectionItem> items)
        {
            return Order(items.Where(item =>
                    !item.Owned && string.Equals(item.Grade, "MYTHIC", StringComparison.OrdinalIgnoreCase)))
                .Select(item => item.AlienId)
                .ToArray();
        }

        private static IOrderedEnumerable<AlienCollectionItem> Order(IEnumerable<AlienCollectionItem> items)
        {
            return items
                .OrderBy(item => GradeOrder.TryGetValue(item.Grade ?? string.Empty, out int rank)
                    ? rank
                    : int.MaxValue)
                .ThenBy(GradeSequence);
        }

        private static long GradeSequence(AlienCollectionItem item)
        {
            if (string.Equals(item.Grade, "NORMAL", StringComparison.OrdinalIgnoreCase))
            {
                return item.AlienId - 21;
            }
            if (string.Equals(item.Grade, "EPIC", StringComparison.OrdinalIgnoreCase))
            {
                return item.AlienId - 14;
            }
            if (string.Equals(item.Grade, "UNIQUE", StringComparison.OrdinalIgnoreCase))
            {
                return item.AlienId - 7;
            }
            if (string.Equals(item.Grade, "LEGEND", StringComparison.OrdinalIgnoreCase))
            {
                return item.AlienId;
            }
            if (string.Equals(item.Grade, "MYTHIC", StringComparison.OrdinalIgnoreCase))
            {
                return item.AlienId - 28;
            }

            return item.AlienId;
        }
    }
}
