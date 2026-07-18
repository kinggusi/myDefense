using System;
using System.Threading.Tasks;
using Fusion;
using MyDefense.Battle.Runtime;
using UnityEngine;

namespace MyDefense.Battle.Tests
{
    /// <summary>
    /// Development-only connection harness. Keep this component in the
    /// FusionConnectionTest scene and never add it to a production scene.
    /// </summary>
    [RequireComponent(typeof(BattleRunnerLifecycle))]
    public sealed class FusionConnectionTestPanel : MonoBehaviour
    {
        [SerializeField] private string _sessionName = "MyDefense-Dev";
        [SerializeField] private string _userId = "dev-user";

        private BattleRunnerLifecycle _lifecycle;
        private BattleWaveStateAuthority _authority;
        private string _lastOperation = "Idle";
        private string _lastError;

        private void Awake()
        {
            _lifecycle = GetComponent<BattleRunnerLifecycle>();
            _authority = GetComponent<BattleWaveStateAuthority>();
        }

        private void OnGUI()
        {
            const float width = 720f;
            const int fontSize = 24;
            int previousFontSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = fontSize;
            GUILayout.BeginArea(new Rect(24f, 24f, width, 460f), GUI.skin.window);
            GUILayout.Label("Fusion Connection Test (Development Only)");
            GUILayout.Label("Session Name");
            _sessionName = GUILayout.TextField(_sessionName, GUILayout.Height(38f));
            GUILayout.Label("User ID");
            _userId = GUILayout.TextField(_userId, GUILayout.Height(38f));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Host", GUILayout.Height(54f)))
                _ = StartHostAsync();
            if (GUILayout.Button("Start Client", GUILayout.Height(54f)))
                _ = StartClientAsync();
            if (GUILayout.Button("Stop", GUILayout.Height(54f)))
                _ = StopAsync();
            GUILayout.EndHorizontal();

            if (_authority != null && _authority.Object != null && _authority.Object.IsValid)
            {
                GUILayout.Space(12f);
                GUILayout.Label($"P1 Gold: {_authority.Player1InGameGold}    P2 Gold: {_authority.Player2InGameGold}");
                int localSlot = 0;
                if (_lifecycle.Runner != null && _authority != null)
                    localSlot = _authority.GetNetworkedPlayerSlot(_lifecycle.Runner.LocalPlayer);
                if (localSlot == 0 && _lifecycle.Runner != null
                    && _lifecycle.PlayerRoster.TryGet(_lifecycle.Runner.LocalPlayer, out BattlePlayerIdentity localIdentity))
                    localSlot = localIdentity.PlayerSlot;
                GUILayout.Label($"Local Slot: {localSlot} (1=top field, 2=bottom field)");
                if (GUILayout.Button("Kidnap (local player)", GUILayout.Height(48f)))
                    _authority.RequestKidnap();
            }

            GUILayout.Space(12f);
            GUILayout.Label($"State: {_lifecycle?.State}");
            GUILayout.Label($"Runner Running: {_lifecycle?.Runner?.IsRunning == true}");
            GUILayout.Label($"Operation: {_lastOperation}");
            if (!string.IsNullOrWhiteSpace(_lastError))
                GUILayout.Label($"Error: {_lastError}");
            GUILayout.EndArea();
            GUI.skin.label.fontSize = previousFontSize;
        }

        private async Task StartHostAsync()
        {
            await StartAsync(() => _lifecycle.StartHostAsync(_sessionName, _userId));
        }

        private async Task StartClientAsync()
        {
            await StartAsync(() => _lifecycle.StartClientAsync(_sessionName, _userId));
        }

        private async Task StartAsync(Func<Task> operation)
        {
            _lastError = null;
            _lastOperation = "Starting";
            try
            {
                await operation();
                _lastOperation = "Started";
            }
            catch (Exception exception)
            {
                _lastError = exception.Message;
                _lastOperation = "Failed";
            }
        }

        private async Task StopAsync()
        {
            _lastError = null;
            _lastOperation = "Stopping";
            try
            {
                await _lifecycle.StopAsync();
                _lastOperation = "Stopped";
            }
            catch (Exception exception)
            {
                _lastError = exception.Message;
                _lastOperation = "Failed";
            }
        }
    }
}
