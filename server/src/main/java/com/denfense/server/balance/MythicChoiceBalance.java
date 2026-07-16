package com.denfense.server.balance;

import com.fasterxml.jackson.annotation.JsonPropertyOrder;

import java.math.BigDecimal;

@JsonPropertyOrder({"modeId", "candidateCount", "freeRerollCount", "paidRerollLimit", "paidRerollCost", "excludePreviousCandidates", "selectionTimeoutSeconds", "autoSelectPolicy", "battleContinuesDuringSelection", "enabled"})
public record MythicChoiceBalance(
        String modeId,
        int candidateCount,
        int freeRerollCount,
        int paidRerollLimit,
        int paidRerollCost,
        boolean excludePreviousCandidates,
        BigDecimal selectionTimeoutSeconds,
        String autoSelectPolicy,
        boolean battleContinuesDuringSelection,
        boolean enabled
) {
}
