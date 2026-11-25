#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Combat;
using MyDefense.Battle.Runtime;
using UnityEngine;

namespace MyDefense.Battle.Showroom
{
    public sealed class BattleShowroomCombat : BattleShowroomWindow
    {
        private const string PreferenceId = "Combat";
        private const string PlayerId = "showroom-local";
        private const string OppositeFieldId = "showroom-opposite-field";
        private const int BoardSize = 24;
        private const int MaxStartingGold = 100000;
        internal const uint SessionTypeId = 0x5348524D;
        private static readonly string[] Presets =
        {
            "Empty board", "Matching pair", "Different pair", "Full board",
            "Insufficient Gold", "Mythic + Mutation", "FAILED", "Monster Wave", "Boss Wave"
        };
        private static readonly string[] TerminalNotes =
        {
            "A selected planet Wave ends this one-Wave fixture without a CLEARED verdict.",
            "Player elimination is reached through the field Monster limit. Spectating another connection needs a second Client.",
            "Legendary preset, arbitrary slot or unlock seed and trusted server checks await their setup contracts."
        };

        [SerializeField] private GameObject _sessionTemplate;
        [SerializeField] private BattleShowroomCombatProbe _probe;

        [Serializable]
        public sealed class Settings
        {
            public int preset;
            public string gold = "100000";
            public string mythicId = "29";
            public int mutation = 4;
        }

        private Settings _settings;
        private NetworkRunner _runner;
        private GameObject _session;
        private BattleWaveExecutor _executor;
        private BattleWaveStateAuthority _authority;
        private string _status = "Start an offline session.";
        private string _commandResult;
        private bool _busy;
        private int _sourceSlot;
        private int _targetSlot = 1;
        private string _launchMap = "NEPTUNE";
        private int _launchWave;
        private int _sessionInitialWave;
        private string _dailyContent;
        private int _dailyStage;
        private bool _launchedFromContent;

        protected override string Title => "Battle Showroom | Combat";
        protected override string SessionDescription => "Fusion Single | Local State Authority";
        public bool IsReady => !_busy && _runner != null && _runner.IsRunning && _authority != null && _authority.IsSpawnedForAccess;

        private void Start()
        {
            _settings = BattleShowroomPreferences.Load<Settings>(PreferenceId);
            _settings.preset = Mathf.Clamp(_settings.preset, 0, Presets.Length - 1);
            _settings.mutation = Mathf.Clamp(_settings.mutation, 0, DevelopmentMythicFixtureRules.Mutations.Count - 1);
            if (BattleShowroomLaunch.TryConsume(out BattleShowroomLaunch launch))
            {
                _launchedFromContent = true;
                _launchMap = launch.Map;
                _launchWave = launch.InitialWave;
                _dailyContent = launch.DailyContent;
                _dailyStage = launch.DailyStage;
                _settings.preset = 0;
            }
            RestartFromControls(save: !_launchedFromContent);
        }

        protected override void DrawControls()
        {
            if (_settings == null)
                return;
            GUILayout.Label("Setup for a fresh session", GUI.skin.box);
            _settings.preset = GUILayout.SelectionGrid(_settings.preset, Presets, 2);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Starting Gold");
            _settings.gold = GUILayout.TextField(_settings.gold ?? string.Empty, 6);
            GUILayout.EndHorizontal();
            if (_settings.preset == 5)
            {
                GUILayout.Label("Fixture Mythic ID");
                _settings.mythicId = GUILayout.TextField(_settings.mythicId ?? string.Empty, 10);
                _settings.mutation = GUILayout.SelectionGrid(_settings.mutation,
                    DevelopmentMythicFixtureRules.Mutations.ToArray(), 3);
            }
            bool previous = GUI.enabled;
            GUI.enabled = !_busy;
            if (GUILayout.Button("Prepare new session"))
                RestartFromControls(save: !_launchedFromContent);
            GUI.enabled = previous;
            GUILayout.Label(_status, GUI.skin.box);
            if (!IsReady)
                return;

            if (GUILayout.Button("Fixture setup: roll a test Injector"))
                RunCommand("Fixture Injector roll", _authority.RequestTestInjectorKidnap);
            DrawActualCommands();
            DrawAuthorityState();
            if (_probe != null)
                _probe.Draw(_sourceSlot);
        }

