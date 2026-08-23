using Fusion;
using MyDefense.Battle.Balance;
using MyDefense.Shared.Contracts;
using UnityEngine;

namespace MyDefense.Battle.Combat
{
    /// <summary>
    /// Small authority boundary for runtime projectile creation. The caller must
    /// provide a canonical ProjectileSpec; clients cannot directly spawn damage.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class BattleProjectileSpawner : NetworkBehaviour
    {
        [SerializeField] private GameObject _projectilePrefab;
        private readonly System.Collections.Generic.HashSet<ulong> _spawnedProjectileIds = new();
        private ulong _nextRuntimeProjectileId = 1;

        public GameObject ProjectilePrefab => _projectilePrefab;

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                _spawnedProjectileIds.Clear();
                _nextRuntimeProjectileId = 1;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _spawnedProjectileIds.Clear();
        }

        public bool TrySpawn(
            ProjectileSpecData spec,
            BattleProjectileSpawnData spawnData,
            out BattleProjectileNetworkState projectile)
        {
            projectile = null;
            if (!HasStateAuthority || Runner == null || !Runner.IsRunning)
                return false;
            if (spawnData.RuntimeProjectileId == 0)
                spawnData.RuntimeProjectileId = _nextRuntimeProjectileId++;
            if (!_spawnedProjectileIds.Add(spawnData.RuntimeProjectileId))
                return false;
            if (!BattleProjectileSpawnValidator.TryValidate(spec, spawnData, out _))
            {
                _spawnedProjectileIds.Remove(spawnData.RuntimeProjectileId);
                return false;
            }
            if (_projectilePrefab == null)
            {
                _spawnedProjectileIds.Remove(spawnData.RuntimeProjectileId);
                return false;
            }

            if (!_projectilePrefab.TryGetComponent(out NetworkObject prefabObject))
            {
                _spawnedProjectileIds.Remove(spawnData.RuntimeProjectileId);
                return false;
            }

            Quaternion rotation = spawnData.Direction.sqrMagnitude > 0.000001f
                ? Quaternion.LookRotation(spawnData.Direction.normalized)
                : Quaternion.identity;
            NetworkObject spawnedObject = Runner.Spawn(prefabObject, spawnData.Origin, rotation);
            if (spawnedObject == null)
            {
                _spawnedProjectileIds.Remove(spawnData.RuntimeProjectileId);
                return false;
            }

            if (!spawnedObject.TryGetComponent(out projectile)
                || !projectile.InitializeFromAuthority(spawnData, spec, Runner))
            {
                if (spawnedObject.IsValid)
                    Runner.Despawn(spawnedObject);
                _spawnedProjectileIds.Remove(spawnData.RuntimeProjectileId);
                projectile = null;
                return false;
            }

            return true;
        }

        public bool TrySpawnFromAttackSnapshot(
            ProjectileSpecData spec,
            AlienAttackSnapshot attackSnapshot,
            string battleSessionId,
            Vector3 origin,
            Vector3 direction,
            NetworkId targetNetworkId,
            out BattleProjectileNetworkState projectile)
        {
            BattleProjectileSpawnData spawnData = new BattleProjectileSpawnData
            {
                ProjectileId = spec == null ? null : spec.ProjectileId,
                BattleSessionId = battleSessionId,
                AttackerServerId = attackSnapshot.AttackerServerId,
                Damage = attackSnapshot.Damage,
                ActiveMutationType = attackSnapshot.ActiveMutationType,
                SplashRadius = attackSnapshot.SplashRadius,
                SplashDamageMultiplier = attackSnapshot.SplashDamageMultiplier,
                BossDamageMultiplier = attackSnapshot.BossDamageMultiplier,
                DotDamagePerTick = attackSnapshot.DotDamagePerTick,
                DotTickCount = attackSnapshot.DotTickCount,
                DotTickIntervalSeconds = attackSnapshot.DotTickIntervalSeconds,
                SlowMultiplier = attackSnapshot.SlowMultiplier,
                SlowDurationSeconds = attackSnapshot.SlowDurationSeconds,
                GoldPerHit = attackSnapshot.GoldPerHit,
                GambleSuccessChance = attackSnapshot.GambleSuccessChance,
                GambleSuccessMultiplier = attackSnapshot.GambleSuccessMultiplier,
                GambleFailureMultiplier = attackSnapshot.GambleFailureMultiplier,
                Origin = origin,
                Direction = direction,
                TargetNetworkId = targetNetworkId
            };
            return TrySpawn(spec, spawnData, out projectile);
        }

        public void SetProjectilePrefabForTests(GameObject prefab)
        {
            _projectilePrefab = prefab;
        }
    }
}
