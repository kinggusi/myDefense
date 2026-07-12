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
    }
}