        private void DrawActualCommands()
        {
            GUILayout.Label("Actual game commands", GUI.skin.box);
            if (GUILayout.Button("Kidnap"))
                RunCommand("Kidnap", _authority.RequestKidnap);
            GUILayout.BeginHorizontal();
            GUILayout.Label("From: " + (_sourceSlot + 1));
            _sourceSlot = (int)GUILayout.HorizontalSlider(_sourceSlot, 0, BoardSize - 1);
            GUILayout.Label("To: " + (_targetSlot + 1));
            _targetSlot = (int)GUILayout.HorizontalSlider(_targetSlot, 0, BoardSize - 1);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Move"))
                RunCommand("Move", () => _authority.RequestMove(_sourceSlot, _targetSlot));
            if (GUILayout.Button("Merge / Swap"))
                RunCommand("Merge / Swap", () => _authority.RequestMergeOrSwap(_sourceSlot, _targetSlot));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Injector"))
                RunCommand("Use Injector", () => _authority.RequestUseInjector(_sourceSlot, _targetSlot));
            if (GUILayout.Button("Mutation"))
                RunCommand("Mutation", () => _authority.RequestMutation(_sourceSlot));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Normal Resonance"))
                RunCommand("Normal Resonance", () => _authority.RequestResonanceUpgrade(CanonicalResonanceTrack.NORMAL));
            if (GUILayout.Button("Mythic Resonance"))
                RunCommand("Mythic Resonance", () => _authority.RequestResonanceUpgrade(CanonicalResonanceTrack.MYTHIC));
            GUILayout.EndHorizontal();
            if (_authority.IsMythicChoiceActive(1))
            {
                GUILayout.Label($"Mythic choice | {_authority.GetMythicChoiceRemainingSeconds(1)} sec");
                GUILayout.BeginHorizontal();
                for (int index = 0; index < 3; index++)
                {
                    if (GUILayout.Button(_authority.GetMythicChoiceCandidate(1, index).ToString(CultureInfo.InvariantCulture)))
                        RunCommand("Mythic choice", () => _authority.RequestMythicChoice(index));
                }
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Reroll"))
                    RunCommand("Mythic reroll", _authority.RequestMythicReroll);
            }
            if (_dailyContent != null && GUILayout.Button("Start next Daily Wave"))
                _executor.StartNextWave();
            else if (_sessionInitialWave != 0 && !_executor.IsP1ValidationStartConsumed && GUILayout.Button("Start selected Wave"))
                _executor.StartNextWave();
            if (!string.IsNullOrEmpty(_commandResult))
                GUILayout.Label(_commandResult, GUI.skin.box);
        }

        private void DrawAuthorityState()
        {
            GUILayout.Space(9);
            GUILayout.Label("Authoritative state", GUI.skin.box);
            GUILayout.Label($"Match {_authority.MatchState} | Player 1 {_authority.Player1BattleState}"
                + $" | Wave {_authority.CurrentWave}");
            GUILayout.Label($"Gold {_authority.GetInGameGoldForPlayerSlot(1)} | Kidnap {_authority.GetKidnapCount(1)}"
                + $" | Board {Occupied()}/{BoardSize}");
            GUILayout.Label($"Monsters P1 {_authority.Player1AliveMonsterCount} / P2 {_authority.Player2AliveMonsterCount}");
            GUILayout.Label($"Kill audit records {_authority.KillAuditRecords.Count}");
            if (_executor.IsBossActive)
                GUILayout.Label($"Boss | {_authority.GetBossRemainingSeconds()} sec");
            for (int row = 0; row < 4; row++)
            {
                GUILayout.BeginHorizontal();
                for (int column = 0; column < 6; column++)
                {
                    int slot = row * 6 + column;
                    string item = !_authority.IsBoardOccupied(1, slot) ? "-" : _authority.IsBoardInjector(1, slot)
                        ? "I" : _authority.GetBoardAlienId(1, slot).ToString(CultureInfo.InvariantCulture);
                    if (GUILayout.Button($"{slot + 1}: {item}"))
                        _sourceSlot = slot;
                }
                GUILayout.EndHorizontal();
            }
            foreach (string note in TerminalNotes)
                GUILayout.Label(note);
        }

