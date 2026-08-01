using System;
using System.Collections.Generic;
using System.Linq;
using MyDefense.Shared.Contracts;
using MyDefense.Battle.Balance;
using UnityEngine;

namespace MyDefense.Battle.Runtime
{
    /// <summary>
    /// Captures the State Authority's current resume state. Fusion still owns
    /// live replication; this immutable snapshot is used to validate reconnect
    /// completeness and can be persisted by a future transport without reading UI.
    /// </summary>
    public static class BattleReconnectSnapshotBuilder
    {
        public static BattleSessionSnapshot Capture(
            BattleSessionContext session,
            BattleWaveStateAuthority authority,
            BattleWaveExecutor executor)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (authority == null) throw new ArgumentNullException(nameof(authority));
            if (executor == null) throw new ArgumentNullException(nameof(executor));
            if (!authority.IsAuthoritative || !authority.IsSpawnedForAccess)
                throw new InvalidOperationException("Only the spawned State Authority can capture a reconnect snapshot.");

            var snapshot = new BattleSessionSnapshot
            {
                battleSessionId = session.BattleSessionId,
                balanceVersion = session.CanonicalBalanceVersion,
                contentHash = session.CanonicalContentHash,
                matchState = authority.MatchState,
                currentWave = authority.CurrentWave,
                currentWaveSpecId = string.IsNullOrWhiteSpace(authority.CurrentWaveId.ToString()) ? "NONE" : authority.CurrentWaveId.ToString(),
                waveType = authority.CurrentWaveType == WaveType.BOSS ? "BOSS" : "REGULAR",
                wavePhase = authority.MatchState != MatchState.RUNNING ? "COMPLETED" : authority.IsWaveRunning ? "ACTIVE" : "WAITING",
                waveTimeRemainingSeconds = 0,
                bossTimeRemainingSeconds = authority.GetBossRemainingSeconds(),
                capturedAtTick = authority.Runner == null ? 0L : authority.Runner.Tick.Raw,
                players = new[] { CreatePlayer(authority, executor, 1), CreatePlayer(authority, executor, 2) },
                boardObjects = CreateBoardObjects(authority),
                mythicChoices = CreateMythicChoices(authority),
                monsters = CreateMonsterSnapshots(session.BattleSessionId)
            };
            BattleSessionSnapshotValidator.Validate(snapshot);
            return snapshot;
        }

        private static BattleSessionPlayerSnapshot CreatePlayer(
            BattleWaveStateAuthority authority,
            BattleWaveExecutor executor,
            int playerSlot)
        {
            PlayerBattleState battleState = playerSlot == 1 ? authority.Player1BattleState : authority.Player2BattleState;
            bool eliminated = playerSlot == 1 ? authority.Player1Eliminated : authority.Player2Eliminated;
            int eliminatedWave = playerSlot == 1 ? authority.Player1EliminatedWave : authority.Player2EliminatedWave;
            executor.TryGetCanonicalSummonCost(authority.GetKidnapCount(playerSlot), out int kidnapCost);
            return new BattleSessionPlayerSnapshot
            {
                playerId = playerSlot == 1 ? authority.Player1UserId.ToString() : authority.Player2UserId.ToString(),
                playerSlot = playerSlot,
                battleState = battleState,
                connectionState = authority.GetPlayerConnectionState(playerSlot),
                inGameGold = authority.GetInGameGoldForPlayerSlot(playerSlot),
                currentKidnapCost = kidnapCost,
                eliminatedWave = eliminated ? eliminatedWave : (int?)null
            };
        }

        private static BattleBoardObjectSnapshot[] CreateBoardObjects(BattleWaveStateAuthority authority)
        {
            var result = new List<BattleBoardObjectSnapshot>();
            for (int playerSlot = 1; playerSlot <= 2; playerSlot++)
            {
                for (int slot = 0; slot < 24; slot++)
                {
                    if (!authority.IsBoardOccupied(playerSlot, slot))
                        continue;
                    byte mutationState = authority.GetBoardMutationState(playerSlot, slot);
                    bool injector = authority.IsBoardInjector(playerSlot, slot);
                    string mutationType = authority.GetBoardMutationType(playerSlot, slot);
                    result.Add(new BattleBoardObjectSnapshot
                    {
                        objectId = ((long)playerSlot << 32) | (uint)(slot + 1),
                        ownerPlayerSlot = playerSlot,
                        objectType = injector ? BattleBoardObjectType.MUTATION_INJECTOR : BattleBoardObjectType.ALIEN,
                        gridX = slot / 6,
                        gridY = slot % 6,
                        alienSpecId = injector ? null : authority.GetBoardAlienId(playerSlot, slot),
                        grade = injector ? null : GradeName(authority.GetBoardGrade(playerSlot, slot)),
                        pendingMutationType = mutationState == 2 ? mutationType : null,
                        activeMutationType = mutationState == 3 ? mutationType : null,
                        mutationRerollCount = 0,
                        mutationType = injector ? mutationType : null,
                        mutationState = (BattleMutationState)mutationState
                    });
                }
            }
            return result.ToArray();
        }

        private static BattleMythicChoiceSnapshot[] CreateMythicChoices(BattleWaveStateAuthority authority)
        {
            var choices = new List<BattleMythicChoiceSnapshot>(2);
            for (int playerSlot = 1; playerSlot <= 2; playerSlot++)
            {
                if (!authority.IsMythicChoiceActive(playerSlot))
                    continue;
                choices.Add(new BattleMythicChoiceSnapshot
                {
                    playerSlot = playerSlot,
                    targetBoardSlot = authority.GetMythicChoiceSlot(playerSlot),
                    candidateAlienIds = Enumerable.Range(0, 3).Select(index => authority.GetMythicChoiceCandidate(playerSlot, index)).ToArray(),
                    freeRerollsRemaining = authority.GetMythicFreeRerollsRemaining(playerSlot),
                    paidRerollsRemaining = authority.GetMythicPaidRerollsRemaining(playerSlot),
                    remainingSeconds = authority.GetMythicChoiceRemainingSeconds(playerSlot)
                });
            }
            return choices.ToArray();
        }

        private static BattleMonsterStateSnapshot[] CreateMonsterSnapshots(string battleSessionId)
        {
            return UnityEngine.Object.FindObjectsByType<BattleMonsterNetworkState>(FindObjectsSortMode.None)
                .Where(monster => monster != null && monster.IsInitialized
                    && string.Equals(monster.BattleSessionId.ToString(), battleSessionId, StringComparison.Ordinal))
                .OrderBy(monster => monster.RuntimeMonsterId)
                .Select(monster => new BattleMonsterStateSnapshot
                {
                    runtimeMonsterId = monster.RuntimeMonsterId,
                    monsterId = monster.MonsterId.ToString(),
                    lanePolicy = monster.LanePolicy.ToString(),
                    fieldOwnerPlayerId = monster.FieldOwnerPlayerId.ToString(),
                    spawnWave = monster.SpawnWave,
                    currentHp = monster.CurrentHp,
                    maxHp = monster.MaxHp,
                    dead = monster.IsDead,
                    x = monster.transform.position.x,
                    y = monster.transform.position.y,
                    z = monster.transform.position.z
                }).ToArray();
        }

        private static string GradeName(byte grade)
            => grade switch { 1 => "EPIC", 2 => "UNIQUE", 3 => "LEGEND", 4 => "MYTHIC", _ => "NORMAL" };
    }
}
