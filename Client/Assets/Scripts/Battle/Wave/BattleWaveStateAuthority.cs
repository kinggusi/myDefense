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
            if (HasStateAuthority)
            {
                MatchStateValue = (int)MyDefense.Shared.Contracts.MatchState.RUNNING;
                IsWaveRunning = false;
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
            _executor = null;
        }

        public bool InitializeSession(
            BattleSessionContext sessionContext,
            IBattlePlayerIdentityProvider playerIdentityProvider)
        {
            if (!HasStateAuthority || _executor == null)
                return false;
            _executor.InitializeSession(sessionContext, playerIdentityProvider);
            return true;
        }

        public bool TryStartNextWave()
        {
            if (!HasStateAuthority || _executor == null)
                return false;
            _executor.StartNextWave();
            CurrentWave = _executor.CurrentRound;
            CurrentWaveId = _executor.CurrentWaveId ?? string.Empty;
            CurrentWaveTypeValue = _executor.IsCurrentWaveBoss ? (int)WaveType.BOSS : (int)WaveType.REGULAR;
            IsWaveRunning = _executor.IsWaveRunning;
            return true;
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
    }
}
