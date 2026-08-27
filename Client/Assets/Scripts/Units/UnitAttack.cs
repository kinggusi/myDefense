using UnityEngine;
using MyDefense.Shared.Contracts;
using MyDefense.Battle;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Combat;
using Fusion;

public class UnitAttack : MonoBehaviour, IAlienAttackSnapshotConsumer
{
    [Header("Battle Settings")]
    public float range = 100f;      // Legacy fallback until a calculated snapshot is supplied.
    public float fireRate = 1f;    
    public GameObject bulletPrefab;

    [SerializeField] private float targetSearchInterval = 0.2f;
    private float nextTargetSearchTime;

    private float fireCountdown = 0f;
    private Transform target;
    private UnitData cachedUnitData;

    private AlienAttackSnapshot attackSnapshot;
    private bool hasAttackSnapshot;
    private BattleWaveStateAuthority battleAuthority;
    private BattleProjectileSpawner projectileSpawner;
    private bool missingProjectilePipelineLogged;

    private void Awake()
    {
        cachedUnitData = GetComponent<UnitData>();
        battleAuthority = FindFirstObjectByType<BattleWaveStateAuthority>();
        projectileSpawner = FindFirstObjectByType<BattleProjectileSpawner>();
    }

    private void OnDisable()
    {
        target = null;
        nextTargetSearchTime = 0f;
    }

    public void ApplyAttackSnapshot(AlienAttackSnapshot snapshot)
    {
        if (cachedUnitData == null)
        {
            Debug.LogError("[UnitAttack] ApplyAttackSnapshot rejected: cachedUnitData is null.");
            return;
        }

        if (snapshot.AttackerServerId != cachedUnitData.serverId)
        {
            Debug.LogError($"[UnitAttack] ApplyAttackSnapshot rejected: AttackerServerId ({snapshot.AttackerServerId}) does not match serverId ({cachedUnitData.serverId}).");
            return;
        }

        if (snapshot.Damage <= 0f || snapshot.AttackRate <= 0f || snapshot.Range <= 0f)
        {
            Debug.LogError($"[UnitAttack] ApplyAttackSnapshot rejected: invalid stats (Damage:{snapshot.Damage}, AttackRate:{snapshot.AttackRate}, Range:{snapshot.Range}).");
            return;
        }

        attackSnapshot = snapshot;
        hasAttackSnapshot = true;
        Debug.Log($"[UnitAttack] Attack snapshot applied. Damage:{snapshot.Damage}, AttackRate:{snapshot.AttackRate}, Range:{snapshot.Range}, Mutation:{snapshot.ActiveMutationType}");
    }

    private bool IsSnapshotValid()
    {
        if (!hasAttackSnapshot) return false;
        if (cachedUnitData == null) return false;
        if (attackSnapshot.AttackerServerId != cachedUnitData.serverId) return false;

        string currentMutation = string.IsNullOrEmpty(cachedUnitData.activeMutationType) ? "NONE" : cachedUnitData.activeMutationType;
        string snapshotMutation = string.IsNullOrEmpty(attackSnapshot.ActiveMutationType) ? "NONE" : attackSnapshot.ActiveMutationType;

        return currentMutation.Equals(snapshotMutation, System.StringComparison.OrdinalIgnoreCase);
    }

    void Update()
    {
        // In a Fusion battle only State Authority searches targets and fires.
        // The resulting NetworkObject projectile is replicated for peers to render.

        // Only Alien objects with UnitData may attack.
        if (cachedUnitData == null || !gameObject.activeInHierarchy || !enabled)
        {
            target = null;
            return;
        }

        if (battleAuthority != null && battleAuthority.IsSpawnedForAccess && !battleAuthority.IsAuthoritative)
        {
            target = null;
            return;
        }

        // Fusion combat must never fall back to prefab/legacy values. The
        // User/System server calculates these stats and the Battle entry
        // snapshot provider applies them before this unit may attack.
        if (battleAuthority != null && battleAuthority.IsSpawnedForAccess && !IsSnapshotValid())
        {
            target = null;
            return;
        }

        // A Legendary material waiting for Mythic choice remains on the board,
        // but cannot attack until the authoritative choice is resolved.
        if (IsAttackSuppressedByMythicChoice())
        {
            target = null;
            return;
        }

        // Drop inactive, destroyed, or out-of-range targets.
        float currentRange = Mathf.Max(0f, IsSnapshotValid() ? attackSnapshot.Range : range);
        if (target != null)
        {
            if (!target.gameObject.activeInHierarchy || Vector3.Distance(transform.position, target.position) > currentRange)
            {
                target = null;
            }
        }

        // Throttle target searches while no valid target exists.
        if (target == null)
        {
            if (Time.time >= nextTargetSearchTime)
            {
                UpdateTarget(currentRange);
                // Clamp the interval to prevent a search every frame.
                float interval = Mathf.Max(0.05f, targetSearchInterval);
                nextTargetSearchTime = Time.time + interval;
            }
        }

        fireCountdown -= Time.deltaTime;

        if (target != null && fireCountdown <= 0f)
        {
            Shoot();
            float currentFireRate = Mathf.Max(0.01f, IsSnapshotValid() ? attackSnapshot.AttackRate : fireRate);
            fireCountdown = 1f / currentFireRate;
        }
    }

