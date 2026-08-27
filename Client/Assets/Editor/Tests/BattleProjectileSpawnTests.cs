using System.Reflection;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Combat;
using MyDefense.Battle.Presentation;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleProjectileSpawnTests
{
    private static ProjectileSpecData MovingSpec(bool enabled = true)
    {
        return new ProjectileSpecData(
            "PROJ_BASIC",
            "BattleProjectile",
            ProjectileMoveType.HOMING,
            8f,
            5f,
            0.2f,
            0,
            true,
            ProjectileLostTargetPolicy.DESTROY,
            enabled);
    }

    private static BattleProjectileSpawnData ValidSpawn()
    {
        return new BattleProjectileSpawnData
        {
            ProjectileId = "PROJ_BASIC",
            BattleSessionId = "battle-test",
                RuntimeProjectileId = 1,
                AttackerServerId = 17,
                TargetNetworkId = new Fusion.NetworkId { Raw = 101 },
                Damage = 25f,
            Origin = Vector3.zero,
            Direction = Vector3.forward
        };
    }

    [Test]
    public void CanonicalSpecAndAuthorityPayloadAreRequired()
    {
        string error;
        Assert.That(BattleProjectileSpawnValidator.TryValidate(MovingSpec(), ValidSpawn(), out error), Is.True, error);
        Assert.That(BattleProjectileSpawnValidator.TryValidate(null, ValidSpawn(), out error), Is.False);
        Assert.That(error, Does.Contain("ProjectileSpec"));
    }

    [Test]
    public void DisabledOrMismatchedProjectileIsRejected()
    {
        string error;
        Assert.That(BattleProjectileSpawnValidator.TryValidate(MovingSpec(false), ValidSpawn(), out error), Is.False);
        Assert.That(error, Does.Contain("disabled"));

        BattleProjectileSpawnData mismatch = ValidSpawn();
        mismatch.ProjectileId = "UNKNOWN";
        Assert.That(BattleProjectileSpawnValidator.TryValidate(MovingSpec(), mismatch, out error), Is.False);
        Assert.That(error, Does.Contain("does not match"));
    }

    [Test]
    public void InvalidDamageSessionAndDirectionAreRejected()
    {
        string error;
        BattleProjectileSpawnData invalid = ValidSpawn();
        invalid.Damage = 0f;
        Assert.That(BattleProjectileSpawnValidator.TryValidate(MovingSpec(), invalid, out error), Is.False);
        Assert.That(error, Does.Contain("damage"));

        invalid = ValidSpawn();
        invalid.BattleSessionId = " ";
        Assert.That(BattleProjectileSpawnValidator.TryValidate(MovingSpec(), invalid, out error), Is.False);
        Assert.That(error, Does.Contain("BattleSessionId"));

        invalid = ValidSpawn();
        invalid.Direction = Vector3.zero;
            Assert.That(BattleProjectileSpawnValidator.TryValidate(MovingSpec(), invalid, out error), Is.False);
            Assert.That(error, Does.Contain("direction"));

            invalid = ValidSpawn();
            invalid.TargetNetworkId = default;
            Assert.That(BattleProjectileSpawnValidator.TryValidate(MovingSpec(), invalid, out error), Is.False);
            Assert.That(error, Does.Contain("TargetNetworkId"));
        }

    [Test]
    public void SpawnerCannotCreateProjectileWithoutStateAuthorityRunnerOrPrefab()
    {
        GameObject host = new GameObject("ProjectileSpawnerTest");
        host.AddComponent<Fusion.NetworkObject>();
        BattleProjectileSpawner spawner = host.AddComponent<BattleProjectileSpawner>();

        BattleProjectileNetworkState projectile;
        Assert.That(spawner.TrySpawn(MovingSpec(), ValidSpawn(), out projectile), Is.False);
        Assert.That(projectile, Is.Null);

        Object.DestroyImmediate(host);
    }

    [TestCase(true, 2, false)]
    [TestCase(false, 1, false)]
    [TestCase(false, 2, true)]
    [TestCase(false, 0, false)]
    public void ProjectilePerspectiveRemap_IsProxyOnlyForPlayerTwo(
        bool hasStateAuthority,
        int localPlayerSlot,
        bool expected)
    {
        Assert.That(
            BattleProjectileNetworkState.RequiresLocalPerspectiveRemap(hasStateAuthority, localPlayerSlot),
            Is.EqualTo(expected));
    }

    [Test]
    public void ProjectilePerspectiveRemap_DecodesAuthoritativeAttackerSlot()
    {
        Assert.That(BattleProjectileNetworkState.ResolveAttackerPlayerSlot((1L << 32) | 1L), Is.EqualTo(1));
        Assert.That(BattleProjectileNetworkState.ResolveAttackerPlayerSlot((2L << 32) | 24L), Is.EqualTo(2));
        Assert.That(BattleProjectileNetworkState.ResolveAttackerPlayerSlot(29L), Is.Zero);
    }

    [Test]
    public void ProjectilePerspectiveRemap_DecodesBoardUnitWithoutSceneSearch()
    {
        Assert.That(
            FusionKidnapBoardView.TryDecodeUnitServerId((2L << 32) | 24L, out int playerSlot, out int slotIndex),
            Is.True);
        Assert.That(playerSlot, Is.EqualTo(2));
        Assert.That(slotIndex, Is.EqualTo(23));
        Assert.That(FusionKidnapBoardView.TryDecodeUnitServerId((2L << 32) | 25L, out _, out _), Is.False);
        Assert.That(FusionKidnapBoardView.TryDecodeUnitServerId(1L, out _, out _), Is.False);
    }

    [Test]
    public void TargetBoundProjectile_DoesNotLetAnotherMonsterColliderStealPrimaryHit()
    {
        var intended = new Fusion.NetworkId { Raw = 101 };
        var otherMonster = new Fusion.NetworkId { Raw = 102 };

        Assert.That(BattleProjectileNetworkState.IsIntendedPrimaryTarget(intended, intended), Is.True);
        Assert.That(BattleProjectileNetworkState.IsIntendedPrimaryTarget(intended, otherMonster), Is.False);
        Assert.That(BattleProjectileNetworkState.IsIntendedPrimaryTarget(intended, default), Is.False);
        Assert.That(BattleProjectileNetworkState.IsIntendedPrimaryTarget(default, otherMonster), Is.True,
            "Untargeted legacy projectiles may still resolve their first valid collision.");
    }

    [Test]
    public void HitTransaction_PreDeadTargetHasNoDamageOrSideEffects()
    {
        var target = new FakeDamageable(0f);
        object plan = ResolveHitTransaction(target, authoritativeIsDead: true, damage: 10f);

        Assert.That(PlanValue<bool>(plan, "Accepted"), Is.False);
        Assert.That(target.ApplyCount, Is.Zero);
        Assert.That(PlanValue<int>(plan, "MutationCount"), Is.Zero);
        Assert.That(PlanValue<int>(plan, "SplashCount"), Is.Zero);
        Assert.That(PlanValue<int>(plan, "GoldCount"), Is.Zero);
        Assert.That(PlanValue<int>(plan, "HitEventCount"), Is.Zero);
        Assert.That(PlanValue<int>(plan, "ConsumeCount"), Is.EqualTo(1));
    }

    [Test]
    public void HitTransaction_LivingLethalHitRunsFatalSideEffectsExactlyOnceWithoutMutationRegistration()
    {
        var target = new FakeDamageable(5f);
        object plan = ResolveHitTransaction(target, authoritativeIsDead: false, damage: 5f);

        Assert.That(PlanValue<bool>(plan, "Accepted"), Is.True);
        Assert.That(target.ApplyCount, Is.EqualTo(1));
        Assert.That(target.IsDead, Is.True);
        Assert.That(PlanValue<int>(plan, "MutationCount"), Is.Zero);
        Assert.That(PlanValue<int>(plan, "SplashCount"), Is.EqualTo(1));
        Assert.That(PlanValue<int>(plan, "GoldCount"), Is.EqualTo(1));
        Assert.That(PlanValue<int>(plan, "HitEventCount"), Is.EqualTo(1));
        Assert.That(PlanValue<int>(plan, "ConsumeCount"), Is.Zero);
    }

    [Test]
    public void HitTransaction_LivingNonlethalHitRunsMutationAndOtherSideEffectsExactlyOnce()
    {
        var target = new FakeDamageable(10f);
        object plan = ResolveHitTransaction(target, authoritativeIsDead: false, damage: 4f);

        Assert.That(PlanValue<bool>(plan, "Accepted"), Is.True);
        Assert.That(target.ApplyCount, Is.EqualTo(1));
        Assert.That(target.IsDead, Is.False);
        Assert.That(PlanValue<int>(plan, "MutationCount"), Is.EqualTo(1));
        Assert.That(PlanValue<int>(plan, "SplashCount"), Is.EqualTo(1));
        Assert.That(PlanValue<int>(plan, "GoldCount"), Is.EqualTo(1));
        Assert.That(PlanValue<int>(plan, "HitEventCount"), Is.EqualTo(1));
        Assert.That(PlanValue<int>(plan, "ConsumeCount"), Is.Zero);
    }

    [Test]
    public void InFlightPolicy_ConsumesLocalOrAuthoritativeDeadTarget()
    {
        var deadTarget = new FakeDamageable(0f);
        var locallyLivingNetworkDeadTarget = new FakeDamageable(10f);

        Assert.That(ShouldConsumeInFlightTarget(deadTarget, false, false), Is.True);
        Assert.That(ShouldConsumeInFlightTarget(locallyLivingNetworkDeadTarget, true, true), Is.True);
        Assert.That(ShouldConsumeInFlightTarget(locallyLivingNetworkDeadTarget, true, false), Is.False);
    }

    [Test]
    public void SplashEligibility_ExcludesDeadNeighborAndKeepsLivingNeighbor()
    {
        var deadNeighbor = new FakeDamageable(0f);
        var livingNeighbor = new FakeDamageable(10f);

        Assert.That(IsEligibleLivingTarget(deadNeighbor, false, false), Is.False);
        Assert.That(IsEligibleLivingTarget(livingNeighbor, true, false), Is.True);
        Assert.That(IsEligibleLivingTarget(livingNeighbor, true, true), Is.False);
    }

    [Test]
    public void DotTickPolicy_LethalTickClearsRemainingTicksAndDoesNotScheduleTimer()
    {
        MethodInfo resolve = typeof(MyDefense.Battle.BattleMonsterNetworkState).GetMethod(
            "ResolveDotTickAfterDamage",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(resolve, Is.Not.Null);

        object plan = resolve.Invoke(null, new object[] { true, 3 });

        Assert.That(PlanValue<int>(plan, "RemainingTicks"), Is.Zero);
        Assert.That(PlanValue<bool>(plan, "ScheduleTimer"), Is.False);
        Assert.That(PlanValue<bool>(plan, "ClearAllEffects"), Is.True);
    }

    private static object ResolveHitTransaction(
        IDamageable target,
        bool authoritativeIsDead,
        float damage)
    {
        MethodInfo resolve = typeof(BattleProjectileNetworkState).GetMethod(
            "ResolveHitTransaction",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(resolve, Is.Not.Null);
        return resolve.Invoke(null, new object[]
        {
            target,
            true,
            authoritativeIsDead,
            new DamagePayload { AttackerId = 17, Amount = damage },
            true,
            true,
            true,
            true
        });
    }

    private static bool ShouldConsumeInFlightTarget(
        IDamageable target,
        bool hasAuthoritativeNetworkState,
        bool authoritativeIsDead)
    {
        MethodInfo policy = typeof(BattleProjectileNetworkState).GetMethod(
            "ShouldConsumeInFlightTarget",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[] { typeof(IDamageable), typeof(bool), typeof(bool) },
            null);
        Assert.That(policy, Is.Not.Null);
        return (bool)policy.Invoke(null, new object[]
        {
            target,
            hasAuthoritativeNetworkState,
            authoritativeIsDead
        });
    }

    private static bool IsEligibleLivingTarget(
        IDamageable target,
        bool hasAuthoritativeNetworkState,
        bool authoritativeIsDead)
    {
        MethodInfo policy = typeof(BattleProjectileNetworkState).GetMethod(
            "IsEligibleLivingTarget",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[] { typeof(IDamageable), typeof(bool), typeof(bool) },
            null);
        Assert.That(policy, Is.Not.Null);
        return (bool)policy.Invoke(null, new object[]
        {
            target,
            hasAuthoritativeNetworkState,
            authoritativeIsDead
        });
    }

    private static T PlanValue<T>(object plan, string propertyName)
    {
        Assert.That(plan, Is.Not.Null);
        PropertyInfo property = plan.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, propertyName);
        return (T)property.GetValue(plan);
    }

    private sealed class FakeDamageable : IDamageable
    {
        public FakeDamageable(float hp)
        {
            CurrentHp = hp;
        }

        public float CurrentHp { get; private set; }
        public bool IsDead => CurrentHp <= 0f;
        public int ApplyCount { get; private set; }

        public void ApplyDamage(DamagePayload payload)
        {
            ApplyCount++;
            CurrentHp = Mathf.Max(0f, CurrentHp - payload.Amount);
        }
    }

}
