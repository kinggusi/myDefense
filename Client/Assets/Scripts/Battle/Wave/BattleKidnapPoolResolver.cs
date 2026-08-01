using System.Collections.Generic;
using MyDefense.Battle.Balance.Canonical;

namespace MyDefense.Battle
{
    public static class BattleKidnapPoolResolver
    {
        public const int TotalKidnapWeight = 10000;
        public const int MutationInjectorWeight = 50;

        public readonly struct KidnapResult
        {
            public readonly long AlienId;
            public readonly byte GradeCode;
            public readonly string MutationType;
            public bool IsInjector => !string.IsNullOrWhiteSpace(MutationType);

            public KidnapResult(long alienId, byte gradeCode, string mutationType)
            {
                AlienId = alienId; GradeCode = gradeCode; MutationType = mutationType;
            }
        }

        public static bool TrySelect(
            IReadOnlyDictionary<string, CanonicalSummonPool> pools,
            string alienPoolId,
            IReadOnlyList<CanonicalInjectorPoolEntry> injectorPool,
            ulong seed,
            out KidnapResult result)
        {
            result = default;
            uint roll = Mix(seed) % TotalKidnapWeight;
            if (roll < MutationInjectorWeight)
            {
                if (TrySelectInjector(injectorPool, seed ^ 0xA24BAED4963EE407UL, out string mutationType))
                {
                    result = new KidnapResult(0, byte.MaxValue, mutationType);
                    return true;
                }
                return false;
            }
            if (!TrySelect(pools, alienPoolId, seed ^ 0x9E3779B97F4A7C15UL, out long alienId, out byte gradeCode)) return false;
            result = new KidnapResult(alienId, gradeCode, null);
            return true;
        }

        private static bool TrySelectInjector(IReadOnlyList<CanonicalInjectorPoolEntry> entries, ulong seed, out string mutationType)
        {
            mutationType = null;
            if (entries == null || entries.Count == 0) return false;
            int total = 0;
            foreach (CanonicalInjectorPoolEntry entry in entries)
                if (entry != null && entry.Active && entry.Weight > 0) total = checked(total + entry.Weight);
            if (total <= 0) return false;
            int pick = (int)(Mix(seed) % (uint)total);
            foreach (CanonicalInjectorPoolEntry entry in entries)
            {
                if (entry == null || !entry.Active || entry.Weight <= 0) continue;
                if (pick < entry.Weight) { mutationType = entry.MutationType; return true; }
                pick -= entry.Weight;
            }
            return false;
        }

        public static bool TrySelectForcedInjector(
            IReadOnlyList<CanonicalInjectorPoolEntry> entries,
            ulong seed,
            out KidnapResult result)
        {
            result = default;
            if (!TrySelectInjector(entries, seed, out string mutationType)) return false;
            result = new KidnapResult(0, byte.MaxValue, mutationType);
            return true;
        }

        public static bool TrySelect(IReadOnlyDictionary<string, CanonicalSummonPool> pools, string poolId, ulong seed, out long alienId, out byte gradeCode)
        {
            alienId = 0;
            gradeCode = 0;
            if (pools == null || string.IsNullOrWhiteSpace(poolId) || !pools.TryGetValue(poolId, out CanonicalSummonPool pool) || pool == null || !pool.Active)
                return false;
            int totalWeight = 0;
            foreach (CanonicalSummonPoolEntry entry in pool.Entries)
            {
                if (entry == null || entry.Weight <= 0 || entry.AlienIds == null || entry.AlienIds.Count == 0) continue;
                totalWeight = checked(totalWeight + entry.Weight);
            }
            if (totalWeight <= 0) return false;
            int weightPick = (int)(Mix(seed) % (uint)totalWeight);
            foreach (CanonicalSummonPoolEntry entry in pool.Entries)
            {
                if (entry == null || entry.Weight <= 0 || entry.AlienIds == null || entry.AlienIds.Count == 0) continue;
                if (weightPick < entry.Weight)
                {
                    int index = (int)(Mix(seed ^ 0x9E3779B97F4A7C15UL) % (uint)entry.AlienIds.Count);
                    alienId = entry.AlienIds[index];
                    gradeCode = GradeCode(entry.Grade);
                    return alienId > 0;
                }
                weightPick -= entry.Weight;
            }
            return false;
        }

        private static byte GradeCode(string grade)
        {
            switch (grade)
            {
                case "NORMAL": return 0;
                case "EPIC": return 1;
                case "UNIQUE": return 2;
                case "LEGEND": return 3;
                case "MYTHIC": return 4;
                default: return byte.MaxValue;
            }
        }

        private static uint Mix(ulong value)
        {
            value ^= value >> 30;
            value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27;
            value *= 0x94D049BB133111EBUL;
            value ^= value >> 31;
            return (uint)value;
        }
    }
}
