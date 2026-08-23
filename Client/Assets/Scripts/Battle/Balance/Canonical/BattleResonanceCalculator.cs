using System;

namespace MyDefense.Battle.Balance.Canonical
{
    public readonly struct BattleResonanceStats
    {
        public float Damage { get; }
        public float AttackRate { get; }
        public float Range { get; }

        public BattleResonanceStats(float damage, float attackRate, float range)
        {
            Damage = damage;
            AttackRate = attackRate;
            Range = range;
        }
    }

    /// <summary>
    /// User/System-owned pure calculator. Battle supplies the current merge grade
    /// and applies the calculated values without duplicating the balance formula.
    /// </summary>
    public static class BattleResonanceCalculator
    {
        public static CanonicalResonanceTrack TrackForGrade(byte grade)
            => grade == 4 ? CanonicalResonanceTrack.MYTHIC : CanonicalResonanceTrack.NORMAL;

        public static bool TryGetNextCost(
            CanonicalResonanceRegistry registry,
            CanonicalResonanceTrack track,
            int currentLevel,
            out int cost)
        {
            cost = 0;
            if (registry == null || currentLevel < 0 || currentLevel >= CanonicalResonanceRegistry.MaxLevel)
                return false;
            if (!registry.TryGet(track, currentLevel + 1, out CanonicalResonanceLevel next))
                return false;
            cost = next.RequiredGold;
            return true;
        }

        public static bool TryPurchaseNextLevel(
            CanonicalResonanceRegistry registry,
            CanonicalResonanceTrack track,
            int currentLevel,
            int currentGold,
            out int nextLevel,
            out int remainingGold)
        {
            nextLevel = currentLevel;
            remainingGold = currentGold;
            if (currentGold < 0 || !TryGetNextCost(registry, track, currentLevel, out int cost)
                || currentGold < cost)
                return false;

            nextLevel = currentLevel + 1;
            remainingGold = currentGold - cost;
            return true;
        }

        public static BattleResonanceStats Apply(
            CanonicalResonanceRegistry registry,
            byte grade,
            int normalLevel,
            int mythicLevel,
            float baseDamage,
            float baseAttackRate,
            float baseRange)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (!IsFinitePositive(baseDamage)) throw new ArgumentOutOfRangeException(nameof(baseDamage));
            if (!IsFinitePositive(baseAttackRate)) throw new ArgumentOutOfRangeException(nameof(baseAttackRate));
            if (!IsFinitePositive(baseRange)) throw new ArgumentOutOfRangeException(nameof(baseRange));
            if (normalLevel < 0 || normalLevel > CanonicalResonanceRegistry.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(normalLevel));
            if (mythicLevel < 0 || mythicLevel > CanonicalResonanceRegistry.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(mythicLevel));

            CanonicalResonanceTrack track = TrackForGrade(grade);
            int level = track == CanonicalResonanceTrack.MYTHIC ? mythicLevel : normalLevel;
            if (level == 0)
                return new BattleResonanceStats(baseDamage, baseAttackRate, baseRange);
            if (!registry.TryGet(track, level, out CanonicalResonanceLevel balance))
                throw new InvalidOperationException("Canonical resonance balance is incomplete for " + track + " level " + level + ".");

            return new BattleResonanceStats(
                baseDamage * balance.AttackMultiplier,
                baseAttackRate * balance.AttackSpeedMultiplier,
                baseRange * balance.RangeMultiplier);
        }

        private static bool IsFinitePositive(float value)
            => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
