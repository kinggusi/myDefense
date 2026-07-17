using System;

namespace MyDefense.Battle.Runtime
{
    public sealed class BattleSessionContext
    {
        public string BattleSessionId { get; }
        public string CanonicalBalanceVersion { get; }
        public string CanonicalContentHash { get; }
        public string BattleContentVersion { get; }
        public string BattleContentHash { get; }
        public long StartedAtTick { get; }

        public BattleSessionContext(
            string battleSessionId,
            string canonicalBalanceVersion,
            string canonicalContentHash,
            string battleContentVersion,
            string battleContentHash,
            long startedAtTick)
        {
            BattleSessionId = RequireText(battleSessionId, nameof(battleSessionId));
            CanonicalBalanceVersion = RequireText(canonicalBalanceVersion, nameof(canonicalBalanceVersion));
            CanonicalContentHash = RequireText(canonicalContentHash, nameof(canonicalContentHash));
            BattleContentVersion = RequireText(battleContentVersion, nameof(battleContentVersion));
            BattleContentHash = RequireText(battleContentHash, nameof(battleContentHash));
            if (startedAtTick < 0)
                throw new ArgumentOutOfRangeException(nameof(startedAtTick), "Session start tick cannot be negative.");

            StartedAtTick = startedAtTick;
        }

        internal static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A non-empty value is required.", parameterName);

            return value;
        }
    }
}
