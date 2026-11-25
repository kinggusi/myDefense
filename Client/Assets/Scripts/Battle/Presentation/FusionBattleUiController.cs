using UnityEngine;
using UnityEngine.UI;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using System;
using System.Collections.Generic;
using Unity.Profiling;

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
        private BattleHudPresentation _presentation;
        private BattleMonsterPresentationCoordinator _monsterPresentation;
        private readonly BattlePresentationValueCache<int,(int free,int paid,int cost)> _rerollDisplay = new();
        private readonly BattlePresentationValueCache<(int player,int slot,long alien,byte grade,byte state,int rerolls,string mutation),(bool available,bool initial,int cost)> _mutationDisplay = new();
        private float _nextDisplayRefresh;
        private BattleWaveExecutor _displayExecutor;
        private string _displayBalanceHash;
        private static readonly ProfilerMarker DisplayMarker=new("Battle.Hud.Refresh");
        public int DisplayPolicyReadCount=>_rerollDisplay.ReadCount;
        public int DisplayMutationReadCount=>_mutationDisplay.ReadCount;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _trace;
        private float _traceTime,_traceWorst;
        private int _traceFrames;
#endif

        private void Awake()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _trace=Array.Exists(Environment.GetCommandLineArgs(),arg=>string.Equals(arg,"-battlePresentationTrace",StringComparison.OrdinalIgnoreCase));
