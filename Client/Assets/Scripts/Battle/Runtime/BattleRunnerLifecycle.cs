using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyDefense.Battle.Runtime
{
    public enum BattleRunnerLifecycleState
    {
        STOPPED = 0,
        STARTING = 1,
        RUNNING = 2,
        STOPPING = 3,
        FAULTED = 4
    }

    /// <summary>
    /// Owns the lifetime of one Fusion runner. Player binding and battle state
    /// replication are intentionally handled by later P0-2 tasks.
    /// </summary>
    public sealed class BattleRunnerLifecycle : MonoBehaviour
    {
        private NetworkRunner _runner;
        private GameObject _runnerObject;
        private BattlePlayerIdentityCallbacks _identityCallbacks;
        private Task _lifecycleTask;

        public BattleRunnerLifecycleState State { get; private set; } = BattleRunnerLifecycleState.STOPPED;
        public NetworkRunner Runner => _runner;
        public BattlePlayerRoster PlayerRoster { get; } = new();
        public BattleMatchStartCoordinator MatchStart { get; private set; }
        public BattleSessionContext SessionContext { get; private set; }
        public bool IsBattleStarted => MatchStart.State == BattleStartState.STARTED;
        public string LastError { get; private set; }
        public event Action<BattleSessionContext> SessionContextCreated;

        private void Awake()
        {
            MatchStart = new BattleMatchStartCoordinator(PlayerRoster);
        }

        public bool SetPlayerReady(PlayerRef playerRef, bool ready)
            => MatchStart.SetReady(playerRef, ready);

        public bool TryStartBattle()
        {
            if (_runner == null || !_runner.IsServer || SessionContext == null)
                return false;
            return MatchStart.TryStart();
        }

        public BattleSessionContext CreateSessionContext(
            string canonicalBalanceVersion,
            string canonicalContentHash,
            string battleContentVersion,
            string battleContentHash,
            long startedAtTick)
        {
            SessionContext = BattleSessionContext.FromRunner(
                _runner,
                canonicalBalanceVersion,
                canonicalContentHash,
                battleContentVersion,
                battleContentHash,
                startedAtTick);
            SessionContextCreated?.Invoke(SessionContext);
            return SessionContext;
        }

        public Task StartHostAsync(string sessionName, NetworkSceneInfo scene = default)
        {
            return StartHostAsync(sessionName, null, scene);
        }

        public Task StartHostAsync(string sessionName, string userId, NetworkSceneInfo scene = default)
            => StartAsync(GameMode.Host, sessionName, userId, scene);

        public Task StartClientAsync(string sessionName, NetworkSceneInfo scene = default)
        {
            return StartClientAsync(sessionName, null, scene);
        }

        public Task StartClientAsync(string sessionName, string userId, NetworkSceneInfo scene = default)
            => StartAsync(GameMode.Client, sessionName, userId, scene);

        public async Task StartHostOrClientAsync(string sessionName, string userId, NetworkSceneInfo scene = default)
        {
            await StartHostAsync(sessionName, userId, scene);
            if (State == BattleRunnerLifecycleState.RUNNING)
                return;

            string hostError = LastError;
            await StartClientAsync(sessionName, userId, scene);
            if (State != BattleRunnerLifecycleState.RUNNING && !string.IsNullOrWhiteSpace(hostError))
                LastError = $"Host failed: {hostError}; Client failed: {LastError}";
        }

        public async Task StopAsync(ShutdownReason reason = ShutdownReason.Ok)
        {
            if (_lifecycleTask != null && !_lifecycleTask.IsCompleted)
                await _lifecycleTask;
            if (_runner == null)
            {
                State = BattleRunnerLifecycleState.STOPPED;
                return;
            }

            _lifecycleTask = StopInternalAsync(reason);
            await _lifecycleTask;
        }

        private async Task StartAsync(GameMode mode, string sessionName, string userId, NetworkSceneInfo scene)
        {
            if (string.IsNullOrWhiteSpace(sessionName))
                throw new ArgumentException("A non-empty Fusion session name is required.", nameof(sessionName));
            if (_lifecycleTask != null && !_lifecycleTask.IsCompleted)
                throw new InvalidOperationException("A runner lifecycle operation is already in progress.");
            if (_runner != null && !_runner.IsShutdown)
                throw new InvalidOperationException("A Fusion runner is already active.");

            State = BattleRunnerLifecycleState.STARTING;
            LastError = null;
            _runnerObject = new GameObject("FusionRunner");
            // Fusion keeps the runner alive across network scene changes.
            // It must remain a root object for DontDestroyOnLoad to work.
            _runner = _runnerObject.AddComponent<NetworkRunner>();
            _identityCallbacks = new BattlePlayerIdentityCallbacks(PlayerRoster, userId);
            _runner.AddCallbacks(_identityCallbacks);
            var sceneManager = _runnerObject.GetComponent<INetworkSceneManager>()
                ?? _runnerObject.AddComponent<NetworkSceneManagerDefault>();
            var objectProvider = _runnerObject.GetComponent<INetworkObjectProvider>()
                ?? _runnerObject.AddComponent<NetworkObjectProviderDefault>();

            try
            {
                if (scene.SceneCount == 0)
                    scene = CreateActiveSceneInfo();

                Task<StartGameResult> startTask = _runner.StartGame(new StartGameArgs
                {
                    GameMode = mode,
                    SessionName = sessionName,
                    Scene = scene,
                    SceneManager = sceneManager,
                    ObjectProvider = objectProvider,
                    ConnectionToken = string.IsNullOrWhiteSpace(userId) ? null : BattlePlayerIdentityToken.Encode(userId),
                    OnGameStarted = _ => { }
                });
                _lifecycleTask = startTask;
                var result = await startTask;
                _lifecycleTask = null;
                if (result.Ok)
                {
                    State = BattleRunnerLifecycleState.RUNNING;
                    string role = mode == GameMode.Host ? "호스트 생성!" : "클라이언트 생성!";
                    Debug.Log($"[Fusion] {role} Session={sessionName} UserId={userId}");
                    return;
                }

                LastError = result.ErrorMessage;
                State = BattleRunnerLifecycleState.FAULTED;
                await StopInternalAsync(result.ShutdownReason);
            }
            catch (Exception exception)
            {
                _lifecycleTask = null;
                LastError = exception.Message;
                State = BattleRunnerLifecycleState.FAULTED;
                await StopInternalAsync(ShutdownReason.Error);
            }
        }

        private static NetworkSceneInfo CreateActiveSceneInfo()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.buildIndex < 0)
                return default;

            NetworkSceneInfo sceneInfo = default;
            sceneInfo.AddSceneRef(
                SceneRef.FromIndex(activeScene.buildIndex),
                LoadSceneMode.Single,
                LocalPhysicsMode.None,
                activeOnLoad: true);
            return sceneInfo;
        }

        private async Task StopInternalAsync(ShutdownReason reason)
        {
            State = BattleRunnerLifecycleState.STOPPING;
            var runner = _runner;
            var runnerObject = _runnerObject;
            _runner = null;
            _runnerObject = null;
            _identityCallbacks = null;
            PlayerRoster.Clear();
            SessionContext = null;
            MatchStart.Reset();
            if (runner != null && !runner.IsShutdown)
                await runner.Shutdown(true, reason);
            else if (runnerObject != null)
                Destroy(runnerObject);
            State = BattleRunnerLifecycleState.STOPPED;
            _lifecycleTask = null;
        }

        private void OnDestroy()
        {
            if (_runner != null && !_runner.IsShutdown)
                _ = StopInternalAsync(ShutdownReason.GameClosed);
        }
    }
}
