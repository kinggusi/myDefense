using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

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
        private Task _lifecycleTask;

        public BattleRunnerLifecycleState State { get; private set; } = BattleRunnerLifecycleState.STOPPED;
        public NetworkRunner Runner => _runner;
        public string LastError { get; private set; }

        public Task StartHostAsync(string sessionName, NetworkSceneInfo scene = default)
        {
            return StartAsync(GameMode.Host, sessionName, scene);
        }

        public Task StartClientAsync(string sessionName, NetworkSceneInfo scene = default)
        {
            return StartAsync(GameMode.Client, sessionName, scene);
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

        private async Task StartAsync(GameMode mode, string sessionName, NetworkSceneInfo scene)
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
            _runnerObject.transform.SetParent(transform, false);
            _runner = _runnerObject.AddComponent<NetworkRunner>();
            var sceneManager = _runnerObject.GetComponent<INetworkSceneManager>()
                ?? _runnerObject.AddComponent<NetworkSceneManagerDefault>();
            var objectProvider = _runnerObject.GetComponent<INetworkObjectProvider>()
                ?? _runnerObject.AddComponent<NetworkObjectProviderDefault>();

            try
            {
                Task<StartGameResult> startTask = _runner.StartGame(new StartGameArgs
                {
                    GameMode = mode,
                    SessionName = sessionName,
                    Scene = scene,
                    SceneManager = sceneManager,
                    ObjectProvider = objectProvider,
                    OnGameStarted = _ => { }
                });
                _lifecycleTask = startTask;
                var result = await startTask;
                _lifecycleTask = null;
                if (result.Ok)
                {
                    State = BattleRunnerLifecycleState.RUNNING;
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

        private async Task StopInternalAsync(ShutdownReason reason)
        {
            State = BattleRunnerLifecycleState.STOPPING;
            var runner = _runner;
            var runnerObject = _runnerObject;
            _runner = null;
            _runnerObject = null;
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
