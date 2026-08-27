using System;
using System.Collections.Generic;
using Fusion;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Presentation;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using UnityEngine;

namespace MyDefense.Battle.Combat
{
    /// <summary>
    /// State-authority-owned projectile state. Clients only render the replicated
    /// transform/state; damage is applied once by the State Authority.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class BattleProjectileNetworkState : NetworkBehaviour
    {
        [Networked] public NetworkString<_32> ProjectileId { get; private set; }
        [Networked] public NetworkString<_64> BattleSessionId { get; private set; }
        [Networked] public ulong RuntimeProjectileId { get; private set; }
        [Networked] public long AttackerServerId { get; private set; }
        [Networked] public float Damage { get; private set; }
        [Networked] public NetworkString<_16> ActiveMutationType { get; private set; }
        [Networked] public float SplashRadius { get; private set; }
        [Networked] public float SplashDamageMultiplier { get; private set; }
        [Networked] public float BossDamageMultiplier { get; private set; }
        [Networked] public float DotDamagePerTick { get; private set; }
        [Networked] public int DotTickCount { get; private set; }
        [Networked] public float DotTickIntervalSeconds { get; private set; }
        [Networked] public float SlowMultiplier { get; private set; }
        [Networked] public float SlowDurationSeconds { get; private set; }
        [Networked] public int GoldPerHit { get; private set; }
        [Networked] public float GambleSuccessChance { get; private set; }
        [Networked] public float GambleSuccessMultiplier { get; private set; }
        [Networked] public float GambleFailureMultiplier { get; private set; }
        [Networked] public int PierceRemaining { get; private set; }
        [Networked] public NetworkBool DestroyOnHit { get; private set; }
        [Networked] public NetworkBool IsConsumed { get; private set; }
        [Networked] public TickTimer LifetimeTimer { get; private set; }
        [Networked] public NetworkId TargetNetworkId { get; private set; }
        [Networked] public Vector3 Direction { get; private set; }
        [Networked] public float Speed { get; private set; }
        [Networked] public float HitRadius { get; private set; }
        [Networked] public int MoveTypeValue { get; private set; }

        private readonly HashSet<NetworkId> _hitTargets = new();
        private BattleWaveStateAuthority _waveAuthority;
        private Transform _presentationAttacker;
        private bool _hasProxyPresentationPosition;
        private Vector3 _proxyPresentationPosition;
        public event Action<HitEvent> AuthoritativeHit;

        public override void Spawned()
        {
            ResetProxyPresentation();
        }

        public bool InitializeFromAuthority(
            BattleProjectileSpawnData spawnData,
            ProjectileSpecData spec,
            NetworkRunner runner)
        {
            if (!HasStateAuthority || spec == null || runner == null || !runner.IsRunning)
                return false;
            if (!BattleProjectileSpawnValidator.TryValidate(spec, spawnData, out _))
                return false;

            ProjectileId = spawnData.ProjectileId;
            BattleSessionId = spawnData.BattleSessionId;
            RuntimeProjectileId = spawnData.RuntimeProjectileId;
            AttackerServerId = spawnData.AttackerServerId;
            Damage = spawnData.Damage;
            ActiveMutationType = string.IsNullOrWhiteSpace(spawnData.ActiveMutationType) ? "NONE" : spawnData.ActiveMutationType;
            SplashRadius = spawnData.SplashRadius;
            SplashDamageMultiplier = spawnData.SplashDamageMultiplier;
            BossDamageMultiplier = Mathf.Max(1f, spawnData.BossDamageMultiplier);
            DotDamagePerTick = Mathf.Max(0f, spawnData.DotDamagePerTick);
            DotTickCount = Mathf.Max(0, spawnData.DotTickCount);
            DotTickIntervalSeconds = Mathf.Max(0f, spawnData.DotTickIntervalSeconds);
            SlowMultiplier = spawnData.SlowMultiplier <= 0f ? 1f : spawnData.SlowMultiplier;
            SlowDurationSeconds = Mathf.Max(0f, spawnData.SlowDurationSeconds);
            GoldPerHit = Mathf.Max(0, spawnData.GoldPerHit);
            GambleSuccessChance = Mathf.Clamp01(spawnData.GambleSuccessChance);
            GambleSuccessMultiplier = spawnData.GambleSuccessMultiplier <= 0f ? 1f : spawnData.GambleSuccessMultiplier;
            GambleFailureMultiplier = spawnData.GambleFailureMultiplier <= 0f ? 1f : spawnData.GambleFailureMultiplier;
            PierceRemaining = spec.PierceCount;
            DestroyOnHit = spec.DestroyOnHit;
            IsConsumed = false;
            LifetimeTimer = TickTimer.CreateFromSeconds(runner, spec.LifetimeSeconds);
            TargetNetworkId = spawnData.TargetNetworkId;
            Direction = spawnData.Direction.normalized;
            Speed = spec.Speed;
            HitRadius = spec.HitRadius;
            MoveTypeValue = (int)spec.MoveType;
            return true;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || Runner == null || !Runner.IsRunning || IsConsumed)
                return;

            if (LifetimeTimer.Expired(Runner))
            {
                ConsumeAndDespawn();
                return;
            }

            NetworkObject targetObject = null;
            if (TargetNetworkId.IsValid)
                Runner.TryFindObject(TargetNetworkId, out targetObject);

            if (targetObject != null && targetObject.IsValid)
            {
                BattleMonsterNetworkState targetMonster = targetObject.GetComponent<BattleMonsterNetworkState>();
                IDamageable damageable = targetObject.GetComponentInChildren<IDamageable>();
                if (ShouldConsumeInFlightTarget(targetMonster, damageable))
                {
                    ConsumeAndDespawn();
                    return;
                }

                Vector3 targetPosition = targetObject.transform.position;
                Vector3 delta = targetPosition - transform.position;
                if (delta.sqrMagnitude <= Mathf.Max(0.05f, HitRadius) * Mathf.Max(0.05f, HitRadius))
                {
                    TryApplyAuthoritativeHit(damageable, targetObject.Id);
                    return;
                }
                if ((ProjectileMoveType)MoveTypeValue == ProjectileMoveType.HOMING)
                    Direction = delta.normalized;
            }
            else if ((ProjectileMoveType)MoveTypeValue == ProjectileMoveType.HOMING)
            {
                ConsumeAndDespawn();
                return;
            }

            if ((ProjectileMoveType)MoveTypeValue == ProjectileMoveType.INSTANT)
            {
                if (targetObject != null)
                {
                    IDamageable damageable = targetObject.GetComponentInChildren<IDamageable>();
                    BattleMonsterNetworkState targetMonster = targetObject.GetComponent<BattleMonsterNetworkState>();
                    if (!ShouldConsumeInFlightTarget(targetMonster, damageable))
                        TryApplyAuthoritativeHit(damageable, targetObject.Id);
                    else
                        ConsumeAndDespawn();
                }
                else
                {
                    ConsumeAndDespawn();
                }
                return;
            }

            transform.position += Direction * Speed * Runner.DeltaTime;
        }

