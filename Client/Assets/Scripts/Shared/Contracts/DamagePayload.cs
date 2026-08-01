using System;

namespace MyDefense.Shared.Contracts
{
    [Serializable]
    public struct DamagePayload
    {
        public string BattleSessionId;
        public ulong RuntimeProjectileId;
        public ulong TargetRuntimeId;
        public long AttackerId;
        public float Amount;
        public bool IsCritical;
        public string ActiveMutationType;

        public bool IsFinitePositive()
        {
            return Amount > 0f && !float.IsNaN(Amount) && !float.IsInfinity(Amount);
        }
    }
}
