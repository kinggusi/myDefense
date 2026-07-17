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
}
