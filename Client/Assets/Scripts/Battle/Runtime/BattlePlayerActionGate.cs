using MyDefense.Shared.Contracts;
using UnityEngine;

namespace MyDefense.Battle.Runtime
{
    /// <summary>
    /// Client-side input gate backed by the replicated BattleWaveStateAuthority.
    /// Lobby scenes without a battle authority remain unaffected.
    /// </summary>
    public static class BattlePlayerActionGate
    {
        public static bool CanUseBattleAction(string actionName)
        {
            BattleWaveStateAuthority authority = Object.FindFirstObjectByType<BattleWaveStateAuthority>();
            if (authority == null || authority.Executor == null)
                return true;

            LaneType localLane = authority.Executor.LocalPlayerLane;
            if (authority.IsPlayerActionAllowed(localLane))
                return true;

            Debug.LogWarning($"[BattlePlayerActionGate] {actionName} blocked for eliminated lane {localLane}.");
            return false;
        }
    }
}
