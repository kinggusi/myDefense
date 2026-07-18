using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;

namespace MyDefense.Battle.Runtime
{
    public enum BattleStartState
    {
        WAITING_FOR_PLAYERS = 0,
        WAITING_FOR_READY = 1,
        STARTED = 2
    }

    public sealed class BattleMatchStartCoordinator
    {
        private readonly BattlePlayerRoster _roster;
        private readonly HashSet<PlayerRef> _readyPlayers = new();

        public BattleStartState State { get; private set; } = BattleStartState.WAITING_FOR_PLAYERS;
        public bool CanStart => _roster.Count == 2
            && State != BattleStartState.STARTED
            && _roster.Players.All(player => _readyPlayers.Contains(player.PlayerRef));

        public BattleMatchStartCoordinator(BattlePlayerRoster roster)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }

        public bool SetReady(PlayerRef playerRef, bool ready)
        {
            if (!_roster.TryGet(playerRef, out _))
                return false;

            if (State == BattleStartState.STARTED)
                return false;

            if (ready)
                _readyPlayers.Add(playerRef);
            else
                _readyPlayers.Remove(playerRef);

            State = _roster.Count < 2
                ? BattleStartState.WAITING_FOR_PLAYERS
                : BattleStartState.WAITING_FOR_READY;
            return true;
        }

        public bool TryStart()
        {
            PruneDisconnectedPlayers();
            if (!CanStart)
                return false;
            State = BattleStartState.STARTED;
            return true;
        }

        private void PruneDisconnectedPlayers()
        {
            _readyPlayers.RemoveWhere(playerRef => !_roster.TryGet(playerRef, out _));
        }

        public void Reset()
        {
            _readyPlayers.Clear();
            State = BattleStartState.WAITING_FOR_PLAYERS;
        }
    }
}
