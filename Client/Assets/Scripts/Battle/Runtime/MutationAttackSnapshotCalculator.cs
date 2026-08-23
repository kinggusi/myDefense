using System;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Shared.Contracts;

namespace MyDefense.Battle.Runtime
{
    /// <summary>
    /// User/System-owned calculation boundary for active Mutation effects.
    /// Battle code consumes the calculated snapshot and never duplicates these values.
    /// </summary>
    public static class MutationAttackSnapshotCalculator
    {
        public static AlienAttackSnapshot Apply(
            AlienAttackSnapshot baseSnapshot,
            CanonicalMutationSpec mutation)
        {
            if (mutation == null || !mutation.Enabled
                || string.Equals(baseSnapshot.ActiveMutationType, "NONE", StringComparison.OrdinalIgnoreCase))
                return baseSnapshot;
            if (!string.Equals(baseSnapshot.ActiveMutationType, mutation.MutationType, StringComparison.Ordinal))
                throw new ArgumentException("Snapshot mutationType does not match the canonical MutationSpec.", nameof(mutation));

            baseSnapshot.Damage *= mutation.AttackMultiplier;
            baseSnapshot.AttackRate *= mutation.AttackSpeedMultiplier;
            baseSnapshot.Range *= mutation.RangeMultiplier;
            baseSnapshot.SplashRadius = mutation.SplashRadius;
            baseSnapshot.SplashDamageMultiplier = mutation.SplashDamageMultiplier;
            baseSnapshot.BossDamageMultiplier = mutation.BossDamageMultiplier;
            baseSnapshot.DotDamagePerTick = baseSnapshot.Damage * mutation.DotDamageMultiplier;
            baseSnapshot.DotTickCount = mutation.DotTickCount;
            baseSnapshot.DotTickIntervalSeconds = mutation.DotTickIntervalSeconds;
            baseSnapshot.SlowMultiplier = mutation.SlowMultiplier;
            baseSnapshot.SlowDurationSeconds = mutation.SlowDurationSeconds;
            baseSnapshot.GoldPerHit = mutation.GoldPerHit;
            baseSnapshot.GambleSuccessChance = mutation.GambleSuccessChance;
            baseSnapshot.GambleSuccessMultiplier = mutation.GambleSuccessMultiplier;
            baseSnapshot.GambleFailureMultiplier = mutation.GambleFailureMultiplier;
            return baseSnapshot;
        }

        public static float ResolveDeterministicDamage(
            AlienAttackSnapshot snapshot,
            ulong runtimeProjectileId,
            bool targetIsBoss)
        {
            float damage = snapshot.Damage;
            if (targetIsBoss)
                damage *= Math.Max(1f, snapshot.BossDamageMultiplier);
            if (snapshot.GambleSuccessChance > 0f)
            {
                uint sample = unchecked((uint)(runtimeProjectileId * 2654435761UL));
                float normalized = sample / (float)uint.MaxValue;
                damage *= normalized < snapshot.GambleSuccessChance
                    ? Math.Max(0.0001f, snapshot.GambleSuccessMultiplier)
                    : Math.Max(0.0001f, snapshot.GambleFailureMultiplier);
            }
            return damage;
        }
    }
}