        private void RunCommand(string label, Action command)
        {
            string before = CommandState();
            command();
            string after = CommandState();
            _commandResult = label + (before == after ? " | no authoritative change" : " | state changed")
                + $"\nGold {_authority.GetInGameGoldForPlayerSlot(1)} | Kidnap {_authority.GetKidnapCount(1)} | Board {Occupied()}";
        }

        private string CommandState()
        {
            string board = string.Join(",", Enumerable.Range(0, BoardSize).Select(slot =>
                $"{_authority.IsBoardOccupied(1, slot)}:{_authority.GetBoardAlienId(1, slot)}:"
                + $"{_authority.GetBoardGrade(1, slot)}:{_authority.GetBoardMutationState(1, slot)}:"
                + $"{_authority.GetBoardMutationType(1, slot)}"));
            string choices = string.Join(",", Enumerable.Range(0, 3).Select(index => _authority.GetMythicChoiceCandidate(1, index)));
            return $"{_authority.GetInGameGoldForPlayerSlot(1)}|{_authority.GetKidnapCount(1)}|"
                + $"{_authority.GetResonanceLevel(1, CanonicalResonanceTrack.NORMAL)}|"
                + $"{_authority.GetResonanceLevel(1, CanonicalResonanceTrack.MYTHIC)}|"
                + $"{_authority.IsMythicChoiceActive(1)}|{choices}|{board}";
        }

        private int Occupied()
            => Enumerable.Range(0, BoardSize).Count(slot => _authority.IsBoardOccupied(1, slot));

        protected override void ResetControls()
        {
            if (_busy)
                return;
            BattleShowroomPreferences.Clear(PreferenceId);
            if (_probe != null)
                _probe.ResetPreferences();
            _settings = new Settings();
            _launchMap = "NEPTUNE";
            _launchWave = 0;
            _dailyContent = null;
            _dailyStage = 0;
            _launchedFromContent = false;
            RestartFromControls(save: false);
        }

        private async void RestartFromControls(bool save = true)
        {
            if (_busy || _settings == null)
                return;
            if (!int.TryParse(_settings.gold, NumberStyles.None, CultureInfo.InvariantCulture, out int gold)
                || gold > MaxStartingGold)
            {
                _status = $"Enter starting Gold 0–{MaxStartingGold}.";
                return;
            }
            string mutation = DevelopmentMythicFixtureRules.Mutations[_settings.mutation];
            long mythicId = 0;
            if (_settings.preset == 5)
            {
                if (!long.TryParse(_settings.mythicId, NumberStyles.None, CultureInfo.InvariantCulture, out mythicId))
                {
                    _status = "Enter a canonical Mythic ID.";
                    return;
                }
                if (!DevelopmentMythicFixtureRules.TryNormalize(mythicId, mutation, out _, out string reason))
                {
                    _status = reason;
                    return;
                }
            }

            int preset = _settings.preset;
            _busy = true;
            _commandResult = null;
            _status = "Starting a new offline Fusion session.";
            try
            {
                await StopSession();
                if (!this)
                    return;
                await StartSession(preset, preset == 4 ? 0 : gold, mythicId, mutation);
                if (save && this)
                    BattleShowroomPreferences.Save(PreferenceId, _settings);
            }
            catch (Exception exception)
            {
                _status = exception.Message;
                await StopSession();
                Debug.LogWarning("[BattleShowroom] " + exception.Message, this);
            }
            finally
            {
                _busy = false;
            }
        }

