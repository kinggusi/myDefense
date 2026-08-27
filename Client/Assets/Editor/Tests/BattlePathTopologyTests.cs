using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using MyDefense.Battle;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyDefense.Battle.Tests
{
    public class BattlePathTopologyTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string BattleScenePath = "Assets/Scenes/Battle.unity";

        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null) Object.DestroyImmediate(_createdObjects[i]);
            }
            _createdObjects.Clear();
        }

        [Test]
        public void BattleScene_HasOnePathForEachLane()
        {
            Scene scene = GetBattleScene(out bool openedByTest);
            try
            {
                WaypointGroup[] groups = GetActiveGroups(scene);
                Assert.That(groups.Count(x => x.Lane == LaneType.Player1Lane), Is.EqualTo(1));
                Assert.That(groups.Count(x => x.Lane == LaneType.Player2Lane), Is.EqualTo(1));
                Assert.That(groups.Count(x => x.Lane == LaneType.BossSharedLane), Is.EqualTo(1));
                Assert.That(groups.Length, Is.EqualTo(3));
            }
            finally
            {
                if (openedByTest) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void AllActivePaths_HaveAtLeastTwoWaypoints()
        {
            Scene scene = GetBattleScene(out bool openedByTest);
            try
            {
                Assert.That(GetActiveGroups(scene).All(x => x.Waypoints.Count >= 2), Is.True);
            }
            finally
            {
                if (openedByTest) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void AllActivePaths_HaveNoNullWaypoints()
        {
            Scene scene = GetBattleScene(out bool openedByTest);
            try
            {
                Assert.That(GetActiveGroups(scene).SelectMany(x => x.Waypoints).All(x => x != null), Is.True);
            }
            finally
            {
                if (openedByTest) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void BattleScene_Player2LaneStartsAtRemoteFieldUpperLeft()
        {
            Scene scene = GetBattleScene(out bool openedByTest);
            try
            {
                WaypointGroup player2 = GetActiveGroups(scene).Single(x => x.Lane == LaneType.Player2Lane);
                Assert.That(
                    player2.Waypoints.Select(waypoint => waypoint.name),
                    Is.EqualTo(new[] { "Wp1", "Wp0", "Wp3", "Wp2" }));
            }
            finally
            {
                if (openedByTest) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void DuplicateLaneGroup_IsRejected()
        {
            PathManager manager = CreateInactivePathManager();
            var groups = new List<WaypointGroup>
            {
                CreateGroup("P1_A", LaneType.Player1Lane),
                CreateGroup("P1_B", LaneType.Player1Lane),
                CreateGroup("P2", LaneType.Player2Lane),
                CreateGroup("Boss", LaneType.BossSharedLane)
            };
            SetField(manager, "_waypointGroups", groups);
            LogAssert.Expect(LogType.Error, new Regex("Duplicate active WaypointGroup detected for Player1Lane"));
            LogAssert.Expect(LogType.Error, new Regex("No valid active path is registered for Player1Lane"));

            manager.InitializePaths();

            var paths = (Dictionary<LaneType, List<Transform>>)GetField(manager, "_paths");
            Assert.That(paths.ContainsKey(LaneType.Player1Lane), Is.False);
            Assert.That(paths.ContainsKey(LaneType.Player2Lane), Is.True);
            Assert.That(paths.ContainsKey(LaneType.BossSharedLane), Is.True);
        }

        [Test]
        public void RegularMonster_KeepsOriginalLaneWhileLooping()
        {
            BattleMonsterMovement movement = CreateMovement(LaneType.Player1Lane, 3);

            SetField(movement, "_targetIndex", 2);
            Invoke(movement, "GetNextWaypoint");
            Invoke(movement, "GetNextWaypoint");

            Assert.That(movement.Lane, Is.EqualTo(LaneType.Player1Lane));
        }

        [Test]
        public void Player1Monster_DoesNotSwitchToPlayer2Lane()
        {
            BattleMonsterMovement movement = CreateMovement(LaneType.Player1Lane, 3);

            for (int i = 0; i < 8; i++) Invoke(movement, "GetNextWaypoint");

            Assert.That(movement.Lane, Is.EqualTo(LaneType.Player1Lane));
            Assert.That(movement.Lane, Is.Not.EqualTo(LaneType.Player2Lane));
        }

        [Test]
        public void Player2Monster_DoesNotSwitchToPlayer1Lane()
        {
            BattleMonsterMovement movement = CreateMovement(LaneType.Player2Lane, 3);

            for (int i = 0; i < 8; i++) Invoke(movement, "GetNextWaypoint");

            Assert.That(movement.Lane, Is.EqualTo(LaneType.Player2Lane));
            Assert.That(movement.Lane, Is.Not.EqualTo(LaneType.Player1Lane));
        }

        [Test]
        public void RegularMonster_LastWaypoint_LoopsToFirst()
        {
            BattleMonsterMovement movement = CreateMovement(LaneType.Player1Lane, 3);
            SetField(movement, "_targetIndex", 2);

            Invoke(movement, "GetNextWaypoint");

            Assert.That(GetField(movement, "_targetIndex"), Is.EqualTo(0));
        }

        [Test]
        public void Boss_LastWaypoint_ReversesDirection()
        {
            BattleMonsterMovement movement = CreateMovement(LaneType.BossSharedLane, 3);
            movement.Speed = 7f;
            SetField(movement, "_targetIndex", 2);
            SetField(movement, "_travelDirection", 1);

            Invoke(movement, "GetNextWaypoint");

            Assert.That(GetField(movement, "_travelDirection"), Is.EqualTo(-1));
            Assert.That(GetField(movement, "_targetIndex"), Is.EqualTo(1));
            Assert.That(movement.Speed, Is.EqualTo(7f));
            Assert.That(GetField(movement, "_isPathCompleted"), Is.EqualTo(false));
        }

        [Test]
        public void Boss_FirstWaypoint_ReversesForwardAgain()
        {
            BattleMonsterMovement movement = CreateMovement(LaneType.BossSharedLane, 3);
            SetField(movement, "_targetIndex", 0);
            SetField(movement, "_travelDirection", -1);

            Invoke(movement, "GetNextWaypoint");

            Assert.That(GetField(movement, "_travelDirection"), Is.EqualTo(1));
            Assert.That(GetField(movement, "_targetIndex"), Is.EqualTo(1));
        }

        [Test]
        public void Boss_TwoWaypointPath_PatrolsContinuously()
        {
            BattleMonsterMovement movement = CreateMovement(LaneType.BossSharedLane, 2);
            movement.Speed = 6f;

            for (int i = 0; i < 3; i++)
            {
                Assert.That(GetField(movement, "_targetIndex"), Is.EqualTo(1));
                Invoke(movement, "GetNextWaypoint");
                Assert.That(GetField(movement, "_targetIndex"), Is.EqualTo(0));
                Assert.That(GetField(movement, "_travelDirection"), Is.EqualTo(-1));
                Invoke(movement, "GetNextWaypoint");
                Assert.That(GetField(movement, "_targetIndex"), Is.EqualTo(1));
                Assert.That(GetField(movement, "_travelDirection"), Is.EqualTo(1));
            }

            Assert.That(movement.Speed, Is.EqualTo(6f));
        }

        [Test]
        public void Boss_InvalidPath_StopsWithSingleError()
        {
            BattleMonsterMovement movement = CreateMovementObject(LaneType.BossSharedLane);
            List<Transform> invalidPath = CreateWaypoints(1);
            LogAssert.Expect(LogType.Error, new Regex("Cannot move on BossSharedLane: invalid path"));

            Assert.That(Invoke<bool>(movement, "TryInitializePath", invalidPath), Is.False);
            Assert.That(Invoke<bool>(movement, "TryInitializePath", invalidPath), Is.False);

            Assert.That(GetField(movement, "_isInitialized"), Is.EqualTo(false));
            Assert.That(GetField(movement, "_isPathCompleted"), Is.EqualTo(true));
        }

        [Test]
        public void BossPatrol_KeepsBossSharedLane()
        {
            BattleMonsterMovement movement = CreateMovement(LaneType.BossSharedLane, 3);

            SetField(movement, "_targetIndex", 2);
            Invoke(movement, "GetNextWaypoint");
            SetField(movement, "_targetIndex", 0);
            Invoke(movement, "GetNextWaypoint");

            Assert.That(movement.Lane, Is.EqualTo(LaneType.BossSharedLane));
        }

        private PathManager CreateInactivePathManager()
        {
            GameObject managerObject = CreateObject("PathManager_Test");
            managerObject.SetActive(false);
            return managerObject.AddComponent<PathManager>();
        }

        private WaypointGroup CreateGroup(string name, LaneType lane)
        {
            GameObject groupObject = CreateObject(name);
            WaypointGroup group = groupObject.AddComponent<WaypointGroup>();
            SetField(group, "_laneType", lane);
            for (int i = 0; i < 2; i++)
            {
                GameObject waypoint = new GameObject($"Wp{i}");
                waypoint.transform.SetParent(groupObject.transform, false);
                waypoint.transform.localPosition = new Vector3(i, 0f, 0f);
            }
            return group;
        }

        private BattleMonsterMovement CreateMovement(LaneType lane, int waypointCount)
        {
            BattleMonsterMovement movement = CreateMovementObject(lane);
            Assert.That(Invoke<bool>(movement, "TryInitializePath", CreateWaypoints(waypointCount)), Is.True);
            return movement;
        }

        private BattleMonsterMovement CreateMovementObject(LaneType lane)
        {
            GameObject movementObject = CreateObject($"Movement_{lane}");
            BattleMonsterMovement movement = movementObject.AddComponent<BattleMonsterMovement>();
            movement.Lane = lane;
            return movement;
        }

        private List<Transform> CreateWaypoints(int count)
        {
            var result = new List<Transform>();
            for (int i = 0; i < count; i++)
            {
                GameObject waypoint = CreateObject($"MovementWaypoint_{_createdObjects.Count}_{i}");
                waypoint.transform.position = new Vector3(i * 2f, 0f, 0f);
                result.Add(waypoint.transform);
            }
            return result;
        }

        private GameObject CreateObject(string name)
        {
            var result = new GameObject(name);
            _createdObjects.Add(result);
            return result;
        }

        private static Scene GetBattleScene(out bool openedByTest)
        {
            Scene scene = SceneManager.GetSceneByPath(BattleScenePath);
            openedByTest = !scene.IsValid() || !scene.isLoaded;
            return openedByTest ? EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Additive) : scene;
        }

        private static WaypointGroup[] GetActiveGroups(Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<WaypointGroup>(true))
                .Where(group => group.isActiveAndEnabled)
                .ToArray();
        }

        private static object GetField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, $"Missing private field: {fieldName}");
            return field.GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, $"Missing private field: {fieldName}");
            field.SetValue(target, value);
        }

        private static void Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstance);
            Assert.That(method, Is.Not.Null, $"Missing private method: {methodName}");
            method.Invoke(target, arguments);
        }

        private static T Invoke<T>(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstance);
            Assert.That(method, Is.Not.Null, $"Missing private method: {methodName}");
            return (T)method.Invoke(target, arguments);
        }
    }
}
