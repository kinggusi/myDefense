using System;
using Fusion;
using MyDefense.Battle.Balance.Canonical;

namespace MyDefense.Battle
{
    public sealed partial class BattleWaveStateAuthority
    {
        [Networked] public int Player1NormalResonanceLevel { get; private set; }
        [Networked] public int Player1MythicResonanceLevel { get; private set; }
        [Networked] public int Player2NormalResonanceLevel { get; private set; }
        [Networked] public int Player2MythicResonanceLevel { get; private set; }

        public event Action<int, CanonicalResonanceTrack, int, int> ResonanceUpgraded;

        public int GetResonanceLevel(int playerSlot, CanonicalResonanceTrack track)
        {
            return (playerSlot, track) switch
            {
                (1, CanonicalResonanceTrack.NORMAL) => Player1NormalResonanceLevel,
                (1, CanonicalResonanceTrack.MYTHIC) => Player1MythicResonanceLevel,
                (2, CanonicalResonanceTrack.NORMAL) => Player2NormalResonanceLevel,
                (2, CanonicalResonanceTrack.MYTHIC) => Player2MythicResonanceLevel,
                _ => 0
            };
        }

        public void RequestResonanceUpgrade(CanonicalResonanceTrack track)
        {
            if (!IsSpawnedForAccess || Object == null || !Object.IsValid || Runner == null || !Runner.IsRunning
                || !IsSupportedResonanceTrack(track))
                return;

            if (HasStateAuthority)
            {
                if (TryResolvePlayerSlot(Runner.LocalPlayer, out int playerSlot))
                    ApplyResonanceUpgrade(playerSlot, track);
                return;
            }

            RPC_RequestResonanceUpgrade((int)track);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestResonanceUpgrade(int trackValue, RpcInfo info = default)
        {
            CanonicalResonanceTrack track = (CanonicalResonanceTrack)trackValue;
            if (IsSupportedResonanceTrack(track) && TryResolvePlayerSlot(info.Source, out int playerSlot))
                ApplyResonanceUpgrade(playerSlot, track);
        }

        private void ApplyResonanceUpgrade(int playerSlot, CanonicalResonanceTrack track)
        {
            if (!HasStateAuthority || _executor == null || (playerSlot != 1 && playerSlot != 2)
                || !IsSupportedResonanceTrack(track))
                return;

            LaneType lane = playerSlot == 1 ? LaneType.Player1Lane : LaneType.Player2Lane;
            if (!IsPlayerActionAllowed(lane) || IsMythicChoiceActive(playerSlot))
                return;

            int currentLevel = GetResonanceLevel(playerSlot, track);
            if (!_executor.TryGetCanonicalResonanceLevel(track, currentLevel + 1, out CanonicalResonanceLevel balance)
                || balance == null || balance.RequiredGold <= 0
                || !TrySpendGold(lane, balance.RequiredGold, out int remainingGold))
                return;

            SetResonanceLevel(playerSlot, track, currentLevel + 1);
            RPC_ResonanceUpgraded(playerSlot, (int)track, currentLevel + 1, remainingGold);
        }

        private void SetResonanceLevel(int playerSlot, CanonicalResonanceTrack track, int level)
        {
            if (playerSlot == 1 && track == CanonicalResonanceTrack.NORMAL) Player1NormalResonanceLevel = level;
            else if (playerSlot == 1 && track == CanonicalResonanceTrack.MYTHIC) Player1MythicResonanceLevel = level;
            else if (playerSlot == 2 && track == CanonicalResonanceTrack.NORMAL) Player2NormalResonanceLevel = level;
            else if (playerSlot == 2 && track == CanonicalResonanceTrack.MYTHIC) Player2MythicResonanceLevel = level;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ResonanceUpgraded(int playerSlot, int trackValue, int level, int remainingGold)
        {
            ResonanceUpgraded?.Invoke(playerSlot, (CanonicalResonanceTrack)trackValue, level, remainingGold);
        }

        private static bool IsSupportedResonanceTrack(CanonicalResonanceTrack track)
            => track == CanonicalResonanceTrack.NORMAL || track == CanonicalResonanceTrack.MYTHIC;
    }
}
