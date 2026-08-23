using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Fusion;
using MyDefense.Battle;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Assert = NUnit.Framework.Assert;
using LogType = UnityEngine.LogType;

namespace MyDefense.Battle.Tests
{
public class BattleWaveExecutorStateTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string BattleScenePath = "Assets/Scenes/Battle.unity";
    private const string MonsterPrefabPath = "Assets/Prefabs/Monsters/Monster.prefab";

    private GameObject _executorObject;
    private BattleWaveExecutor _executor;
    private BattleWaveExecutor _previousExecutorInstance;
    private GameObject _bossObject;
    private int _sessionNumber;

    [SetUp]
    public void SetUp()
    {
        _previousExecutorInstance = BattleWaveExecutor.Instance;
        SetExecutorInstance(null);
        _executorObject = new GameObject("BattleWaveExecutor_Test");
        _executor = _executorObject.AddComponent<BattleWaveExecutor>();
        SetExecutorInstance(_executor);
        IMonsterDefinitionProvider monsters = new StateTestMonsterProvider();
        ConfigureBalance(
            new ResourcesBattleBalanceProvider(monsters, ProductionAlienIds()),
            monsters,
            new StateTestPrefabResolver());
        SetField("_totalMonsterGoal", 1);
        InitializeRuntimeSession();
        Assert.That(BattleWaveExecutor.Instance, Is.SameAs(_executor));
    }

    [TearDown]
    public void TearDown()
    {
        if (_bossObject != null)
        {
            Object.DestroyImmediate(_bossObject);
        }

        if (_executorObject != null)
        {
            Object.DestroyImmediate(_executorObject);
        }

        SetExecutorInstance(_previousExecutorInstance);
    }

    [Test]
    public void InitializeSession_SetsBothPlayersActiveAndMatchRunning()
    {
        Assert.That(_executor.Player1BattleState, Is.EqualTo(PlayerBattleState.ACTIVE));
        Assert.That(_executor.Player2BattleState, Is.EqualTo(PlayerBattleState.ACTIVE));
        Assert.That(_executor.MatchState, Is.EqualTo(MatchState.RUNNING));
    }

    [Test]
    public void Player1Limit_EliminatesOnlyPlayer1AndKeepsMatchRunning()
    {
        RegisterSpawn(LaneType.Player1Lane);

        Assert.That(_executor.Player1BattleState, Is.EqualTo(PlayerBattleState.ELIMINATED));
        Assert.That(_executor.Player2BattleState, Is.EqualTo(PlayerBattleState.ACTIVE));
        Assert.That(_executor.MatchState, Is.EqualTo(MatchState.RUNNING));
    }

    [Test]
    public void Player2Limit_EliminatesOnlyPlayer2AndKeepsMatchRunning()
    {
        RegisterSpawn(LaneType.Player2Lane);

        Assert.That(_executor.Player1BattleState, Is.EqualTo(PlayerBattleState.ACTIVE));
        Assert.That(_executor.Player2BattleState, Is.EqualTo(PlayerBattleState.ELIMINATED));
        Assert.That(_executor.MatchState, Is.EqualTo(MatchState.RUNNING));
    }

    [Test]
    public void BothPlayerLimits_FailMatchOnce()
    {
        int terminalTransitions = 0;
        _executor.OnMatchStateChanged += _ => terminalTransitions++;

        RegisterSpawn(LaneType.Player1Lane);
        RegisterSpawn(LaneType.Player2Lane);
        RegisterSpawn(LaneType.Player2Lane);

        Assert.That(_executor.MatchState, Is.EqualTo(MatchState.FAILED));
        Assert.That(terminalTransitions, Is.EqualTo(1));
    }

