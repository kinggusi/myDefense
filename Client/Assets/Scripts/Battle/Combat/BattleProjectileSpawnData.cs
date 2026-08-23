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
        public Vector3 Origin;
        public Vector3 Direction;
        public NetworkId TargetNetworkId;
    }
}
