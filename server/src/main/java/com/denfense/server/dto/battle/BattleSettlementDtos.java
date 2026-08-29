package com.denfense.server.dto.battle;

import java.time.LocalDateTime;
import java.util.List;

public final class BattleSettlementDtos {
    private BattleSettlementDtos() {
    }

    public record Player(
            String playerId,
            int playerSlot,
            boolean eliminated,
            Integer eliminatedWave,
            int kills,
            int supportKills,
            int bossKills,
            int initialInGameGold,
            int inGameGoldEarned,
            int inGameGoldSpent,
            int finalInGameGold,
            boolean abandoned
    ) {
        public Player(String playerId, int playerSlot, boolean eliminated, Integer eliminatedWave,
                      int kills, int supportKills, int bossKills, int initialInGameGold,
                      int inGameGoldEarned, int inGameGoldSpent, int finalInGameGold) {
            this(playerId, playerSlot, eliminated, eliminatedWave, kills, supportKills, bossKills,
                    initialInGameGold, inGameGoldEarned, inGameGoldSpent, finalInGameGold, false);
        }
    }

    public record Monster(String monsterSpecId, int totalKills, int bossKills, int totalKillGold) {
    }

    public record PartialWaveKill(
            String runtimeMonsterId,
            int spawnWave,
            String monsterSpecId,
            String lanePolicy,
            Integer playerSlot,
            int spawnOrder,
            int spawnOrdinal,
            String killerPlayerId,
            String supportPlayerId,
            long killedAtTick
    ) {
    }

    public record Request(
            String requestId,
            String battleSessionId,
            String balanceVersion,
            String contentHash,
            String result,
            int finalWave,
            String mapId,
            LocalDateTime startedAt,
            LocalDateTime finishedAt,
            List<Player> players,
            List<Monster> monsterKills,
            List<PartialWaveKill> partialWaveKills,
            String summaryHash
    ) {
        public Request {
            partialWaveKills = partialWaveKills == null ? List.of() : partialWaveKills;
        }

        public Request(String requestId, String battleSessionId, String balanceVersion, String contentHash,
                       String result, int finalWave, String mapId, LocalDateTime startedAt,
                       LocalDateTime finishedAt, List<Player> players, List<Monster> monsterKills,
                       String summaryHash) {
            this(requestId, battleSessionId, balanceVersion, contentHash, result, finalWave, mapId,
                    startedAt, finishedAt, players, monsterKills, List.of(), summaryHash);
        }

        public Request(String requestId, String battleSessionId, String balanceVersion, String contentHash,
                       String result, int finalWave, LocalDateTime startedAt, LocalDateTime finishedAt,
                       List<Player> players, List<Monster> monsterKills, String summaryHash) {
            this(requestId, battleSessionId, balanceVersion, contentHash, result, finalWave, null,
                    startedAt, finishedAt, players, monsterKills, List.of(), summaryHash);
        }
    }

    public record Reward(Long userId, String rewardKey, String rewardType,
                         int gold, int universalPiece, int diamond) {
    }

    public record Response(String battleSessionId, String status,
                           boolean alreadyProcessed, List<Reward> rewards) {
    }
}
