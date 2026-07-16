package com.denfense.server.balance;

import com.fasterxml.jackson.annotation.JsonPropertyOrder;

@JsonPropertyOrder({"sourceGrade", "requiredCount", "sameSpeciesRequired", "resultType", "resultGrade", "enabled"})
public record MergeRuleBalance(
        String sourceGrade,
        int requiredCount,
        boolean sameSpeciesRequired,
        String resultType,
        String resultGrade,
        boolean enabled
) {
}
