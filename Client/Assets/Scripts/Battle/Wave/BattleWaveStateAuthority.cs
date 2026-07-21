using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;

namespace MyDefense.Battle
{
    /// <summary>
    /// State-authority boundary for the existing wave executor.
    /// Networked wave fields are introduced by P0-3-2; this component owns
    /// which peer is allowed to invoke the executor in the meantime.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(BattleWaveExecutor))]
    public sealed class BattleWaveStateAuthority : NetworkBehaviour
    {
        private const int DefaultInitialInGameGold = 500;
        private BattleWaveExecutor _executor;

        [Networked] public int CurrentWave { get; private set; }
        [Networked] public NetworkString<_32> CurrentWaveId { get; private set; }
        [Networked] public int CurrentWaveTypeValue { get; private set; }
        [Networked] public NetworkBool IsWaveRunning { get; private set; }
        [Networked] public int MatchStateValue { get; private set; }
        [Networked] public int Player1AliveMonsterCount { get; private set; }
        [Networked] public int Player2AliveMonsterCount { get; private set; }
        [Networked] public int PlayerMonsterLimit { get; private set; }
        [Networked] public NetworkBool Player1WarningReached { get; private set; }
        [Networked] public NetworkBool Player2WarningReached { get; private set; }
        [Networked] public NetworkBool Player1DangerReached { get; private set; }
        [Networked] public NetworkBool Player2DangerReached { get; private set; }
        [Networked] public NetworkBool Player1Eliminated { get; private set; }
        [Networked] public NetworkBool Player2Eliminated { get; private set; }
        [Networked] public int Player1BattleStateValue { get; private set; }
        [Networked] public int Player2BattleStateValue { get; private set; }
        [Networked] public int Player1InGameGold { get; private set; }
        [Networked] public int Player2InGameGold { get; private set; }
        [Networked] public int Player1KidnapCount { get; private set; }
        [Networked] public int Player2KidnapCount { get; private set; }
        [Networked] public PlayerRef Player1Ref { get; private set; }
        [Networked] public PlayerRef Player2Ref { get; private set; }
        [Networked, Capacity(24)] private NetworkArray<NetworkBool> Player1BoardOccupied => default;
        [Networked, Capacity(24)] private NetworkArray<NetworkBool> Player2BoardOccupied => default;

        public event Action<int, int> KidnapApplied;
        public event Action<int, int, int, bool> BoardChanged;

        public BattleWaveExecutor Executor => _executor;
        public bool IsSpawnedForAccess { get; private set; }
        public bool IsAuthoritative => HasStateAuthority;
        public WaveType CurrentWaveType => (WaveType)CurrentWaveTypeValue;
        public MatchState MatchState => (MatchState)MatchStateValue;
        public PlayerBattleState Player1BattleState => (PlayerBattleState)Player1BattleStateValue;
        public PlayerBattleState Player2BattleState => (PlayerBattleState)Player2BattleStateValue;

        public int GetKidnapCount(int playerSlot)
        {
            return playerSlot == 1 ? Player1KidnapCount : playerSlot == 2 ? Player2KidnapCount : 0;
        }

        public int GetInGameGoldForPlayerSlot(int playerSlot)
        {
            return playerSlot == 1 ? Player1InGameGold : playerSlot == 2 ? Player2InGameGold : 0;
        }

        public bool IsBoardOccupied(int playerSlot, int slotIndex)
        {
            if (!IsValidBoardIndex(slotIndex))
                return false;
            return playerSlot == 1 ? Player1BoardOccupied[slotIndex]
                : playerSlot == 2 && Player2BoardOccupied[slotIndex];
        }

        public int GetNetworkedPlayerSlot(PlayerRef playerRef)
        {
            if (!playerRef.IsRealPlayer)
                return 0;
            if (playerRef == Player1Ref)
                return 1;
            if (playerRef == Player2Ref)
                return 2;
            return 0;
        }

        public bool IsPlayerActionAllowed(LaneType lane)
        {
            return lane switch
            {
                LaneType.Player1Lane => Player1BattleState != PlayerBattleState.ELIMINATED,
                LaneType.Player2Lane => Player2BattleState != PlayerBattleState.ELIMINATED,
                _ => false
            };
        }