        private async Task StartSession(int preset, int gold, long mythicId, string mutation)
        {
            if (_sessionTemplate == null)
                throw new InvalidOperationException("Offline Battle template is missing.");

            string launchMap = _launchMap;
            int initialWave = _launchWave != 0 ? _launchWave : preset == 7 ? 1 : preset == 8 ? 10 : 0;
            if (_dailyContent != null)
                initialWave = 0;
            string nonce = Guid.NewGuid().ToString("N");
            string sessionId = initialWave == 0 ? "SHOWROOM-" + nonce : $"P1VAL-{launchMap}-W{initialWave:D3}-{nonce}";
            if (_dailyContent != null)
            {
                string prefix = _dailyContent == DailyBattleExecutionPlanBuilder.CultivationContentType
                    ? DailyBattleDevelopmentSessionProfile.SessionPrefix
                    : DailyBattleDevelopmentSessionProfile.MutationLabSessionPrefix;
                sessionId = prefix + _dailyStage + "-" + nonce;
            }

            var runnerObject = new GameObject("Offline Battle Runner");
            _runner = runnerObject.AddComponent<NetworkRunner>();
            NetworkRunner runner = _runner;
            runner.ProvideInput = false;
            StartGameResult result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Single,
                Address = NetAddress.LocalhostIPv4(0),
                PlayerCount = 1,
                SessionName = sessionId,
                SceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>(),
                ObjectProvider = runnerObject.AddComponent<BattleShowroomObjectProvider>()
            });
            if (!this || runner == null)
                return;
            if (!result.Ok || runner.GameMode != GameMode.Single || !runner.LocalPlayer.IsRealPlayer)
                throw new InvalidOperationException("Fusion Single could not start: " + result.ErrorMessage);

            _session = Instantiate(_sessionTemplate);
            _session.name = "Offline Battle State";
            _session.SetActive(true);
            var lifecycle = _session.GetComponent<BattleRunnerLifecycle>();
            _executor = _session.GetComponent<BattleWaveExecutor>();
            _authority = _session.GetComponent<BattleWaveStateAuthority>();
            var networkObject = _session.GetComponent<NetworkObject>();
            new NetworkObjectBaker().Bake(_session);
            networkObject.NetworkTypeId = NetworkObjectTypeId.FromCustom(SessionTypeId);
            runner.Attach(networkObject, runner.LocalPlayer, allocate: true);
            if (!_authority.IsSpawnedForAccess || !_authority.HasStateAuthority
                || !lifecycle.PlayerRoster.TryAdd(runner.LocalPlayer, PlayerId, out _))
                throw new InvalidOperationException("Local Battle State Authority could not bind Player 1.");
            if (!_executor.TryGetCanonicalSessionMetadata(out string version, out string hash, out string contentVersion, out string contentHash))
                throw new InvalidOperationException("Canonical Battle Balance is unavailable.");

            var context = new BattleSessionContext(sessionId, version, hash, contentVersion, contentHash, 0, launchMap);
            if (_dailyContent != null)
            {
                InitializeDaily(context);
                if (!_executor.TryArmDevelopmentDailyInitialWave(_launchWave, out string reason))
                    throw new InvalidOperationException(reason);
            }
            else if (!_authority.InitializeSession(context, new BattlePlayerIdentityMap(PlayerId, OppositeFieldId)))
                throw new InvalidOperationException("The offline Battle context could not initialize.");

            if (!_authority.InitializeInGameGold(gold, _dailyContent != null ? 0 : gold))
                throw new InvalidOperationException("Offline starting Gold was rejected.");
            if (initialWave != 0)
            {
                BattleP1ValidationParseState parsed = BattleP1ValidationSessionProfile.Parse(sessionId, out BattleP1ValidationSessionProfile profile, out string reason);
                if (parsed != BattleP1ValidationParseState.Valid)
                    throw new InvalidOperationException(reason);
                if (!_executor.TryArmP1ValidationInitialWave(profile, out reason))
                    throw new InvalidOperationException(reason);
            }
            _sessionInitialWave = initialWave;
            if (_probe != null)
                _probe.Bind(_authority, _session.GetComponent<BattleProjectileSpawner>());

