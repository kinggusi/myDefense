using System.Globalization;
using System.Linq;
using System.Text;

namespace MyDefense.Shared.Contracts
{
    public enum BattleBoardObjectType
    {
        ALIEN = 0,
        MUTATION_INJECTOR = 1
    }

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

    /// <summary>
    /// Reconnect/resume snapshot for a two-player battle. This is a transport
    /// contract only; Fusion remains authoritative for live state.
    /// </summary>
    [System.Serializable]
    public sealed class BattleSessionSnapshot
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string battleSessionId;
        public string balanceVersion;
        public string contentHash;
        public MatchState matchState;
        public int currentWave;
        public string currentWaveSpecId;
        public string waveType;
        public string wavePhase;
        public int waveTimeRemainingSeconds;
        public int bossTimeRemainingSeconds;
        public long capturedAtTick;
        public BattleSessionPlayerSnapshot[] players;
        public BattleBoardObjectSnapshot[] boardObjects;
    }

    [System.Serializable]
    public sealed class BattleSessionPlayerSnapshot
    {
        public string playerId;
        public int playerSlot;
        public PlayerBattleState battleState;
        public PlayerConnectionState connectionState;
        public int inGameGold;
        public int currentKidnapCost;
        public int? eliminatedWave;
    }

    [System.Serializable]
    public sealed class BattleBoardObjectSnapshot
    {
        public long objectId;
        public int ownerPlayerSlot;
        public BattleBoardObjectType objectType;
        public int gridX;
        public int gridY;
        public long? alienSpecId;
        public string grade;
        public string pendingMutationType;
        public string activeMutationType;
        public int mutationRerollCount;
        public string mutationType;
    }

    public static class BattleSessionSnapshotValidator
    {
        public static void Validate(BattleSessionSnapshot snapshot)
        {
            if (snapshot == null) throw new System.ArgumentNullException(nameof(snapshot));
            if (snapshot.schemaVersion != BattleSessionSnapshot.CurrentSchemaVersion)
                throw new System.ArgumentException("Unsupported snapshot schema version.", nameof(snapshot));
            if (string.IsNullOrWhiteSpace(snapshot.battleSessionId) || string.IsNullOrWhiteSpace(snapshot.balanceVersion)
                || string.IsNullOrWhiteSpace(snapshot.contentHash) || string.IsNullOrWhiteSpace(snapshot.currentWaveSpecId)
                || (snapshot.waveType != "REGULAR" && snapshot.waveType != "BOSS")
                || (snapshot.wavePhase != "SPAWNING" && snapshot.wavePhase != "ACTIVE" && snapshot.wavePhase != "WAITING" && snapshot.wavePhase != "COMPLETED"))
                throw new System.ArgumentException("Snapshot identity and wave fields are required.", nameof(snapshot));
            if (snapshot.currentWave < 0 || snapshot.waveTimeRemainingSeconds < 0 || snapshot.bossTimeRemainingSeconds < 0
                || snapshot.capturedAtTick < 0)
                throw new System.ArgumentException("Snapshot counters cannot be negative.", nameof(snapshot));
            if (snapshot.players == null || snapshot.players.Length != 2)
                throw new System.ArgumentException("A snapshot requires exactly two players.", nameof(snapshot));
            var slots = new System.Collections.Generic.HashSet<int>();
            foreach (var player in snapshot.players)
            {
                if (player == null || (player.playerSlot != 1 && player.playerSlot != 2) || !slots.Add(player.playerSlot)
                    || string.IsNullOrWhiteSpace(player.playerId) || player.inGameGold < 0 || player.currentKidnapCost < 0
                    || (player.battleState == PlayerBattleState.ELIMINATED && !player.eliminatedWave.HasValue)
                    || (player.battleState != PlayerBattleState.ELIMINATED && player.eliminatedWave.HasValue)
                    || (player.eliminatedWave.HasValue && (player.eliminatedWave.Value <= 0 || player.eliminatedWave.Value > snapshot.currentWave)))
                    throw new System.ArgumentException("Invalid player snapshot.", nameof(snapshot));
            }
            if (!slots.SetEquals(new[] { 1, 2 }))
                throw new System.ArgumentException("Snapshot player slots must be {1,2}.", nameof(snapshot));
            if (snapshot.boardObjects == null)
                throw new System.ArgumentException("Board objects are required.", nameof(snapshot));
            var objectIds = new System.Collections.Generic.HashSet<long>();
            foreach (var boardObject in snapshot.boardObjects)
            {
                if (boardObject == null || boardObject.objectId <= 0 || !objectIds.Add(boardObject.objectId)
                    || (boardObject.ownerPlayerSlot != 1 && boardObject.ownerPlayerSlot != 2)
                    || boardObject.gridX < 0 || boardObject.gridX >= 4 || boardObject.gridY < 0 || boardObject.gridY >= 6
                    || boardObject.mutationRerollCount < 0
                    || (boardObject.alienSpecId.HasValue && boardObject.alienSpecId.Value <= 0)
                    || (boardObject.objectType == BattleBoardObjectType.ALIEN && (!boardObject.alienSpecId.HasValue || string.IsNullOrWhiteSpace(boardObject.grade)))
                    || (boardObject.objectType == BattleBoardObjectType.MUTATION_INJECTOR && string.IsNullOrWhiteSpace(boardObject.mutationType)))
                    throw new System.ArgumentException("Invalid board object snapshot.", nameof(snapshot));
            }
        }
    }

    public static class BattleSessionSnapshotJson
    {
        public static string Serialize(BattleSessionSnapshot snapshot)
        {
            BattleSessionSnapshotValidator.Validate(snapshot);
            var json = new System.Text.StringBuilder();
            json.Append("{\"schemaVersion\":").Append(snapshot.schemaVersion)
                .Append(",\"battleSessionId\":").Append(String(snapshot.battleSessionId))
                .Append(",\"balanceVersion\":").Append(String(snapshot.balanceVersion))
                .Append(",\"contentHash\":").Append(String(snapshot.contentHash))
                .Append(",\"matchState\":").Append(String(snapshot.matchState.ToString()))
                .Append(",\"currentWave\":").Append(snapshot.currentWave)
                .Append(",\"currentWaveSpecId\":").Append(String(snapshot.currentWaveSpecId))
                .Append(",\"waveType\":").Append(String(snapshot.waveType))
                .Append(",\"wavePhase\":").Append(String(snapshot.wavePhase))
                .Append(",\"waveTimeRemainingSeconds\":").Append(snapshot.waveTimeRemainingSeconds)
                .Append(",\"bossTimeRemainingSeconds\":").Append(snapshot.bossTimeRemainingSeconds)
                .Append(",\"capturedAtTick\":").Append(snapshot.capturedAtTick)
                .Append(",\"players\":[");
            for (var i = 0; i < snapshot.players.Length; i++)
            {
                if (i > 0) json.Append(',');
                var player = snapshot.players[i];
                json.Append("{\"playerId\":").Append(String(player.playerId))
                    .Append(",\"playerSlot\":").Append(player.playerSlot)
                    .Append(",\"battleState\":").Append(String(player.battleState.ToString()))
                    .Append(",\"connectionState\":").Append(String(player.connectionState.ToString()))
                    .Append(",\"inGameGold\":").Append(player.inGameGold)
                    .Append(",\"currentKidnapCost\":").Append(player.currentKidnapCost)
                    .Append(",\"eliminatedWave\":");
                if (player.eliminatedWave.HasValue) json.Append(player.eliminatedWave.Value); else json.Append("null");
                json.Append('}');
            }
            json.Append("],\"boardObjects\":[");
            for (var i = 0; i < snapshot.boardObjects.Length; i++)
            {
                if (i > 0) json.Append(',');
                var board = snapshot.boardObjects[i];
                json.Append("{\"objectId\":").Append(board.objectId)
                    .Append(",\"ownerPlayerSlot\":").Append(board.ownerPlayerSlot)
                    .Append(",\"objectType\":").Append(String(board.objectType.ToString()))
                    .Append(",\"gridX\":").Append(board.gridX)
                    .Append(",\"gridY\":").Append(board.gridY)
                    .Append(",\"alienSpecId\":");
                if (board.alienSpecId.HasValue) json.Append(board.alienSpecId.Value); else json.Append("null");
                json.Append(",\"grade\":").Append(String(board.grade))
                    .Append(",\"pendingMutationType\":").Append(String(board.pendingMutationType))
                    .Append(",\"activeMutationType\":").Append(String(board.activeMutationType))
                    .Append(",\"mutationRerollCount\":").Append(board.mutationRerollCount)
                    .Append(",\"mutationType\":").Append(String(board.mutationType)).Append('}');
            }
            return json.Append("]}").ToString();
        }

        private static string String(string value)
        {
            if (value == null) return "null";
            foreach (char character in value)
                if (character < 0x20) throw new System.ArgumentException("Control characters are not allowed in snapshot JSON.", nameof(value));
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
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
            if (value == null) { builder.Append("null,"); return; }
            builder.Append('"');
            foreach (var character in value)
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