#endif
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
            _presentation = GetComponent<BattleHudPresentation>();
            if (_presentation == null) _presentation = gameObject.AddComponent<BattleHudPresentation>();
            _presentation.Initialize(this);
            _monsterPresentation=GetComponent<BattleMonsterPresentationCoordinator>();
            if(_monsterPresentation==null)_monsterPresentation=gameObject.AddComponent<BattleMonsterPresentationCoordinator>();
            _monsterPresentation.Initialize(_runnerLifecycle,_boardView);
        }

        private void OnEnable()
        {
            _rerollDisplay.Clear(); _mutationDisplay.Clear(); _nextDisplayRefresh=0;
            if (_presentation != null) _presentation.Show();
            _monsterPresentation?.Resume();
            if (_stateAuthority != null)
            {
                _stateAuthority.KidnapApplied -= HandleKidnapApplied;
                _stateAuthority.KidnapApplied += HandleKidnapApplied;
            }
            if (_kidnapButton != null)
            {
                _kidnapButton.onClick.RemoveListener(OnClickKidnap);
                _kidnapButton.onClick.AddListener(OnClickKidnap);
            }
        }

        private void OnDisable()
        {
            if (_presentation != null) _presentation.Hide();
            _monsterPresentation?.Suspend();
            if (_stateAuthority != null)
                _stateAuthority.KidnapApplied -= HandleKidnapApplied;
            if (_kidnapButton != null)
                _kidnapButton.onClick.RemoveListener(OnClickKidnap);
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(_trace)
            {
                _traceTime+=Time.unscaledDeltaTime;_traceFrames++;_traceWorst=Mathf.Max(_traceWorst,Time.unscaledDeltaTime);
                if(_traceTime>=2)
                {
                    Debug.Log($"[BattlePresentation] displayPolicyReads={DisplayPolicyReadCount} mutationReads={DisplayMutationReadCount} picks={_boardView?.Interaction?.PhysicsPickCount??0} cachedPoses={BattleUnitPoseCache.PoseCount} poseCreations={BattleUnitPoseCache.CreatedCount} frameMeanMs={1000*_traceTime/Mathf.Max(1,_traceFrames):F2} frameMaxMs={1000*_traceWorst:F2}");
                    _traceTime=0;_traceFrames=0;_traceWorst=0;
                }
            }
#endif
            if (_presentation != null && Time.unscaledTime>=_nextDisplayRefresh)
            {
                using(DisplayMarker.Auto()) _presentation.Render(CaptureDisplayState());
                _nextDisplayRefresh=Time.unscaledTime+.05f;
            }
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
            if (_presentation != null && _presentation.IsVisible) return;
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
            _nextDisplayRefresh=0;
        }

        // Presentation adapters: every command still uses the original State Authority request.
        public void SelectMythic(int direction)
        {
            if (TryGetActivePlayer(out int slot)) _boardView?.SelectNextLocalMythicSlot(slot, direction);
        }

        public void OnClickMutation()
        {
            if (!TryGetActivePlayer(out int player) || _boardView == null) return;
            int slot = _boardView.EnsureSelectedLocalMythicSlot(player);
            if (slot < 0 || !CaptureDisplayState().CanMutate) return;
            _nextAllowedMutationClickTime = Time.unscaledTime + .25f;
            _stateAuthority.RequestMutation(slot);
        }

        public void OnClickResonance(CanonicalResonanceTrack track)
        {
            if (!TryGetActivePlayer(out _)) return;
            BattleHudPresentation.DisplayState state = CaptureDisplayState();
            if (!(track == CanonicalResonanceTrack.NORMAL ? state.CanNormalResonance : state.CanMythicResonance)) return;
            _nextAllowedResonanceClickTime = Time.unscaledTime + .25f;
            _stateAuthority.RequestResonanceUpgrade(track);
        }

        public void OnClickMythicChoice(int index)
        {
            if (index >= 0 && index < 3 && CaptureDisplayState().CanChoose) _stateAuthority.RequestMythicChoice(index);
        }

        public void OnClickMythicReroll()
        {
            if (CaptureDisplayState().CanReroll) _stateAuthority.RequestMythicReroll();
        }

        private bool TryGetActivePlayer(out int slot)
        {
            slot = 0;
            if (_stateAuthority == null || !_stateAuthority.IsSpawnedForAccess || _runnerLifecycle?.Runner == null) return false;
            slot = _stateAuthority.GetNetworkedPlayerSlot(_runnerLifecycle.Runner.LocalPlayer);
            if (slot != 1 && slot != 2) return false;
            PlayerBattleState player = slot == 1 ? _stateAuthority.Player1BattleState : _stateAuthority.Player2BattleState;
            return BattleHudPresentation.AllowsCommands(_runnerLifecycle.State == BattleRunnerLifecycleState.RUNNING,
                _stateAuthority.MatchState,player);
        }

        public BattleHudPresentation.DisplayState CaptureDisplayState()
        {
            var display = new BattleHudPresentation.DisplayState { Map = _runnerLifecycle?.MapId ?? string.Empty };
            if (_stateAuthority == null || !_stateAuthority.IsSpawnedForAccess || _runnerLifecycle?.Runner == null) return display;
            int local = _stateAuthority.GetNetworkedPlayerSlot(_runnerLifecycle.Runner.LocalPlayer);
            if (local != 1 && local != 2) return display;
            BattleWaveExecutor executor=_stateAuthority.Executor;
            string balanceHash=executor?.BattleBalanceContentHash;
            if(_displayExecutor!=executor||_displayBalanceHash!=balanceHash)
            {_displayExecutor=executor;_displayBalanceHash=balanceHash;_rerollDisplay.Clear();_mutationDisplay.Clear();}
            string map = _stateAuthority.AuthoritativeMapId.ToString();
            if (!string.IsNullOrWhiteSpace(map)) display.Map = map;
            display.Wave = _stateAuthority.CurrentWave;
            display.Gold = _stateAuthority.GetInGameGoldForPlayerSlot(local);
            display.SelectionDetail=_boardView?.Interaction?.SelectionDetail??string.Empty;
            // Reached flags are sticky audit facts. Recover the current warning display
            // from replicated counts and the same canonical thresholds on either peer.
            int warningThreshold=_stateAuthority.Executor == null ? int.MaxValue : _stateAuthority.Executor.MonsterWarningThreshold;
            int dangerThreshold=_stateAuthority.Executor == null ? int.MaxValue : _stateAuthority.Executor.MonsterDangerThreshold;
            BattleHudPresentation.AssignPlayerDisplays(display,local,display.Map.StartsWith("DAILY_",System.StringComparison.Ordinal),_stateAuthority.PlayerMonsterLimit,
                BattleHudPresentation.PlayerDisplay.FromReplicatedCount(_stateAuthority.Player1UserId.ToString(),_stateAuthority.Player1AliveMonsterCount,
                    _stateAuthority.Player1BattleState,warningThreshold,dangerThreshold),
                BattleHudPresentation.PlayerDisplay.FromReplicatedCount(_stateAuthority.Player2UserId.ToString(),_stateAuthority.Player2AliveMonsterCount,
                    _stateAuthority.Player2BattleState,warningThreshold,dangerThreshold));
            PlayerBattleState playerState = local == 1 ? _stateAuthority.Player1BattleState : _stateAuthority.Player2BattleState;
            display.Status = _stateAuthority.MatchState == MatchState.CLEARED ? "전투 클리어"
                : _stateAuthority.MatchState == MatchState.FAILED ? "전투 종료 · 실패"
                : playerState != PlayerBattleState.ACTIVE ? "탈락 · 동료 필드 관전 중"
                : _stateAuthority.BossTimer.IsRunning ? $"BOSS · 남은 시간 {_stateAuthority.GetBossRemainingSeconds()}초"
                : display.Map.StartsWith("DAILY_",System.StringComparison.Ordinal) ? "솔로 전투 진행 중" : "협동 전투 진행 중";
            bool canAct = TryGetActivePlayer(out _);
            display.ChoiceActive = _stateAuthority.IsMythicChoiceActive(local);
            display.CanChoose = display.ChoiceActive && canAct;
            if(display.ChoiceActive)display.ChoiceHint=$"후보 3종 중 하나를 선택하세요 · {_stateAuthority.GetMythicChoiceRemainingSeconds(local)}초";
            bool hasEmpty = false;
            for (int i = 0; i < 24; i++) if (!_stateAuthority.IsBoardOccupied(local,i)) { hasEmpty = true; break; }
            bool hasCost = _stateAuthority.Executor != null && _stateAuthority.Executor.TryGetCanonicalSummonCost(_stateAuthority.GetKidnapCount(local), out display.KidnapCost);
            display.CanKidnap = canAct && !display.ChoiceActive && hasEmpty && hasCost && display.Gold >= display.KidnapCost && Time.unscaledTime >= _nextAllowedClickTime;
            for (int i = 0; i < 3; i++)
            {
                long id = _stateAuthority.GetMythicChoiceCandidate(local,i);
                display.Choices[i] = id > 0 ? "신화 후보 " + (i+1) + "\n\nAlien " + id + "\n\n선택" : string.Empty;
            }
            int free = _stateAuthority.GetMythicFreeRerolls(local), paid = _stateAuthority.GetMythicPaidRerolls(local);
            object session=(object)_runnerLifecycle.SessionContext ?? _runnerLifecycle.Runner;
            var policy=_rerollDisplay.Get(session,0,()=>
            {
                BattleMergeResultResolver.TryGetMythicRerollPolicy(out int f,out int p,out int c,out _);
                return (f,p,c);
            });
            int freeLimit=policy.free,paidLimit=policy.paid,paidCost=policy.cost;
            display.Reroll = $"후보 리롤  ·  무료 {free}/{freeLimit}  ·  유료 {paid}/{paidLimit} ({paidCost:N0} G)";
            display.CanReroll = display.CanChoose && (free < freeLimit || (paid < paidLimit && display.Gold >= paidCost));
            FillResonance(display,local,CanonicalResonanceTrack.NORMAL,canAct && !display.ChoiceActive);
            FillResonance(display,local,CanonicalResonanceTrack.MYTHIC,canAct && !display.ChoiceActive);
            int selected = _boardView == null ? -1 : _boardView.EnsureSelectedLocalMythicSlot(local);
            if (selected >= 0)
            {
                display.CanSelectMythic = canAct && !display.ChoiceActive;
                string mutation = _stateAuthority.GetBoardMutationType(local,selected);
                byte mutationState = _stateAuthority.GetBoardMutationState(local,selected);
                string label = mutationState == 4 ? "봉인" : mutationState == 3 ? "활성" : "순수";
                var mutationKey=(local,selected,_stateAuthority.GetBoardAlienId(local,selected),_stateAuthority.GetBoardGrade(local,selected),mutationState,_stateAuthority.GetBoardMutationRerollCount(local,selected),mutation);
                var mutationAction=_mutationDisplay.Get(session,mutationKey,()=>
                {
                    bool available=_stateAuthority.TryGetMutationAction(local,selected,out bool activate,out int value);
                    return (available,activate,value);
                });
                bool action=mutationAction.available,initial=mutationAction.initial;int cost=mutationAction.cost;
                display.MutationDetail = $"슬롯 {selected+1} · Alien {_stateAuthority.GetBoardAlienId(local,selected)}\n{label} · {(string.IsNullOrWhiteSpace(mutation)?"NONE":mutation)}\n재변이 {_stateAuthority.GetBoardMutationRerollCount(local,selected)}회";
                display.MutationAction = $"{(initial?"생체변이 활성화":"생체변이 재설정")}  ·  {cost:N0} G";
                display.CanMutate = canAct && !display.ChoiceActive && action && display.Gold >= cost && Time.unscaledTime >= _nextAllowedMutationClickTime;
            }
            return display;
        }

        private void FillResonance(BattleHudPresentation.DisplayState display, int player, CanonicalResonanceTrack track, bool canAct)
        {
            int level = _stateAuthority.GetResonanceLevel(player,track);
            bool nextExists = false; CanonicalResonanceLevel next = default;
            if (_stateAuthority.Executor != null) nextExists = _stateAuthority.Executor.TryGetCanonicalResonanceLevel(track,level+1,out next);
            string name = track == CanonicalResonanceTrack.NORMAL ? "일반 공명" : "신화 공명";
            string text = nextExists ? $"{name} Lv.{level} → {level+1}\n{next.RequiredGold:N0} G" : $"{name} Lv.{level} · MAX";
            bool enabled = canAct && nextExists && display.Gold >= next.RequiredGold && Time.unscaledTime >= _nextAllowedResonanceClickTime;
            if (track == CanonicalResonanceTrack.NORMAL) { display.NormalResonance = text; display.CanNormalResonance = enabled; }
            else { display.MythicResonance = text; display.CanMythicResonance = enabled; }
        }

        private void HandleKidnapApplied(int playerSlot, int _, long __)
        {
            _nextDisplayRefresh=0;
            if (_runnerLifecycle?.Runner != null
                && _stateAuthority.GetNetworkedPlayerSlot(_runnerLifecycle.Runner.LocalPlayer) == playerSlot)
                _nextAllowedClickTime = Time.unscaledTime + 0.1f;
        }
    }

    /// <summary>One presentation query per immutable session/key. It never caches command approval.</summary>
    public sealed class BattlePresentationValueCache<TKey,TValue>
    {
        private bool _hasValue; private object _session;private TKey _key;private TValue _value;
        public int ReadCount {get;private set;}
        public TValue Get(object session,TKey key,Func<TValue> read)
        {
            if(_hasValue && ReferenceEquals(_session,session) && EqualityComparer<TKey>.Default.Equals(_key,key))return _value;
            _value=read();ReadCount++;_key=key;_session=session;_hasValue=true;return _value;
        }
        public void Clear(){_hasValue=false;_session=null;_key=default;_value=default;}
    }
}
