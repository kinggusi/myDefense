using System;
using Fusion;
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

        public BattleWaveExecutor Executor => _executor;
        public bool IsAuthoritative => HasStateAuthority;
        public MatchState MatchState => _executor != null ? _executor.MatchState : MatchState.RUNNING;

        public override void Spawned()
        {
            _executor = GetComponent<BattleWaveExecutor>();
            if (_executor == null)
                throw new InvalidOperationException("BattleWaveStateAuthority requires BattleWaveExecutor on the same NetworkObject.");
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
            return true;
        }
    }
}
