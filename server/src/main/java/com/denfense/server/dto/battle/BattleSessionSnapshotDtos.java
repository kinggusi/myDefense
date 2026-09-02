package com.denfense.server.dto.battle;

import java.math.BigInteger;
import java.util.List;

/**
 * Reconnect/resume snapshot contract. Live authority remains in Fusion; this
 * DTO describes the state needed to reconstruct a battle view.
 */
public final class BattleSessionSnapshotDtos {
    private BattleSessionSnapshotDtos() {}

    public record Snapshot(
            int schemaVersion,
            String battleSessionId,
            String mapId,
            String balanceVersion,
            String contentHash,
            String matchState,
            int currentWave,
            String currentWaveSpecId,
            String waveType,
            String wavePhase,
            int waveTimeRemainingSeconds,
            int bossTimeRemainingSeconds,
            long capturedAtTick,
            List<Player> players,
            List<BoardObject> boardObjects,
            List<MythicChoice> mythicChoices,
            List<Monster> monsters) {}

    public record Player(
            String playerId,
            int playerSlot,
            String battleState,
            String connectionState,
            int inGameGold,
            int currentKidnapCost,
            int normalResonanceLevel,
            int mythicResonanceLevel,
            Integer eliminatedWave) {}

    public record BoardObject(
            long objectId,
            int ownerPlayerSlot,
            String objectType,
            int gridX,
            int gridY,
            Long alienSpecId,
            String grade,
            String pendingMutationType,
            String activeMutationType,
            int mutationRerollCount,
            String mutationType,
            String mutationState) {}

    public record MythicChoice(
            int playerSlot,
            int targetBoardSlot,
            List<Long> candidateAlienIds,
            int freeRerollsRemaining,
            int paidRerollsRemaining,
            int remainingSeconds) {}

    public record Monster(
            BigInteger runtimeMonsterId,
            String monsterId,
            String lanePolicy,
            String fieldOwnerPlayerId,
            int spawnWave,
            float currentHp,
            float maxHp,
            boolean dead,
            float x,
            float y,
            float z) {}
}
