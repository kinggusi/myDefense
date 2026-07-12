using System;

namespace MyDefense.Shared.Contracts
{
    [Serializable]
    public struct DamagePayload
    {
        public long AttackerId;
        public float Amount;
        public bool IsCritical;
    }
}
