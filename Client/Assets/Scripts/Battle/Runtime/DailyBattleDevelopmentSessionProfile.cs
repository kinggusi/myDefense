#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Shared.Contracts;

namespace MyDefense.Battle.Runtime
{
    public enum DailyBattleDevelopmentParseState
    {
        NotDailyBattle = 0,
        Valid = 1,
        Malformed = 2
    }

    /// <summary>
    /// Development-only entry point for an isolated Player 1 Daily Battle.
    /// Production must receive DailyBattleSessionContext from its trusted API adapter.
    /// </summary>
    public sealed class DailyBattleDevelopmentSessionProfile
    {
        public const string SessionPrefix = "P22-CULT-S";

        public string SessionName { get; }
        public int Stage { get; }
        public string MapId => DailyBattleExecutionPlanBuilder.CultivationMapId;

        private DailyBattleDevelopmentSessionProfile(string sessionName, int stage)
        {
            SessionName = sessionName;
            Stage = stage;
        }

        public DailyBattleSessionContext CreateContext(ICanonicalCompositeBattleBalanceProvider provider)
        {
            if (provider == null || !provider.IsValid)
                throw new ArgumentException("A valid canonical provider is required.", nameof(provider));
            return new DailyBattleSessionContext
            {
                schemaVersion = DailyBattleSessionContext.CurrentSchemaVersion,
                runId = "dev:" + SessionName,
                battleSessionId = SessionName,
                contentType = DailyBattleExecutionPlanBuilder.CultivationContentType,
                stage = Stage,
                mapId = MapId,
                balanceVersion = provider.CanonicalBalanceVersion,
                contentHash = provider.CanonicalContentHash
            };
        }

        public static DailyBattleDevelopmentParseState Parse(
            string sessionName,
            out DailyBattleDevelopmentSessionProfile profile,
            out string error)
        {
            profile = null;
            error = null;
            if (string.IsNullOrWhiteSpace(sessionName)
                || !sessionName.StartsWith(SessionPrefix, StringComparison.Ordinal))
                return DailyBattleDevelopmentParseState.NotDailyBattle;
            int suffixIndex = SessionPrefix.Length;
            if (sessionName.Length <= suffixIndex + 1
                || sessionName[suffixIndex] < '1'
                || sessionName[suffixIndex] > '5'
                || sessionName[suffixIndex + 1] != '-')
            {
                error = "Daily Development session must match P22-CULT-S{1..5}-<unique> exactly.";
                return DailyBattleDevelopmentParseState.Malformed;
            }
            string uniqueToken = sessionName.Substring(suffixIndex + 2);
            if (!IsSafeUniqueToken(uniqueToken))
            {
                error = "Daily Development session unique token must be 3-64 ASCII letters, digits, '-' or '_', with alphanumeric ends.";
                return DailyBattleDevelopmentParseState.Malformed;
            }
            profile = new DailyBattleDevelopmentSessionProfile(
                sessionName,
                sessionName[suffixIndex] - '0');
            return DailyBattleDevelopmentParseState.Valid;
        }

        private static bool IsSafeUniqueToken(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 3 || value.Length > 64
                || !IsAsciiLetterOrDigit(value[0])
                || !IsAsciiLetterOrDigit(value[value.Length - 1]))
                return false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!IsAsciiLetterOrDigit(character) && character != '-' && character != '_')
                    return false;
            }
            return true;
        }

        private static bool IsAsciiLetterOrDigit(char value)
        {
            return value >= '0' && value <= '9'
                || value >= 'A' && value <= 'Z'
                || value >= 'a' && value <= 'z';
        }
    }
}
#endif