    internal bool IsAttackSuppressedByMythicChoice()
    {
        if (cachedUnitData == null)
            return false;
        if (battleAuthority == null)
            battleAuthority = FindFirstObjectByType<BattleWaveStateAuthority>();
        if (battleAuthority == null)
            return false;

        int playerSlot = (int)(cachedUnitData.serverId >> 32);
        int boardSlot = (int)((cachedUnitData.serverId & uint.MaxValue) - 1L);
        return battleAuthority.IsBoardSlotLockedForMythicChoice(playerSlot, boardSlot);
    }

    void UpdateTarget(float currentRange)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Monster");
        
        if (enemies.Length == 0) {
            return;
        }

        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy) continue;

            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null && shortestDistance <= currentRange)
        {
            target = nearestEnemy.transform;
        }
    }

    void Shoot()
    {
        if (cachedUnitData == null || target == null) {
            return;
        }

        Vector3 spawnPos = transform.position + Vector3.up * 1.0f;
        if (TryShootAuthoritativeProjectile(spawnPos))
            return;

        // Non-Fusion/offline fixtures retain the legacy local projectile path.
        if (battleAuthority != null && battleAuthority.IsSpawnedForAccess)
            return;
        if (bulletPrefab == null)
            return;
        GameObject bulletGO = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Bullet bullet = bulletGO.GetComponent<Bullet>();
        if (bullet != null)
        {
            float finalDamage = IsSnapshotValid() ? attackSnapshot.Damage : bullet.damage;

            DamagePayload payload = new DamagePayload
            {
                AttackerId = cachedUnitData.serverId,
                Amount = finalDamage,
                IsCritical = false
            };
            bullet.SetDamagePayload(payload);
            bullet.Seek(target);
        }
    }

    private bool TryShootAuthoritativeProjectile(Vector3 spawnPosition)
    {
        if (battleAuthority == null)
            battleAuthority = FindFirstObjectByType<BattleWaveStateAuthority>();
        if (battleAuthority == null || !battleAuthority.IsSpawnedForAccess || !battleAuthority.IsAuthoritative)
            return false;

        projectileSpawner ??= FindFirstObjectByType<BattleProjectileSpawner>();
        BattleWaveExecutor executor = battleAuthority.Executor;
        NetworkObject targetObject = target == null ? null : target.GetComponentInParent<NetworkObject>();
        if (projectileSpawner == null || targetObject == null || !targetObject.IsValid || executor == null
            || !executor.TryGetCanonicalBasicProjectile(cachedUnitData.specId, out ProjectileSpecData projectileSpec))
        {
            if (!missingProjectilePipelineLogged)
            {
                missingProjectilePipelineLogged = true;
                Debug.LogError("[UnitAttack] Canonical Fusion projectile pipeline is unavailable.");
            }
            return false;
        }

        if (!IsSnapshotValid())
            return false;
        AlienAttackSnapshot snapshot = attackSnapshot;
        string sessionId = executor.RuntimeSession?.BattleSessionId;
        Vector3 direction = (target.position - spawnPosition).normalized;
        bool spawned = projectileSpawner.TrySpawnFromAttackSnapshot(
            projectileSpec,
            snapshot,
            sessionId,
            spawnPosition,
            direction,
            targetObject.Id,
            out _);
        if (spawned)
            missingProjectilePipelineLogged = false;
        return spawned;
    }

}
