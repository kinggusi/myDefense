using System;

namespace MyDefense.Shared.Contracts
{
    [Serializable]
    public sealed class BattleSessionRosterPlayer
    {
        public int playerSlot;
        public string playerId;
    }

    [Serializable]
    public sealed class BattleSessionRosterRegisterRequest
    {
        public string battleSessionId;
        public string mapId;
        public string balanceVersion;
        public string contentHash;
        public BattleSessionRosterPlayer[] players;
    }

    [Serializable]
    public sealed class BattleSessionRosterRegisterResponse
    {
        public string battleSessionId;
        public string status;
        public int playerCount;
    }
}
