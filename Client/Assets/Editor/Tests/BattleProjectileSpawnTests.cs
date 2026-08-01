using MyDefense.Battle.Balance;
using MyDefense.Battle.Combat;
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

}
