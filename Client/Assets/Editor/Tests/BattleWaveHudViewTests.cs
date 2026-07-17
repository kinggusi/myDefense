using System;
using System.Linq;
using System.Reflection;
using MyDefense.Battle;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace MyDefense.Battle.Tests
{
    public class BattleWaveHudViewTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly Color _normalColor = new Color(0.90f, 0.90f, 0.90f, 1f);
        private readonly Color _warningColor = new Color(1f, 0.80f, 0.20f, 1f);
        private readonly Color _dangerColor = new Color(1f, 0.40f, 0.10f, 1f);
        private readonly Color _eliminatedColor = new Color(1f, 0.10f, 0.15f, 1f);

        private GameObject _executorObject;
        private BattleWaveExecutor _executor;
        private GameObject _hudObject;
        private BattleWaveHudView _hudView;
        private TMP_Text _player1Text;
        private TMP_Text _player2Text;
        private GameObject _bossObject;

        [SetUp]
        public void SetUp()
        {
            _executorObject = new GameObject("BattleWaveExecutor_HudTest");
            _executor = _executorObject.AddComponent<BattleWaveExecutor>();
            SetExecutorField("_totalMonsterGoal", 100);
            _executor.InitializeSession();

            _hudObject = new GameObject("BattleWaveHudView_Test");
            _hudObject.SetActive(false);
            _player1Text = CreateText("P1MonsterCountText_Test");
            _player2Text = CreateText("P2MonsterCountText_Test");
            _hudView = _hudObject.AddComponent<BattleWaveHudView>();
            SetHudField("_waveExecutor", _executor);
            SetHudField("_player1MonsterCountText", _player1Text);
            SetHudField("_player2MonsterCountText", _player2Text);
            SetHudField("_normalColor", _normalColor);
            SetHudField("_warningColor", _warningColor);
            SetHudField("_dangerColor", _dangerColor);
            SetHudField("_eliminatedColor", _eliminatedColor);
            _hudObject.SetActive(true);
            InvokeHud("OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            if (_bossObject != null) UnityEngine.Object.DestroyImmediate(_bossObject);
            if (_hudView != null) InvokeHud("OnDisable");
            if (_hudObject != null) UnityEngine.Object.DestroyImmediate(_hudObject);
            if (_executorObject != null) UnityEngine.Object.DestroyImmediate(_executorObject);
        }

        [Test]
        public void HudInitialState_ShowsBothPlayers()
        {
            Assert.That(_player1Text.text, Is.EqualTo("P1 0 / 100"));
            Assert.That(_player2Text.text, Is.EqualTo("P2 0 / 100"));
            Assert.That(_player1Text.color, Is.EqualTo(_normalColor));
            Assert.That(_player2Text.color, Is.EqualTo(_normalColor));
        }

        [Test]
        public void Player1CountChange_UpdatesOnlyPlayer1Text()
        {
            string player2Before = _player2Text.text;

            RegisterSpawn(LaneType.Player1Lane);

            Assert.That(_player1Text.text, Is.EqualTo("P1 1 / 100"));
            Assert.That(_player2Text.text, Is.EqualTo(player2Before));
        }

        [Test]
        public void Player2CountChange_UpdatesOnlyPlayer2Text()
        {
            string player1Before = _player1Text.text;

            RegisterSpawn(LaneType.Player2Lane);

            Assert.That(_player1Text.text, Is.EqualTo(player1Before));
            Assert.That(_player2Text.text, Is.EqualTo("P2 1 / 100"));
        }

        [Test]
        public void Count79_UsesNormalStyle()
        {
            RegisterSpawn(LaneType.Player1Lane, 79);

            Assert.That(_player1Text.text, Is.EqualTo("P1 79 / 100"));
            Assert.That(_player1Text.color, Is.EqualTo(_normalColor));
        }

        [Test]
        public void Count80_UsesWarningStyle()
        {
            RegisterSpawn(LaneType.Player1Lane, 80);

            Assert.That(_player1Text.text, Is.EqualTo("P1 80 / 100"));
            Assert.That(_player1Text.color, Is.EqualTo(_warningColor));
        }

        [Test]
        public void Count90_UsesDangerStyle()
        {
            RegisterSpawn(LaneType.Player2Lane, 90);

            Assert.That(_player2Text.text, Is.EqualTo("P2 90 / 100"));
            Assert.That(_player2Text.color, Is.EqualTo(_dangerColor));
        }

        [Test]
        public void CanonicalThresholds_DriveLimitWarningAndDangerDisplay()
        {
            SetExecutorField("_totalMonsterGoal", 10);
            SetExecutorField("_monsterWarningThreshold", 4);
            SetExecutorField("_monsterDangerThreshold", 7);
            _executor.InitializeSession();

            RegisterSpawn(LaneType.Player1Lane, 4);
            RegisterSpawn(LaneType.Player2Lane, 7);

            Assert.That(_player1Text.text, Is.EqualTo("P1 4 / 10"));
            Assert.That(_player1Text.color, Is.EqualTo(_warningColor));
            Assert.That(_player2Text.text, Is.EqualTo("P2 7 / 10"));
            Assert.That(_player2Text.color, Is.EqualTo(_dangerColor));
        }

        [Test]
        public void EliminatedPlayer_UsesEliminatedLabelAndStyle()
        {
            RegisterSpawn(LaneType.Player1Lane, 100);

            Assert.That(_executor.Player1BattleState, Is.EqualTo(PlayerBattleState.ELIMINATED));
            Assert.That(_player1Text.text, Is.EqualTo("P1 ELIMINATED \u00B7 100 / 100"));
            Assert.That(_player1Text.color, Is.EqualTo(_eliminatedColor));
        }

        [Test]
        public void EliminatedPlayerCountDecrease_RemainsEliminated()
        {
            RegisterSpawn(LaneType.Player1Lane, 100);

            _executor.RegisterMonsterKilled(LaneType.Player1Lane);

            Assert.That(_executor.Player1BattleState, Is.EqualTo(PlayerBattleState.ELIMINATED));
            Assert.That(_player1Text.text, Is.EqualTo("P1 ELIMINATED \u00B7 99 / 100"));
            Assert.That(_player1Text.color, Is.EqualTo(_eliminatedColor));
        }

        [Test]
        public void InitializeSession_ResetsBothHudCounters()
        {
            RegisterSpawn(LaneType.Player1Lane, 90);
            RegisterSpawn(LaneType.Player2Lane, 80);

            _executor.InitializeSession();

            Assert.That(_player1Text.text, Is.EqualTo("P1 0 / 100"));
            Assert.That(_player2Text.text, Is.EqualTo("P2 0 / 100"));
            Assert.That(_player1Text.color, Is.EqualTo(_normalColor));
            Assert.That(_player2Text.color, Is.EqualTo(_normalColor));
        }

        [Test]
        public void DisableEnable_DoesNotDuplicateSubscriptions()
        {
            InvokeHud("OnDisable");
            Assert.That(CountHudEventHandlers("OnPlayerMonsterCountChanged"), Is.Zero);

            InvokeHud("OnEnable");
            InvokeHud("OnDisable");
            InvokeHud("OnEnable");

            Assert.That(CountHudEventHandlers("OnPlayerMonsterCountChanged"), Is.EqualTo(1));
            Assert.That(CountHudEventHandlers("OnPlayerBattleStateChanged"), Is.EqualTo(1));
        }

        [Test]
        public void BossState_DoesNotIncreasePlayerCounters()
        {
            _bossObject = new GameObject("Boss_HudTest");

            InvokeExecutor("ActivateBoss", _bossObject);

            Assert.That(_executor.Player1AliveMonsterCount, Is.Zero);
            Assert.That(_executor.Player2AliveMonsterCount, Is.Zero);
            Assert.That(_player1Text.text, Is.EqualTo("P1 0 / 100"));
            Assert.That(_player2Text.text, Is.EqualTo("P2 0 / 100"));
        }

        private TMP_Text CreateText(string name)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            textObject.transform.SetParent(_hudObject.transform, false);
            return textObject.AddComponent<TextMeshProUGUI>();
        }

        private void RegisterSpawn(LaneType lane, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                InvokeExecutor("RegisterMonsterSpawned", lane);
            }
        }

        private int CountHudEventHandlers(string eventFieldName)
        {
            FieldInfo field = typeof(BattleWaveExecutor).GetField(eventFieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, $"Missing event field: {eventFieldName}");
            var handlers = field.GetValue(_executor) as Delegate;
            if (handlers == null) return 0;

            return handlers.GetInvocationList().Count(x => ReferenceEquals(x.Target, _hudView));
        }

        private void SetExecutorField(string fieldName, object value)
        {
            FieldInfo field = typeof(BattleWaveExecutor).GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, $"Missing executor field: {fieldName}");
            field.SetValue(_executor, value);
        }

        private void SetHudField(string fieldName, object value)
        {
            FieldInfo field = typeof(BattleWaveHudView).GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, $"Missing HUD field: {fieldName}");
            field.SetValue(_hudView, value);
        }

        private void InvokeExecutor(string methodName, params object[] arguments)
        {
            MethodInfo method = typeof(BattleWaveExecutor).GetMethod(methodName, PrivateInstance);
            Assert.That(method, Is.Not.Null, $"Missing executor method: {methodName}");
            method.Invoke(_executor, arguments);
        }

        private void InvokeHud(string methodName)
        {
            MethodInfo method = typeof(BattleWaveHudView).GetMethod(methodName, PrivateInstance);
            Assert.That(method, Is.Not.Null, $"Missing HUD method: {methodName}");
            method.Invoke(_hudView, null);
        }
    }
}
