package com.denfense.server.balance;

import java.util.List;

/** Canonical planet/wave reward contract. */
public record BattleRewardBalance(
        int maxWave,
        int minimumRewardWave,
        int failureRewardBaseGold,
        int failureRewardCapPercent,
        List<Checkpoint> checkpoints,
        List<MapFirstClear> mapFirstClears
) {
    public record Checkpoint(int wave, int gold, int universalPiece) {}
    public record MapFirstClear(String mapId, int wave, int diamond) {}
}
