using System;

namespace MyDefense.Battle.Runtime
{
    public enum BattleMonsterLanePolicy
    {
        EACH_FIELD,
        BOSS_SHARED
    }

    public readonly struct BattleRuntimeMonsterKey : IEquatable<BattleRuntimeMonsterKey>, IComparable<BattleRuntimeMonsterKey>
    {
        public string BattleSessionId { get; }
        public ulong RuntimeMonsterId { get; }

        public BattleRuntimeMonsterKey(string battleSessionId, ulong runtimeMonsterId)
        {
            BattleSessionId = BattleSessionContext.RequireText(battleSessionId, nameof(battleSessionId));
            if (runtimeMonsterId == 0)
                throw new ArgumentOutOfRangeException(nameof(runtimeMonsterId), "Runtime monster ID starts at one.");

            RuntimeMonsterId = runtimeMonsterId;
        }

        public bool Equals(BattleRuntimeMonsterKey other)
        {
            return RuntimeMonsterId == other.RuntimeMonsterId
                && string.Equals(BattleSessionId, other.BattleSessionId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is BattleRuntimeMonsterKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int sessionHash = BattleSessionId == null ? 0 : StringComparer.Ordinal.GetHashCode(BattleSessionId);
                return (sessionHash * 397) ^ RuntimeMonsterId.GetHashCode();
            }
        }

        public int CompareTo(BattleRuntimeMonsterKey other)
        {
            int sessionOrder = string.Compare(BattleSessionId, other.BattleSessionId, StringComparison.Ordinal);
            return sessionOrder != 0 ? sessionOrder : RuntimeMonsterId.CompareTo(other.RuntimeMonsterId);
        }

        public override string ToString() => BattleSessionId + ":" + RuntimeMonsterId;
    }

    public sealed class BattleSpawnSequenceIssuer
    {
        private ulong _nextSequence = 1;

        public ulong IssueNext()
        {
            if (_nextSequence == 0)
                throw new OverflowException("Battle spawn sequence is exhausted.");

            ulong issued = _nextSequence;
            _nextSequence = issued == ulong.MaxValue ? 0 : issued + 1;
            return issued;
        }
    }

    public interface IBattlePlayerIdentityProvider
    {
        bool TryGetPlayerId(LaneType lane, out string playerId);
    }

    public sealed class BattlePlayerIdentityMap : IBattlePlayerIdentityProvider
    {
        private readonly string _player1Id;
        private readonly string _player2Id;

        public BattlePlayerIdentityMap(string player1Id, string player2Id)
        {
            _player1Id = BattleSessionContext.RequireText(player1Id, nameof(player1Id));
            _player2Id = BattleSessionContext.RequireText(player2Id, nameof(player2Id));
            if (string.Equals(_player1Id, _player2Id, StringComparison.Ordinal))
                throw new ArgumentException("Player identities must be distinct.", nameof(player2Id));
        }

        public bool TryGetPlayerId(LaneType lane, out string playerId)
        {
            switch (lane)
            {
                case LaneType.Player1Lane:
                    playerId = _player1Id;
                    return true;
                case LaneType.Player2Lane:
                    playerId = _player2Id;
                    return true;
                default:
                    playerId = null;
                    return false;
            }
        }
    }

    public sealed class BattleMonsterRuntimeIdentity
    {
        public BattleSessionContext Session { get; }
        public string BattleSessionId => Session.BattleSessionId;
        public ulong RuntimeMonsterId { get; }
        public string MonsterId { get; }
        public BattleMonsterLanePolicy LanePolicy { get; }
        public string FieldOwnerPlayerId { get; }
        public int SpawnWave { get; }
        public ulong SpawnSequence { get; }
        public string CanonicalBalanceVersion => Session.CanonicalBalanceVersion;
        public string CanonicalContentHash => Session.CanonicalContentHash;
        public BattleRuntimeMonsterKey RuntimeKey => new BattleRuntimeMonsterKey(BattleSessionId, RuntimeMonsterId);

        public BattleMonsterRuntimeIdentity(
            BattleSessionContext session,
            ulong runtimeMonsterId,
            string monsterId,
            BattleMonsterLanePolicy lanePolicy,
            string fieldOwnerPlayerId,
            int spawnWave,
            ulong spawnSequence)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            if (runtimeMonsterId == 0)
                throw new ArgumentOutOfRangeException(nameof(runtimeMonsterId));
            if (spawnSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(spawnSequence));
            if (runtimeMonsterId != spawnSequence)
                throw new ArgumentException("Runtime monster ID must equal its spawn sequence.", nameof(runtimeMonsterId));
            if (spawnWave < 1)
                throw new ArgumentOutOfRangeException(nameof(spawnWave));

            MonsterId = BattleSessionContext.RequireText(monsterId, nameof(monsterId));
            LanePolicy = lanePolicy;
            if (lanePolicy == BattleMonsterLanePolicy.EACH_FIELD)
            {
                FieldOwnerPlayerId = BattleSessionContext.RequireText(fieldOwnerPlayerId, nameof(fieldOwnerPlayerId));
            }
            else if (lanePolicy == BattleMonsterLanePolicy.BOSS_SHARED)
            {
                if (!string.IsNullOrEmpty(fieldOwnerPlayerId))
                    throw new ArgumentException("BOSS_SHARED monsters cannot have a field owner.", nameof(fieldOwnerPlayerId));
                FieldOwnerPlayerId = null;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(lanePolicy));
            }

            RuntimeMonsterId = runtimeMonsterId;
            SpawnWave = spawnWave;
            SpawnSequence = spawnSequence;
        }
    }
}