        public override void Spawned()
        {
            IsSpawnedForAccess = true;
            _executor = GetComponent<BattleWaveExecutor>();
            if (_executor == null)
                throw new InvalidOperationException("BattleWaveStateAuthority requires BattleWaveExecutor on the same NetworkObject.");

            _executor.OnRegularWaveCompleted += HandleRegularWaveCompleted;
            _executor.OnBossDefeated += HandleWaveCompleted;
            _executor.OnRoundChanged += HandleRoundChanged;
            _executor.OnMatchStateChanged += HandleMatchStateChanged;
            _executor.OnPlayerMonsterCountChanged += HandlePlayerMonsterCountChanged;
            _executor.OnPlayerBattleStateChanged += HandlePlayerBattleStateChanged;
            _executor.OnPlayerMonsterWarningReached += HandlePlayerMonsterWarningReached;
            _executor.OnPlayerMonsterDangerReached += HandlePlayerMonsterDangerReached;
            _executor.OnPlayerMonsterLimitReached += HandlePlayerMonsterLimitReached;
            if (HasStateAuthority)
            {
                MatchStateValue = (int)MyDefense.Shared.Contracts.MatchState.RUNNING;
                IsWaveRunning = false;
                ResetFieldLimitEvents();
                SyncPlayerBattleStates();
                SyncAliveMonsterCounts();
                InitializeInGameGold(DefaultInitialInGameGold, DefaultInitialInGameGold);
                Player1KidnapCount = 0;
                Player2KidnapCount = 0;
                ResetBoardOccupancy();
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            BattleRunnerLifecycle lifecycle = FindFirstObjectByType<BattleRunnerLifecycle>();
            if (lifecycle == null || lifecycle.Runner == null)
                return;

            if (lifecycle.PlayerRoster.TryGetByUserIdForSlot(1, out BattlePlayerIdentity player1))
                Player1Ref = player1.PlayerRef;
            if (lifecycle.PlayerRoster.TryGetByUserIdForSlot(2, out BattlePlayerIdentity player2))
                Player2Ref = player2.PlayerRef;
        }

        public void RequestKidnap()
        {
            if (Object != null && Object.IsValid)
                RPC_RequestKidnap();
        }

        public void RequestMove(int fromSlotIndex, int toSlotIndex)
        {
            if (!IsSpawnedForAccess || Object == null || !Object.IsValid)
                return;

            // A host is already the State Authority. Applying its local input
            // directly avoids relying on a self-targeted RPC, while remote
            // clients continue to use the authoritative RPC path below.
            if (HasStateAuthority)
            {
                if (TryResolvePlayerSlot(Runner.LocalPlayer, out int playerSlot))
                    ApplyMove(playerSlot, fromSlotIndex, toSlotIndex);
                return;
            }

            RPC_RequestMove(fromSlotIndex, toSlotIndex);
        }

        public void RequestMerge(int sourceSlotIndex, int targetSlotIndex)
        {
            // Merge is intentionally gated until the networked board carries
            // canonical alienId/grade metadata. Occupancy alone cannot prove
            // the same-species rule and must never authorize a merge.
            Debug.LogWarning("[Fusion] Merge request deferred: board identity metadata is not initialized.");
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestMove(int fromSlotIndex, int toSlotIndex, RpcInfo info = default)
        {
            if (!TryResolvePlayerSlot(info.Source, out int playerSlot)
                || !IsValidBoardIndex(fromSlotIndex) || !IsValidBoardIndex(toSlotIndex)
                || fromSlotIndex == toSlotIndex)
                return;

            ApplyMove(playerSlot, fromSlotIndex, toSlotIndex);
        }

        private void ApplyMove(int playerSlot, int fromSlotIndex, int toSlotIndex)
        {
            if (!IsValidBoardIndex(fromSlotIndex) || !IsValidBoardIndex(toSlotIndex)
                || fromSlotIndex == toSlotIndex
                || !IsPlayerActionAllowed(playerSlot == 1 ? LaneType.Player1Lane : playerSlot == 2 ? LaneType.Player2Lane : (LaneType)(-1)))
                return;

            NetworkArray<NetworkBool> board = playerSlot == 1 ? Player1BoardOccupied : Player2BoardOccupied;
            if (!board[fromSlotIndex] || board[toSlotIndex])
                return;

            board.Set(fromSlotIndex, false);
            board.Set(toSlotIndex, true);
            RPC_BoardChanged(playerSlot, fromSlotIndex, toSlotIndex, false);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_BoardChanged(int playerSlot, int fromSlotIndex, int toSlotIndex, bool merged)
        {
            BoardChanged?.Invoke(playerSlot, fromSlotIndex, toSlotIndex, merged);
        }

        private bool TryResolvePlayerSlot(PlayerRef playerRef, out int playerSlot)
        {
            // The RPC source must be resolved from replicated Fusion identity
            // first. The local roster can still be initializing on the host or
            // on a late-joining peer when the move request arrives.
            if (playerRef == Player1Ref)
            {
                playerSlot = 1;
                return true;
            }

            if (playerRef == Player2Ref)
            {
                playerSlot = 2;
                return true;
            }

            playerSlot = 0;
            return false;
        }

        public static bool IsValidBoardIndex(int index) => index >= 0 && index < 24;

        public static int FindFirstEmptyBoardSlot(IReadOnlyList<bool> occupied)
        {
            if (occupied == null || occupied.Count < 24)
                return -1;

            for (int index = 0; index < 24; index++)
            {
                if (!occupied[index])
                    return index;
            }

            return -1;
        }

        private static int FindFirstEmptyBoardSlot(NetworkArray<NetworkBool> board)
        {
            bool[] occupied = new bool[24];
            for (int index = 0; index < 24; index++)
                occupied[index] = board[index];
            return FindFirstEmptyBoardSlot(occupied);
        }

        private void ResetBoardOccupancy()
        {
            for (int index = 0; index < 24; index++)
            {
                Player1BoardOccupied.Set(index, false);
                Player2BoardOccupied.Set(index, false);
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestKidnap(RpcInfo info = default)
        {
            if (!HasStateAuthority || _executor == null)
                return;

            BattleRunnerLifecycle lifecycle = FindFirstObjectByType<BattleRunnerLifecycle>();
            if (lifecycle == null)
                return;
            PlayerRef source = info.Source.IsRealPlayer ? info.Source : Runner.LocalPlayer;
            if (!lifecycle.PlayerRoster.TryGet(source, out BattlePlayerIdentity identity))
                return;

            if (identity.PlayerSlot != 1 && identity.PlayerSlot != 2)
                return;
            LaneType lane = identity.PlayerSlot == 1 ? LaneType.Player1Lane : LaneType.Player2Lane;
            int useCount = identity.PlayerSlot == 1 ? Player1KidnapCount : Player2KidnapCount;
            if (!IsPlayerActionAllowed(lane) || !_executor.TryGetCanonicalSummonCost(useCount, out int cost))
                return;

            NetworkArray<NetworkBool> board = identity.PlayerSlot == 1 ? Player1BoardOccupied : Player2BoardOccupied;
            int slotIndex = FindFirstEmptyBoardSlot(board);
            if (slotIndex < 0)
            {
                Debug.Log($"[Fusion] Kidnap rejected: board is full for slot {identity.PlayerSlot}.");
                return;
            }

            if (!TrySpendGold(lane, cost, out int remainingGold))
            {
                Debug.Log($"[Fusion] Kidnap rejected: insufficient gold for slot {identity.PlayerSlot}.");
                return;
            }

            if (identity.PlayerSlot == 1) Player1KidnapCount++;
            else Player2KidnapCount++;
            board.Set(slotIndex, true);
            RPC_KidnapApplied(identity.PlayerSlot, slotIndex);
            Debug.Log($"[Fusion] Kidnap request authorized: slot={identity.PlayerSlot}, cost={cost}, remaining={remainingGold}.");
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_KidnapApplied(int playerSlot, int slotIndex)
        {
            KidnapApplied?.Invoke(playerSlot, slotIndex);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            IsSpawnedForAccess = false;
            if (_executor == null)
                return;

            _executor.OnRegularWaveCompleted -= HandleRegularWaveCompleted;
            _executor.OnBossDefeated -= HandleWaveCompleted;
            _executor.OnRoundChanged -= HandleRoundChanged;
            _executor.OnMatchStateChanged -= HandleMatchStateChanged;
            _executor.OnPlayerMonsterCountChanged -= HandlePlayerMonsterCountChanged;
            _executor.OnPlayerBattleStateChanged -= HandlePlayerBattleStateChanged;
            _executor.OnPlayerMonsterWarningReached -= HandlePlayerMonsterWarningReached;
            _executor.OnPlayerMonsterDangerReached -= HandlePlayerMonsterDangerReached;
            _executor.OnPlayerMonsterLimitReached -= HandlePlayerMonsterLimitReached;
            _executor = null;
        }

        public bool InitializeSession(
            BattleSessionContext sessionContext,
            IBattlePlayerIdentityProvider playerIdentityProvider)
        {
            if (!HasStateAuthority || _executor == null)
                return false;
            _executor.InitializeSession(sessionContext, playerIdentityProvider);
            ResetFieldLimitEvents();
            SyncPlayerBattleStates();
            SyncAliveMonsterCounts();
            return true;
        }

        public bool ValidateWaveStart(out string reason)
        {
            if (!HasStateAuthority)
                return FailValidation("Only State Authority may start a wave.", out reason);
            if (_executor == null)
                return FailValidation("BattleWaveExecutor is unavailable.", out reason);
            if (MatchState != MatchState.RUNNING)
                return FailValidation("MatchState must be RUNNING to start a wave.", out reason);
            if (IsWaveRunning)
                return FailValidation("A wave is already running.", out reason);
            if (_executor.IsBossActive)
                return FailValidation("The current Boss is still active.", out reason);
            if (_executor.SpawnedMonsterCount > 0)
                return FailValidation("Alive monsters remain from the previous wave.", out reason);
            if (_executor.AreAllPlayersEliminated)
                return FailValidation("All players are eliminated.", out reason);

            reason = string.Empty;
            return true;
        }

        public bool ValidateWaveEnd(out string reason)
        {
            if (!HasStateAuthority)
                return FailValidation("Only State Authority may validate wave completion.", out reason);
            if (_executor == null)
                return FailValidation("BattleWaveExecutor is unavailable.", out reason);
            if (CurrentWave <= 0)
                return FailValidation("No wave has started.", out reason);
            if (MatchState != MatchState.RUNNING)
                return FailValidation("MatchState is already terminal.", out reason);
            if (IsWaveRunning || _executor.IsBossActive)
                return FailValidation("The current wave is still running.", out reason);

            reason = string.Empty;
            return true;
        }

        public bool ValidateMatchState(MatchState expected)
        {
            return MatchState == expected;
        }

        public int GetInGameGold(LaneType lane)
        {
            return lane switch
            {
                LaneType.Player1Lane => Player1InGameGold,
                LaneType.Player2Lane => Player2InGameGold,
                _ => 0
            };
        }

        public bool InitializeInGameGold(int player1Gold, int player2Gold)
        {
            if (!HasStateAuthority || player1Gold < 0 || player2Gold < 0)
                return false;

            Player1InGameGold = player1Gold;
            Player2InGameGold = player2Gold;
            return true;
        }

        public bool TrySpendGold(LaneType lane, int amount, out int remainingGold)
        {
            remainingGold = GetInGameGold(lane);
            if (!HasStateAuthority || amount <= 0)
                return false;

            if (lane == LaneType.Player1Lane)
            {
                if (Player1InGameGold < amount) return false;
                Player1InGameGold -= amount;
                remainingGold = Player1InGameGold;
                return true;
            }

            if (lane == LaneType.Player2Lane)
            {
                if (Player2InGameGold < amount) return false;
                Player2InGameGold -= amount;
                remainingGold = Player2InGameGold;
                return true;
            }

            return false;
        }

        public bool TryAwardGold(LaneType lane, int amount)
        {
            if (!HasStateAuthority || amount < 0)
                return false;

            if (lane == LaneType.Player1Lane)
            {
                Player1InGameGold = AddGoldSafely(Player1InGameGold, amount);
                return true;
            }

            if (lane == LaneType.Player2Lane)
            {
                Player2InGameGold = AddGoldSafely(Player2InGameGold, amount);
                return true;
            }

            return false;
        }

        private static int AddGoldSafely(int current, int amount)
        {
            long total = (long)current + amount;
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        public bool TryStartNextWave()
        {
            string reason;
            if (!ValidateWaveStart(out reason))
                return false;
            _executor.StartNextWave();
            CurrentWave = _executor.CurrentRound;
            CurrentWaveId = _executor.CurrentWaveId ?? string.Empty;
            CurrentWaveTypeValue = _executor.IsCurrentWaveBoss ? (int)WaveType.BOSS : (int)WaveType.REGULAR;
            IsWaveRunning = _executor.IsWaveRunning;
            return true;
        }

        private static bool FailValidation(string message, out string reason)
        {
            reason = message;
            return false;
        }

        private void HandleRegularWaveCompleted(int _)
        {
            HandleWaveCompleted();
        }

        private void HandleRoundChanged(int round)
        {
            if (!HasStateAuthority)
                return;

            CurrentWave = round;
            if (_executor == null)
                return;

            CurrentWaveId = _executor.CurrentWaveId ?? string.Empty;
            CurrentWaveTypeValue = _executor.IsCurrentWaveBoss ? (int)WaveType.BOSS : (int)WaveType.REGULAR;
            IsWaveRunning = _executor.IsWaveRunning;
        }

        private void HandleWaveCompleted()
        {
            if (!HasStateAuthority)
                return;
            IsWaveRunning = false;
        }

        private void HandleMatchStateChanged(MatchState state)
        {
            if (!HasStateAuthority)
                return;
            MatchStateValue = (int)state;
            IsWaveRunning = state == MatchState.RUNNING && _executor != null && _executor.IsWaveRunning;
        }

        private void HandlePlayerMonsterCountChanged(LaneType lane, int count, int limit)
        {
            if (!HasStateAuthority)
                return;

            PlayerMonsterLimit = limit;
            if (lane == LaneType.Player1Lane)
                Player1AliveMonsterCount = count;
            else if (lane == LaneType.Player2Lane)
                Player2AliveMonsterCount = count;
        }

        private void HandlePlayerBattleStateChanged(LaneType lane, PlayerBattleState state)
        {
            if (!HasStateAuthority)
                return;
            if (lane == LaneType.Player1Lane)
            {
                Player1BattleStateValue = (int)state;
                if (state == PlayerBattleState.ELIMINATED)
                    Player1Eliminated = true;
            }
            else if (lane == LaneType.Player2Lane)
            {
                Player2BattleStateValue = (int)state;
                if (state == PlayerBattleState.ELIMINATED)
                    Player2Eliminated = true;
            }
        }

        private void HandlePlayerMonsterWarningReached(LaneType lane, int _)
        {
            if (!HasStateAuthority)
                return;
            if (lane == LaneType.Player1Lane)
                Player1WarningReached = true;
            else if (lane == LaneType.Player2Lane)
                Player2WarningReached = true;
        }

        private void HandlePlayerMonsterDangerReached(LaneType lane, int _)
        {
            if (!HasStateAuthority)
                return;
            if (lane == LaneType.Player1Lane)
                Player1DangerReached = true;
            else if (lane == LaneType.Player2Lane)
                Player2DangerReached = true;
        }

        private void HandlePlayerMonsterLimitReached(LaneType lane)
        {
            if (!HasStateAuthority)
                return;
            if (lane == LaneType.Player1Lane)
                Player1Eliminated = true;
            else if (lane == LaneType.Player2Lane)
                Player2Eliminated = true;
        }

        private void ResetFieldLimitEvents()
        {
            if (!HasStateAuthority)
                return;
            Player1WarningReached = false;
            Player2WarningReached = false;
            Player1DangerReached = false;
            Player2DangerReached = false;
            Player1Eliminated = false;
            Player2Eliminated = false;
        }

        private void SyncAliveMonsterCounts()
        {
            if (!HasStateAuthority || _executor == null)
                return;

            Player1AliveMonsterCount = _executor.Player1AliveMonsterCount;
            Player2AliveMonsterCount = _executor.Player2AliveMonsterCount;
            PlayerMonsterLimit = _executor.MonsterLimit;
        }

        private void SyncPlayerBattleStates()
        {
            if (!HasStateAuthority || _executor == null)
                return;

            Player1BattleStateValue = (int)_executor.Player1BattleState;
            Player2BattleStateValue = (int)_executor.Player2BattleState;
        }
    }
}
