using System;

namespace MyDefense.Shared.Contracts
{
    /// <summary>Authority-side record of one accepted, already-calculated hit.</summary>
    [Serializable]
    public readonly struct HitEvent
    {
        public string BattleSessionId { get; }
        public ulong RuntimeProjectileId { get; }
        public ulong TargetRuntimeId { get; }
        public long AttackerId { get; }
        public DamagePayload Payload { get; }
        public long Tick { get; }

        public HitEvent(string battleSessionId, ulong runtimeProjectileId, ulong targetRuntimeId,
            long attackerId, DamagePayload payload, long tick)
        {
            if (string.IsNullOrWhiteSpace(battleSessionId)) throw new ArgumentException("Battle session is required.", nameof(battleSessionId));
            if (runtimeProjectileId == 0) throw new ArgumentOutOfRangeException(nameof(runtimeProjectileId));
            if (targetRuntimeId == 0) throw new ArgumentOutOfRangeException(nameof(targetRuntimeId));
            if (attackerId <= 0) throw new ArgumentOutOfRangeException(nameof(attackerId));
            if (!payload.IsFinitePositive()) throw new ArgumentException("Hit damage must be finite and positive.", nameof(payload));
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
            BattleSessionId = battleSessionId;
            RuntimeProjectileId = runtimeProjectileId;
            TargetRuntimeId = targetRuntimeId;
            AttackerId = attackerId;
            Payload = payload;
            Tick = tick;
        }
    }
}
