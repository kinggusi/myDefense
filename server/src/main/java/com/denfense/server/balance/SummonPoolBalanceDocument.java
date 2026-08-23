package com.denfense.server.balance;

import java.util.List;

public record SummonPoolBalanceDocument(List<SummonPoolBalance> pools) {
    public SummonPoolBalanceDocument {
        pools = pools == null ? null : List.copyOf(pools);
    }
}
