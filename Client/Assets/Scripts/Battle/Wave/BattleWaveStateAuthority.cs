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
    public sealed partial class BattleWaveStateAuthority : NetworkBehaviour
    {
        private const byte MutationStateNone = 0;
        private const byte MutationStateInjector = 1;
        private const byte MutationStatePending = 2;
        private const byte MutationStateActive = 3;
        private const byte MutationStateSealed = 4;

        /// <summary>
        /// Resolves inherited DNA without exposing the locked Mythic's mutation effect.
        /// The canonical isLocked marker is the current unlock proxy until a per-player
        /// entry snapshot is available.
        /// </summary>
        public static BattleMutationState ResolveMythicMutationState(long alienId, string inheritedMutation)
        {
            if (string.IsNullOrWhiteSpace(inheritedMutation)) return BattleMutationState.NONE;
            bool eligible = BattleMergeResultResolver.TryGetMythicMutationEligibility(alienId, out bool value) && value;
            return eligible ? BattleMutationState.ACTIVE : BattleMutationState.SEALED;
        }

        public static bool CanApplyInjectorToTarget(byte grade, BattleMutationState mutationState, bool mythicEligible)
        {
            if (grade > 4 || (grade == 4 && !mythicEligible))
                return false;
            return mutationState == BattleMutationState.NONE
                || (grade == 4 && mutationState == BattleMutationState.ACTIVE);
        }

        // Development-only smoke-test budget. Production initial Gold must come from canonical balance.
        private const int DefaultInitialInGameGold = 100000;
        public const float DisconnectRewardGraceSeconds = 120f;
        private BattleWaveExecutor _executor;
        private readonly BattleKillDeduplicator _killDeduplicator = new();

        [Networked] public int CurrentWave { get; private set; }
        [Networked] public int HighestClearedWave { get; private set; }
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
        [Networked] public int Player1EliminatedWave { get; private set; }
        [Networked] public int Player2EliminatedWave { get; private set; }
        [Networked] public int Player1BattleStateValue { get; private set; }
        [Networked] public int Player2BattleStateValue { get; private set; }
        [Networked] public int Player1InGameGold { get; private set; }
        [Networked] public int Player2InGameGold { get; private set; }
        [Networked] public int Player1InitialInGameGold { get; private set; }
        [Networked] public int Player2InitialInGameGold { get; private set; }
        [Networked] public int Player1InGameGoldEarned { get; private set; }
        [Networked] public int Player2InGameGoldEarned { get; private set; }
        [Networked] public int Player1InGameGoldSpent { get; private set; }
        [Networked] public int Player2InGameGoldSpent { get; private set; }
        [Networked] public int TeamInGameGold { get; private set; }
        [Networked] public int Player1KidnapCount { get; private set; }
        [Networked] public int Player2KidnapCount { get; private set; }
        [Networked] public TickTimer BossTimer { get; private set; }
        [Networked] public PlayerRef Player1Ref { get; private set; }
        [Networked] public PlayerRef Player2Ref { get; private set; }
        // User IDs are replicated because Fusion connection tokens are only
        // available to the State Authority. Remote peers still need the same
        // canonical identity map to initialize their local presentation.
        [Networked] public NetworkString<_64> Player1UserId { get; private set; }
        [Networked] public NetworkString<_64> Player2UserId { get; private set; }
        [Networked] public int Player1ConnectionStateValue { get; private set; }
        [Networked] public int Player2ConnectionStateValue { get; private set; }
        [Networked] public TickTimer Player1DisconnectGraceTimer { get; private set; }
        [Networked] public TickTimer Player2DisconnectGraceTimer { get; private set; }
        [Networked, Capacity(24)] private NetworkArray<NetworkBool> Player1BoardOccupied => default;
        [Networked, Capacity(24)] private NetworkArray<NetworkBool> Player2BoardOccupied => default;
        [Networked, Capacity(24)] private NetworkArray<long> Player1BoardAlienIds => default;
        [Networked, Capacity(24)] private NetworkArray<long> Player2BoardAlienIds => default;
        [Networked, Capacity(24)] private NetworkArray<byte> Player1BoardGrades => default;
        [Networked, Capacity(24)] private NetworkArray<byte> Player2BoardGrades => default;
        [Networked, Capacity(24)] private NetworkArray<NetworkString<_16>> Player1BoardMutationTypes => default;
        [Networked, Capacity(24)] private NetworkArray<NetworkString<_16>> Player2BoardMutationTypes => default;
        [Networked, Capacity(24)] private NetworkArray<byte> Player1BoardMutationStates => default;
        [Networked, Capacity(24)] private NetworkArray<byte> Player2BoardMutationStates => default;
        [Networked, Capacity(24)] private NetworkArray<byte> Player1BoardMutationRerollCounts => default;
        [Networked, Capacity(24)] private NetworkArray<byte> Player2BoardMutationRerollCounts => default;
        [Networked] private NetworkBool Player1MythicChoiceActive { get; set; }
        [Networked] private NetworkBool Player2MythicChoiceActive { get; set; }
        [Networked] private int Player1MythicChoiceSlot { get; set; }
        [Networked] private int Player2MythicChoiceSlot { get; set; }
        [Networked] private int Player1MythicFreeRerolls { get; set; }
        [Networked] private int Player1MythicPaidRerolls { get; set; }
        [Networked] private int Player2MythicFreeRerolls { get; set; }
        [Networked] private int Player2MythicPaidRerolls { get; set; }
        [Networked] private TickTimer Player1MythicChoiceTimer { get; set; }
        [Networked] private TickTimer Player2MythicChoiceTimer { get; set; }
        [Networked, Capacity(3)] private NetworkArray<long> Player1MythicChoiceCandidates => default;
        [Networked, Capacity(3)] private NetworkArray<long> Player2MythicChoiceCandidates => default;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Networked] private NetworkBool ForceNextInjector { get; set; }
        [Networked] private PlayerRef ForceNextInjectorPlayer { get; set; }