        private void LateUpdate()
        {
            if (HasStateAuthority)
                return;

            _waveAuthority ??= FindFirstObjectByType<BattleWaveStateAuthority>();
            if (_waveAuthority == null || _waveAuthority.Runner == null
                || _waveAuthority.Object == null || !_waveAuthority.Object.IsValid)
                return;

            int localSlot = _waveAuthority.GetNetworkedPlayerSlot(_waveAuthority.Runner.LocalPlayer);
            if (!RequiresLocalPerspectiveRemap(false, localSlot))
                return;

            if (!_hasProxyPresentationPosition)
            {
                _presentationAttacker ??= FindPresentationAttacker();
                if (_presentationAttacker == null)
                    return;

                _proxyPresentationPosition = _presentationAttacker.position + Vector3.up;
                _hasProxyPresentationPosition = true;
            }

            Vector3 targetPosition = ResolvePresentationTargetPosition();
            float presentationSpeed = Mathf.Max(0f, Speed);
            _proxyPresentationPosition = Vector3.MoveTowards(
                _proxyPresentationPosition,
                targetPosition,
                presentationSpeed * Time.deltaTime);
            transform.position = _proxyPresentationPosition;

            Vector3 direction = targetPosition - _proxyPresentationPosition;
            if (direction.sqrMagnitude > 0.000001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        public static bool RequiresLocalPerspectiveRemap(bool hasStateAuthority, int localPlayerSlot)
            => !hasStateAuthority && localPlayerSlot == 2;

        public static int ResolveAttackerPlayerSlot(long attackerServerId)
        {
            int playerSlot = (int)(attackerServerId >> 32);
            return playerSlot == 1 || playerSlot == 2 ? playerSlot : 0;
        }

        private Transform FindPresentationAttacker()
        {
            if (ResolveAttackerPlayerSlot(AttackerServerId) == 0)
                return null;

            FusionKidnapBoardView boardView = FusionKidnapBoardView.Active;
            return boardView != null && boardView.TryGetUnitTransform(AttackerServerId, out Transform unit)
                ? unit
                : null;
        }

        private Vector3 ResolvePresentationTargetPosition()
        {
            if (Runner == null || !TargetNetworkId.IsValid
                || !Runner.TryFindObject(TargetNetworkId, out NetworkObject targetObject)
                || targetObject == null || !targetObject.IsValid)
                return _proxyPresentationPosition + Direction;

            BattleMonsterNetworkState monster = targetObject.GetComponent<BattleMonsterNetworkState>();
            return monster != null && monster.TryGetPresentationPosition(out Vector3 presentationPosition)
                ? presentationPosition
                : targetObject.transform.position;
        }

        private void ResetProxyPresentation()
        {
            _waveAuthority = null;
            _presentationAttacker = null;
            _hasProxyPresentationPosition = false;
            _proxyPresentationPosition = default;
        }

        public bool TryApplyAuthoritativeHit(IDamageable target, NetworkId targetId, bool isCritical = false)
        {
            if (!HasStateAuthority || IsConsumed || target == null || !targetId.IsValid)
                return false;
            // A homing/basic attack is bound to the authoritative target chosen at
            // spawn time. Physics callbacks may overlap another Monster while the
            // projectile travels through a crowded lane; that collider must not
            // steal the primary hit. Explicit GIANT splash is applied separately.
            if (!IsIntendedPrimaryTarget(TargetNetworkId, targetId))
                return false;

            BattleMonsterNetworkState monster = (target as Component)?.GetComponentInParent<BattleMonsterNetworkState>();
            if (targetId.IsValid && !_hitTargets.Add(targetId))
                return false;

            ulong targetRuntimeId = monster == null ? 0UL : monster.RuntimeMonsterId;
            AlienAttackSnapshot mutationSnapshot = BuildMutationSnapshot();
            float resolvedDamage = MutationAttackSnapshotCalculator.ResolveDeterministicDamage(
                mutationSnapshot,
                RuntimeProjectileId,
                monster != null && monster.LanePolicy == BattleMonsterLanePolicy.BOSS_SHARED);
            DamagePayload payload = new DamagePayload
            {
                BattleSessionId = BattleSessionId.ToString(),
                RuntimeProjectileId = RuntimeProjectileId,
                TargetRuntimeId = targetRuntimeId,
                AttackerId = AttackerServerId,
                Amount = resolvedDamage,
                IsCritical = isCritical,
                ActiveMutationType = ActiveMutationType.ToString()
            };

            bool hasNetworkState = HasValidNetworkState(monster);
            HitTransactionPlan transaction = ResolveHitTransaction(
                target,
                hasNetworkState,
                hasNetworkState && monster.IsDead,
                payload,
                hasMutationEffect: monster != null && HasMutationStatusEffect(),
                hasSplashEffect: monster != null && SplashRadius > 0f && SplashDamageMultiplier > 0f,
                hasGoldEffect: GoldPerHit > 0,
                hasHitEventIdentity: targetRuntimeId != 0);
            if (!transaction.Accepted)
            {
                if (transaction.ConsumeCount > 0)
                    ConsumeAndDespawn();
                return false;
            }

            if (transaction.MutationCount > 0)
            {
                monster.ApplyMutationEffect(
                    DotDamagePerTick,
                    DotTickCount,
                    DotTickIntervalSeconds,
                    SlowMultiplier,
                    SlowDurationSeconds,
                    AttackerServerId,
                    ActiveMutationType.ToString());
            }
            if (transaction.SplashCount > 0)
                ApplySplashDamage(monster, payload);
            if (transaction.GoldCount > 0)
                AwardMutationHitGold();
            if (transaction.HitEventCount > 0)
            {
                try
                {
                    AuthoritativeHit?.Invoke(new HitEvent(
                        BattleSessionId.ToString(), RuntimeProjectileId, targetRuntimeId,
                        AttackerServerId, payload, Runner == null ? 0L : (long)Runner.Tick));
                }
                catch (ArgumentException)
                {
                    // Audit metadata must not undo an already-applied authoritative hit.
                }
            }

            if (PierceRemaining > 0)
                PierceRemaining--;
            if (DestroyOnHit || PierceRemaining <= 0)
                ConsumeAndDespawn();
            return true;
        }

        public static bool IsIntendedPrimaryTarget(NetworkId expectedTargetId, NetworkId collidedTargetId)
            => collidedTargetId.IsValid
                && (!expectedTargetId.IsValid || expectedTargetId == collidedTargetId);

        private static bool IsEligibleLivingTarget(
            IDamageable target,
            bool hasAuthoritativeNetworkState,
            bool authoritativeIsDead)
            => target != null
                && !target.IsDead
                && (!hasAuthoritativeNetworkState || !authoritativeIsDead);

        private static HitTransactionPlan ResolveHitTransaction(
            IDamageable target,
            bool hasAuthoritativeNetworkState,
            bool authoritativeIsDead,
            DamagePayload payload,
            bool hasMutationEffect,
            bool hasSplashEffect,
            bool hasGoldEffect,
            bool hasHitEventIdentity)
        {
            if (!IsEligibleLivingTarget(target, hasAuthoritativeNetworkState, authoritativeIsDead))
                return HitTransactionPlan.RejectedDeadTarget;

            target.ApplyDamage(payload);
            bool killedByThisHit = target.IsDead;
            return new HitTransactionPlan(
                accepted: true,
                mutationCount: !killedByThisHit && hasMutationEffect ? 1 : 0,
                splashCount: hasSplashEffect ? 1 : 0,
                goldCount: hasGoldEffect ? 1 : 0,
                hitEventCount: hasHitEventIdentity ? 1 : 0,
                consumeCount: 0);
        }

        private bool HasMutationStatusEffect()
            => (DotDamagePerTick > 0f && DotTickCount > 0 && DotTickIntervalSeconds > 0f)
                || (SlowMultiplier > 0f && SlowMultiplier < 1f && SlowDurationSeconds > 0f);

        private AlienAttackSnapshot BuildMutationSnapshot()
        {
            return new AlienAttackSnapshot
            {
                AttackerServerId = AttackerServerId,
                Damage = Damage,
                AttackRate = 1f,
                Range = 1f,
                ActiveMutationType = ActiveMutationType.ToString(),
                SplashRadius = SplashRadius,
                SplashDamageMultiplier = SplashDamageMultiplier,
                BossDamageMultiplier = BossDamageMultiplier,
                DotDamagePerTick = DotDamagePerTick,
                DotTickCount = DotTickCount,
                DotTickIntervalSeconds = DotTickIntervalSeconds,
                SlowMultiplier = SlowMultiplier,
                SlowDurationSeconds = SlowDurationSeconds,
                GoldPerHit = GoldPerHit,
                GambleSuccessChance = GambleSuccessChance,
                GambleSuccessMultiplier = GambleSuccessMultiplier,
                GambleFailureMultiplier = GambleFailureMultiplier
            };
        }

        private void ApplySplashDamage(BattleMonsterNetworkState primary, DamagePayload primaryPayload)
        {
            if (primary == null || SplashRadius <= 0f || SplashDamageMultiplier <= 0f)
                return;
            Collider[] hits = Physics.OverlapSphere(primary.transform.position, SplashRadius);
            for (int index = 0; index < hits.Length; index++)
            {
                BattleMonsterNetworkState nearby = hits[index] == null
                    ? null
                    : hits[index].GetComponentInParent<BattleMonsterNetworkState>();
                if (nearby == null || nearby == primary || nearby.Object == null || !nearby.Object.IsValid)
                    continue;
                IDamageable nearbyDamageable = nearby.GetComponentInChildren<IDamageable>();
                if (!IsEligibleLivingTarget(nearby, nearbyDamageable)
                    || !_hitTargets.Add(nearby.Object.Id))
                    continue;
                DamagePayload splash = primaryPayload;
                splash.TargetRuntimeId = nearby.RuntimeMonsterId;
                splash.Amount = Damage * SplashDamageMultiplier;
                nearbyDamageable.ApplyDamage(splash);
            }
        }

        private void AwardMutationHitGold()
        {
            if (GoldPerHit <= 0)
                return;
            BattleWaveStateAuthority authority = FindFirstObjectByType<BattleWaveStateAuthority>();
            int playerSlot = (int)(AttackerServerId >> 32);
            authority?.TryAwardMutationHitGold(playerSlot, GoldPerHit);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryApplyAuthoritativeHit(other == null ? null : other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryApplyAuthoritativeHit(collision == null || collision.collider == null
                ? null
                : collision.collider.gameObject);
        }

        private bool TryApplyAuthoritativeHit(GameObject hitObject)
        {
            if (!HasStateAuthority || IsConsumed || hitObject == null)
                return false;

            NetworkObject targetObject = hitObject.GetComponentInParent<NetworkObject>();
            if (targetObject == null || !targetObject.IsValid || targetObject == Object)
                return false;

            IDamageable target = hitObject.GetComponentInParent<IDamageable>();
            if (target == null)
                return false;

            return TryApplyAuthoritativeHit(target, targetObject.Id);
        }

        private static bool IsEligibleLivingTarget(BattleMonsterNetworkState monster, IDamageable target)
        {
            bool hasNetworkState = HasValidNetworkState(monster);
            return IsEligibleLivingTarget(
                target,
                hasNetworkState,
                hasNetworkState && monster.IsDead);
        }

        private static bool ShouldConsumeInFlightTarget(BattleMonsterNetworkState monster, IDamageable target)
        {
            bool hasNetworkState = HasValidNetworkState(monster);
            return ShouldConsumeInFlightTarget(
                target,
                hasNetworkState,
                hasNetworkState && monster.IsDead);
        }

        private static bool ShouldConsumeInFlightTarget(
            IDamageable target,
            bool hasAuthoritativeNetworkState,
            bool authoritativeIsDead)
            => !IsEligibleLivingTarget(target, hasAuthoritativeNetworkState, authoritativeIsDead);

        private static bool HasValidNetworkState(BattleMonsterNetworkState monster)
            => monster != null && monster.Object != null && monster.Object.IsValid;

        private readonly struct HitTransactionPlan
        {
            public static readonly HitTransactionPlan RejectedDeadTarget = new(
                accepted: false,
                mutationCount: 0,
                splashCount: 0,
                goldCount: 0,
                hitEventCount: 0,
                consumeCount: 1);

            public HitTransactionPlan(
                bool accepted,
                int mutationCount,
                int splashCount,
                int goldCount,
                int hitEventCount,
                int consumeCount)
            {
                Accepted = accepted;
                MutationCount = mutationCount;
                SplashCount = splashCount;
                GoldCount = goldCount;
                HitEventCount = hitEventCount;
                ConsumeCount = consumeCount;
            }

            public bool Accepted { get; }
            public int MutationCount { get; }
            public int SplashCount { get; }
            public int GoldCount { get; }
            public int HitEventCount { get; }
            public int ConsumeCount { get; }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _hitTargets.Clear();
            ResetProxyPresentation();
        }

        private void ConsumeAndDespawn()
        {
            if (!HasStateAuthority || IsConsumed)
                return;

            IsConsumed = true;
            if (Runner != null && Runner.IsRunning && Object != null && Object.IsValid)
                Runner.Despawn(Object);
        }
    }

    public static class BattleProjectileSpawnValidator
    {
        public static bool TryValidate(
            ProjectileSpecData spec,
            BattleProjectileSpawnData spawnData,
            out string error)
        {
            error = null;
            if (spec == null)
                return Fail("ProjectileSpec is required.", out error);
            if (!spec.Enabled)
                return Fail("ProjectileSpec is disabled.", out error);
            if (string.IsNullOrWhiteSpace(spawnData.ProjectileId)
                || !string.Equals(spec.ProjectileId, spawnData.ProjectileId, StringComparison.Ordinal))
                return Fail("Spawn projectileId does not match the canonical ProjectileSpec.", out error);
            if (string.IsNullOrWhiteSpace(spawnData.BattleSessionId))
                return Fail("A BattleSessionId is required.", out error);
            if (spawnData.RuntimeProjectileId == 0)
                return Fail("A unique RuntimeProjectileId is required.", out error);
            if (spawnData.Damage <= 0f || float.IsNaN(spawnData.Damage) || float.IsInfinity(spawnData.Damage))
                return Fail("Projectile damage must be finite and greater than zero.", out error);
            if (spawnData.Direction.sqrMagnitude <= 0.000001f)
                return Fail("Projectile direction must be non-zero.", out error);
            if ((spec.MoveType == ProjectileMoveType.HOMING || spec.MoveType == ProjectileMoveType.INSTANT)
                && !spawnData.TargetNetworkId.IsValid)
                return Fail("A target-bound projectile requires a valid TargetNetworkId.", out error);
            if (spawnData.SplashRadius < 0f || spawnData.SplashDamageMultiplier < 0f
                || spawnData.BossDamageMultiplier < 0f || spawnData.DotDamagePerTick < 0f
                || spawnData.DotTickCount < 0 || spawnData.DotTickIntervalSeconds < 0f
                || spawnData.SlowMultiplier < 0f || spawnData.SlowDurationSeconds < 0f
                || spawnData.GoldPerHit < 0 || spawnData.GambleSuccessChance < 0f
                || spawnData.GambleSuccessChance > 1f || spawnData.GambleSuccessMultiplier < 0f
                || spawnData.GambleFailureMultiplier < 0f)
                return Fail("Mutation projectile values are invalid.", out error);
            if (spec.Speed <= 0f && spec.MoveType != ProjectileMoveType.INSTANT)
                return Fail("A moving projectile requires a positive canonical speed.", out error);
            return true;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
