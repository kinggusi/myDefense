using System.Collections.Generic;
using AlienUpgrade.Core;
using NUnit.Framework;

namespace AlienUpgrade.Core.Tests
{
    public sealed class AlienCollectionOrderingTests
    {
        [Test]
        public void OwnedAliens_AreOrderedByFixedGradeAndSequence()
        {
            var items = new List<AlienCollectionItem>
            {
                Item(48, "MYTHIC", 50, 999, true),
                Item(7, "LEGEND", 50, 999, true),
                Item(14, "UNIQUE", 50, 999, true),
                Item(21, "EPIC", 50, 999, true),
                Item(28, "NORMAL", 50, 999, true),
                Item(22, "NORMAL", 1, 0, true),
                Item(15, "EPIC", 1, 0, true),
                Item(8, "UNIQUE", 1, 0, true),
                Item(1, "LEGEND", 1, 0, true),
                Item(29, "MYTHIC", 1, 0, true)
            };

            IReadOnlyList<long> result = AlienCollectionOrdering.OwnedAlienIds(items);

            Assert.That(result, Is.EqualTo(new long[] { 22, 28, 15, 21, 8, 14, 1, 7, 29, 48 }));
        }

        [Test]
        public void LevelAndPieces_DoNotChangeFixedOrder()
        {
            var items = new[]
            {
                Item(24, "NORMAL", 50, 999, true),
                Item(22, "NORMAL", 1, 0, true),
                Item(23, "NORMAL", 25, 500, true)
            };

            Assert.That(AlienCollectionOrdering.OwnedAlienIds(items), Is.EqualTo(new long[] { 22, 23, 24 }));
        }

        [Test]
        public void OwnedList_ExcludesEveryUnownedAlien()
        {
            var items = new[]
            {
                Item(22, "NORMAL", 1, 1, true),
                Item(23, "NORMAL", 1, 1, false),
                Item(29, "MYTHIC", 1, 1, false)
            };

            Assert.That(AlienCollectionOrdering.OwnedAlienIds(items), Is.EqualTo(new long[] { 22 }));
        }

        [Test]
        public void LockedMythicList_IncludesOnlyUnownedMythicsInSequence()
        {
            var items = new List<AlienCollectionItem>
            {
                Item(1, "LEGEND", 0, 0, false),
                Item(30, "MYTHIC", 1, 49, true),
                Item(22, "NORMAL", 0, 0, false)
            };
            for (long id = 48; id >= 29; id--)
            {
                if (id != 30)
                {
                    items.Add(Item(id, id % 2 == 0 ? "MYTHIC" : "mythic", 0, 0, false));
                }
            }

            var expected = new List<long>();
            for (long id = 29; id <= 48; id++)
            {
                if (id != 30)
                {
                    expected.Add(id);
                }
            }
            Assert.That(AlienCollectionOrdering.LockedMythicAlienIds(items), Is.EqualTo(expected));
        }

        [Test]
        public void Mythics_AppearInExactlyOneOwnedOrLockedSection()
        {
            var items = new List<AlienCollectionItem>();
            for (long id = 29; id <= 48; id++)
            {
                items.Add(Item(id, "MYTHIC", 1, 0, id % 3 == 0));
            }

            var owned = AlienCollectionOrdering.OwnedAlienIds(items);
            var locked = AlienCollectionOrdering.LockedMythicAlienIds(items);
            var combined = new HashSet<long>(owned);
            foreach (long id in locked)
            {
                Assert.That(combined.Add(id), Is.True, "Mythic must not appear in both sections: " + id);
            }
            Assert.That(combined.Count, Is.EqualTo(20));
        }

        private static AlienCollectionItem Item(long id, string grade, int level, int pieces, bool owned)
        {
            return new AlienCollectionItem
            {
                AlienId = id,
                Grade = grade,
                Level = level,
                Pieces = pieces,
                Owned = owned
            };
        }
    }
}
