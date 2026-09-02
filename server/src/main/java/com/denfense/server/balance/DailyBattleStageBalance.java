package com.denfense.server.balance;

import com.fasterxml.jackson.annotation.JsonPropertyOrder;

import java.math.BigDecimal;

@JsonPropertyOrder({
        "contentType", "mapId", "stage", "wave", "timeLimitSeconds", "monsterSpecId",
        "spawnCount", "spawnIntervalSeconds", "hpMultiplier", "moveSpeedMultiplier",
        "lanePolicy", "boss", "statusEffectType", "statusEffectValue", "enabled"
})
public record DailyBattleStageBalance(
        String contentType,
        String mapId,
        int stage,
        int wave,
        int timeLimitSeconds,
        String monsterSpecId,
        int spawnCount,
        BigDecimal spawnIntervalSeconds,
        BigDecimal hpMultiplier,
        BigDecimal moveSpeedMultiplier,
        String lanePolicy,
        boolean boss,
        String statusEffectType,
        BigDecimal statusEffectValue,
        boolean enabled
) {
}
