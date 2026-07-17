package com.denfense.server.dto.battle;

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
            List<BoardObject> boardObjects) {}

    public record Player(
            String playerId,
            int playerSlot,
            String battleState,
            String connectionState,
            int inGameGold,
            int currentKidnapCost,
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
            String mutationType) {}
}
