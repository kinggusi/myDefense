using System;
using UnityEngine;
using Fusion;

namespace MyDefense.Battle.Combat
{
    [Serializable]
    public struct BattleProjectileSpawnData
    {
        public string ProjectileId;
        public string BattleSessionId;
        public ulong RuntimeProjectileId;
        public long AttackerServerId;
        public float Damage;
        public string ActiveMutationType;
        public Vector3 Origin;
        public Vector3 Direction;
        public NetworkId TargetNetworkId;
    }
}
