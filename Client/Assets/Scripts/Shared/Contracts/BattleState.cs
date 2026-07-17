using System.Linq;
using System.Globalization;
using System.Text;

namespace MyDefense.Shared.Contracts
{
    /// <summary>
    /// Authoritative combat participation state for a player.
    /// The normal transition is ACTIVE -> ELIMINATED -> SPECTATING.
    /// </summary>
    public enum PlayerBattleState
    {
        ACTIVE = 0,
        ELIMINATED = 1,
        SPECTATING = 2
    }

    /// <summary>
    /// Authoritative lifecycle state for a battle match.
    /// RUNNING can transition once to either CLEARED or FAILED.
    /// </summary>
    public enum MatchState
    {
        RUNNING = 0,
        CLEARED = 1,
        FAILED = 2
    }

    /// <summary>
    /// Transport connection state for a battle participant.
    /// It is independent from PlayerBattleState and MatchState.
    /// Final leave and reward eligibility are settlement concerns, not connection states.
    /// </summary>
    public enum PlayerConnectionState
    {
        CONNECTED = 0,
        DISCONNECTED = 1
    }

    public enum LegendaryChoicePhase
    {
        OPEN = 0,
        SELECTED = 1,
        AUTO_SELECTED = 2,
        CLOSED = 3
    }

    [System.Serializable]
    public sealed class LegendaryChoiceState
    {
        public string choiceId;
        public string battleSessionId;
        public long materialAlienIdA;
        public long materialAlienIdB;
        public long[] candidateAlienIds;
        public int rerollCount;
        public int freeRerollsRemaining;
        public int paidRerollsRemaining;
        public int selectionTimeoutSeconds;
        public int remainingSeconds;
        public LegendaryChoicePhase phase;
        public long? selectedAlienId;
        public string autoSelectPolicy;
        public bool battleContinuesDuringSelection;
    }

    public static class LegendaryChoiceStateValidator
    {
        public static void Validate(LegendaryChoiceState state)
        {
            if (state == null) throw new System.ArgumentNullException(nameof(state));
            if (string.IsNullOrWhiteSpace(state.choiceId) || string.IsNullOrWhiteSpace(state.battleSessionId)
                || state.materialAlienIdA <= 0 || state.materialAlienIdB <= 0
                || state.materialAlienIdA == state.materialAlienIdB)
                throw new System.ArgumentException("Choice identity and two distinct materials are required.", nameof(state));
            if (state.candidateAlienIds == null || state.candidateAlienIds.Length != 3
                || state.candidateAlienIds.Any(id => id <= 0) || state.candidateAlienIds.Distinct().Count() != 3)
                throw new System.ArgumentException("Exactly three distinct candidates are required.", nameof(state));
            if (state.rerollCount < 0 || state.freeRerollsRemaining < 0 || state.paidRerollsRemaining < 0
                || state.selectionTimeoutSeconds <= 0 || state.remainingSeconds < 0
                || state.remainingSeconds > state.selectionTimeoutSeconds || string.IsNullOrWhiteSpace(state.autoSelectPolicy))
                throw new System.ArgumentException("Invalid choice counters or timeout.", nameof(state));
            if ((state.phase == LegendaryChoicePhase.SELECTED || state.phase == LegendaryChoicePhase.AUTO_SELECTED)
                && (!state.selectedAlienId.HasValue || !state.candidateAlienIds.Contains(state.selectedAlienId.Value)))
                throw new System.ArgumentException("A selected phase requires one of the candidates.", nameof(state));
            if ((state.phase == LegendaryChoicePhase.OPEN || state.phase == LegendaryChoicePhase.CLOSED)
                && state.selectedAlienId.HasValue)
                throw new System.ArgumentException("Open or closed choices cannot expose a selection.", nameof(state));
        }
    }

    /// <summary>
    /// Canonical wire serializer for the Legendary choice state. JsonUtility does
    /// not support nullable value types and emits enum values numerically, while
    /// the Spring DTO uses a nullable selectedAlienId and string phase.
    /// </summary>
    public static class LegendaryChoiceStateJson
    {
        public static string Serialize(LegendaryChoiceState state)
        {
            LegendaryChoiceStateValidator.Validate(state);
            var builder = new StringBuilder(512);
            builder.Append('{');
            AppendString(builder, "choiceId", state.choiceId);
            AppendString(builder, "battleSessionId", state.battleSessionId);
            AppendNumber(builder, "materialAlienIdA", state.materialAlienIdA);
            AppendNumber(builder, "materialAlienIdB", state.materialAlienIdB);
            AppendCandidates(builder, state.candidateAlienIds);
            AppendNumber(builder, "rerollCount", state.rerollCount);
            AppendNumber(builder, "freeRerollsRemaining", state.freeRerollsRemaining);
            AppendNumber(builder, "paidRerollsRemaining", state.paidRerollsRemaining);
            AppendNumber(builder, "selectionTimeoutSeconds", state.selectionTimeoutSeconds);
            AppendNumber(builder, "remainingSeconds", state.remainingSeconds);
            AppendString(builder, "phase", state.phase.ToString());
            AppendNullableNumber(builder, "selectedAlienId", state.selectedAlienId);
            AppendString(builder, "autoSelectPolicy", state.autoSelectPolicy);
            AppendBoolean(builder, "battleContinuesDuringSelection", state.battleContinuesDuringSelection, false);
            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendCandidates(StringBuilder builder, long[] values)
        {
            AppendName(builder, "candidateAlienIds");
            builder.Append('[');
            for (var index = 0; index < values.Length; index++)
            {
                if (index > 0) builder.Append(',');
                builder.Append(values[index].ToString(CultureInfo.InvariantCulture));
            }
            builder.Append("],");
        }

        private static void AppendString(StringBuilder builder, string name, string value)
        {
            AppendName(builder, name);
            if (value == null)
            {
                builder.Append("null,");
                return;
            }
            builder.Append('"');
            foreach (var character in value)
            {
                if (character == '"' || character == '\\') builder.Append('\\');
                builder.Append(character);
            }
            builder.Append("\",");
        }

        private static void AppendNumber(StringBuilder builder, string name, long value)
        {
            AppendName(builder, name);
            builder.Append(value.ToString(CultureInfo.InvariantCulture)).Append(',');
        }

        private static void AppendNullableNumber(StringBuilder builder, string name, long? value)
        {
            AppendName(builder, name);
            builder.Append(value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "null").Append(',');
        }

        private static void AppendBoolean(StringBuilder builder, string name, bool value, bool trailingComma)
        {
            AppendName(builder, name);
            builder.Append(value ? "true" : "false");
            if (trailingComma) builder.Append(',');
        }

        private static void AppendName(StringBuilder builder, string name)
        {
            builder.Append('"').Append(name).Append("\":");
        }
    }
}
