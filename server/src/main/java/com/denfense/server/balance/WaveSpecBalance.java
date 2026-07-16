package com.denfense.server.balance;

import com.fasterxml.jackson.annotation.JsonPropertyOrder;

import java.math.BigDecimal;

@JsonPropertyOrder({"modeId", "wave", "hpMultiplier", "interWaveDelaySeconds", "isBossWave", "bossTimeLimitSeconds", "spawnGroupId", "enabled"})
public record WaveSpecBalance(
        String modeId,
        int wave,
        BigDecimal hpMultiplier,
        BigDecimal interWaveDelaySeconds,
        boolean isBossWave,
        BigDecimal bossTimeLimitSeconds,
        String spawnGroupId,
        boolean enabled
) {
}
