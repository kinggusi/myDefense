using System;
using System.Collections.Generic;
using Fusion;
using MyDefense.Battle.Balance;
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
        public event Action<HitEvent> AuthoritativeHit;

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
                Vector3 targetPosition = targetObject.transform.position;
                Vector3 delta = targetPosition - transform.position;
                if (delta.sqrMagnitude <= Mathf.Max(0.05f, HitRadius) * Mathf.Max(0.05f, HitRadius))
                {
                    IDamageable damageable = targetObject.GetComponentInChildren<IDamageable>();
                    if (damageable != null)
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
                    if (damageable != null)
                        TryApplyAuthoritativeHit(damageable, targetObject.Id);
                }
                else
                {
                    ConsumeAndDespawn();
                }
                return;
            }

            transform.position += Direction * Speed * Runner.DeltaTime;
        }

        public bool TryApplyAuthoritativeHit(IDamageable target, NetworkId targetId, bool isCritical = false)
        {
            if (!HasStateAuthority || IsConsumed || target == null || !targetId.IsValid)
                return false;
            if (targetId.IsValid && !_hitTargets.Add(targetId))
                return false;

            BattleMonsterNetworkState monster = (target as Component)?.GetComponentInParent<BattleMonsterNetworkState>();
            ulong targetRuntimeId = monster == null ? 0UL : monster.RuntimeMonsterId;
            DamagePayload payload = new DamagePayload
            {
                BattleSessionId = BattleSessionId.ToString(),
                RuntimeProjectileId = RuntimeProjectileId,
                TargetRuntimeId = targetRuntimeId,
                AttackerId = AttackerServerId,
                Amount = Damage,
                IsCritical = isCritical,
                ActiveMutationType = ActiveMutationType.ToString()
            };
            target.ApplyDamage(payload);
            if (targetRuntimeId != 0)
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

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _hitTargets.Clear();
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