#endif

        public event Action<int, int, long> KidnapApplied;
        public event Action<int, int, string> InjectorApplied;
        public event Action<int, int, int, string> MutationApplied;
        public event Action<int, int, int, bool, long, byte> BoardChanged;
        public event Action<int, int, int> BoardSwapped;

        public BattleWaveExecutor Executor => _executor;
        public IReadOnlyList<BattleKillAuditRecord> KillAuditRecords => _killDeduplicator.Records;
        public bool IsSpawnedForAccess { get; private set; }
        public bool IsAuthoritative => HasStateAuthority;
        public WaveType CurrentWaveType => (WaveType)CurrentWaveTypeValue;
        public MatchState MatchState => (MatchState)MatchStateValue;
        public PlayerBattleState Player1BattleState => (PlayerBattleState)Player1BattleStateValue;
        public PlayerBattleState Player2BattleState => (PlayerBattleState)Player2BattleStateValue;
        public PlayerConnectionState Player1ConnectionState => (PlayerConnectionState)Player1ConnectionStateValue;
        public PlayerConnectionState Player2ConnectionState => (PlayerConnectionState)Player2ConnectionStateValue;

        public int GetKidnapCount(int playerSlot)
        {
            return playerSlot == 1 ? Player1KidnapCount : playerSlot == 2 ? Player2KidnapCount : 0;
        }

        public int GetInGameGoldForPlayerSlot(int playerSlot)
        {
            return playerSlot == 1 ? Player1InGameGold : playerSlot == 2 ? Player2InGameGold : 0;
        }

        public int GetInitialInGameGoldForPlayerSlot(int playerSlot)
        {
            return playerSlot == 1 ? Player1InitialInGameGold : playerSlot == 2 ? Player2InitialInGameGold : 0;
        }

        public int GetInGameGoldEarnedForPlayerSlot(int playerSlot)
        {
            return playerSlot == 1 ? Player1InGameGoldEarned : playerSlot == 2 ? Player2InGameGoldEarned : 0;
        }

        public int GetInGameGoldSpentForPlayerSlot(int playerSlot)
        {
            return playerSlot == 1 ? Player1InGameGoldSpent : playerSlot == 2 ? Player2InGameGoldSpent : 0;
        }

        public int GetFinalInGameGoldForPlayerSlot(int playerSlot)
        {
            return GetInGameGoldForPlayerSlot(playerSlot);
        }

        /// <summary>
        /// Creates the immutable player seed consumed by BattleSummary at match
        /// end. All four wallet values are read from replicated authority state;
        /// callers never recompute the ledger from UI or kill events.
        /// </summary>
        public bool TryCreatePlayerSummarySeed(
            int playerSlot,
            bool eliminated,
            int? eliminatedWave,
            out BattlePlayerSummarySeed seed)
        {
            seed = null;
            if (!HasStateAuthority || !IsSpawnedForAccess || (playerSlot != 1 && playerSlot != 2))
                return false;

            string playerId = playerSlot == 1 ? Player1UserId.ToString() : Player2UserId.ToString();
            if (string.IsNullOrWhiteSpace(playerId))
                return false;

            seed = new BattlePlayerSummarySeed(
                playerId,
                playerSlot,
                eliminated,
                eliminatedWave,
                GetInitialInGameGoldForPlayerSlot(playerSlot),
                GetInGameGoldEarnedForPlayerSlot(playerSlot),
                GetInGameGoldSpentForPlayerSlot(playerSlot),
                GetFinalInGameGoldForPlayerSlot(playerSlot),
                IsPlayerAbandoned(playerSlot));
            return true;
        }

        public bool IsPlayerAbandoned(int playerSlot)
        {
            if (GetPlayerConnectionState(playerSlot) != PlayerConnectionState.DISCONNECTED
                || Runner == null || !Runner.IsRunning)
                return false;
            TickTimer timer = playerSlot == 1 ? Player1DisconnectGraceTimer
                : playerSlot == 2 ? Player2DisconnectGraceTimer
                : default;
            return timer.Expired(Runner);
        }

        public PlayerConnectionState GetPlayerConnectionState(int playerSlot)
            => playerSlot == 1 ? Player1ConnectionState
                : playerSlot == 2 ? Player2ConnectionState
                : PlayerConnectionState.DISCONNECTED;

        public bool SetPlayerConnectionState(
            int playerSlot,
            string userId,
            PlayerRef playerRef,
            bool connected)
        {
            if (!HasStateAuthority || (playerSlot != 1 && playerSlot != 2) || string.IsNullOrWhiteSpace(userId))
                return false;

            NetworkString<_64> storedUserId = playerSlot == 1 ? Player1UserId : Player2UserId;
            if (!string.IsNullOrWhiteSpace(storedUserId.ToString())
                && !string.Equals(storedUserId.ToString(), userId.Trim(), StringComparison.Ordinal))
                return false;

            int state = (int)(connected ? PlayerConnectionState.CONNECTED : PlayerConnectionState.DISCONNECTED);
            if (playerSlot == 1)
            {
                Player1UserId = userId.Trim();
                Player1Ref = connected ? playerRef : default;
                Player1ConnectionStateValue = state;
                Player1DisconnectGraceTimer = connected || Runner == null
                    ? default
                    : TickTimer.CreateFromSeconds(Runner, DisconnectRewardGraceSeconds);
            }
            else
            {
                Player2UserId = userId.Trim();
                Player2Ref = connected ? playerRef : default;
                Player2ConnectionStateValue = state;
                Player2DisconnectGraceTimer = connected || Runner == null
                    ? default
                    : TickTimer.CreateFromSeconds(Runner, DisconnectRewardGraceSeconds);
            }
            return true;
        }

        public int GetTeamInGameGold() => TeamInGameGold;

        /// <summary>
        /// Awards an authority-calculated Mutation economy hit. The projectile
        /// carries only canonical GoldPerHit and this method owns the replicated ledger.
        /// </summary>
        public bool TryAwardMutationHitGold(int playerSlot, int amount)
        {
            if (!HasStateAuthority || !IsSpawnedForAccess || amount <= 0 || (playerSlot != 1 && playerSlot != 2))
                return false;
            if (playerSlot == 1)
            {
                Player1InGameGold = AddGoldSafely(Player1InGameGold, amount);
                Player1InGameGoldEarned = AddGoldSafely(Player1InGameGoldEarned, amount);
            }
            else
            {
                Player2InGameGold = AddGoldSafely(Player2InGameGold, amount);
                Player2InGameGoldEarned = AddGoldSafely(Player2InGameGoldEarned, amount);
            }
            return true;
        }

        public bool IsBoardOccupied(int playerSlot, int slotIndex)
        {
            if (!IsValidBoardIndex(slotIndex))
                return false;
            return playerSlot == 1 ? Player1BoardOccupied[slotIndex]
                : playerSlot == 2 && Player2BoardOccupied[slotIndex];
        }

        public long GetBoardAlienId(int playerSlot, int slotIndex)
        {
            if (!IsValidBoardIndex(slotIndex)) return 0;
            return playerSlot == 1 ? Player1BoardAlienIds[slotIndex]
                : playerSlot == 2 ? Player2BoardAlienIds[slotIndex] : 0;
        }

        public byte GetBoardGrade(int playerSlot, int slotIndex)
        {
            if (!IsValidBoardIndex(slotIndex)) return 0;
            return playerSlot == 1 ? Player1BoardGrades[slotIndex]
                : playerSlot == 2 ? Player2BoardGrades[slotIndex] : (byte)0;
        }

        public bool IsBoardInjector(int playerSlot, int slotIndex)
        {
            if (!IsValidBoardIndex(slotIndex)) return false;
            return GetBoardMutationState(playerSlot, slotIndex) == MutationStateInjector;
        }

        public string GetBoardMutationType(int playerSlot, int slotIndex)
        {
            if (!IsValidBoardIndex(slotIndex)) return null;
            return playerSlot == 1 ? Player1BoardMutationTypes[slotIndex].ToString()
                : playerSlot == 2 ? Player2BoardMutationTypes[slotIndex].ToString() : null;
        }

        public byte GetBoardMutationState(int playerSlot, int slotIndex)
        {
            if (!IsValidBoardIndex(slotIndex)) return MutationStateNone;
            return playerSlot == 1 ? Player1BoardMutationStates[slotIndex]
                : playerSlot == 2 ? Player2BoardMutationStates[slotIndex] : MutationStateNone;
        }

        public bool IsBoardMutationSealed(int playerSlot, int slotIndex)
            => GetBoardMutationState(playerSlot, slotIndex) == MutationStateSealed;

        public int GetBoardMutationRerollCount(int playerSlot, int slotIndex)
        {
            if (!IsValidBoardIndex(slotIndex)) return 0;
            return playerSlot == 1 ? Player1BoardMutationRerollCounts[slotIndex]
                : playerSlot == 2 ? Player2BoardMutationRerollCounts[slotIndex] : 0;
        }

        public bool TryGetMutationAction(int playerSlot, int slotIndex, out bool initialActivation, out int cost)
        {
            initialActivation = false;
            cost = 0;
            if (_executor == null || !IsValidBoardIndex(slotIndex) || !IsBoardOccupied(playerSlot, slotIndex)
                || IsBoardInjector(playerSlot, slotIndex) || GetBoardGrade(playerSlot, slotIndex) != 4
                || !BattleMergeResultResolver.TryGetMythicMutationEligibility(GetBoardAlienId(playerSlot, slotIndex), out bool eligible)
                || !eligible)
                return false;

            byte state = GetBoardMutationState(playerSlot, slotIndex);
            if (state == MutationStateNone)
                initialActivation = true;
            else if (state != MutationStateActive || string.IsNullOrWhiteSpace(GetBoardMutationType(playerSlot, slotIndex)))
                return false;

            return _executor.TryGetCanonicalMutationCost(
                initialActivation,
                GetBoardMutationRerollCount(playerSlot, slotIndex),
                out cost);
        }

        public int GetNetworkedPlayerSlot(PlayerRef playerRef)
        {
            if (!playerRef.IsRealPlayer)
                return 0;
            if (IsSpawnedForAccess)
            {
                if (playerRef == Player1Ref)
                    return 1;
                if (playerRef == Player2Ref)
                    return 2;
            }

            // The local roster is populated from Fusion player callbacks before
            // the replicated Player1Ref/Player2Ref fields necessarily arrive on
            // every peer. Use that authoritative identity mapping as a short
            // synchronization fallback so the local UI and host input are not
            // rejected during that window.
            BattleRunnerLifecycle lifecycle = FindFirstObjectByType<BattleRunnerLifecycle>();
            if (lifecycle != null && lifecycle.PlayerRoster.TryGet(playerRef, out BattlePlayerIdentity identity))
                return identity.PlayerSlot;
            return 0;
        }

        public bool IsMythicChoiceActive(int playerSlot) => playerSlot == 1 ? Player1MythicChoiceActive : playerSlot == 2 && Player2MythicChoiceActive;

        /// <summary>
        /// Returns whether the Legendary material currently holding the pending
        /// Mythic result is locked while the player chooses a candidate. The
        /// State Authority remains the final gate; this query is also used by
        /// local presentation to avoid dragging a visually locked unit.
        /// </summary>
        public bool IsBoardSlotLockedForMythicChoice(int playerSlot, int slotIndex)
        {
            if (!IsValidBoardIndex(slotIndex) || !IsMythicChoiceActive(playerSlot))
                return false;
            int choiceSlot = playerSlot == 1 ? Player1MythicChoiceSlot : playerSlot == 2 ? Player2MythicChoiceSlot : -1;
            return choiceSlot == slotIndex;
        }

        public long GetMythicChoiceCandidate(int playerSlot, int index)
        {
            if (index < 0 || index >= 3) return 0;
            return playerSlot == 1 ? Player1MythicChoiceCandidates[index] : playerSlot == 2 ? Player2MythicChoiceCandidates[index] : 0;
        }

        public int GetMythicFreeRerolls(int playerSlot) => playerSlot == 1 ? Player1MythicFreeRerolls : playerSlot == 2 ? Player2MythicFreeRerolls : 0;
        public int GetMythicPaidRerolls(int playerSlot) => playerSlot == 1 ? Player1MythicPaidRerolls : playerSlot == 2 ? Player2MythicPaidRerolls : 0;
        public int GetMythicFreeRerollsRemaining(int playerSlot)
        {
            return BattleMergeResultResolver.TryGetMythicRerollPolicy(out int freeLimit, out _, out _, out _)
                ? RemainingRerolls(freeLimit, GetMythicFreeRerolls(playerSlot))
                : 0;
        }

        public int GetMythicPaidRerollsRemaining(int playerSlot)
        {
            return BattleMergeResultResolver.TryGetMythicRerollPolicy(out _, out int paidLimit, out _, out _)
                ? RemainingRerolls(paidLimit, GetMythicPaidRerolls(playerSlot))
                : 0;
        }

        public static int RemainingRerolls(int limit, int used)
            => Math.Max(0, limit - used);
        public int GetMythicChoiceSlot(int playerSlot)
            => playerSlot == 1 ? Player1MythicChoiceSlot : playerSlot == 2 ? Player2MythicChoiceSlot : -1;

        public int GetMythicChoiceRemainingSeconds(int playerSlot)
        {
            if (Runner == null || !Runner.IsRunning || !IsMythicChoiceActive(playerSlot))
                return 0;
            TickTimer timer = playerSlot == 1 ? Player1MythicChoiceTimer : Player2MythicChoiceTimer;
            return Mathf.Max(0, Mathf.CeilToInt(timer.RemainingTime(Runner) ?? 0f));
        }

        public int GetBossRemainingSeconds()
        {
            if (Runner == null || !Runner.IsRunning || !BossTimer.IsRunning)
                return 0;
            return Mathf.Max(0, Mathf.CeilToInt(BossTimer.RemainingTime(Runner) ?? 0f));
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
            _executor.OnBossTimeout += HandleBossTimeout;
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
                HighestClearedWave = 0;
                IsWaveRunning = false;
                ResetFieldLimitEvents();
                SyncPlayerBattleStates();
                SyncAliveMonsterCounts();
                InitializeInGameGold(DefaultInitialInGameGold, DefaultInitialInGameGold);
                TeamInGameGold = 0;
                _killDeduplicator.Clear();
                Player1KidnapCount = 0;
                Player2KidnapCount = 0;
                Player1ConnectionStateValue = (int)PlayerConnectionState.DISCONNECTED;
                Player2ConnectionStateValue = (int)PlayerConnectionState.DISCONNECTED;
                Player1DisconnectGraceTimer = default;
                Player2DisconnectGraceTimer = default;
                BossTimer = default;
                ResetBoardOccupancy();
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            UpdateBossTimer();
            UpdateMythicChoiceTimers();

            BattleRunnerLifecycle lifecycle = FindFirstObjectByType<BattleRunnerLifecycle>();
            if (lifecycle == null || lifecycle.Runner == null)
                return;

            if (lifecycle.PlayerRoster.TryGetByUserIdForSlot(1, out BattlePlayerIdentity player1))
            {
                Player1Ref = player1.PlayerRef;
                Player1UserId = player1.UserId;
                Player1ConnectionStateValue = (int)PlayerConnectionState.CONNECTED;
                Player1DisconnectGraceTimer = default;
            }
            else
            {
                Player1Ref = default;
                if (!string.IsNullOrWhiteSpace(Player1UserId.ToString()))
                {
                    if (Player1ConnectionState != PlayerConnectionState.DISCONNECTED)
                        Player1DisconnectGraceTimer = TickTimer.CreateFromSeconds(Runner, DisconnectRewardGraceSeconds);
                    Player1ConnectionStateValue = (int)PlayerConnectionState.DISCONNECTED;
                }
            }
            if (lifecycle.PlayerRoster.TryGetByUserIdForSlot(2, out BattlePlayerIdentity player2))
            {
                Player2Ref = player2.PlayerRef;
                Player2UserId = player2.UserId;
                Player2ConnectionStateValue = (int)PlayerConnectionState.CONNECTED;
                Player2DisconnectGraceTimer = default;
            }
            else
            {
                Player2Ref = default;
                if (!string.IsNullOrWhiteSpace(Player2UserId.ToString()))
                {
                    if (Player2ConnectionState != PlayerConnectionState.DISCONNECTED)
                        Player2DisconnectGraceTimer = TickTimer.CreateFromSeconds(Runner, DisconnectRewardGraceSeconds);
                    Player2ConnectionStateValue = (int)PlayerConnectionState.DISCONNECTED;
                }
            }
        }

        private void UpdateBossTimer()
        {
            if (_executor == null || Runner == null || !Runner.IsRunning)
                return;

            if (!_executor.IsBossActive)
            {
                BossTimer = default;
                return;
            }

            if (!BossTimer.IsRunning)
            {
                float duration = _executor.ActiveBossTimeLimitSeconds;
                if (duration > 0f)
                    BossTimer = TickTimer.CreateFromSeconds(Runner, duration);
                return;
            }

            if (!BossTimer.Expired(Runner))
                return;

            BossTimer = default;
            _executor.TryResolveBossTimeoutFromAuthority();
        }

        public void RequestKidnap()
        {
            if (Object == null || !Object.IsValid) return;
            int playerSlot = GetNetworkedPlayerSlot(Runner.LocalPlayer);
            if (playerSlot <= 0 || IsMythicChoiceActive(playerSlot)) return;
            if (HasStateAuthority) ProcessKidnap(Runner.LocalPlayer);
            else RPC_RequestKidnap();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void RequestTestInjectorKidnap()
        {
            if (Object == null || !Object.IsValid
                || !TryResolvePlayerSlot(Runner.LocalPlayer, out int playerSlot)
                || IsMythicChoiceActive(playerSlot)) return;
            if (HasStateAuthority)
            {
                ForceNextInjector = true;
                ForceNextInjectorPlayer = Runner.LocalPlayer;
                ProcessKidnap(Runner.LocalPlayer);
                return;
            }
            RPC_RequestTestInjector(Runner.LocalPlayer);
            RPC_RequestKidnap();
        }
#endif

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
                    if (!IsMythicChoiceActive(playerSlot)) ApplyMove(playerSlot, fromSlotIndex, toSlotIndex);
                return;
            }

            RPC_RequestMove(fromSlotIndex, toSlotIndex);
        }

        public void RequestMerge(int sourceSlotIndex, int targetSlotIndex)
            => RequestMergeOrSwap(sourceSlotIndex, targetSlotIndex);

        public void RequestMergeOrSwap(int sourceSlotIndex, int targetSlotIndex)
        {
            if (!IsSpawnedForAccess || Object == null || !Object.IsValid)
                return;
            if (HasStateAuthority)
            {
                if (TryResolvePlayerSlot(Runner.LocalPlayer, out int playerSlot))
                    if (!IsMythicChoiceActive(playerSlot)) ApplyMergeOrSwap(playerSlot, sourceSlotIndex, targetSlotIndex);
                return;
            }
            RPC_RequestMergeOrSwap(sourceSlotIndex, targetSlotIndex);
        }

        public void RequestUseInjector(int sourceSlotIndex, int targetSlotIndex)
        {
            if (!IsSpawnedForAccess || Object == null || !Object.IsValid) return;
            if (HasStateAuthority)
            {
                if (TryResolvePlayerSlot(Runner.LocalPlayer, out int playerSlot)) ApplyUseInjector(playerSlot, sourceSlotIndex, targetSlotIndex);
                return;
            }
            RPC_RequestUseInjector(sourceSlotIndex, targetSlotIndex);
        }

        public void RequestMythicChoice(int candidateIndex)
        {
            if (!IsSpawnedForAccess || Object == null || !Object.IsValid || candidateIndex < 0 || candidateIndex >= 3)
                return;
            if (HasStateAuthority)
            {
                if (TryResolvePlayerSlot(Runner.LocalPlayer, out int playerSlot))
                    ApplyMythicChoice(playerSlot, candidateIndex);
                return;
            }
            RPC_RequestMythicChoice(candidateIndex);
        }

        public void RequestMythicReroll()
        {
            if (!IsSpawnedForAccess || Object == null || !Object.IsValid) return;
            if (HasStateAuthority)
            {
                if (TryResolvePlayerSlot(Runner.LocalPlayer, out int playerSlot)) ApplyMythicReroll(playerSlot);
                return;
            }
            RPC_RequestMythicReroll();
        }

        public void RequestMutation(int slotIndex)
        {
            if (!IsSpawnedForAccess || Object == null || !Object.IsValid || !IsValidBoardIndex(slotIndex))
                return;
            if (HasStateAuthority)
            {
                if (TryResolvePlayerSlot(Runner.LocalPlayer, out int playerSlot))
                    ApplyMutationRequest(playerSlot, slotIndex);
                return;
            }
            RPC_RequestMutation(slotIndex);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestMergeOrSwap(int sourceSlotIndex, int targetSlotIndex, RpcInfo info = default)
        {
            if (TryResolvePlayerSlot(info.Source, out int playerSlot))
                ApplyMergeOrSwap(playerSlot, sourceSlotIndex, targetSlotIndex);
        }

        private void ApplyMergeOrSwap(int playerSlot, int sourceSlotIndex, int targetSlotIndex)
        {
            if (!IsValidBoardIndex(sourceSlotIndex) || !IsValidBoardIndex(targetSlotIndex)
                || sourceSlotIndex == targetSlotIndex || IsMythicChoiceActive(playerSlot)) return;

            NetworkArray<NetworkBool> board = playerSlot == 1 ? Player1BoardOccupied : Player2BoardOccupied;
            NetworkArray<long> alienIds = playerSlot == 1 ? Player1BoardAlienIds : Player2BoardAlienIds;
            NetworkArray<byte> grades = playerSlot == 1 ? Player1BoardGrades : Player2BoardGrades;
            if (!board[sourceSlotIndex] || !board[targetSlotIndex])
            {
                ApplyMove(playerSlot, sourceSlotIndex, targetSlotIndex);
                return;
            }

            if (CanMerge(sourceSlotIndex, targetSlotIndex, board[sourceSlotIndex], board[targetSlotIndex],
                    alienIds[sourceSlotIndex], alienIds[targetSlotIndex], grades[sourceSlotIndex], grades[targetSlotIndex]))
                ApplyMerge(playerSlot, sourceSlotIndex, targetSlotIndex);
            else
                ApplySwap(playerSlot, sourceSlotIndex, targetSlotIndex);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestUseInjector(int sourceSlotIndex, int targetSlotIndex, RpcInfo info = default)
        {
            if (TryResolvePlayerSlot(info.Source, out int playerSlot)) ApplyUseInjector(playerSlot, sourceSlotIndex, targetSlotIndex);
        }

        private void ApplyUseInjector(int playerSlot, int sourceSlotIndex, int targetSlotIndex)
        {
            if (!IsValidBoardIndex(sourceSlotIndex) || !IsValidBoardIndex(targetSlotIndex) || sourceSlotIndex == targetSlotIndex) return;
            LaneType lane = playerSlot == 1 ? LaneType.Player1Lane : playerSlot == 2 ? LaneType.Player2Lane : (LaneType)(-1);
            if (!IsPlayerActionAllowed(lane) || IsMythicChoiceActive(playerSlot)) return;
            NetworkArray<NetworkBool> board = playerSlot == 1 ? Player1BoardOccupied : Player2BoardOccupied;
            NetworkArray<long> alienIds = playerSlot == 1 ? Player1BoardAlienIds : Player2BoardAlienIds;
            NetworkArray<byte> grades = playerSlot == 1 ? Player1BoardGrades : Player2BoardGrades;
            NetworkArray<NetworkString<_16>> mutations = playerSlot == 1 ? Player1BoardMutationTypes : Player2BoardMutationTypes;
            NetworkArray<byte> mutationStates = playerSlot == 1 ? Player1BoardMutationStates : Player2BoardMutationStates;
            NetworkArray<byte> mutationRerolls = playerSlot == 1 ? Player1BoardMutationRerollCounts : Player2BoardMutationRerollCounts;
            string mutationType = mutations[sourceSlotIndex].ToString();
            byte targetMutationState = mutationStates[targetSlotIndex];
            bool mythicEligible = grades[targetSlotIndex] != 4
                || (BattleMergeResultResolver.TryGetMythicMutationEligibility(alienIds[targetSlotIndex], out bool eligible) && eligible);
            if (!board[sourceSlotIndex] || mutationStates[sourceSlotIndex] != MutationStateInjector
                || string.IsNullOrWhiteSpace(mutationType) || !board[targetSlotIndex]
                || alienIds[targetSlotIndex] <= 0 || grades[targetSlotIndex] > 4
                || !CanApplyInjectorToTarget(grades[targetSlotIndex], (BattleMutationState)targetMutationState, mythicEligible)) return;
            board.Set(sourceSlotIndex, false);
            alienIds.Set(sourceSlotIndex, 0);
            grades.Set(sourceSlotIndex, 0);
            mutations.Set(sourceSlotIndex, default);
            mutationStates.Set(sourceSlotIndex, MutationStateNone);
            mutationRerolls.Set(sourceSlotIndex, 0);
            mutations.Set(targetSlotIndex, mutationType);
            mutationStates.Set(targetSlotIndex, grades[targetSlotIndex] == 4 ? MutationStateActive : MutationStatePending);
            mutationRerolls.Set(targetSlotIndex, 0);
            RPC_MutationApplied(playerSlot, sourceSlotIndex, targetSlotIndex, mutationType);
            Debug.Log($"[Fusion] Mutation Injector applied: player={playerSlot}, source={sourceSlotIndex}, target={targetSlotIndex}, mutation={mutationType}.");
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_MutationApplied(int playerSlot, int sourceSlotIndex, int targetSlotIndex, NetworkString<_16> mutationType)
        {
            MutationApplied?.Invoke(playerSlot, sourceSlotIndex, targetSlotIndex, mutationType.ToString());
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestMythicChoice(int candidateIndex, RpcInfo info = default)
        {
            if (TryResolvePlayerSlot(info.Source, out int playerSlot))
                ApplyMythicChoice(playerSlot, candidateIndex);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestMythicReroll(RpcInfo info = default)
        {
            if (TryResolvePlayerSlot(info.Source, out int playerSlot)) ApplyMythicReroll(playerSlot);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestMutation(int slotIndex, RpcInfo info = default)
        {
            if (TryResolvePlayerSlot(info.Source, out int playerSlot))
                ApplyMutationRequest(playerSlot, slotIndex);
        }

        private void ApplyMutationRequest(int playerSlot, int slotIndex)
        {
            if (!HasStateAuthority || _executor == null || IsMythicChoiceActive(playerSlot)
                || !TryGetMutationAction(playerSlot, slotIndex, out bool initialActivation, out int cost))
                return;

            LaneType lane = playerSlot == 1 ? LaneType.Player1Lane : playerSlot == 2 ? LaneType.Player2Lane : (LaneType)(-1);
            if (!IsPlayerActionAllowed(lane))
                return;

            string currentMutation = initialActivation ? null : GetBoardMutationType(playerSlot, slotIndex);
            int rerollCount = GetBoardMutationRerollCount(playerSlot, slotIndex);
            ulong tick = Runner == null ? 0UL : (ulong)Runner.Tick.Raw;
            ulong seed = tick ^ (ulong)(playerSlot * 1009 + slotIndex * 131 + rerollCount * 8191 + (initialActivation ? 17 : 37));
            if (!_executor.TryResolveCanonicalMutation(seed, currentMutation, out string nextMutation)
                || string.IsNullOrWhiteSpace(nextMutation)
                || (!initialActivation && string.Equals(nextMutation, currentMutation, StringComparison.OrdinalIgnoreCase))
                || !TrySpendGold(lane, cost, out int remainingGold))
                return;

            NetworkArray<NetworkString<_16>> mutations = playerSlot == 1 ? Player1BoardMutationTypes : Player2BoardMutationTypes;
            NetworkArray<byte> states = playerSlot == 1 ? Player1BoardMutationStates : Player2BoardMutationStates;
            NetworkArray<byte> rerolls = playerSlot == 1 ? Player1BoardMutationRerollCounts : Player2BoardMutationRerollCounts;
            mutations.Set(slotIndex, nextMutation);
            states.Set(slotIndex, MutationStateActive);
            rerolls.Set(slotIndex, initialActivation ? (byte)0 : (byte)Math.Min(byte.MaxValue, rerollCount + 1));
            RPC_MutationApplied(playerSlot, -1, slotIndex, nextMutation);
            Debug.Log($"[Fusion] Mythic Mutation {(initialActivation ? "activated" : "rerolled")}: player={playerSlot}, slot={slotIndex}, mutation={nextMutation}, cost={cost}, remaining={remainingGold}.");
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
                || !IsPlayerActionAllowed(playerSlot == 1 ? LaneType.Player1Lane : playerSlot == 2 ? LaneType.Player2Lane : (LaneType)(-1))
                || IsMythicChoiceActive(playerSlot))
                return;

            NetworkArray<NetworkBool> board = playerSlot == 1 ? Player1BoardOccupied : Player2BoardOccupied;
            NetworkArray<long> alienIds = playerSlot == 1 ? Player1BoardAlienIds : Player2BoardAlienIds;
            NetworkArray<byte> grades = playerSlot == 1 ? Player1BoardGrades : Player2BoardGrades;
            NetworkArray<NetworkString<_16>> mutations = playerSlot == 1 ? Player1BoardMutationTypes : Player2BoardMutationTypes;
            NetworkArray<byte> mutationStates = playerSlot == 1 ? Player1BoardMutationStates : Player2BoardMutationStates;
            NetworkArray<byte> mutationRerolls = playerSlot == 1 ? Player1BoardMutationRerollCounts : Player2BoardMutationRerollCounts;
            if (!board[fromSlotIndex] || board[toSlotIndex])
                return;

            long alienId = alienIds[fromSlotIndex];
            board.Set(fromSlotIndex, false);
            board.Set(toSlotIndex, true);
            alienIds.Set(fromSlotIndex, 0);
            alienIds.Set(toSlotIndex, alienId);
            byte grade = grades[fromSlotIndex];
            grades.Set(fromSlotIndex, 0);
            grades.Set(toSlotIndex, grade);
            mutations.Set(toSlotIndex, mutations[fromSlotIndex]);
            mutations.Set(fromSlotIndex, default);
            mutationStates.Set(toSlotIndex, mutationStates[fromSlotIndex]);
            mutationStates.Set(fromSlotIndex, MutationStateNone);
            mutationRerolls.Set(toSlotIndex, mutationRerolls[fromSlotIndex]);
            mutationRerolls.Set(fromSlotIndex, 0);
            RPC_BoardChanged(playerSlot, fromSlotIndex, toSlotIndex, false, alienId, grade);
        }

        private void ApplySwap(int playerSlot, int sourceSlotIndex, int targetSlotIndex)
        {
            if (!IsValidBoardIndex(sourceSlotIndex) || !IsValidBoardIndex(targetSlotIndex)
                || sourceSlotIndex == targetSlotIndex
                || !IsPlayerActionAllowed(playerSlot == 1 ? LaneType.Player1Lane : playerSlot == 2 ? LaneType.Player2Lane : (LaneType)(-1))
                || IsMythicChoiceActive(playerSlot)) return;

            NetworkArray<NetworkBool> board = playerSlot == 1 ? Player1BoardOccupied : Player2BoardOccupied;
            NetworkArray<long> alienIds = playerSlot == 1 ? Player1BoardAlienIds : Player2BoardAlienIds;
            NetworkArray<byte> grades = playerSlot == 1 ? Player1BoardGrades : Player2BoardGrades;
            NetworkArray<NetworkString<_16>> mutations = playerSlot == 1 ? Player1BoardMutationTypes : Player2BoardMutationTypes;
            NetworkArray<byte> mutationStates = playerSlot == 1 ? Player1BoardMutationStates : Player2BoardMutationStates;
            NetworkArray<byte> mutationRerolls = playerSlot == 1 ? Player1BoardMutationRerollCounts : Player2BoardMutationRerollCounts;
            if (!board[sourceSlotIndex] || !board[targetSlotIndex]) return;

            long sourceAlienId = alienIds[sourceSlotIndex];
            long targetAlienId = alienIds[targetSlotIndex];
            byte sourceGrade = grades[sourceSlotIndex];
            byte targetGrade = grades[targetSlotIndex];
            NetworkString<_16> sourceMutation = mutations[sourceSlotIndex];
            NetworkString<_16> targetMutation = mutations[targetSlotIndex];
            alienIds.Set(sourceSlotIndex, targetAlienId);
            alienIds.Set(targetSlotIndex, sourceAlienId);
            grades.Set(sourceSlotIndex, targetGrade);
            grades.Set(targetSlotIndex, sourceGrade);
            mutations.Set(sourceSlotIndex, targetMutation);
            mutations.Set(targetSlotIndex, sourceMutation);
            byte sourceMutationState = mutationStates[sourceSlotIndex];
            mutationStates.Set(sourceSlotIndex, mutationStates[targetSlotIndex]);
            mutationStates.Set(targetSlotIndex, sourceMutationState);
            byte sourceMutationRerolls = mutationRerolls[sourceSlotIndex];
            mutationRerolls.Set(sourceSlotIndex, mutationRerolls[targetSlotIndex]);
            mutationRerolls.Set(targetSlotIndex, sourceMutationRerolls);
            RPC_BoardSwapped(playerSlot, sourceSlotIndex, targetSlotIndex);
        }

        private void ApplyMerge(int playerSlot, int sourceSlotIndex, int targetSlotIndex)
        {
            if (!IsValidBoardIndex(sourceSlotIndex) || !IsValidBoardIndex(targetSlotIndex)
                || sourceSlotIndex == targetSlotIndex
                || !IsPlayerActionAllowed(playerSlot == 1 ? LaneType.Player1Lane : playerSlot == 2 ? LaneType.Player2Lane : (LaneType)(-1))
                || IsMythicChoiceActive(playerSlot))
                return;

            NetworkArray<NetworkBool> board = playerSlot == 1 ? Player1BoardOccupied : Player2BoardOccupied;
            NetworkArray<long> alienIds = playerSlot == 1 ? Player1BoardAlienIds : Player2BoardAlienIds;
            NetworkArray<byte> grades = playerSlot == 1 ? Player1BoardGrades : Player2BoardGrades;
            NetworkArray<NetworkString<_16>> mutations = playerSlot == 1 ? Player1BoardMutationTypes : Player2BoardMutationTypes;
            NetworkArray<byte> mutationStates = playerSlot == 1 ? Player1BoardMutationStates : Player2BoardMutationStates;
            NetworkArray<byte> mutationRerolls = playerSlot == 1 ? Player1BoardMutationRerollCounts : Player2BoardMutationRerollCounts;
            if (!CanMerge(
                    sourceSlotIndex,
                    targetSlotIndex,
                    board[sourceSlotIndex],
                    board[targetSlotIndex],
                    alienIds[sourceSlotIndex],
                    alienIds[targetSlotIndex],
                    grades[sourceSlotIndex],
                    grades[targetSlotIndex]))
                return;

            string sourceMutation = mutations[sourceSlotIndex].ToString();
            string targetMutation = mutations[targetSlotIndex].ToString();

            ulong seed = (ulong)Runner.Tick.Raw ^ (ulong)(playerSlot * 397 + sourceSlotIndex * 17 + targetSlotIndex);
            if (grades[sourceSlotIndex] == 3)
            {
                if (!BattleMergeResultResolver.TryResolveMythicCandidates(seed, out long[] candidates)) return;
                SetMythicChoice(playerSlot, targetSlotIndex, candidates);
                mutations.Set(targetSlotIndex, ResolveInheritedMutation(sourceMutation, targetMutation, seed));
                mutationStates.Set(targetSlotIndex, string.IsNullOrWhiteSpace(mutations[targetSlotIndex].ToString())
                    ? MutationStateNone : MutationStatePending);
                mutationRerolls.Set(targetSlotIndex, 0);
                board.Set(sourceSlotIndex, false);
                alienIds.Set(sourceSlotIndex, 0);
                grades.Set(sourceSlotIndex, 0);
                mutations.Set(sourceSlotIndex, default);
                mutationStates.Set(sourceSlotIndex, MutationStateNone);
                mutationRerolls.Set(sourceSlotIndex, 0);
                RPC_BoardChanged(playerSlot, sourceSlotIndex, targetSlotIndex, true, 0, 3);
                Debug.Log($"[Fusion] Mythic choice opened: slot={playerSlot}, target={targetSlotIndex}, candidates={string.Join(",", candidates)}.");
                return;
            }

            if (!BattleMergeResultResolver.TryResolveRandomNextGrade(grades[sourceSlotIndex], seed, out long resultAlienId, out byte resultGrade)) return;

            board.Set(sourceSlotIndex, false);
            alienIds.Set(sourceSlotIndex, 0);
            grades.Set(sourceSlotIndex, 0);
            mutations.Set(sourceSlotIndex, default);
            mutationStates.Set(sourceSlotIndex, MutationStateNone);
            mutationRerolls.Set(sourceSlotIndex, 0);
            alienIds.Set(targetSlotIndex, resultAlienId);
            grades.Set(targetSlotIndex, resultGrade);
            string inheritedMutation = ResolveInheritedMutation(sourceMutation, targetMutation, seed);
            mutations.Set(targetSlotIndex, inheritedMutation);
            mutationStates.Set(targetSlotIndex, string.IsNullOrWhiteSpace(inheritedMutation)
                ? MutationStateNone : MutationStatePending);
            mutationRerolls.Set(targetSlotIndex, 0);
            RPC_BoardChanged(playerSlot, sourceSlotIndex, targetSlotIndex, true, resultAlienId, resultGrade);
            Debug.Log($"[Fusion] Merge request authorized: slot={playerSlot}, source={sourceSlotIndex}, target={targetSlotIndex}, alienId={alienIds[targetSlotIndex]}, grade={grades[targetSlotIndex]}.");
        }

        private void SetMythicChoice(int playerSlot, int targetSlotIndex, long[] candidates)
        {
            NetworkArray<long> target = playerSlot == 1 ? Player1MythicChoiceCandidates : Player2MythicChoiceCandidates;
            for (int index = 0; index < 3; index++) target.Set(index, candidates[index]);
            if (playerSlot == 1)
            {
                Player1MythicChoiceSlot = targetSlotIndex;
                Player1MythicChoiceActive = true;
                Player1MythicFreeRerolls = 0;
                Player1MythicPaidRerolls = 0;
                Player1MythicChoiceTimer = CreateMythicChoiceTimer();
            }
            else
            {
                Player2MythicChoiceSlot = targetSlotIndex;
                Player2MythicChoiceActive = true;
                Player2MythicFreeRerolls = 0;
                Player2MythicPaidRerolls = 0;
                Player2MythicChoiceTimer = CreateMythicChoiceTimer();
            }
        }

        private TickTimer CreateMythicChoiceTimer()
        {
            if (Runner == null || !Runner.IsRunning || !BattleMergeResultResolver.TryGetMythicRerollPolicy(
                    out _, out _, out _, out int timeoutSeconds) || timeoutSeconds <= 0)
                return default;
            return TickTimer.CreateFromSeconds(Runner, timeoutSeconds);
        }

        private void UpdateMythicChoiceTimers()
        {
            if (Runner == null || !Runner.IsRunning) return;
            if (Player1MythicChoiceActive)
            {
                if (Player1MythicChoiceTimer.IsRunning && Player1MythicChoiceTimer.Expired(Runner))
                    ApplyMythicChoice(1, 0);
            }
            else
            {
                Player1MythicChoiceTimer = default;
            }

            if (Player2MythicChoiceActive)
            {
                if (Player2MythicChoiceTimer.IsRunning && Player2MythicChoiceTimer.Expired(Runner))
                    ApplyMythicChoice(2, 0);
            }
            else
            {
                Player2MythicChoiceTimer = default;
            }
        }

        private void ApplyMythicChoice(int playerSlot, int candidateIndex)
        {
            if (!HasStateAuthority)
                return;
            if (candidateIndex < 0 || candidateIndex >= 3) return;
            NetworkArray<long> candidates = playerSlot == 1 ? Player1MythicChoiceCandidates : Player2MythicChoiceCandidates;
            bool active = playerSlot == 1 ? Player1MythicChoiceActive : Player2MythicChoiceActive;
            int targetSlot = playerSlot == 1 ? Player1MythicChoiceSlot : Player2MythicChoiceSlot;
            if (!active || !IsValidBoardIndex(targetSlot) || candidates[candidateIndex] <= 0) return;
            NetworkArray<NetworkBool> board = playerSlot == 1 ? Player1BoardOccupied : Player2BoardOccupied;
            NetworkArray<long> alienIds = playerSlot == 1 ? Player1BoardAlienIds : Player2BoardAlienIds;
            NetworkArray<byte> grades = playerSlot == 1 ? Player1BoardGrades : Player2BoardGrades;
            NetworkArray<NetworkString<_16>> mutations = playerSlot == 1 ? Player1BoardMutationTypes : Player2BoardMutationTypes;
            NetworkArray<byte> mutationStates = playerSlot == 1 ? Player1BoardMutationStates : Player2BoardMutationStates;
            NetworkArray<byte> mutationRerolls = playerSlot == 1 ? Player1BoardMutationRerollCounts : Player2BoardMutationRerollCounts;
            if (!board[targetSlot] || grades[targetSlot] != 3) return;
            alienIds.Set(targetSlot, candidates[candidateIndex]);
            grades.Set(targetSlot, 4);
            string inheritedMutation = mutations[targetSlot].ToString();
            mutationStates.Set(targetSlot, (byte)ResolveMythicMutationState(candidates[candidateIndex], inheritedMutation));
            mutationRerolls.Set(targetSlot, 0);
            if (playerSlot == 1)
            {
                Player1MythicChoiceActive = false;
                Player1MythicChoiceTimer = default;
            }
            else
            {
                Player2MythicChoiceActive = false;
                Player2MythicChoiceTimer = default;
            }
            RPC_BoardChanged(playerSlot, targetSlot, targetSlot, true, candidates[candidateIndex], 4);
            Debug.Log($"[Fusion] Mythic choice selected: slot={playerSlot}, target={targetSlot}, alienId={candidates[candidateIndex]}.");
        }

        private void ApplyMythicReroll(int playerSlot)
        {
            if (!HasStateAuthority)
                return;
            if (!BattleMergeResultResolver.TryGetMythicRerollPolicy(out int freeLimit, out int paidLimit, out int paidCost, out _)) return;
            if (!IsMythicChoiceActive(playerSlot)) return;
            int freeUsed = GetMythicFreeRerolls(playerSlot);
            int paidUsed = GetMythicPaidRerolls(playerSlot);
            if (freeUsed >= freeLimit && paidUsed >= paidLimit) return;
            long[] previous = new long[3];
            for (int index = 0; index < 3; index++) previous[index] = GetMythicChoiceCandidate(playerSlot, index);
            ulong seed = (ulong)Runner.Tick.Raw ^ (ulong)(playerSlot * 997 + freeUsed * 31 + paidUsed * 131);
            if (!BattleMergeResultResolver.TryResolveMythicCandidates(seed, previous, out long[] next)) return;
            if (freeUsed >= freeLimit && !TrySpendGold(playerSlot == 1 ? LaneType.Player1Lane : LaneType.Player2Lane, paidCost, out _)) return;
            NetworkArray<long> target = playerSlot == 1 ? Player1MythicChoiceCandidates : Player2MythicChoiceCandidates;
            for (int index = 0; index < 3; index++) target.Set(index, next[index]);
            if (playerSlot == 1)
            {
                if (freeUsed < freeLimit) Player1MythicFreeRerolls++;
                else Player1MythicPaidRerolls++;
            }
            else
            {
                if (freeUsed < freeLimit) Player2MythicFreeRerolls++;
                else Player2MythicPaidRerolls++;
            }
            // A successful reroll grants a fresh canonical selection window.
            // The duration is read from mythic-choice-balance.json, so the
            // current balance (10 seconds) is applied consistently on both
            // State Authority and clients.
            if (playerSlot == 1)
                Player1MythicChoiceTimer = CreateMythicChoiceTimer();
            else
                Player2MythicChoiceTimer = CreateMythicChoiceTimer();
            Debug.Log($"[Fusion] Mythic choice rerolled: player={playerSlot}, free={GetMythicFreeRerolls(playerSlot)}, paid={GetMythicPaidRerolls(playerSlot)}.");
        }

        public static bool CanMerge(
            int sourceSlotIndex,
            int targetSlotIndex,
            bool sourceOccupied,
            bool targetOccupied,
            long sourceAlienId,
            long targetAlienId,
            byte sourceGrade,
            byte targetGrade)
        {
            return IsValidBoardIndex(sourceSlotIndex)
                && IsValidBoardIndex(targetSlotIndex)
                && sourceSlotIndex != targetSlotIndex
                && sourceOccupied
                && targetOccupied
                && sourceAlienId > 0
                && sourceAlienId == targetAlienId
                && sourceGrade == targetGrade
                && sourceGrade <= 3;
        }

        /// <summary>
        /// Carries pending Mutation DNA through a merge. When both materials
        /// carry different DNA, the State Authority chooses one deterministically
        /// from the merge seed (50/50) so peers never diverge.
        /// </summary>
        public static string ResolveInheritedMutation(string sourceMutation, string targetMutation, ulong seed)
        {
            bool hasSource = !string.IsNullOrWhiteSpace(sourceMutation);
            bool hasTarget = !string.IsNullOrWhiteSpace(targetMutation);
            if (!hasSource) return hasTarget ? targetMutation : string.Empty;
            if (!hasTarget || string.Equals(sourceMutation, targetMutation, System.StringComparison.Ordinal)) return sourceMutation;
            return (seed & 1UL) == 0UL ? sourceMutation : targetMutation;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_BoardChanged(int playerSlot, int fromSlotIndex, int toSlotIndex, bool merged, long resultAlienId, byte resultGrade)
        {
            BoardChanged?.Invoke(playerSlot, fromSlotIndex, toSlotIndex, merged, resultAlienId, resultGrade);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_BoardSwapped(int playerSlot, int sourceSlotIndex, int targetSlotIndex)
        {
            BoardSwapped?.Invoke(playerSlot, sourceSlotIndex, targetSlotIndex);
        }

        private bool TryResolvePlayerSlot(PlayerRef playerRef, out int playerSlot)
        {
            // The RPC source must be resolved from replicated Fusion identity
            // first. The local roster can still be initializing on the host or
            // on a late-joining peer when the move request arrives.
            if (IsSpawnedForAccess && playerRef == Player1Ref)
            {
                playerSlot = 1;
                return true;
            }

            if (IsSpawnedForAccess && playerRef == Player2Ref)
            {
                playerSlot = 2;
                return true;
            }

            BattleRunnerLifecycle lifecycle = FindFirstObjectByType<BattleRunnerLifecycle>();
            if (lifecycle != null && lifecycle.PlayerRoster.TryGet(playerRef, out BattlePlayerIdentity identity))
            {
                playerSlot = identity.PlayerSlot;
                return playerSlot == 1 || playerSlot == 2;
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
            Player1MythicChoiceActive = false;
            Player2MythicChoiceActive = false;
            Player1MythicChoiceSlot = -1;
            Player2MythicChoiceSlot = -1;
            Player1MythicFreeRerolls = 0;
            Player1MythicPaidRerolls = 0;
            Player2MythicFreeRerolls = 0;
            Player2MythicPaidRerolls = 0;
            Player1MythicChoiceTimer = default;
            Player2MythicChoiceTimer = default;
            for (int index = 0; index < 24; index++)
            {
                Player1BoardOccupied.Set(index, false);
                Player2BoardOccupied.Set(index, false);
                Player1BoardAlienIds.Set(index, 0);
                Player2BoardAlienIds.Set(index, 0);
                Player1BoardGrades.Set(index, 0);
                Player2BoardGrades.Set(index, 0);
                Player1BoardMutationTypes.Set(index, default);
                Player2BoardMutationTypes.Set(index, default);
                Player1BoardMutationStates.Set(index, MutationStateNone);
                Player2BoardMutationStates.Set(index, MutationStateNone);
                Player1BoardMutationRerollCounts.Set(index, 0);
                Player2BoardMutationRerollCounts.Set(index, 0);
            }
            for (int index = 0; index < 3; index++)
            {
                Player1MythicChoiceCandidates.Set(index, 0);
                Player2MythicChoiceCandidates.Set(index, 0);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestTestInjector(PlayerRef requestedPlayer, RpcInfo info = default)
        {
            if (!HasStateAuthority || requestedPlayer != info.Source) return;
            ForceNextInjector = true;
            ForceNextInjectorPlayer = requestedPlayer;
        }
#endif

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestKidnap(RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            ProcessKidnap(info.Source.IsRealPlayer ? info.Source : Runner.LocalPlayer);
        }

        private void ProcessKidnap(PlayerRef source)
        {
            if (!HasStateAuthority || _executor == null) return;

            BattleRunnerLifecycle lifecycle = FindFirstObjectByType<BattleRunnerLifecycle>();
            if (lifecycle == null)
                return;
            if (!lifecycle.PlayerRoster.TryGet(source, out BattlePlayerIdentity identity))
                return;

            if (identity.PlayerSlot != 1 && identity.PlayerSlot != 2)
                return;
            if (IsMythicChoiceActive(identity.PlayerSlot))
                return;
            LaneType lane = identity.PlayerSlot == 1 ? LaneType.Player1Lane : LaneType.Player2Lane;
            int useCount = identity.PlayerSlot == 1 ? Player1KidnapCount : Player2KidnapCount;
            if (!IsPlayerActionAllowed(lane) || !_executor.TryGetCanonicalSummonCost(useCount, out int cost))
                return;

            NetworkArray<NetworkBool> board = identity.PlayerSlot == 1 ? Player1BoardOccupied : Player2BoardOccupied;
            NetworkArray<long> alienIds = identity.PlayerSlot == 1 ? Player1BoardAlienIds : Player2BoardAlienIds;
            NetworkArray<NetworkString<_16>> mutationTypes = identity.PlayerSlot == 1 ? Player1BoardMutationTypes : Player2BoardMutationTypes;
            NetworkArray<byte> mutationStates = identity.PlayerSlot == 1 ? Player1BoardMutationStates : Player2BoardMutationStates;
            NetworkArray<byte> mutationRerolls = identity.PlayerSlot == 1 ? Player1BoardMutationRerollCounts : Player2BoardMutationRerollCounts;
            int slotIndex = FindFirstEmptyBoardSlot(board);
            if (slotIndex < 0)
            {
                Debug.Log($"[Fusion] Kidnap rejected: board is full for slot {identity.PlayerSlot}.");
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool forceInjector = ForceNextInjector && ForceNextInjectorPlayer == source;
            if (forceInjector)
            {
                ForceNextInjector = false;
                ForceNextInjectorPlayer = PlayerRef.None;
            }
            bool resolved = forceInjector
                ? _executor.TryGetCanonicalTestInjectorResult(identity.PlayerSlot, useCount, slotIndex, out BattleKidnapPoolResolver.KidnapResult kidnapResult)
                : _executor.TryGetCanonicalKidnapResult(identity.PlayerSlot, useCount, slotIndex, out kidnapResult);
#else
            bool resolved = _executor.TryGetCanonicalKidnapResult(identity.PlayerSlot, useCount, slotIndex, out BattleKidnapPoolResolver.KidnapResult kidnapResult);
#endif
            if (!resolved)
            {
                Debug.LogError("[Fusion] Kidnap rejected: STANDARD_SUMMON_POOL is unavailable.");
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
            alienIds.Set(slotIndex, kidnapResult.AlienId);
            mutationTypes.Set(slotIndex, kidnapResult.MutationType ?? default);
            mutationStates.Set(slotIndex, kidnapResult.IsInjector ? MutationStateInjector : MutationStateNone);
            mutationRerolls.Set(slotIndex, 0);
            byte gradeCode = kidnapResult.IsInjector ? byte.MaxValue : kidnapResult.GradeCode;
            if (identity.PlayerSlot == 1) Player1BoardGrades.Set(slotIndex, gradeCode);
            else Player2BoardGrades.Set(slotIndex, gradeCode);
            if (kidnapResult.IsInjector) RPC_InjectorApplied(identity.PlayerSlot, slotIndex, kidnapResult.MutationType);
            else RPC_KidnapApplied(identity.PlayerSlot, slotIndex, kidnapResult.AlienId);
            Debug.Log($"[Fusion] Kidnap request authorized: slot={identity.PlayerSlot}, result={(kidnapResult.IsInjector ? kidnapResult.MutationType : kidnapResult.AlienId.ToString())}, cost={cost}, remaining={remainingGold}.");
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_KidnapApplied(int playerSlot, int slotIndex, long alienId)
        {
            KidnapApplied?.Invoke(playerSlot, slotIndex, alienId);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_InjectorApplied(int playerSlot, int slotIndex, NetworkString<_16> mutationType)
        {
            InjectorApplied?.Invoke(playerSlot, slotIndex, mutationType.ToString());
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            IsSpawnedForAccess = false;
            _killDeduplicator.Clear();
            if (_executor == null)
                return;

            _executor.OnRegularWaveCompleted -= HandleRegularWaveCompleted;
            _executor.OnBossDefeated -= HandleWaveCompleted;
            _executor.OnBossTimeout -= HandleBossTimeout;
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
            HighestClearedWave = 0;
            _executor.InitializeSession(sessionContext, playerIdentityProvider);
            ResetFieldLimitEvents();
            SyncPlayerBattleStates();
            SyncAliveMonsterCounts();
            return true;
        }

        public bool ValidateWaveStart(out string reason)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_executor != null && _executor.IsP1ValidationArmed && _executor.IsP1ValidationStartConsumed)
                return FailValidation("The one allowed P1 validation Wave has already started.", out reason);
#endif
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
            Player1InitialInGameGold = player1Gold;
            Player2InitialInGameGold = player2Gold;
            Player1InGameGoldEarned = 0;
            Player2InGameGoldEarned = 0;
            Player1InGameGoldSpent = 0;
            Player2InGameGoldSpent = 0;
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
                Player1InGameGoldSpent = AddGoldSafely(Player1InGameGoldSpent, amount);
                remainingGold = Player1InGameGold;
                return true;
            }

            if (lane == LaneType.Player2Lane)
            {
                if (Player2InGameGold < amount) return false;
                Player2InGameGold -= amount;
                Player2InGameGoldSpent = AddGoldSafely(Player2InGameGoldSpent, amount);
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
                Player1InGameGoldEarned = AddGoldSafely(Player1InGameGoldEarned, amount);
                return true;
            }

            if (lane == LaneType.Player2Lane)
            {
                Player2InGameGold = AddGoldSafely(Player2InGameGold, amount);
                Player2InGameGoldEarned = AddGoldSafely(Player2InGameGoldEarned, amount);
                return true;
            }

            return false;
        }

        public bool TryAwardTeamGold(int amount)
        {
            if (!HasStateAuthority || amount < 0)
                return false;

            TeamInGameGold = AddGoldSafely(TeamInGameGold, amount);
            return true;
        }

        public static bool TryApplyCooperativeKillGold(
            BattleMonsterLanePolicy lanePolicy,
            int canonicalKillGold,
            ref int player1Gold,
            ref int player2Gold,
            ref int player1Earned,
            ref int player2Earned)
        {
            if (canonicalKillGold <= 0
                || (lanePolicy != BattleMonsterLanePolicy.EACH_FIELD
                    && lanePolicy != BattleMonsterLanePolicy.BOSS_SHARED))
                return false;

            player1Gold = AddGoldSafely(player1Gold, canonicalKillGold);
            player2Gold = AddGoldSafely(player2Gold, canonicalKillGold);
            player1Earned = AddGoldSafely(player1Earned, canonicalKillGold);
            player2Earned = AddGoldSafely(player2Earned, canonicalKillGold);
            return true;
        }

        public bool TryAwardCooperativeKillGold(BattleMonsterLanePolicy lanePolicy, int canonicalKillGold)
        {
            if (!HasStateAuthority)
                return false;

            int player1Gold = Player1InGameGold;
            int player2Gold = Player2InGameGold;
            int player1Earned = Player1InGameGoldEarned;
            int player2Earned = Player2InGameGoldEarned;
            if (!TryApplyCooperativeKillGold(
                    lanePolicy,
                    canonicalKillGold,
                    ref player1Gold,
                    ref player2Gold,
                    ref player1Earned,
                    ref player2Earned))
                return false;

            Player1InGameGold = player1Gold;
            Player2InGameGold = player2Gold;
            Player1InGameGoldEarned = player1Earned;
            Player2InGameGoldEarned = player2Earned;
            return true;
        }

        public bool TryAwardMonsterKill(BattleMonsterNetworkState monster)
        {
            MonsterStat stat = monster == null ? null : monster.GetComponent<MonsterStat>();
            if (!HasStateAuthority || stat == null || !stat.IsDead || !monster.IsInitialized || _executor == null)
                return false;

            if (!_executor.TryGetCanonicalMonsterDefinition(monster.MonsterId.ToString(), out BattleMonsterDefinition definition)
                || definition.KillGold <= 0)
            {
                Debug.LogError($"[Battle] Cannot award killGold: unknown or disabled monster '{monster.MonsterId}'.");
                return false;
            }
            int killGold = definition.KillGold;

            string sessionId = monster.BattleSessionId.ToString();
            if (string.IsNullOrWhiteSpace(sessionId) || monster.RuntimeMonsterId == 0)
                return false;

            BattleRuntimeMonsterKey key;
            try
            {
                key = new BattleRuntimeMonsterKey(sessionId, monster.RuntimeMonsterId);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (!_killDeduplicator.TryReserve(key))
                return false;

            bool awarded;
            if (monster.LanePolicy == BattleMonsterLanePolicy.BOSS_SHARED)
            {
                if (!_executor.TryResolveBossDefeatFromAuthority(monster))
                {
                    _killDeduplicator.Release(key);
                    return false;
                }
                awarded = TryAwardCooperativeKillGold(monster.LanePolicy, killGold);
            }
            else if (monster.LanePolicy == BattleMonsterLanePolicy.EACH_FIELD)
            {
                awarded = TryAwardCooperativeKillGold(monster.LanePolicy, killGold);
            }
            else
            {
                _killDeduplicator.Release(key);
                awarded = false;
            }

            if (!awarded)
            {
                _killDeduplicator.Release(key);
                return false;
            }

            TryRegisterKillAudit(monster, stat, key, killGold);
            foreach (long attackerId in stat.DamageAttackerIds)
            {
                if (attackerId == stat.LastDamageAttackerId)
                    continue;
                if (TryResolvePlayerUserIdFromAttacker(attackerId, out string supportPlayerId))
                {
                    _killDeduplicator.TryAttachSupport(key, supportPlayerId);
                    break;
                }
            }
            return awarded;
        }

        /// <summary>
        /// Records one assist for an already accepted kill. Assist statistics do
        /// not award Gold and can never reserve a second kill key.
        /// </summary>
        public bool TryRegisterSupportKill(BattleMonsterNetworkState monster, long supportPlayerId)
        {
            if (!HasStateAuthority || monster == null || !monster.IsInitialized || supportPlayerId <= 0)
                return false;

            BattleRuntimeMonsterKey key;
            try
            {
                key = new BattleRuntimeMonsterKey(monster.BattleSessionId.ToString(), monster.RuntimeMonsterId);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return TryResolvePlayerUserIdFromAttacker(supportPlayerId, out string userId)
                && _killDeduplicator.TryAttachSupport(key, userId);
        }

        private void TryRegisterKillAudit(
            BattleMonsterNetworkState monster,
            MonsterStat stat,
            BattleRuntimeMonsterKey key,
            int killGold)
        {
            if (monster == null || stat == null || stat.LastDamageAttackerId <= 0)
                return;

            if (!TryResolvePlayerUserIdFromAttacker(stat.LastDamageAttackerId, out string killerPlayerId))
                return;
            string ownerPlayerId = monster.LanePolicy == BattleMonsterLanePolicy.EACH_FIELD
                ? monster.FieldOwnerPlayerId.ToString()
                : null;
            long killedAtTick = Runner == null ? 0L : (long)Runner.Tick;
            try
            {
                _killDeduplicator.TryAttachAudit(new BattleKillAuditRecord(
                    key,
                    monster.MonsterId.ToString(),
                    killerPlayerId,
                    ownerPlayerId,
                    monster.LanePolicy,
                    monster.SpawnWave,
                    killedAtTick,
                    killGold: killGold));
            }
            catch (ArgumentException)
            {
                Debug.LogWarning($"[Battle] Kill audit skipped for runtime monster '{key}'.");
            }
        }

        private bool TryResolvePlayerUserIdFromAttacker(long attackerId, out string playerUserId)
        {
            playerUserId = null;
            if (attackerId <= 0)
                return false;

            int playerSlot = (int)(attackerId >> 32);
            playerUserId = playerSlot == 1 ? Player1UserId.ToString()
                : playerSlot == 2 ? Player2UserId.ToString()
                : null;
            return !string.IsNullOrWhiteSpace(playerUserId);
        }

        /// <summary>
        /// Handles a Boss death from the State Authority's MonsterStat callback.
        /// The runtime key and executor BossState both make this path one-shot;
        /// duplicate death callbacks cannot advance the wave or award Gold twice.
        /// </summary>
        public bool TryHandleAuthoritativeBossDefeat(BattleMonsterNetworkState monster)
        {
            MonsterStat stat = monster == null ? null : monster.GetComponent<MonsterStat>();
            if (stat == null || !stat.IsDead || monster.LanePolicy != BattleMonsterLanePolicy.BOSS_SHARED)
                return false;

            return TryAwardMonsterKill(monster);
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

        private void HandleRegularWaveCompleted(int completedWave)
        {
            HighestClearedWave = Math.Max(HighestClearedWave, completedWave);
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
            HighestClearedWave = Math.Max(HighestClearedWave, CurrentWave);
            BossTimer = default;
            IsWaveRunning = false;
        }

        private void HandleBossTimeout()
        {
            if (!HasStateAuthority)
                return;
            BossTimer = default;
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
                {
                    Player1Eliminated = true;
                    Player1EliminatedWave = Math.Max(1, CurrentWave);
                }
            }
            else if (lane == LaneType.Player2Lane)
            {
                Player2BattleStateValue = (int)state;
                if (state == PlayerBattleState.ELIMINATED)
                {
                    Player2Eliminated = true;
                    Player2EliminatedWave = Math.Max(1, CurrentWave);
                }
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
            {
                Player1Eliminated = true;
                Player1EliminatedWave = Math.Max(1, CurrentWave);
            }
            else if (lane == LaneType.Player2Lane)
            {
                Player2Eliminated = true;
                Player2EliminatedWave = Math.Max(1, CurrentWave);
            }
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
            Player1EliminatedWave = 0;
            Player2EliminatedWave = 0;
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
