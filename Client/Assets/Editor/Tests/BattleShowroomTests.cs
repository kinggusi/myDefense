using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using MyDefense.Battle;
using MyDefense.Battle.Combat;
using MyDefense.Battle.Runtime;
using MyDefense.Battle.Showroom;
using MyDefenseGame.Editor.FeatureTesting;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Assert = NUnit.Framework.Assert;

namespace MyDefenseGame.Editor.Tests
{
    public sealed class BattleShowroomTests
    {
        [TestCase(BattleShowroomScenes.CharacterId, BattleShowroomScenes.Character, typeof(BattleShowroomCharacter))]
        [TestCase(BattleShowroomScenes.CombatId, BattleShowroomScenes.Combat, typeof(BattleShowroomCombat))]
        [TestCase(BattleShowroomScenes.ContentId, BattleShowroomScenes.Content, typeof(BattleShowroomContent))]
        public void EachShowroomHasItsOwnHubSceneAndRemainsOutsideProductionSettings(
            string taskId, string path, Type controllerType)
        {
            FeatureTestCase entry = FeatureTestCatalog.CreateDefault().Cases.Single(item => item.TaskId == taskId);
            Assert.That(entry.ScenePath, Is.EqualTo(path));
            Assert.That(BattleShowroomScenes.IsShowroomScene(path), Is.True);
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == path), Is.False);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null);

            Scene scene = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                Assert.That(components, Has.None.Null, "Showroom scenes must not contain missing scripts.");
                Assert.That(components.Count(component => component.GetType() == controllerType), Is.EqualTo(1));
                Assert.That(components.OfType<BattleShowroomWindow>().Count(), Is.EqualTo(1));
                Assert.That(components.OfType<BattleAutoSessionStarter>(), Is.Empty);
                Assert.That(components.OfType<BattleSceneSessionAdapter>(), Is.Empty);
                Assert.That(components.OfType<MyDefense.Battle.Presentation.FusionKidnapBoardView>(), Is.Empty);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void CombatTemplateHasOneRealAuthorityExecutorAndProjectilePipeline()
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(BattleShowroomScenes.Combat);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                var executor = roots.SelectMany(root => root.GetComponentsInChildren<BattleWaveExecutor>(true)).Single();
                var path = roots.SelectMany(root => root.GetComponentsInChildren<WaypointGroup>(true)).ToArray();
                Assert.That(executor.gameObject.activeSelf, Is.False);
                Assert.That(executor.GetComponent<NetworkObject>(), Is.Not.Null);
                Assert.That(executor.GetComponent<BattleWaveStateAuthority>(), Is.Not.Null);
                Assert.That(executor.GetComponent<BattleProjectileSpawner>().ProjectilePrefab, Is.Not.Null);
                var serialized = new SerializedObject(executor);
                Assert.That(serialized.FindProperty("_autoStartOnPlay").boolValue, Is.False);
                var spawn = (Transform)serialized.FindProperty("_spawnPoint").objectReferenceValue;
                Assert.That(spawn, Is.Not.Null);
                Assert.That(spawn.gameObject.scene, Is.EqualTo(scene));
                Assert.That(path.Select(item => item.Lane), Is.EquivalentTo(Enum.GetValues(typeof(LaneType))));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void SceneScopedPreferencesReloadAndResetIndependently()
        {
            string character = "CharacterTest-" + Guid.NewGuid().ToString("N");
            string other = "ContentTest-" + Guid.NewGuid().ToString("N");
            try
            {
                var original = new BattleShowroomCharacter.Settings();
                var chosen = new BattleShowroomCharacter.Settings { alienId = "33", rotation = 20f, battleAngle = true };
                BattleShowroomPreferences.Save(character, chosen);
                var restored = BattleShowroomPreferences.Load<BattleShowroomCharacter.Settings>(character);
                Assert.That(restored.alienId, Is.EqualTo(chosen.alienId));
                Assert.That(restored.rotation, Is.EqualTo(chosen.rotation));
                Assert.That(restored.battleAngle, Is.True);
                BattleShowroomPreferences.Save(other, new BattleShowroomContent.Settings { stage = 5 });
                BattleShowroomPreferences.Clear(character);
                Assert.That(BattleShowroomPreferences.Load<BattleShowroomCharacter.Settings>(character).alienId,
                    Is.EqualTo(original.alienId));
                Assert.That(BattleShowroomPreferences.Load<BattleShowroomContent.Settings>(other).stage, Is.EqualTo(5));
            }
            finally
            {
                BattleShowroomPreferences.Clear(character);
                BattleShowroomPreferences.Clear(other);
            }
        }
    }

    public sealed class BattleShowroomCombatPlaySmokeTests
    {
        private const string TestOwnsPlayEntry = "BattleShowroomCombatPlaySmokeTests.OwnsPlayEntry";
        private const string PreviousPlayEntry = "BattleShowroomCombatPlaySmokeTests.PreviousPlayEntry";
        private const string PreviousPreference = "BattleShowroomCombatPlaySmokeTests.PreviousPreference";
        private const string HadPreviousPreference = "BattleShowroomCombatPlaySmokeTests.HadPreviousPreference";
        private const string CombatPreference = "MyDefense.BattleShowroom.v1.Combat";

        [UnitySetUp]
        public IEnumerator StartInIsolatedCombatScene()
        {
            if (Application.isPlaying)
            {
                Assert.That(SessionState.GetBool(TestOwnsPlayEntry, false), Is.True);
                yield break;
            }
            Assert.That(SessionState.GetBool(TestOwnsPlayEntry, false), Is.False,
                "A prior smoke run has an unrestored editor Play start setting.");
            if (!Application.isBatchMode)
            {
                for (int index = 0; index < SceneManager.sceneCount; index++)
                    Assert.That(SceneManager.GetSceneAt(index).isDirty, Is.False, "Save the open Scene before running this Play smoke.");
            }

            var combatScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BattleShowroomScenes.Combat);
            Assert.That(combatScene, Is.Not.Null);
            SessionState.SetString(PreviousPlayEntry, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(HadPreviousPreference, PlayerPrefs.HasKey(CombatPreference));
            SessionState.SetString(PreviousPreference, PlayerPrefs.GetString(CombatPreference));
            SessionState.SetBool(TestOwnsPlayEntry, true);
            BattleShowroomPreferences.Save("Combat", new BattleShowroomCombat.Settings());
            EditorSceneManager.playModeStartScene = combatScene;
            yield return new EnterPlayMode();
        }

        [UnityTest]
        public IEnumerator ActualCombatSceneStartsSingleAndKidnapUpdatesRealAuthority()
        {
            BattleShowroomCombat combat = UnityEngine.Object.FindFirstObjectByType<BattleShowroomCombat>();
            Assert.That(combat, Is.Not.Null);
            double deadline = Time.realtimeSinceStartupAsDouble + 15;
            while (!combat.IsReady && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Assert.That(combat.IsReady, Is.True, "Offline Battle Showroom startup timed out.");
            var authority = UnityEngine.Object.FindFirstObjectByType<BattleWaveStateAuthority>();
            Assert.That(authority, Is.Not.Null);
            Assert.That(authority.Runner.GameMode, Is.EqualTo(GameMode.Single));
            Assert.That(authority.HasStateAuthority, Is.True);
            Assert.That(authority.GetKidnapCount(1), Is.Zero);
            Assert.That(authority.IsBoardOccupied(1, 0), Is.False);
            Assert.That(authority.Executor.TryGetCanonicalSummonCost(0, out int cost), Is.True);
            int before = authority.GetInGameGoldForPlayerSlot(1);

            authority.RequestKidnap();

            Assert.That(authority.GetKidnapCount(1), Is.EqualTo(1));
            Assert.That(authority.GetInGameGoldForPlayerSlot(1), Is.EqualTo(before - cost));
            Assert.That(authority.IsBoardOccupied(1, 0), Is.True);
            Assert.That(authority.GetBoardAlienId(1, 0), Is.GreaterThan(0));
        }

        [UnityTearDown]
        public IEnumerator RestorePlaySettings()
        {
            if (!SessionState.GetBool(TestOwnsPlayEntry, false))
                yield break;
            if (Application.isPlaying)
            {
                BattleShowroomCombat combat = UnityEngine.Object.FindFirstObjectByType<BattleShowroomCombat>();
                if (combat != null)
                {
                    var stop = typeof(BattleShowroomCombat).GetMethod("StopSession", BindingFlags.Instance | BindingFlags.NonPublic);
                    var shutdown = (Task)stop.Invoke(combat, null);
                    double deadline = Time.realtimeSinceStartupAsDouble + 10;
                    while (!shutdown.IsCompleted && Time.realtimeSinceStartupAsDouble < deadline)
                        yield return null;
                }
                yield return new ExitPlayMode();
            }

            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                SessionState.GetString(PreviousPlayEntry, string.Empty));
            if (SessionState.GetBool(HadPreviousPreference, false))
                PlayerPrefs.SetString(CombatPreference, SessionState.GetString(PreviousPreference, string.Empty));
            else
                PlayerPrefs.DeleteKey(CombatPreference);
            PlayerPrefs.Save();
            SessionState.EraseBool(TestOwnsPlayEntry);
            SessionState.EraseBool(HadPreviousPreference);
            SessionState.EraseString(PreviousPlayEntry);
            SessionState.EraseString(PreviousPreference);
        }
    }
}
