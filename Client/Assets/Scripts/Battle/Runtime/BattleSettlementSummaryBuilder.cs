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
                summaryHash = summaryHash
            };
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
