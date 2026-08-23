using System;
using System.Threading.Tasks;
using UnityEngine;
using MyDefense.Battle.Runtime;

namespace MyDefense.Battle
{
    /// <summary>
    /// Production Battle entry point. Both peers may use auto mode: the first
    /// process becomes Host and subsequent processes fall back to Client.
    /// Role/session/user can be supplied through RuntimeEnvironmentConfig.
    /// </summary>
    public sealed class BattleAutoSessionStarter : MonoBehaviour
    {
        [SerializeField] private bool _startOnEnable = true;
        [SerializeField] private string _defaultSessionName = "MyDefense-Dev";

        private BattleRunnerLifecycle _lifecycle;
        private Task _startTask;

        private void OnEnable()
        {
            if (_startOnEnable)
                _startTask = StartSessionAsync();
        }

        private async Task StartSessionAsync()
        {
            _lifecycle = GetComponent<BattleRunnerLifecycle>()
                ?? FindFirstObjectByType<BattleRunnerLifecycle>();
            if (_lifecycle == null || _lifecycle.State != BattleRunnerLifecycleState.STOPPED)
                return;

            string sessionName = string.IsNullOrWhiteSpace(RuntimeEnvironmentConfig.FusionSessionName)
                ? _defaultSessionName
                : RuntimeEnvironmentConfig.FusionSessionName;
            string userId = RuntimeEnvironmentConfig.FusionUserId;
            if (string.IsNullOrWhiteSpace(userId))
                userId = $"peer-{System.Diagnostics.Process.GetCurrentProcess().Id}";

            string role = RuntimeEnvironmentConfig.FusionRole;
            switch (role)
            {
                case "host":
                    await _lifecycle.StartHostAsync(sessionName, userId);
                    break;
                case "client":
                    await _lifecycle.StartClientAsync(sessionName, userId);
                    break;
                default:
                    await _lifecycle.StartHostOrClientAsync(sessionName, userId);
                    break;
            }

            if (_lifecycle.State == BattleRunnerLifecycleState.RUNNING)
                Debug.Log($"[Fusion] 자동 세션 입장 완료: role={role}, session={sessionName}, user={userId}");
            else
                Debug.LogError($"[Fusion] 자동 세션 입장 실패: {_lifecycle.LastError}");
        }
    }
}
