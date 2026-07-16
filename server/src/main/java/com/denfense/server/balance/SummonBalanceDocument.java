package com.denfense.server.balance;

import java.util.List;

public record SummonBalanceDocument(List<SummonBalance> summons) {
    public SummonBalanceDocument {
        summons = summons == null ? null : List.copyOf(summons);
    }
}
