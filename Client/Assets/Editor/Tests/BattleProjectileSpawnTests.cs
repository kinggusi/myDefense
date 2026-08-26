using MyDefense.Battle.Balance;
using MyDefense.Battle.Combat;
using MyDefense.Battle.Presentation;
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

}
