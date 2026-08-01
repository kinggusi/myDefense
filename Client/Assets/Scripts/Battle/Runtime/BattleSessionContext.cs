using System;
using Fusion;
using MyDefense.Shared.Contracts;
using MatchStateContract = MyDefense.Shared.Contracts.MatchState;

namespace MyDefense.Battle.Runtime
{
    public sealed class BattleSessionContext
    {
        public string BattleSessionId { get; }
        public string CanonicalBalanceVersion { get; }
        public string CanonicalContentHash { get; }
        public string BattleContentVersion { get; }
        public string BattleContentHash { get; }
        public string MapId { get; }
        public long StartedAtTick { get; }
        public MatchStateContract MatchState { get; private set; } = MatchStateContract.RUNNING;

        public BattleSessionContext(
            string battleSessionId,
            string canonicalBalanceVersion,
            string canonicalContentHash,
            string battleContentVersion,
            string battleContentHash,
            long startedAtTick,
            string mapId = null)
        {
            BattleSessionId = RequireText(battleSessionId, nameof(battleSessionId));
            CanonicalBalanceVersion = RequireText(canonicalBalanceVersion, nameof(canonicalBalanceVersion));
            CanonicalContentHash = RequireText(canonicalContentHash, nameof(canonicalContentHash));
            BattleContentVersion = RequireText(battleContentVersion, nameof(battleContentVersion));
            BattleContentHash = RequireText(battleContentHash, nameof(battleContentHash));
            MapId = string.IsNullOrWhiteSpace(mapId) ? null : mapId.Trim();
            if (startedAtTick < 0)
                throw new ArgumentOutOfRangeException(nameof(startedAtTick), "Session start tick cannot be negative.");

            StartedAtTick = startedAtTick;
        }

        public static BattleSessionContext FromRunner(
            NetworkRunner runner,
            string canonicalBalanceVersion,
            string canonicalContentHash,
            string battleContentVersion,
            string battleContentHash,
            long startedAtTick,
            string mapId = null)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));
            if (!runner.IsRunning || !runner.SessionInfo.IsValid || string.IsNullOrWhiteSpace(runner.SessionInfo.Name))
                throw new InvalidOperationException("A running Fusion runner with a valid session is required.");

            return new BattleSessionContext(
                runner.SessionInfo.Name,
                canonicalBalanceVersion,
                canonicalContentHash,
                battleContentVersion,
                battleContentHash,
                startedAtTick,
                mapId);
        }

        public bool TryTransitionMatchState(MatchStateContract nextState)
        {
            if (nextState == MatchStateContract.RUNNING)
                return MatchState == MatchStateContract.RUNNING;
            if (MatchState != MatchStateContract.RUNNING)
                return false;
            MatchState = nextState;
            return true;
        }

        internal static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A non-empty value is required.", parameterName);

            return value;
        }
    }
}
