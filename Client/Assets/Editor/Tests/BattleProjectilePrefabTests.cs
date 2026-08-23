using Fusion;
using MyDefense.Battle.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Assert = NUnit.Framework.Assert;

public sealed class BattleProjectilePrefabTests
{
    [Test]
    public void NetworkProjectilePrefabContainsRequiredFusionComponents()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Battle/BattleProjectile.prefab");
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.TryGetComponent<NetworkObject>(out _), Is.True);
        Assert.That(prefab.TryGetComponent<NetworkTransform>(out _), Is.True);
        Assert.That(prefab.TryGetComponent<BattleProjectileNetworkState>(out _), Is.True);
    }

    [Test]
    public void BattleSceneConnectsAuthorityProjectileSpawnerToCanonicalPrefab()
    {
        const string scenePath = "Assets/Scenes/Battle.unity";
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool openedByTest = !scene.IsValid() || !scene.isLoaded;
        if (openedByTest) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            BattleProjectileSpawner[] spawners = System.Linq.Enumerable.ToArray(
                System.Linq.Enumerable.SelectMany(scene.GetRootGameObjects(), root => root.GetComponentsInChildren<BattleProjectileSpawner>(true)));
            Assert.That(spawners, Has.Length.EqualTo(1));
            Assert.That(spawners[0].GetComponent<NetworkObject>(), Is.Not.Null);
            Assert.That(spawners[0].ProjectilePrefab, Is.EqualTo(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Battle/BattleProjectile.prefab")));
        }
        finally
        {
            if (openedByTest) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
