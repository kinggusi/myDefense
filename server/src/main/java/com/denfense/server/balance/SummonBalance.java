package com.denfense.server.balance;

import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonPropertyOrder({"modeId", "summonType", "baseCost", "costIncreasePerUse", "maxUses", "resultPoolId", "enabled"})
public record SummonBalance(
        String modeId,
        String summonType,
        int baseCost,
        int costIncreasePerUse,
        int maxUses,
        String resultPoolId,
        boolean enabled
) {
}
