package com.denfense.server.balance;

import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonPropertyOrder({"modeId", "playerCount", "maxAliveMonsterCountPerField", "warningThreshold", "dangerThreshold"})
public record FieldLimitBalance(
        String modeId,
        int playerCount,
        int maxAliveMonsterCountPerField,
        int warningThreshold,
        int dangerThreshold
) {
}
