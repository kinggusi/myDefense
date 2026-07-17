using System;
using System.Globalization;
using System.Text;

namespace MyDefense.Shared.Contracts
{
    /// <summary>
    /// String values accepted by the Spring Battle settlement endpoint.
    /// These transport results are intentionally separate from MatchState.
    /// </summary>
    public static class BattleSettlementResultValues
    {
        public const string Victory = "VICTORY";
        public const string Defeat = "DEFEAT";
        public const string Aborted = "ABORTED";

        public static bool IsDefined(string value)
        {
            return string.Equals(value, Victory, StringComparison.Ordinal)
                || string.Equals(value, Defeat, StringComparison.Ordinal)
                || string.Equals(value, Aborted, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Canonical JSON request contract for POST /api/battle/settlements.
    /// Field names intentionally match Spring's BattleSettlementDtos.Request record.
    /// startedAt and finishedAt use ISO-8601 local date-time strings.
    /// </summary>
    [Serializable]
    public sealed class BattleSettlementSummary
    {
        public string requestId;
        public string battleSessionId;
        public string balanceVersion;
        public string contentHash;
        public string result;
        public int finalWave;
        public string startedAt;
        public string finishedAt;
        public BattleSettlementPlayerSummary[] players;
        public BattleSettlementMonsterSummary[] monsterKills;
        public string summaryHash;
    }

    /// <summary>
    /// Per-player portion of BattleSettlementSummary.
    /// Field names intentionally match Spring's BattleSettlementDtos.Player record.
    /// </summary>
    [Serializable]
    public sealed class BattleSettlementPlayerSummary
    {
        public string playerId;
        public int playerSlot;
        public bool eliminated;
        public int? eliminatedWave;
        public int kills;
        public int supportKills;
        public int bossKills;
        public int initialInGameGold;
        public int inGameGoldEarned;
        public int inGameGoldSpent;
        public int finalInGameGold;
    }

    /// <summary>
    /// Per-Monster portion of BattleSettlementSummary.
    /// Field names intentionally match Spring's BattleSettlementDtos.Monster record.
    /// </summary>
    [Serializable]
    public sealed class BattleSettlementMonsterSummary
    {
        public string monsterSpecId;
        public int totalKills;
        public int bossKills;
        public int totalKillGold;
    }

    /// <summary>
    /// Serializes the settlement transport contract without relying on JsonUtility.
    /// JsonUtility does not support Nullable&lt;int&gt;, while eliminatedWave must be
    /// emitted as either JSON null or an integer for the Spring endpoint.
    /// </summary>
    public static class BattleSettlementSummaryJson
    {
        public static string Serialize(BattleSettlementSummary summary)
        {
            if (summary == null)
            {
                throw new ArgumentNullException(nameof(summary));
            }
            if (!BattleSettlementResultValues.IsDefined(summary.result))
            {
                throw new ArgumentException(
                    "result must be VICTORY, DEFEAT, or ABORTED.",
                    nameof(summary));
            }

            var builder = new StringBuilder(1024);
            builder.Append('{');
            AppendStringProperty(builder, "requestId", summary.requestId);
            AppendStringProperty(builder, "battleSessionId", summary.battleSessionId);
            AppendStringProperty(builder, "balanceVersion", summary.balanceVersion);
            AppendStringProperty(builder, "contentHash", summary.contentHash);
            AppendStringProperty(builder, "result", summary.result);
            AppendIntProperty(builder, "finalWave", summary.finalWave);
            AppendStringProperty(builder, "startedAt", summary.startedAt);
            AppendStringProperty(builder, "finishedAt", summary.finishedAt);
            AppendPlayersProperty(builder, summary.players);
            AppendMonstersProperty(builder, summary.monsterKills);
            AppendStringProperty(builder, "summaryHash", summary.summaryHash, false);
            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendPlayersProperty(
            StringBuilder builder,
            BattleSettlementPlayerSummary[] players)
        {
            AppendPropertyName(builder, "players");
            if (players == null)
            {
                builder.Append("null,");
                return;
            }

            builder.Append('[');
            for (var index = 0; index < players.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                BattleSettlementPlayerSummary player = players[index]
                    ?? throw new ArgumentException("players must not contain null elements.", nameof(players));
                builder.Append('{');
                AppendStringProperty(builder, "playerId", player.playerId);
                AppendIntProperty(builder, "playerSlot", player.playerSlot);
                AppendBoolProperty(builder, "eliminated", player.eliminated);
                AppendNullableIntProperty(builder, "eliminatedWave", player.eliminatedWave);
                AppendIntProperty(builder, "kills", player.kills);
                AppendIntProperty(builder, "supportKills", player.supportKills);
                AppendIntProperty(builder, "bossKills", player.bossKills);
                AppendIntProperty(builder, "initialInGameGold", player.initialInGameGold);
                AppendIntProperty(builder, "inGameGoldEarned", player.inGameGoldEarned);
                AppendIntProperty(builder, "inGameGoldSpent", player.inGameGoldSpent);
                AppendIntProperty(builder, "finalInGameGold", player.finalInGameGold, false);
                builder.Append('}');
            }

            builder.Append("],");
        }

        private static void AppendMonstersProperty(
            StringBuilder builder,
            BattleSettlementMonsterSummary[] monsters)
        {
            AppendPropertyName(builder, "monsterKills");
            if (monsters == null)
            {
                builder.Append("null,");
                return;
            }

            builder.Append('[');
            for (var index = 0; index < monsters.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                BattleSettlementMonsterSummary monster = monsters[index]
                    ?? throw new ArgumentException("monsterKills must not contain null elements.", nameof(monsters));
                builder.Append('{');
                AppendStringProperty(builder, "monsterSpecId", monster.monsterSpecId);
                AppendIntProperty(builder, "totalKills", monster.totalKills);
                AppendIntProperty(builder, "bossKills", monster.bossKills);
                AppendIntProperty(builder, "totalKillGold", monster.totalKillGold, false);
                builder.Append('}');
            }

            builder.Append("],");
        }

        private static void AppendStringProperty(
            StringBuilder builder,
            string name,
            string value,
            bool trailingComma = true)
        {
            AppendPropertyName(builder, name);
            AppendEscapedString(builder, value);
            AppendComma(builder, trailingComma);
        }

        private static void AppendIntProperty(
            StringBuilder builder,
            string name,
            int value,
            bool trailingComma = true)
        {
            AppendPropertyName(builder, name);
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
            AppendComma(builder, trailingComma);
        }

        private static void AppendNullableIntProperty(
            StringBuilder builder,
            string name,
            int? value)
        {
            AppendPropertyName(builder, name);
            builder.Append(value.HasValue
                ? value.Value.ToString(CultureInfo.InvariantCulture)
                : "null");
            builder.Append(',');
        }

        private static void AppendBoolProperty(StringBuilder builder, string name, bool value)
        {
            AppendPropertyName(builder, name);
            builder.Append(value ? "true," : "false,");
        }

        private static void AppendPropertyName(StringBuilder builder, string name)
        {
            builder.Append('"').Append(name).Append("\":");
        }

        private static void AppendComma(StringBuilder builder, bool trailingComma)
        {
            if (trailingComma)
            {
                builder.Append(',');
            }
        }

        private static void AppendEscapedString(StringBuilder builder, string value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('"');
            foreach (char character in value)
            {
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 0x20)
                        {
                            builder.Append("\\u")
                                .Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(character);
                        }
                        break;
                }
            }
            builder.Append('"');
        }
    }
}