    [Test]
    public void EliminatedLane_RejectsNewMonsterRegistration()
    {
        RegisterSpawn(LaneType.Player1Lane);
        int countAtElimination = _executor.Player1AliveMonsterCount;

        RegisterSpawn(LaneType.Player1Lane);

        Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(countAtElimination));
        Assert.That(Invoke<bool>("CanSpawnInLane", LaneType.Player1Lane), Is.False);
        Assert.That(Invoke<bool>("CanSpawnInLane", LaneType.Player2Lane), Is.True);
    }

    [Test]
    public void EliminatedLane_RejectsNewMonsterSpawnBeforeInstantiation()
    {
        SetField("_totalMonsterGoal", 1);
        GameObject validPrefab = new GameObject("EliminatedLaneSpawnPrefab_Test");
        validPrefab.AddComponent<BattleMonsterMovement>();
        GameObject spawnPoint = new GameObject("EliminatedLaneSpawnPoint_Test");
        try
        {
            SetField("_monsterPrefab", validPrefab);
            SetField("_spawnPoint", spawnPoint.transform);
            RegisterSpawn(LaneType.Player1Lane);

            int countAtElimination = _executor.Player1AliveMonsterCount;
            bool spawned = Invoke<bool>("SpawnMonster", LaneType.Player1Lane, 5f, 1f);

            Assert.That(spawned, Is.False);
            Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(countAtElimination));
            Assert.That(Object.FindObjectsByType<BattleMonsterMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(x => x.gameObject != validPrefab), Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(validPrefab);
            Object.DestroyImmediate(spawnPoint);
        }
    }

    [Test]
    public void BossTimeout_FailsMatchAndRaisesTimeoutOnce()
    {
        ActivateBoss();
        int timeoutEvents = 0;
        _executor.OnBossTimeout += () => timeoutEvents++;

        Assert.That(Invoke<bool>("TryResolveBossTimeout"), Is.True);
        Assert.That(Invoke<bool>("TryResolveBossTimeout"), Is.False);

        Assert.That(_executor.MatchState, Is.EqualTo(MatchState.FAILED));
        Assert.That(timeoutEvents, Is.EqualTo(1));
    }

    [Test]
    public void BossDefeat_CancelsTimeoutTransition()
    {
        ActivateBoss();
        int timeoutEvents = 0;
        int defeatEvents = 0;
        _executor.OnBossTimeout += () => timeoutEvents++;
        _executor.OnBossDefeated += () => defeatEvents++;

        Assert.That(Invoke<bool>("HandleBossDefeated"), Is.True);
        Assert.That(Invoke<bool>("TryResolveBossTimeout"), Is.False);

        Assert.That(_executor.MatchState, Is.EqualTo(MatchState.RUNNING));
        Assert.That(defeatEvents, Is.EqualTo(1));
        Assert.That(timeoutEvents, Is.Zero);
    }

    [Test]
    public void AuthoritativeBossDefeat_RequiresDeadCurrentInstanceAndIsOneShot()
    {
        ActivateBoss();
        MonsterStat stat = _bossObject.AddComponent<MonsterStat>();
        FieldInfo deadField = typeof(MonsterStat).GetField("isDead", PrivateInstance);
        deadField.SetValue(stat, true);
        BattleMonsterNetworkState bossState = _bossObject.AddComponent<BattleMonsterNetworkState>();
        GameObject staleObject = new GameObject("StaleBoss_Test");
        MonsterStat staleStat = staleObject.AddComponent<MonsterStat>();
        deadField.SetValue(staleStat, true);
        BattleMonsterNetworkState staleState = staleObject.AddComponent<BattleMonsterNetworkState>();
        int defeatEvents = 0;
        _executor.OnBossDefeated += () => defeatEvents++;

        Assert.That(_executor.TryResolveBossDefeatFromAuthority(staleState), Is.False);
        Assert.That(_executor.TryResolveBossDefeatFromAuthority(bossState), Is.True);
        Assert.That(_executor.TryResolveBossDefeatFromAuthority(bossState), Is.False);
        Assert.That(defeatEvents, Is.EqualTo(1));
        Assert.That(_executor.IsBossActive, Is.False);

        Object.DestroyImmediate(staleObject);
    }

    [Test]
    public void TimeoutDefeatRace_ProducesOneTerminalTransition()
    {
        ActivateBoss();
        int terminalTransitions = 0;
        int timeoutEvents = 0;
        int defeatEvents = 0;
        _executor.OnMatchStateChanged += _ => terminalTransitions++;
        _executor.OnBossTimeout += () => timeoutEvents++;
        _executor.OnBossDefeated += () => defeatEvents++;

        Assert.That(Invoke<bool>("TryResolveBossTimeout"), Is.True);
        Assert.That(Invoke<bool>("HandleBossDefeated"), Is.False);

        Assert.That(terminalTransitions, Is.EqualTo(1));
        Assert.That(timeoutEvents, Is.EqualTo(1));
        Assert.That(defeatEvents, Is.Zero);
    }

    [Test]
    public void FailedMatch_RejectsStartNextWave()
    {
        RegisterSpawn(LaneType.Player1Lane);
        RegisterSpawn(LaneType.Player2Lane);
        int roundAtFailure = _executor.CurrentRound;

        _executor.StartNextWave();

        Assert.That(_executor.CurrentRound, Is.EqualTo(roundAtFailure));
        Assert.That(_executor.IsWaveRunning, Is.False);
    }

    [Test]
    public void InitializeSession_ReleasesBossAndClearsCoroutineReferences()
    {
        ActivateBoss();

        InitializeRuntimeSession();

        Assert.That(_bossObject == null, Is.True);
        Assert.That(GetField("_currentBossInstance"), Is.Null);
        Assert.That(GetField("_waveLoopCoroutine"), Is.Null);
        Assert.That(GetField("_activeWaveCoroutine"), Is.Null);
        Assert.That(GetField("_bossTimerCoroutine"), Is.Null);
        Assert.That(_executor.IsBossActive, Is.False);
        Assert.That(_executor.MatchState, Is.EqualTo(MatchState.RUNNING));
    }

    [Test]
    public void SpawnCompleted_WithAliveMonsters_KeepsWaveRunning()
    {
        SetField("_totalMonsterGoal", 100);
        BeginRegularWave();
        int completedEvents = 0;
        _executor.OnRegularWaveCompleted += _ => completedEvents++;

        RegisterSpawn(LaneType.Player1Lane);
        MarkRegularSpawnCompleted();

        Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(1));
        Assert.That(_executor.IsWaveRunning, Is.True);
        Assert.That(GetField("_regularWaveSpawnCompleted"), Is.EqualTo(true));
        Assert.That(completedEvents, Is.Zero);
    }

    [Test]
    public void LastRegularMonsterKilled_CompletesWaveOnce()
    {
        SetField("_totalMonsterGoal", 100);
        BeginRegularWave();
        int completedEvents = 0;
        _executor.OnRegularWaveCompleted += _ => completedEvents++;

        RegisterSpawn(LaneType.Player1Lane);
        RegisterSpawn(LaneType.Player2Lane);
        MarkRegularSpawnCompleted();
        _executor.RegisterMonsterKilled(LaneType.Player1Lane);

        Assert.That(_executor.IsWaveRunning, Is.True);

        _executor.RegisterMonsterKilled(LaneType.Player2Lane);
        _executor.RegisterMonsterKilled(LaneType.Player2Lane);

        Assert.That(_executor.IsWaveRunning, Is.False);
        Assert.That(completedEvents, Is.EqualTo(1));
    }

    [Test]
    public void StartNextWave_WithAliveMonsters_IsRejected()
    {
        SetField("_totalMonsterGoal", 100);
        BeginRegularWave();
        RegisterSpawn(LaneType.Player1Lane);
        MarkRegularSpawnCompleted();
        int roundWithAliveMonster = _executor.CurrentRound;

        bool started = Invoke<bool>("TryBeginNextWave");

        Assert.That(started, Is.False);
        Assert.That(_executor.CurrentRound, Is.EqualTo(roundWithAliveMonster));
        Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(1));
    }

    [Test]
    public void EliminatedLaneRemainingMonsters_BlockWaveCompletion()
    {
        BeginRegularWave();
        RegisterSpawn(LaneType.Player1Lane);
        MarkRegularSpawnCompleted();

        Assert.That(_executor.Player1BattleState, Is.EqualTo(PlayerBattleState.ELIMINATED));
        Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(1));
        Assert.That(_executor.Player2BattleState, Is.EqualTo(PlayerBattleState.ACTIVE));
        Assert.That(_executor.MatchState, Is.EqualTo(MatchState.RUNNING));
        Assert.That(_executor.IsWaveRunning, Is.True);
    }

    [Test]
    public void EliminatedLaneCleared_AllowsNextWaveForActivePlayer()
    {
        BeginRegularWave();
        RegisterSpawn(LaneType.Player1Lane);
        MarkRegularSpawnCompleted();

        _executor.RegisterMonsterKilled(LaneType.Player1Lane);

        Assert.That(_executor.IsWaveRunning, Is.False);
        Assert.That(_executor.Player1BattleState, Is.EqualTo(PlayerBattleState.ELIMINATED));
        Assert.That(_executor.Player2BattleState, Is.EqualTo(PlayerBattleState.ACTIVE));
        Assert.That(Invoke<bool>("TryBeginNextWave"), Is.True);
        Assert.That(_executor.CurrentRound, Is.EqualTo(2));
        Assert.That(Invoke<bool>("CanSpawnInLane", LaneType.Player1Lane), Is.False);
        Assert.That(Invoke<bool>("CanSpawnInLane", LaneType.Player2Lane), Is.True);
    }

    [Test]
    public void BothPlayersEliminated_StillFailsMatchOnce()
    {
        int terminalTransitions = 0;
        _executor.OnMatchStateChanged += _ => terminalTransitions++;

        RegisterSpawn(LaneType.Player1Lane);
        RegisterSpawn(LaneType.Player2Lane);
        RegisterSpawn(LaneType.Player1Lane);

        Assert.That(_executor.Player1BattleState, Is.EqualTo(PlayerBattleState.ELIMINATED));
        Assert.That(_executor.Player2BattleState, Is.EqualTo(PlayerBattleState.ELIMINATED));
        Assert.That(_executor.MatchState, Is.EqualTo(MatchState.FAILED));
        Assert.That(terminalTransitions, Is.EqualTo(1));
    }

    [Test]
    public void BossDefeat_StillAllowsWaveCompletion()
    {
        BeginBossWave();
        ActivateBoss();
        int bossRound = _executor.CurrentRound;

        Assert.That(Invoke<bool>("TryBeginNextWave"), Is.False);
        Assert.That(_executor.CurrentRound, Is.EqualTo(bossRound));
        Assert.That(Invoke<bool>("HandleBossDefeated"), Is.True);
        Assert.That(_executor.IsWaveRunning, Is.False);
        Assert.That(Invoke<bool>("TryBeginNextWave"), Is.True);
        Assert.That(_executor.CurrentRound, Is.EqualTo(11));
    }

    [Test]
    public void BossTimeout_StillBlocksFutureWaves()
    {
        BeginBossWave();
        ActivateBoss();
        int bossRound = _executor.CurrentRound;

        Assert.That(Invoke<bool>("TryResolveBossTimeout"), Is.True);
        Assert.That(Invoke<bool>("TryBeginNextWave"), Is.False);

        Assert.That(_executor.MatchState, Is.EqualTo(MatchState.FAILED));
        Assert.That(_executor.CurrentRound, Is.EqualTo(bossRound));
        Assert.That(_executor.IsWaveRunning, Is.False);
    }

    [Test]
    public void BattleScene_HasSingleBattleWaveExecutor()
    {
        Scene scene = GetBattleScene(out bool openedByTest);
        try
        {
            BattleWaveExecutor[] executors = GetSceneComponents<BattleWaveExecutor>(scene);
            Assert.That(executors.Count(x => x != _executor && x.isActiveAndEnabled), Is.EqualTo(1));
        }
        finally
        {
            if (openedByTest) EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void BattleScene_HasNoActiveLegacyWaveManager()
    {
        Scene scene = GetBattleScene(out bool openedByTest);
        try
        {
            WaveManager[] managers = GetSceneComponents<WaveManager>(scene);
            Assert.That(managers.Count(x => x.isActiveAndEnabled), Is.Zero);
        }
        finally
        {
            if (openedByTest) EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void MonsterPrefab_HasSingleBattleMonsterMovement()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponentsInChildren<BattleMonsterMovement>(true).Length, Is.EqualTo(1));
    }

    [Test]
    public void MonsterCountThresholds_RaiseWarningAndDangerOnceWhenCrossed()
    {
        SetField("_totalMonsterGoal", 100);
        SetField("_monsterWarningThreshold", 2);
        SetField("_monsterDangerThreshold", 3);
        int warningEvents = 0;
        int dangerEvents = 0;
        _executor.OnPlayerMonsterWarningReached += (lane, count) =>
        {
            Assert.That(lane, Is.EqualTo(LaneType.Player1Lane));
            Assert.That(count, Is.GreaterThanOrEqualTo(2));
            warningEvents++;
        };
        _executor.OnPlayerMonsterDangerReached += (lane, count) =>
        {
            Assert.That(lane, Is.EqualTo(LaneType.Player1Lane));
            Assert.That(count, Is.GreaterThanOrEqualTo(3));
            dangerEvents++;
        };

        RegisterSpawn(LaneType.Player1Lane);
        RegisterSpawn(LaneType.Player1Lane);
        RegisterSpawn(LaneType.Player1Lane);
        RegisterSpawn(LaneType.Player1Lane);

        Assert.That(warningEvents, Is.EqualTo(1));
        Assert.That(dangerEvents, Is.EqualTo(1));
    }

    [Test]
    public void MonsterPrefab_HasFusionNetworkTransformAndObject()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<NetworkObject>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<NetworkTransform>(), Is.Not.Null);
    }

    [Test]
    public void MonsterPrefab_HasNoLegacyMonsterMovement()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponentsInChildren<MonsterMovement>(true).Length, Is.Zero);
    }

    [Test]
    public void MissingBattleMonsterMovement_FaultsWithoutRegisteringCount()
    {
        GameObject invalidPrefab = new GameObject("InvalidMonsterPrefab_Test");
        GameObject spawnPoint = new GameObject("SpawnPoint_Test");
        try
        {
            SetField("_monsterPrefab", invalidPrefab);
            SetField("_spawnPoint", spawnPoint.transform);
            BeginRegularWave();
            LogAssert.Expect(LogType.Error, new Regex("must contain exactly one BattleMonsterMovement"));

            bool spawned = Invoke<bool>("SpawnMonster", LaneType.Player1Lane, 5f, 1f);

            Assert.That(spawned, Is.False);
            Assert.That(_executor.Player1AliveMonsterCount, Is.Zero);
            Assert.That(_executor.Player2AliveMonsterCount, Is.Zero);
            Assert.That(GetField("_isFaulted"), Is.EqualTo(true));
            Assert.That(invalidPrefab.GetComponent<BattleMonsterMovement>(), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(invalidPrefab);
            Object.DestroyImmediate(spawnPoint);
        }
    }

    [Test]
    public void SpawnedRegularMonster_UsesRequestedLane()
    {
        GameObject validPrefab = new GameObject("ValidMonsterPrefab_Test");
        validPrefab.AddComponent<BattleMonsterMovement>();
        GameObject spawnPoint = new GameObject("SpawnPoint_Test");
        GameObject spawnedObject = null;
        try
        {
            SetField("_totalMonsterGoal", 100);
            SetField("_monsterPrefab", validPrefab);
            SetField("_spawnPoint", spawnPoint.transform);
            BeginRegularWave();

            bool spawned = Invoke<bool>("SpawnMonster", LaneType.Player2Lane, 7f, 1f);
            BattleMonsterMovement movement = Object.FindObjectsByType<BattleMonsterMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(x => x.gameObject != validPrefab);
            spawnedObject = movement.gameObject;

            Assert.That(spawned, Is.True);
            Assert.That(movement.Lane, Is.EqualTo(LaneType.Player2Lane));
            Assert.That(movement.Speed, Is.EqualTo(7f));
            Assert.That(_executor.Player2AliveMonsterCount, Is.EqualTo(1));
        }
        finally
        {
            if (spawnedObject != null) Object.DestroyImmediate(spawnedObject);
            Object.DestroyImmediate(validPrefab);
            Object.DestroyImmediate(spawnPoint);
        }
    }

    [Test]
    public void Player1MonsterDeath_DecrementsOnlyPlayer1Lane()
    {
        SetField("_totalMonsterGoal", 100);
        RegisterSpawn(LaneType.Player1Lane);
        RegisterSpawn(LaneType.Player2Lane);
        GameObject monsterObject = new GameObject("Player1Monster_Test");
        try
        {
            MonsterStat stat = monsterObject.AddComponent<MonsterStat>();
            stat.InitializeHp(1f);
            stat.InitializeBattleContext(LaneType.Player1Lane, true);

            ExpectEditModeDestroyError();
            stat.TakeDamage(1f);

            Assert.That(stat.IsDead, Is.True);
            Assert.That(_executor.Player1AliveMonsterCount, Is.Zero);
            Assert.That(_executor.Player2AliveMonsterCount, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(monsterObject);
        }
    }

    [Test]
    public void Player2MonsterDeath_DecrementsOnlyPlayer2Lane()
    {
        SetField("_totalMonsterGoal", 100);
        RegisterSpawn(LaneType.Player1Lane);
        RegisterSpawn(LaneType.Player2Lane);
        GameObject monsterObject = new GameObject("Player2Monster_Test");
        try
        {
            MonsterStat stat = monsterObject.AddComponent<MonsterStat>();
            stat.InitializeHp(1f);
            stat.InitializeBattleContext(LaneType.Player2Lane, true);

            ExpectEditModeDestroyError();
            stat.TakeDamage(1f);

            Assert.That(stat.IsDead, Is.True);
            Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(1));
            Assert.That(_executor.Player2AliveMonsterCount, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(monsterObject);
        }
    }

    [Test]
    public void BossSharedLaneMonsterDeath_DoesNotDecrementPlayerLaneCounts()
    {
        SetField("_totalMonsterGoal", 100);
        RegisterSpawn(LaneType.Player1Lane);
        RegisterSpawn(LaneType.Player2Lane);
        GameObject monsterObject = new GameObject("BossSharedLaneMonster_Test");
        try
        {
            MonsterStat stat = monsterObject.AddComponent<MonsterStat>();
            stat.InitializeHp(1f);
            stat.InitializeBattleContext(LaneType.BossSharedLane, false);

            ExpectEditModeDestroyError();
            stat.TakeDamage(1f);

            Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(1));
            Assert.That(_executor.Player2AliveMonsterCount, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(monsterObject);
        }
    }

    [Test]
    public void NonCountingPlayerLaneMonsterDeath_DoesNotDecrementPlayerLaneCounts()
    {
        SetField("_totalMonsterGoal", 100);
        RegisterSpawn(LaneType.Player1Lane);
        GameObject monsterObject = new GameObject("NonCountingPlayerLaneMonster_Test");
        try
        {
            MonsterStat stat = monsterObject.AddComponent<MonsterStat>();
            stat.InitializeHp(1f);
            stat.InitializeBattleContext(LaneType.Player1Lane, false);

            ExpectEditModeDestroyError();
            stat.TakeDamage(1f);

            Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(1));
            Assert.That(_executor.Player2AliveMonsterCount, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(monsterObject);
        }
    }

    [Test]
    public void UninitializedBattleContextMonsterDeath_DoesNotDecrementPlayerLaneCounts()
    {
        SetField("_totalMonsterGoal", 100);
        RegisterSpawn(LaneType.Player1Lane);
        RegisterSpawn(LaneType.Player2Lane);
        GameObject monsterObject = new GameObject("UninitializedBattleContextMonster_Test");
        try
        {
            MonsterStat stat = monsterObject.AddComponent<MonsterStat>();
            stat.InitializeHp(1f);

            ExpectEditModeDestroyError();
            stat.TakeDamage(1f);

            Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(1));
            Assert.That(_executor.Player2AliveMonsterCount, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(monsterObject);
        }
    }

    [Test]
    public void RepeatedDamageAfterDeath_DecrementsLaneCountOnlyOnce()
    {
        SetField("_totalMonsterGoal", 100);
        RegisterSpawn(LaneType.Player1Lane);
        RegisterSpawn(LaneType.Player1Lane);
        GameObject monsterObject = new GameObject("RepeatedDamageMonster_Test");
        try
        {
            MonsterStat stat = monsterObject.AddComponent<MonsterStat>();
            stat.InitializeHp(1f);
            stat.InitializeBattleContext(LaneType.Player1Lane, true);

            ExpectEditModeDestroyError();
            stat.TakeDamage(1f);
            stat.TakeDamage(1f);

            Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(1));
            Assert.That(_executor.Player2AliveMonsterCount, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(monsterObject);
        }
    }

    [Test]
    public void SpawnedBoss_UsesBossSharedLane()
    {
        GameObject prefab = new GameObject("BossPrefab_Test");
        prefab.AddComponent<BattleMonsterMovement>();
        GameObject spawnedBoss = Object.Instantiate(prefab);
        try
        {
            bool configured = Invoke<bool>("TryConfigureSpawnedMovement", spawnedBoss, LaneType.BossSharedLane, 2f);
            BattleMonsterMovement movement = spawnedBoss.GetComponent<BattleMonsterMovement>();

            Assert.That(configured, Is.True);
            Assert.That(movement.Lane, Is.EqualTo(LaneType.BossSharedLane));
            Assert.That(movement.Speed, Is.EqualTo(2f));
        }
        finally
        {
            Object.DestroyImmediate(spawnedBoss);
            Object.DestroyImmediate(prefab);
        }
    }

    private void ActivateBoss()
    {
        _bossObject = new GameObject("Boss_Test");
        Invoke("ActivateBoss", _bossObject);
    }

    private void RegisterSpawn(LaneType lane)
    {
        Invoke("RegisterMonsterSpawned", lane);
    }

    private void BeginRegularWave()
    {
        SetField("_currentRound", 1);
        Invoke("BeginWaveExecution", false);
    }

    private void BeginBossWave()
    {
        SetField("_currentRound", 10);
        Invoke("BeginWaveExecution", true);
    }

    private void MarkRegularSpawnCompleted()
    {
        Invoke("MarkRegularWaveSpawnCompleted");
    }

    private void ConfigureBalance(
        IBattleBalanceProvider provider,
        IMonsterDefinitionProvider monsters,
        IBattleMonsterPrefabResolver resolver)
    {
        Invoke("ConfigureBalanceDependenciesForTests", provider, monsters, resolver);
    }

    private void InitializeRuntimeSession()
    {
        _sessionNumber++;
        _executor.InitializeSession(
            new BattleSessionContext(
                "state-fixture-session-" + _sessionNumber,
                "fixture-canonical-v1",
                "fixture-canonical-hash",
                "fixture-battle-v1",
                "fixture-battle-hash",
                _sessionNumber),
            new BattlePlayerIdentityMap("fixture-player-alpha", "fixture-player-beta"));
    }

    private static void ExpectEditModeDestroyError()
    {
        LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
    }

    private static void SetExecutorInstance(BattleWaveExecutor instance)
    {
        PropertyInfo property = typeof(BattleWaveExecutor).GetProperty(
            nameof(BattleWaveExecutor.Instance),
            BindingFlags.Public | BindingFlags.Static);
        Assert.That(property, Is.Not.Null, "Missing BattleWaveExecutor.Instance property.");
        MethodInfo setter = property.GetSetMethod(true);
        Assert.That(setter, Is.Not.Null, "Missing BattleWaveExecutor.Instance setter.");
        setter.Invoke(null, new object[] { instance });
    }

    private static Scene GetBattleScene(out bool openedByTest)
    {
        Scene scene = SceneManager.GetSceneByPath(BattleScenePath);
        openedByTest = !scene.IsValid() || !scene.isLoaded;
        return openedByTest ? EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Additive) : scene;
    }

    private static IAlienIdProvider ProductionAlienIds()
    {
        Assert.That(CanonicalBattleAlienIdProvider.TryCreate(out CanonicalBattleAlienIdProvider provider, out string error), Is.True, error);
        return provider;
    }

    private static T[] GetSceneComponents<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .ToArray();
    }

    private object GetField(string fieldName)
    {
        FieldInfo field = typeof(BattleWaveExecutor).GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Missing private field: {fieldName}");
        return field.GetValue(_executor);
    }

    private void SetField(string fieldName, object value)
    {
        FieldInfo field = typeof(BattleWaveExecutor).GetField(fieldName, PrivateInstance);
        Assert.That(field, Is.Not.Null, $"Missing private field: {fieldName}");
        field.SetValue(_executor, value);
    }

    private void Invoke(string methodName, params object[] arguments)
    {
        MethodInfo method = typeof(BattleWaveExecutor).GetMethod(methodName, PrivateInstance);
        Assert.That(method, Is.Not.Null, $"Missing private method: {methodName}");
        method.Invoke(_executor, arguments);
    }

    private T Invoke<T>(string methodName, params object[] arguments)
    {
        MethodInfo method = typeof(BattleWaveExecutor).GetMethod(methodName, PrivateInstance);
        Assert.That(method, Is.Not.Null, $"Missing private method: {methodName}");
        return (T)method.Invoke(_executor, arguments);
    }

    private sealed class StateTestMonsterProvider : IMonsterDefinitionProvider
    {
        public bool TryGet(string monsterId, out BattleMonsterDefinition definition)
        {
            if (monsterId == TemporaryBattleMonsterDefinitionProvider.NormalMonsterId)
            {
                definition = new BattleMonsterDefinition(monsterId, "NORMAL", 30f, 5f, "Monster", true);
                return true;
            }

            if (monsterId == TemporaryBattleMonsterDefinitionProvider.BossMonsterId)
            {
                definition = new BattleMonsterDefinition(monsterId, "BOSS", 30f, 2f, "Monster", false);
                return true;
            }

            definition = null;
            return false;
        }
    }

    private sealed class StateTestPrefabResolver : IBattleMonsterPrefabResolver
    {
        public bool TryResolve(string prefabKey, out GameObject prefab)
        {
            prefab = null;
            return false;
        }
    }
}
}
