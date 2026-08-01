package com.denfense.server.balance;

import java.util.List;

public record SummonPoolEntryBalance(String grade, int weight, List<Long> alienIds) {
    public SummonPoolEntryBalance {
        alienIds = alienIds == null ? null : List.copyOf(alienIds);
    }
}
