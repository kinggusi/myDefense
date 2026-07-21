using UnityEngine;
using UnityEngine.UI;
using MyDefense.Battle.Runtime;

namespace MyDefense.Battle.Presentation
{
    /// <summary>
    /// Production Battle UI bridge. It never changes Gold locally; all spending
    /// is requested through BattleWaveStateAuthority and reflected from network state.
    /// </summary>
    public sealed class FusionBattleUiController : MonoBehaviour
    {
        [SerializeField] private Button _kidnapButton;
        [SerializeField] private Text _goldText;
        [SerializeField] private Text _costText;
        [SerializeField] private BattleRunnerLifecycle _runnerLifecycle;
        [SerializeField] private BattleWaveStateAuthority _stateAuthority;

        private float _nextAllowedClickTime;

        private void Awake()
        {
            _runnerLifecycle ??= FindFirstObjectByType<BattleRunnerLifecycle>();
            _stateAuthority ??= FindFirstObjectByType<BattleWaveStateAuthority>();
            _kidnapButton ??= GameObject.Find("Btn_Summon")?.GetComponent<Button>();
            _goldText ??= GameObject.Find("Text_InGame_Gold")?.GetComponent<Text>();
            if (_costText == null && _kidnapButton != null)
                _costText = _kidnapButton.GetComponentInChildren<Text>(true);
        }

        private void OnEnable()
        {
            if (_stateAuthority != null)
                _stateAuthority.KidnapApplied += HandleKidnapApplied;
            if (_kidnapButton != null)
                _kidnapButton.onClick.AddListener(OnClickKidnap);
        }

        private void OnDisable()
        {
            if (_stateAuthority != null)
                _stateAuthority.KidnapApplied -= HandleKidnapApplied;
            if (_kidnapButton != null)
                _kidnapButton.onClick.RemoveListener(OnClickKidnap);
        }

        private void Update()
        {
            if (_stateAuthority == null
                || !_stateAuthority.IsSpawnedForAccess
                || _runnerLifecycle == null
                || _goldText == null)
                return;

            int slot = _runnerLifecycle.Runner == null
                ? 0
                : _stateAuthority.GetNetworkedPlayerSlot(_runnerLifecycle.Runner.LocalPlayer);
            bool running = _runnerLifecycle.State == BattleRunnerLifecycleState.RUNNING && slot != 0;
            int kidnapCount = running ? _stateAuthority.GetKidnapCount(slot) : 0;
            int gold = running ? _stateAuthority.GetInGameGoldForPlayerSlot(slot) : 0;
            _goldText.text = $"Gold: {gold:N0}";

            if (_costText != null && _stateAuthority.Executor != null
                && _stateAuthority.Executor.TryGetCanonicalSummonCost(kidnapCount, out int cost))
                _costText.text = $"왹져 소환 ({cost:N0} G)";

            if (_kidnapButton != null)
                _kidnapButton.interactable = running && Time.unscaledTime >= _nextAllowedClickTime;
        }

        public void OnClickKidnap()
        {
            if (_stateAuthority == null
                || !_stateAuthority.IsSpawnedForAccess
                || _runnerLifecycle?.Runner == null)
                return;
            int slot = _stateAuthority.GetNetworkedPlayerSlot(_runnerLifecycle.Runner.LocalPlayer);
            if (slot == 0 || Time.unscaledTime < _nextAllowedClickTime)
                return;

            _nextAllowedClickTime = Time.unscaledTime + 0.25f;
            _stateAuthority.RequestKidnap();
        }

        private void HandleKidnapApplied(int playerSlot, int _)
        {
            if (_runnerLifecycle?.Runner != null
                && _stateAuthority.GetNetworkedPlayerSlot(_runnerLifecycle.Runner.LocalPlayer) == playerSlot)
                _nextAllowedClickTime = Time.unscaledTime + 0.1f;
        }
    }
}
