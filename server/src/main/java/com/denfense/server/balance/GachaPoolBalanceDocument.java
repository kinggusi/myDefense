package com.denfense.server.balance;

import java.util.List;

public record GachaPoolBalanceDocument(List<GachaPoolBalance> pools) {
    public GachaPoolBalanceDocument {
        if (pools != null) {
            pools = List.copyOf(pools);
        }
    }
}