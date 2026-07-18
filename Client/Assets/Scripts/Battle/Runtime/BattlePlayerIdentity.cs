using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion;

namespace MyDefense.Battle.Runtime
{
    public sealed class BattlePlayerIdentity
    {
        public PlayerRef PlayerRef { get; }
        public string UserId { get; }
        public int PlayerSlot { get; }

        internal BattlePlayerIdentity(PlayerRef playerRef, string userId, int playerSlot)
        {
            PlayerRef = playerRef;
            UserId = userId;
            PlayerSlot = playerSlot;
        }
    }

    public sealed class BattlePlayerRoster
    {
        private readonly Dictionary<PlayerRef, BattlePlayerIdentity> _byPlayer = new();
        private readonly Dictionary<string, BattlePlayerIdentity> _byUser = new(StringComparer.Ordinal);

        public int Count => _byPlayer.Count;
        public IReadOnlyList<BattlePlayerIdentity> Players => _byPlayer.Values.OrderBy(identity => identity.PlayerSlot).ToList();

        public bool TryAdd(PlayerRef playerRef, string userId, out BattlePlayerIdentity identity)
        {
            identity = null;
            if (!playerRef.IsRealPlayer || string.IsNullOrWhiteSpace(userId))
                return false;
            userId = userId.Trim();
            if (_byPlayer.ContainsKey(playerRef) || _byUser.ContainsKey(userId) || _byPlayer.Count >= 2)
                return false;

            int playerSlot = _byPlayer.Values.Any(existing => existing.PlayerSlot == 1) ? 2 : 1;
            identity = new BattlePlayerIdentity(playerRef, userId, playerSlot);
            _byPlayer.Add(playerRef, identity);
            _byUser.Add(userId, identity);
            return true;
        }

        public bool Remove(PlayerRef playerRef)
        {
            if (!_byPlayer.Remove(playerRef, out BattlePlayerIdentity identity))
                return false;
            _byUser.Remove(identity.UserId);
            return true;
        }

        public void Clear()
        {
            _byPlayer.Clear();
            _byUser.Clear();
        }

        public bool TryGet(PlayerRef playerRef, out BattlePlayerIdentity identity) => _byPlayer.TryGetValue(playerRef, out identity);

        public bool TryGetByUserId(string userId, out BattlePlayerIdentity identity)
            => _byUser.TryGetValue(userId ?? string.Empty, out identity);
    }

    public static class BattlePlayerIdentityToken
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private const int MaxTokenBytes = 256;

        public static byte[] Encode(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("A non-empty user ID is required.", nameof(userId));
            byte[] token = StrictUtf8.GetBytes(userId.Trim());
            if (token.Length > MaxTokenBytes)
                throw new ArgumentException("User ID token is too long.", nameof(userId));
            return token;
        }

        public static bool TryDecode(byte[] token, out string userId)
        {
            userId = null;
            if (token == null || token.Length == 0)
                return false;
            try
            {
                if (token.Length > MaxTokenBytes)
                    return false;
                userId = StrictUtf8.GetString(token).Trim();
                return !string.IsNullOrWhiteSpace(userId);
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }
    }
}