            _status = $"Offline session ready | {launchMap}" + (initialWave == 0 ? string.Empty : $" | W{initialWave:D3} ready");
            if (_dailyContent != null)
                _status += $" | Stage {_dailyStage} | W{_launchWave:D2} ready";
            ApplyPreset(preset, mythicId, mutation);
        }

        private void InitializeDaily(BattleSessionContext context)
        {
            if (!_executor.TryGetCanonicalDailyBattleProvider(out ICanonicalCompositeBattleBalanceProvider provider))
                throw new InvalidOperationException("Canonical Daily Battle catalog is unavailable.");
            DailyBattleDevelopmentParseState parsed = DailyBattleDevelopmentSessionProfile.Parse(
                context.BattleSessionId, out DailyBattleDevelopmentSessionProfile profile, out string reason);
            if (parsed != DailyBattleDevelopmentParseState.Valid)
                throw new InvalidOperationException(reason);
            var session = profile.CreateContext(provider);
            if (!DailyBattleExecutionPlanBuilder.TryBuild(
                    session, provider, DailyBattleSessionTrust.DevelopmentFixture, out DailyBattleExecutionPlan plan, out reason))
                throw new InvalidOperationException(reason);
            if (!_authority.InitializeDailySession(context, new DailyBattlePlayerIdentityMap(PlayerId), plan))
                throw new InvalidOperationException("The offline Daily Battle context could not initialize.");
        }

        private void ApplyPreset(int preset, long mythicId, string mutation)
        {
            if (preset == 1 || preset == 2 || preset == 3)
            {
                for (int slot = 0; slot < BoardSize; slot++)
                {
                    int before = _authority.GetKidnapCount(1);
                    _authority.RequestKidnap();
                    if (before == _authority.GetKidnapCount(1))
                        break;
                    if (preset != 3 && TrySelectPair(preset == 1))
                        break;
                }
                _status += $" | Prepared through Kidnap: {Occupied()}";
                if (preset != 3 && !TrySelectPair(preset == 1))
                    _status += " | Requested pair unavailable with this Gold / draw.";
            }
            else if (preset == 5)
                _authority.RequestDevelopmentMythicFixture(mythicId, mutation);
            else if (preset == 6 && !_executor.TryForceDevelopmentPartialSettlementFailureFromAuthority())
                _status += " | FAILED transition rejected.";
        }

        private bool TrySelectPair(bool matching)
        {
            for (int source = 0; source < BoardSize; source++)
            {
                if (!_authority.IsBoardOccupied(1, source) || _authority.IsBoardInjector(1, source))
                    continue;
                for (int target = source + 1; target < BoardSize; target++)
                {
                    if (!_authority.IsBoardOccupied(1, target) || _authority.IsBoardInjector(1, target))
                        continue;
                    bool same = _authority.GetBoardAlienId(1, source) == _authority.GetBoardAlienId(1, target)
                        && _authority.GetBoardGrade(1, source) == _authority.GetBoardGrade(1, target);
                    if (same != matching)
                        continue;
                    _sourceSlot = source;
                    _targetSlot = target;
                    return true;
                }
            }
            return false;
        }

        protected override async void ReturnToHub()
        {
            if (_busy)
                return;
            _busy = true;
            if (!_launchedFromContent && _settings != null)
                BattleShowroomPreferences.Save(PreferenceId, _settings);
            await StopSession();
            if (this)
                base.ReturnToHub();
        }

        private async Task StopSession()
        {
            NetworkRunner runner = _runner;
            GameObject session = _session;
            if (_probe != null)
                _probe.ClearSession();
            _runner = null;
            _session = null;
            _authority = null;
            _executor = null;
            if (runner != null && !runner.IsShutdown)
                await runner.Shutdown(true);
            if (session != null)
                Destroy(session);
            await Task.Yield();
        }

        private async void OnDestroy()
        {
            await StopSession();
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(GameObject sessionTemplate, BattleShowroomCombatProbe probe)
        {
            _sessionTemplate = sessionTemplate;
            _probe = probe;
        }
#endif
    }

    public sealed class BattleShowroomObjectProvider : NetworkObjectProviderDefault
    {
        public override void ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
        {
            if (context.TypeId == NetworkObjectTypeId.FromCustom(BattleShowroomCombat.SessionTypeId))
            {
                if (!context.IsBeingDestroyed && context.Object != null)
                    Destroy(context.Object.gameObject);
                return;
            }
            base.ReleaseInstance(runner, context);
        }
    }
}
#endif
