using System;
using System.Text;

namespace MyDefense.Shared.Contracts
{
    [Serializable]
    public sealed class DailyBattleSessionContext
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string runId;
        public string battleSessionId;
        public string contentType;
        public int stage;
        public string mapId;
        public string balanceVersion;
        public string contentHash;
    }

    public static class DailyBattleSessionContextValidator
    {
        public static void Validate(DailyBattleSessionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.schemaVersion != DailyBattleSessionContext.CurrentSchemaVersion)
                throw new ArgumentException("Unsupported Daily Battle Session schemaVersion.", nameof(context));
            if (string.IsNullOrWhiteSpace(context.runId)
                || string.IsNullOrWhiteSpace(context.battleSessionId)
                || string.IsNullOrWhiteSpace(context.balanceVersion)
                || string.IsNullOrWhiteSpace(context.contentHash))
                throw new ArgumentException("Daily Battle Session identity is required.", nameof(context));
            if (context.stage < 1 || context.stage > 5)
                throw new ArgumentException("Daily Battle stage must be between 1 and 5.", nameof(context));

            string expectedMapId = context.contentType switch
            {
                "CULTIVATION_ZONE" => "DAILY_CULTIVATION_ZONE",
                "MUTATION_LAB" => "DAILY_MUTATION_LAB",
                _ => null,
            };
            if (expectedMapId == null || !string.Equals(context.mapId, expectedMapId, StringComparison.Ordinal))
                throw new ArgumentException("Daily Battle contentType/mapId mismatch.", nameof(context));
        }
    }

    public static class DailyBattleSessionContextJson
    {
        public static string Serialize(DailyBattleSessionContext context)
        {
            DailyBattleSessionContextValidator.Validate(context);
            var builder = new StringBuilder(320);
            builder.Append("{\"schemaVersion\":").Append(context.schemaVersion)
                .Append(",\"runId\":").Append(String(context.runId))
                .Append(",\"battleSessionId\":").Append(String(context.battleSessionId))
                .Append(",\"contentType\":").Append(String(context.contentType))
                .Append(",\"stage\":").Append(context.stage)
                .Append(",\"mapId\":").Append(String(context.mapId))
                .Append(",\"balanceVersion\":").Append(String(context.balanceVersion))
                .Append(",\"contentHash\":").Append(String(context.contentHash))
                .Append('}');
            return builder.ToString();
        }

        private static string String(string value)
        {
            if (value == null) return "null";
            var builder = new StringBuilder(value.Length + 2).Append('"');
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
                        if (character < 0x20) builder.Append("\\u").Append(((int)character).ToString("x4"));
                        else builder.Append(character);
                        break;
                }
            }
            return builder.Append('"').ToString();
        }
    }
}
