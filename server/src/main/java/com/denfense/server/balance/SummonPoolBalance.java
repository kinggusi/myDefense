package com.denfense.server.balance;

import java.util.List;

public record SummonPoolBalance(String poolId, String name, boolean active,
                                List<SummonPoolEntryBalance> entries) {
    public SummonPoolBalance {
        entries = entries == null ? null : List.copyOf(entries);
    }
}
