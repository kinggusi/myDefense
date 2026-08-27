#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;

namespace MyDefense.Battle.Runtime
{
    public enum BattleP1ValidationParseState
    {
        NotValidation = 0,
        Valid = 1,
        Malformed = 2
    }

    /// <summary>
    /// Immutable development-only contract encoded in a Fusion room name.
    /// Production players never compile or consume this validation profile.
    /// </summary>
    public sealed class BattleP1ValidationSessionProfile
    {
        public const string Prefix = "P1VAL-";
        public const int MinimumNonceLength = 12;
        public const int MaximumNonceLength = 32;

        private static readonly HashSet<string> CanonicalMapIds = new(StringComparer.Ordinal)
        {
            "NEPTUNE",
            "URANUS",
            "SATURN",
            "JUPITER",
            "MARS",
            "EARTH",
            "VENUS",
            "MERCURY",
            "SUN"
        };

        private BattleP1ValidationSessionProfile(
            string sessionName,
            string mapId,
            int initialWave,
            string nonce)
        {
            SessionName = sessionName;
            MapId = mapId;
            InitialWave = initialWave;
            Nonce = nonce;
        }

        public string SessionName { get; }
        public string MapId { get; }
        public int InitialWave { get; }
        public string Nonce { get; }

        public static BattleP1ValidationParseState Parse(
            string sessionName,
            out BattleP1ValidationSessionProfile profile,
            out string reason)
        {
            profile = null;
            reason = string.Empty;

            if (string.IsNullOrWhiteSpace(sessionName))
                return BattleP1ValidationParseState.NotValidation;

            string trimmed = sessionName.Trim();
            if (!trimmed.StartsWith("P1VAL", StringComparison.OrdinalIgnoreCase))
                return BattleP1ValidationParseState.NotValidation;
            if (!string.Equals(trimmed, sessionName, StringComparison.Ordinal))
                return Malformed("P1 validation session names must not contain surrounding whitespace.", out reason);

            if (!trimmed.StartsWith(Prefix, StringComparison.Ordinal))
                return Malformed("P1 validation session names must begin with the exact uppercase prefix 'P1VAL-'.", out reason);

            string[] parts = trimmed.Split('-');
            if (parts.Length != 4 || !string.Equals(parts[0], "P1VAL", StringComparison.Ordinal))
                return Malformed("Expected P1VAL-{MAP}-W{NNN}-{12..32 hex nonce}.", out reason);

            string mapId = parts[1];
            if (!CanonicalMapIds.Contains(mapId))
                return Malformed("P1 validation mapId must be one of the nine canonical planet ids.", out reason);

            string waveToken = parts[2];
            if (waveToken.Length != 4
                || waveToken[0] != 'W'
                || !IsAsciiDigit(waveToken[1])
                || !IsAsciiDigit(waveToken[2])
                || !IsAsciiDigit(waveToken[3])
                || !int.TryParse(waveToken.Substring(1), out int initialWave)
                || initialWave < 1
                || initialWave > 80)
            {
                return Malformed("P1 validation Wave must use W001 through W080.", out reason);
            }

            string nonce = parts[3];
            if (nonce.Length < MinimumNonceLength || nonce.Length > MaximumNonceLength)
                return Malformed("P1 validation nonce must contain 12 through 32 hexadecimal characters.", out reason);
            for (int index = 0; index < nonce.Length; index++)
            {
                char value = nonce[index];
                bool hexadecimal = value >= '0' && value <= '9'
                    || value >= 'a' && value <= 'f'
                    || value >= 'A' && value <= 'F';
                if (!hexadecimal)
                    return Malformed("P1 validation nonce must contain hexadecimal characters only.", out reason);
            }

            profile = new BattleP1ValidationSessionProfile(trimmed, mapId, initialWave, nonce);
            return BattleP1ValidationParseState.Valid;
        }

        private static bool IsAsciiDigit(char value)
        {
            return value >= '0' && value <= '9';
        }

        private static BattleP1ValidationParseState Malformed(string message, out string reason)
        {
            reason = message;
            return BattleP1ValidationParseState.Malformed;
        }
    }
}
#endif
