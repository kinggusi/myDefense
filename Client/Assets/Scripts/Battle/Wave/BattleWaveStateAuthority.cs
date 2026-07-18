using System;
using Fusion;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;

namespace MyDefense.Battle
{
    /// <summary>
    /// State-authority boundary for the existing wave executor.
    /// Networked wave fields are introduced by P0-3-2; this component owns
    /// which peer is allowed to invoke the executor in the meantime.
    /// </summary>
    public sealed class BattleWaveStateAuthority : NetworkBehaviour
    {
        private BattleWaveExecutor _executor;

        [Networked] public int CurrentWave { get; private set; }
        [Networked] public NetworkString<_32> CurrentWaveId { get; private set; }
        [Networked] public int CurrentWaveTypeValue { get; private set; }
        [Networked] public NetworkBool IsWaveRunning { get; private set; }
        [Networked] public int MatchStateValue { get; private set; }
        [Networked] public int Player1AliveMonsterCount { get; private set; }
        [Networked] public int Player2AliveMonsterCount { get; private set; }
        [Networked] public int PlayerMonsterLimit { get; private set; }

        public BattleWaveExecutor Executor => _executor;
        public bool IsAuthoritative => HasStateAuthority;
        public WaveType CurrentWaveType => (WaveType)CurrentWaveTypeValue;
        public MatchState MatchState => (MatchState)MatchStateValue;

        public override void Spawned()
        {
            _executor = GetComponent<BattleWaveExecutor>();
            if (_executor == null)
                throw new InvalidOperationException("BattleWaveStateAuthority requires BattleWaveExecutor on the same NetworkObject.");

            _executor.OnRegularWaveCompleted += HandleRegularWaveCompleted;
            _executor.OnBossDefeated += HandleWaveCompleted;
            _executor.OnRoundChanged += HandleRoundChanged;
            _executor.OnMatchStateChanged += HandleMatchStateChanged;
            _executor.OnPlayerMonsterCountChanged += HandlePlayerMonsterCountChanged;
            if (HasStateAuthority)
            {
                MatchStateValue = (int)MyDefense.Shared.Contracts.MatchState.RUNNING;
                IsWaveRunning = false;
                SyncAliveMonsterCounts();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_executor == null)
                return;

            _executor.OnRegularWaveCompleted -= HandleRegularWaveCompleted;
            _executor.OnBossDefeated -= HandleWaveCompleted;
            _executor.OnRoundChanged -= HandleRoundChanged;
            _executor.OnMatchStateChanged -= HandleMatchStateChanged;
            _executor.OnPlayerMonsterCountChanged -= HandlePlayerMonsterCountChanged;
            _executor = null;
        }

        public bool InitializeSession(
            BattleSessionContext sessionContext,
            IBattlePlayerIdentityProvider playerIdentityProvider)
        {
            if (!HasStateAuthority || _executor == null)
                return false;
            _executor.InitializeSession(sessionContext, playerIdentityProvider);
            SyncAliveMonsterCounts();
            return true;
        }

        public bool ValidateWaveStart(out string reason)
        {
            if (!HasStateAuthority)
                return FailValidation("Only State Authority may start a wave.", out reason);
            if (_executor == null)
                return FailValidation("BattleWaveExecutor is unavailable.", out reason);
            if (MatchState != MatchState.RUNNING)
                return FailValidation("MatchState must be RUNNING to start a wave.", out reason);
            if (IsWaveRunning)
                return FailValidation("A wave is already running.", out reason);
            if (_executor.IsBossActive)
                return FailValidation("The current Boss is still active.", out reason);
            if (_executor.SpawnedMonsterCount > 0)
                return FailValidation("Alive monsters remain from the previous wave.", out reason);
            if (_executor.AreAllPlayersEliminated)
                return FailValidation("All players are eliminated.", out reason);

            reason = string.Empty;
            return true;
        }

        public bool ValidateWaveEnd(out string reason)
        {
            if (!HasStateAuthority)
                return FailValidation("Only State Authority may validate wave completion.", out reason);
            if (_executor == null)
                return FailValidation("BattleWaveExecutor is unavailable.", out reason);
            if (CurrentWave <= 0)
                return FailValidation("No wave has started.", out reason);
            if (MatchState != MatchState.RUNNING)
                return FailValidation("MatchState is already terminal.", out reason);
            if (IsWaveRunning || _executor.IsBossActive)
                return FailValidation("The current wave is still running.", out reason);

            reason = string.Empty;
            return true;
        }

        public bool ValidateMatchState(MatchState expected)
        {
            return MatchState == expected;
        }

        public bool TryStartNextWave()
        {
            string reason;
            if (!ValidateWaveStart(out reason))
                return false;
            _executor.StartNextWave();
            CurrentWave = _executor.CurrentRound;
            CurrentWaveId = _executor.CurrentWaveId ?? string.Empty;
            CurrentWaveTypeValue = _executor.IsCurrentWaveBoss ? (int)WaveType.BOSS : (int)WaveType.REGULAR;
            IsWaveRunning = _executor.IsWaveRunning;
            return true;
        }

        private static bool FailValidation(string message, out string reason)
        {
            reason = message;
            return false;
        }

        private void HandleRegularWaveCompleted(int _)
        {
            HandleWaveCompleted();
        }

        private void HandleRoundChanged(int round)
        {
            if (!HasStateAuthority)
                return;

            CurrentWave = round;
            if (_executor == null)
                return;

            CurrentWaveId = _executor.CurrentWaveId ?? string.Empty;
            CurrentWaveTypeValue = _executor.IsCurrentWaveBoss ? (int)WaveType.BOSS : (int)WaveType.REGULAR;
            IsWaveRunning = _executor.IsWaveRunning;
        }

        private void HandleWaveCompleted()
        {
            if (!HasStateAuthority)
                return;
            IsWaveRunning = false;
        }

        private void HandleMatchStateChanged(MatchState state)
        {
            if (!HasStateAuthority)
                return;
            MatchStateValue = (int)state;
            IsWaveRunning = state == MatchState.RUNNING && _executor != null && _executor.IsWaveRunning;
        }

        private void HandlePlayerMonsterCountChanged(LaneType lane, int count, int limit)
        {
            if (!HasStateAuthority)
                return;

            PlayerMonsterLimit = limit;
            if (lane == LaneType.Player1Lane)
                Player1AliveMonsterCount = count;
            else if (lane == LaneType.Player2Lane)
                Player2AliveMonsterCount = count;
        }

        private void SyncAliveMonsterCounts()
        {
            if (!HasStateAuthority || _executor == null)
                return;

            Player1AliveMonsterCount = _executor.Player1AliveMonsterCount;
            Player2AliveMonsterCount = _executor.Player2AliveMonsterCount;
            PlayerMonsterLimit = _executor.MonsterLimit;
        }
    }
}
