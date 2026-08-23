using System.Reflection;
using MyDefense.Battle.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleSpectatorCameraControllerTests
    {
        private const string BattleScenePath = "Assets/Scenes/Battle.unity";

        [Test]
        public void ControllerExposesSpectatorStateAndTargetSlot()
        {
            Assert.That(typeof(BattleSpectatorCameraController).GetProperty(nameof(BattleSpectatorCameraController.IsSpectating)), Is.Not.Null);
            Assert.That(typeof(BattleSpectatorCameraController).GetProperty(nameof(BattleSpectatorCameraController.SpectatorTargetSlot)), Is.Not.Null);
            Assert.That(typeof(BattleSpectatorCameraController).GetMethod("EnterSpectatorMode", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(BattleSpectatorCameraController).GetMethod("RestoreNormalCamera", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        }

        [Test]
        public void BattleSceneMainCameraHasSpectatorController()
        {
            Scene scene = EditorSceneManager.GetSceneByPath(BattleScenePath);
            bool openedByTest = false;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Additive);
                openedByTest = true;
            }

            try
            {
                GameObject cameraObject = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == "Main Camera")
                    {
                        cameraObject = root;
                        break;
                    }
                }
                Assert.That(cameraObject, Is.Not.Null);
                Camera camera = cameraObject.GetComponent<Camera>();
                BattleSpectatorCameraController controller = cameraObject.GetComponent<BattleSpectatorCameraController>();
                if (camera == null || controller == null)
                    Assert.Fail("Main Camera is missing the required camera/spectator components.");
            }
            finally
            {
                if (openedByTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
