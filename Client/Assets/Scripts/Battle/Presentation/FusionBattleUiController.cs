using UnityEngine;
using UnityEngine.UI;
using MyDefense.Battle.Balance.Canonical;
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
        [SerializeField] private FusionKidnapBoardView _boardView;

        private float _nextAllowedClickTime;
        private float _nextAllowedMutationClickTime;
        private float _nextAllowedResonanceClickTime;
        private GUIStyle _choiceTitleStyle;
        private GUIStyle _choiceButtonStyle;

        private void Awake()
        {
            _runnerLifecycle ??= FindFirstObjectByType<BattleRunnerLifecycle>();
            _stateAuthority ??= FindFirstObjectByType<BattleWaveStateAuthority>();
            _boardView ??= FindFirstObjectByType<FusionKidnapBoardView>();
            _kidnapButton ??= GameObject.Find("Btn_Summon")?.GetComponent<Button>();
            _goldText ??= GameObject.Find("Text_InGame_Gold")?.GetComponent<Text>();
            if (_goldText != null)
            {
                _goldText.horizontalOverflow = HorizontalWrapMode.Overflow;
                _goldText.fontSize = Mathf.Min(42, _goldText.fontSize);
            }
            if (_costText == null && _kidnapButton != null)
                _costText = _kidnapButton.GetComponentInChildren<Text>(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevelopmentMythicMutationFixturePanel fixturePanel =
                GetComponent<DevelopmentMythicMutationFixturePanel>()
                ?? gameObject.AddComponent<DevelopmentMythicMutationFixturePanel>();
            fixturePanel.Initialize(_stateAuthority);
#endif
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

            bool mythicChoiceActive = running && _stateAuthority.IsMythicChoiceActive(slot);

            if (_costText != null && _stateAuthority.Executor != null
                && _stateAuthority.Executor.TryGetCanonicalSummonCost(kidnapCount, out int cost))
                _costText.text = $"왹져 소환 ({cost:N0} G)";

            if (_kidnapButton != null)
                _kidnapButton.interactable = running && !mythicChoiceActive && Time.unscaledTime >= _nextAllowedClickTime;
        }

        private void OnGUI()
        {
            if (_stateAuthority == null || _runnerLifecycle?.Runner == null
                || !_stateAuthority.IsSpawnedForAccess)
                return;

            int playerSlot = _stateAuthority.GetNetworkedPlayerSlot(_runnerLifecycle.Runner.LocalPlayer);
            if (playerSlot == 0)
                return;

            _choiceTitleStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _choiceButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };

            if (!_stateAuthority.IsMythicChoiceActive(playerSlot))
            {
                DrawResonancePanel(playerSlot);
                DrawMutationPanel(playerSlot);
                return;
            }

            const float width = 820f;
            const float height = 520f;
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 15f, panel.y + 20f, width - 30f, 55f), "MYTHIC AUGMENT", _choiceTitleStyle);
            GUI.Label(new Rect(panel.x + 15f, panel.y + 75f, width - 30f, 32f), "Choose one powerful mutation", GUI.skin.label);

            for (int index = 0; index < 3; index++)
            {
                long alienId = _stateAuthority.GetMythicChoiceCandidate(playerSlot, index);
                Rect button = new Rect(panel.x + 25f + index * 265f, panel.y + 125f, 245f, 265f);
                if (alienId > 0 && GUI.Button(button, $"ALIEN {alienId}\n\nAUGMENT {index + 1}\n\nSELECT", _choiceButtonStyle))
                {
                    _stateAuthority.RequestMythicChoice(index);
                    Event.current.Use();
                    return;
                }
            }

            int freeUsed = _stateAuthority.GetMythicFreeRerolls(playerSlot);
            int paidUsed = _stateAuthority.GetMythicPaidRerolls(playerSlot);
            BattleMergeResultResolver.TryGetMythicRerollPolicy(
                out int freeLimit, out int paidLimit, out int paidCost, out _);
            Rect rerollButton = new Rect(panel.x + 25f, panel.y + 425f, width - 50f, 55f);
            if (GUI.Button(rerollButton,
                    $"Reroll (free {freeUsed}/{freeLimit}, paid {paidUsed}/{paidLimit}, {paidCost:N0} Gold)", _choiceButtonStyle))
            {
                _stateAuthority.RequestMythicReroll();
                Event.current.Use();
                return;
            }
        }

        private void DrawResonancePanel(int playerSlot)
        {
            BattleWaveExecutor executor = _stateAuthority.Executor;
            if (executor == null)
                return;

            const float width = 360f;
            const float height = 145f;
            Rect panel = new Rect(Screen.width - width - 20f, Screen.height - height - 20f, width, height);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 15f, panel.y + 10f, width - 30f, 28f), "BATTLE RESONANCE", _choiceTitleStyle);

            DrawResonanceButton(executor, playerSlot, CanonicalResonanceTrack.NORMAL,
                new Rect(panel.x + 15f, panel.y + 48f, width - 30f, 38f), "Normal~Legendary");
            DrawResonanceButton(executor, playerSlot, CanonicalResonanceTrack.MYTHIC,
                new Rect(panel.x + 15f, panel.y + 94f, width - 30f, 38f), "Mythic");
        }

        private void DrawResonanceButton(
            BattleWaveExecutor executor,
            int playerSlot,
            CanonicalResonanceTrack track,
            Rect rect,
            string label)
        {
            int currentLevel = _stateAuthority.GetResonanceLevel(playerSlot, track);
            bool hasNext = executor.TryGetCanonicalResonanceLevel(track, currentLevel + 1, out CanonicalResonanceLevel next);
            int gold = _stateAuthority.GetInGameGoldForPlayerSlot(playerSlot);
            string buttonLabel = hasNext
                ? $"{label} Lv.{currentLevel} -> {currentLevel + 1} ({next.RequiredGold:N0} G)"
                : $"{label} Lv.{currentLevel} MAX";

            bool previousEnabled = GUI.enabled;
            GUI.enabled = hasNext && gold >= next.RequiredGold && Time.unscaledTime >= _nextAllowedResonanceClickTime;
            if (GUI.Button(rect, buttonLabel, _choiceButtonStyle))
            {
                _nextAllowedResonanceClickTime = Time.unscaledTime + 0.25f;
                _stateAuthority.RequestResonanceUpgrade(track);
                Event.current.Use();
            }
            GUI.enabled = previousEnabled;
        }

        private void DrawMutationPanel(int playerSlot)
        {
            if (_boardView == null)
                return;
            int slotIndex = _boardView.EnsureSelectedLocalMythicSlot(playerSlot);
            if (slotIndex < 0)
                return;

            string mutationType = _stateAuthority.GetBoardMutationType(playerSlot, slotIndex);
            byte mutationState = _stateAuthority.GetBoardMutationState(playerSlot, slotIndex);
            int rerollCount = _stateAuthority.GetBoardMutationRerollCount(playerSlot, slotIndex);
            bool canAct = _stateAuthority.TryGetMutationAction(playerSlot, slotIndex, out bool initialActivation, out int cost);
            int gold = _stateAuthority.GetInGameGoldForPlayerSlot(playerSlot);

            const float width = 360f;
            const float height = 190f;
            Rect panel = new Rect(20f, Screen.height - height - 20f, width, height);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 15f, panel.y + 10f, width - 30f, 28f), "MYTHIC MUTATION", _choiceTitleStyle);

            if (GUI.Button(new Rect(panel.x + 15f, panel.y + 48f, 42f, 30f), "<"))
                slotIndex = _boardView.SelectNextLocalMythicSlot(playerSlot, -1);
            GUI.Label(new Rect(panel.x + 68f, panel.y + 50f, 220f, 26f), $"Slot {slotIndex + 1} / Alien {_stateAuthority.GetBoardAlienId(playerSlot, slotIndex)}");
            if (GUI.Button(new Rect(panel.x + width - 57f, panel.y + 48f, 42f, 30f), ">"))
                slotIndex = _boardView.SelectNextLocalMythicSlot(playerSlot, 1);

            string stateLabel = mutationState == 4 ? "SEALED" : mutationState == 3 ? "ACTIVE" : "PURE";
            GUI.Label(new Rect(panel.x + 15f, panel.y + 85f, width - 30f, 26f),
                $"State: {stateLabel}   Type: {(string.IsNullOrWhiteSpace(mutationType) ? "NONE" : mutationType)}   Rerolls: {rerollCount}");

            string actionLabel = initialActivation ? $"Activate Mutation ({cost:N0} G)" : $"Reroll Mutation ({cost:N0} G)";
            bool previousEnabled = GUI.enabled;
            GUI.enabled = canAct && gold >= cost && Time.unscaledTime >= _nextAllowedMutationClickTime;
            if (GUI.Button(new Rect(panel.x + 15f, panel.y + 125f, width - 30f, 48f), actionLabel, _choiceButtonStyle))
            {
                _nextAllowedMutationClickTime = Time.unscaledTime + 0.25f;
                _stateAuthority.RequestMutation(slotIndex);
                Event.current.Use();
            }
            GUI.enabled = previousEnabled;
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

        private void HandleKidnapApplied(int playerSlot, int _, long __)
        {
            if (_runnerLifecycle?.Runner != null
                && _stateAuthority.GetNetworkedPlayerSlot(_runnerLifecycle.Runner.LocalPlayer) == playerSlot)
                _nextAllowedClickTime = Time.unscaledTime + 0.1f;
        }
    }
}
