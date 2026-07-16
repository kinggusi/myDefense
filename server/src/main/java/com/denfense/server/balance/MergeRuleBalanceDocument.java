package com.denfense.server.balance;

import java.util.List;

public record MergeRuleBalanceDocument(List<MergeRuleBalance> mergeRules) {
    public MergeRuleBalanceDocument {
        mergeRules = mergeRules == null ? null : List.copyOf(mergeRules);
    }
}
