using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MyDefense.Shared.Contracts;

namespace MyDefense.Battle.Runtime
{
    /// <summary>
    /// Converts the immutable BattleSummary ledger into the wire contract used
    /// by Spring. Request identity, timestamps and summaryHash are supplied by
    /// the settlement coordinator because they are transport/idempotency data,
    /// not Battle simulation state.
    /// </summary>
    public static class BattleSettlementSummaryBuilder
    {
        public static BattleSettlementSummary Build(
            BattleSummary summary,
            string requestId,
            DateTime startedAt,
            DateTime finishedAt,
            string summaryHash)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));
            if (string.IsNullOrWhiteSpace(requestId)) throw new ArgumentException("requestId is required.", nameof(requestId));
            if (string.IsNullOrWhiteSpace(summaryHash)) throw new ArgumentException("summaryHash is required.", nameof(summaryHash));
            if (startedAt > finishedAt) throw new ArgumentException("startedAt must not be after finishedAt.", nameof(finishedAt));
            if (summary.Players == null || summary.Players.Count != 2)
                throw new ArgumentException("Settlement requires exactly two player summaries.", nameof(summary));

            string result = ToSettlementResult(summary.Result);
            var players = summary.Players
                .OrderBy(player => player.PlayerSlot)
                .Select(ToPlayer)
                .ToArray();
            if (!players.Select(player => player.playerSlot).SequenceEqual(new[] { 1, 2 }))
                throw new ArgumentException("Player slots must be exactly 1 and 2.", nameof(summary));

            BattleSettlementMonsterSummary[] monsters = (summary.Kills?.ByMonster ?? Array.Empty<BattleMonsterKillSummary>())
                .Select(monster => new BattleSettlementMonsterSummary
                {
                    monsterSpecId = monster.MonsterId,
                    totalKills = monster.KillCount,
                    bossKills = monster.BossKillCount,
                    totalKillGold = monster.TotalKillGold
                })
                .ToArray();

            ValidateAggregateConsistency(summary, players, monsters);
            BuildUnfinishedWaveEvidence(
                summary,
                out BattleSettlementWaveSpawnFactSummary[] waveSpawnFacts,
                out BattleSettlementPartialWaveKillSummary[] partialWaveKills);

            return new BattleSettlementSummary
            {
                requestId = requestId,
                battleSessionId = summary.BattleSessionId,
                balanceVersion = summary.CanonicalBalanceVersion,
                contentHash = summary.CanonicalContentHash,
                result = result,
                finalWave = summary.FinalWave,
                mapId = summary.MapId,
                startedAt = startedAt.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
                finishedAt = finishedAt.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
                players = players,
                monsterKills = monsters,
                waveSpawnFacts = waveSpawnFacts,
                partialWaveKills = partialWaveKills,
                summaryHash = summaryHash
            };
        }

        private static void BuildUnfinishedWaveEvidence(
            BattleSummary summary,
            out BattleSettlementWaveSpawnFactSummary[] waveSpawnFacts,
            out BattleSettlementPartialWaveKillSummary[] partialWaveKills)
        {
            waveSpawnFacts = Array.Empty<BattleSettlementWaveSpawnFactSummary>();
            partialWaveKills = Array.Empty<BattleSettlementPartialWaveKillSummary>();
            if (summary.Result != MatchState.FAILED)
                return;

            int unfinishedWave = checked(summary.FinalWave + 1);
            var playersById = summary.Players.ToDictionary(player => player.PlayerId, StringComparer.Ordinal);
            var playersBySlot = summary.Players.ToDictionary(player => player.PlayerSlot);
            List<BattleSpawnAuditRecord> spawns = (summary.SpawnRecords ?? Array.Empty<BattleSpawnAuditRecord>())
                .Where(record => record.SpawnWave == unfinishedWave)
                .OrderBy(record => record.RuntimeMonsterId)
                .ToList();

            foreach (BattleSpawnAuditRecord spawn in spawns)
            {
                ValidateActiveSlot(spawn.FieldOwnerPlayerSlot, spawn.LanePolicy, unfinishedWave, playersBySlot);
            }

            waveSpawnFacts = spawns.Select(ToWaveSpawnFact).ToArray();
            var spawnByRuntimeKey = spawns.ToDictionary(record => record.RuntimeKey);
            var partial = new List<BattleSettlementPartialWaveKillSummary>();
            foreach (BattleKillAuditRecord kill in (summary.Kills?.AuditRecords ?? Array.Empty<BattleKillAuditRecord>())
                         .Where(record => record.SpawnWave == unfinishedWave)
                         .OrderBy(record => record.RuntimeMonsterId))
            {
                if (!spawnByRuntimeKey.TryGetValue(kill.RuntimeKey, out BattleSpawnAuditRecord spawn)
                    || !string.Equals(spawn.MonsterId, kill.MonsterId, StringComparison.Ordinal)
                    || spawn.LanePolicy != kill.LanePolicy)
                {
                    throw new ArgumentException(
                        "Unfinished-Wave Kill identity does not match authoritative Spawn evidence.",
                        nameof(summary));
                }

                if (!playersById.TryGetValue(kill.KillerPlayerId, out BattlePlayerSummary killer)
                    || !IsActiveAtWave(killer, unfinishedWave))
                {
                    throw new ArgumentException("Unfinished-Wave killer slot is not active.", nameof(summary));
                }

                int? supportSlot = null;
                if (!string.IsNullOrWhiteSpace(kill.SupportPlayerId))
                {
                    if (!playersById.TryGetValue(kill.SupportPlayerId, out BattlePlayerSummary support)
                        || !IsActiveAtWave(support, unfinishedWave)
                        || support.PlayerSlot == killer.PlayerSlot)
                    {
                        throw new ArgumentException("Unfinished-Wave support slot is invalid.", nameof(summary));
                    }
                    supportSlot = support.PlayerSlot;
                }

                if (spawn.LanePolicy == BattleMonsterLanePolicy.EACH_FIELD)
                {
                    BattlePlayerSummary owner = playersBySlot[spawn.FieldOwnerPlayerSlot.Value];
                    if (!string.Equals(owner.PlayerId, kill.FieldOwnerPlayerId, StringComparison.Ordinal))
                    {
                        throw new ArgumentException(
                            "Kill field owner does not match authoritative Spawn owner slot.",
                            nameof(summary));
                    }
                }
                else if (!string.IsNullOrEmpty(kill.FieldOwnerPlayerId))
                {
                    throw new ArgumentException("BOSS_SHARED Kill cannot have a field owner.", nameof(summary));
                }

                partial.Add(new BattleSettlementPartialWaveKillSummary
                {
                    runtimeMonsterId = RuntimeIdText(spawn.RuntimeMonsterId),
                    spawnWave = spawn.SpawnWave,
                    spawnGroupId = spawn.SpawnGroupId,
                    monsterSpecId = spawn.MonsterId,
                    lanePolicy = spawn.LanePolicy.ToString(),
                    fieldOwnerPlayerSlot = spawn.FieldOwnerPlayerSlot,
                    spawnOrder = spawn.SpawnOrder,
                    spawnOrdinal = spawn.SpawnOrdinal,
                    killerPlayerSlot = killer.PlayerSlot,
                    supportPlayerSlot = supportSlot
                });
            }

            partialWaveKills = partial.ToArray();
        }

        private static BattleSettlementWaveSpawnFactSummary ToWaveSpawnFact(BattleSpawnAuditRecord spawn)
        {
            return new BattleSettlementWaveSpawnFactSummary
            {
                runtimeMonsterId = RuntimeIdText(spawn.RuntimeMonsterId),
                spawnWave = spawn.SpawnWave,
                spawnGroupId = spawn.SpawnGroupId,
                monsterSpecId = spawn.MonsterId,
                lanePolicy = spawn.LanePolicy.ToString(),
                fieldOwnerPlayerSlot = spawn.FieldOwnerPlayerSlot,
                spawnOrder = spawn.SpawnOrder,
                spawnOrdinal = spawn.SpawnOrdinal
            };
        }

        private static void ValidateActiveSlot(
            int? ownerSlot,
            BattleMonsterLanePolicy lanePolicy,
            int wave,
            IReadOnlyDictionary<int, BattlePlayerSummary> playersBySlot)
        {
            if (lanePolicy == BattleMonsterLanePolicy.BOSS_SHARED)
            {
                if (ownerSlot.HasValue)
                    throw new ArgumentException("BOSS_SHARED Spawn cannot have a field owner slot.");
                return;
            }

            if (lanePolicy != BattleMonsterLanePolicy.EACH_FIELD
                || !ownerSlot.HasValue
                || !playersBySlot.TryGetValue(ownerSlot.Value, out BattlePlayerSummary owner)
                || !IsActiveAtWave(owner, wave))
            {
                throw new ArgumentException("EACH_FIELD Spawn owner slot is not active.");
            }
        }

        private static bool IsActiveAtWave(BattlePlayerSummary player, int wave)
        {
            return player != null && (!player.EliminatedWave.HasValue || wave <= player.EliminatedWave.Value);
        }

        private static string RuntimeIdText(ulong runtimeMonsterId)
        {
            if (runtimeMonsterId == 0)
                throw new ArgumentOutOfRangeException(nameof(runtimeMonsterId));
            return runtimeMonsterId.ToString(CultureInfo.InvariantCulture);
        }

        private static void ValidateAggregateConsistency(
            BattleSummary summary,
            IReadOnlyList<BattleSettlementPlayerSummary> players,
            IReadOnlyList<BattleSettlementMonsterSummary> monsters)
        {
            IReadOnlyList<BattleKillAuditRecord> audit = summary.Kills?.AuditRecords
                ?? Array.Empty<BattleKillAuditRecord>();
            int killCount = audit.Count;
            int supportCount = audit.Count(record => !string.IsNullOrWhiteSpace(record.SupportPlayerId));
            int bossCount = audit.Count(record => record.LanePolicy == BattleMonsterLanePolicy.BOSS_SHARED);
            int killGold = audit.Sum(record => record.KillGold);

            if (players.Sum(player => player.kills) != killCount
                || players.Sum(player => player.supportKills) != supportCount
                || players.Sum(player => player.bossKills) != bossCount
                || monsters.Sum(monster => monster.totalKills) != killCount
                || monsters.Sum(monster => monster.bossKills) != bossCount
                || monsters.Sum(monster => monster.totalKillGold) != killGold
                || (summary.Kills?.ByLanePolicy.Sum(item => item.KillCount) ?? 0) != killCount
                || (summary.Kills?.ByLanePolicy.Sum(item => item.AwardedGold) ?? 0) != killGold
                || (summary.Kills?.ProcessedRuntimeKeys.Count ?? 0) != killCount)
            {
                throw new ArgumentException("Player, Monster, Kill, Support, Boss, or KillGold totals are inconsistent.", nameof(summary));
            }

            foreach (BattleSettlementPlayerSummary player in players)
            {
                if ((long)player.initialInGameGold + player.inGameGoldEarned - player.inGameGoldSpent
                    != player.finalInGameGold)
                {
                    throw new ArgumentException("Player in-game Gold ledger is inconsistent.", nameof(summary));
                }
            }
        }

        private static BattleSettlementPlayerSummary ToPlayer(BattlePlayerSummary player)
        {
            if (player == null) throw new ArgumentException("Player summary cannot be null.");
            return new BattleSettlementPlayerSummary
            {
                playerId = player.PlayerId,
                playerSlot = player.PlayerSlot,
                eliminated = player.Eliminated,
                eliminatedWave = player.EliminatedWave,
                kills = player.Kills,
                supportKills = player.SupportKills,
                bossKills = player.BossKills,
                initialInGameGold = player.InitialInGameGold,
                inGameGoldEarned = player.InGameGoldEarned,
                inGameGoldSpent = player.InGameGoldSpent,
                finalInGameGold = player.FinalInGameGold,
                abandoned = player.Abandoned
            };
        }

        private static string ToSettlementResult(MatchState result)
        {
            switch (result)
            {
                case MatchState.CLEARED: return BattleSettlementResultValues.Victory;
                case MatchState.FAILED: return BattleSettlementResultValues.Defeat;
                default: throw new ArgumentException("A terminal MatchState is required.", nameof(result));
            }
        }
    }
}
