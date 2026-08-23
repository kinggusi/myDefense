using System;

namespace MyDefense.Shared.Contracts
{
    [Serializable]
    public struct AlienAttackSnapshot
    {
        public long AttackerServerId;
        public float Damage;
        public float AttackRate;
        public float Range;
        public string ActiveMutationType;
        public float SplashRadius;
        public float SplashDamageMultiplier;
        public float BossDamageMultiplier;
        public float DotDamagePerTick;
        public int DotTickCount;
        public float DotTickIntervalSeconds;
        public float SlowMultiplier;
        public float SlowDurationSeconds;
        public int GoldPerHit;
        public float GambleSuccessChance;
        public float GambleSuccessMultiplier;
        public float GambleFailureMultiplier;

        /// <summary>Builds a snapshot from values already calculated by StatCalculator.</summary>
        public static AlienAttackSnapshot FromCalculatedStats(long attackerServerId, float damage,
            float attackRate, float range, string activeMutationType)
        {
            if (attackerServerId <= 0) throw new ArgumentOutOfRangeException(nameof(attackerServerId));
            if (damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) throw new ArgumentOutOfRangeException(nameof(damage));
            if (attackRate <= 0f || float.IsNaN(attackRate) || float.IsInfinity(attackRate)) throw new ArgumentOutOfRangeException(nameof(attackRate));
            if (range <= 0f || float.IsNaN(range) || float.IsInfinity(range)) throw new ArgumentOutOfRangeException(nameof(range));
            return new AlienAttackSnapshot
            {
                AttackerServerId = attackerServerId,
                Damage = damage,
                AttackRate = attackRate,
                Range = range,
                ActiveMutationType = string.IsNullOrWhiteSpace(activeMutationType) ? "NONE" : activeMutationType,
                BossDamageMultiplier = 1f,
                SlowMultiplier = 1f,
                GambleSuccessMultiplier = 1f,
                GambleFailureMultiplier = 1f
            };
        }
    }
}
