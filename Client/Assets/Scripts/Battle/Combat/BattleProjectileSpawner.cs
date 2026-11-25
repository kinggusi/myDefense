using Fusion;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Presentation;
using MyDefense.Shared.Contracts;
using UnityEngine;

namespace MyDefense.Battle.Combat
{
    /// <summary>
    /// Small authority boundary for runtime projectile creation. The caller must
    /// provide a canonical ProjectileSpec; clients cannot directly spawn damage.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(950)]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class BattleProjectileSpawner : NetworkBehaviour
    {
        [SerializeField] private GameObject _projectilePrefab;
        private readonly System.Collections.Generic.HashSet<ulong> _spawnedProjectileIds = new();
        private ulong _nextRuntimeProjectileId = 1;

        public GameObject ProjectilePrefab => _projectilePrefab;
        public static event System.Action<BattleProjectileSpawner, ulong, long, Vector3> PresentationPrimaryHit;
        public static event System.Action<BattleProjectileSpawner> PresentationSessionReset;
        private readonly System.Collections.Generic.Queue<(ulong projectile, long attacker, NetworkId target, string session, ulong runtime)> _presentationHits = new();
        private readonly System.Collections.Generic.HashSet<(string session, ulong projectile, NetworkId target, ulong runtime)> _presentedHits = new();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public event System.Action<BattleProjectileNetworkState> DevelopmentProjectileSpawned;
#endif

        internal void PublishAcceptedPrimaryHit(ulong projectileId, long attackerId, NetworkId targetId,string session,ulong targetRuntimeId)
        {
            if (HasStateAuthority && Runner != null && Runner.IsRunning && PresentationPrimaryHit != null)
                RPC_PresentationPrimaryHit(projectileId, attackerId, targetId,session,targetRuntimeId);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PresentationPrimaryHit(ulong projectileId, long attackerId, NetworkId targetId,string session,ulong targetRuntimeId)
        {
            if (!string.IsNullOrWhiteSpace(session)&&targetRuntimeId!=0&&_presentedHits.Add((session,projectileId,targetId,targetRuntimeId)))
                _presentationHits.Enqueue((projectileId,attackerId,targetId,session,targetRuntimeId));
        }

        private void LateUpdate()
        {
            // Monster local-field remapping runs before this callback. A late
            // RPC for an already removed target is dropped, never fabricated.
            while (_presentationHits.Count > 0)
            {
                var hit = _presentationHits.Dequeue();
                if (Runner == null || !Runner.TryFindObject(hit.target, out NetworkObject target)
                    || target == null || !target.IsValid) continue;
                BattleMonsterNetworkState monster = target.GetComponent<BattleMonsterNetworkState>();
                if(monster==null||!monster.IsInitialized||!MatchesPresentationHitIdentity(hit.session,hit.runtime,monster.BattleSessionId.ToString(),monster.RuntimeMonsterId))continue;
                Vector3 position = monster != null && monster.TryGetPresentationPosition(out Vector3 local)
                    ? local : target.transform.position;
                position=BattleMonsterArtView.ResolveHitPoint(target.transform,position);
                try { PresentationPrimaryHit?.Invoke(this, hit.projectile, hit.attacker, position); }
                catch (System.Exception exception) { Debug.LogException(exception); }
            }
        }

        public static bool MatchesPresentationHitIdentity(string expectedSession,ulong expectedRuntime,string actualSession,ulong actualRuntime)
            =>!string.IsNullOrWhiteSpace(expectedSession)&&expectedRuntime!=0&&expectedSession==actualSession&&expectedRuntime==actualRuntime;

        public override void Spawned()
        {
            ResetPresentationSession();
            if (HasStateAuthority)
            {
                _spawnedProjectileIds.Clear();
                _nextRuntimeProjectileId = 1;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _spawnedProjectileIds.Clear();
            ResetPresentationSession();
        }

        private void ResetPresentationSession()
        {
            _presentationHits.Clear(); _presentedHits.Clear();
            try { PresentationSessionReset?.Invoke(this); }
            catch (System.Exception exception) { Debug.LogException(exception); }
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

            if (spawnedObject.TryGetComponent(out projectile))
                projectile.BindPresentationSpawner(this);
            if (projectile == null || !projectile.InitializeFromAuthority(spawnData, spec, Runner))
            {
                if (spawnedObject.IsValid)
                    Runner.Despawn(spawnedObject);
                _spawnedProjectileIds.Remove(spawnData.RuntimeProjectileId);
                projectile = null;
                return false;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevelopmentProjectileSpawned?.Invoke(projectile);
#endif
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
