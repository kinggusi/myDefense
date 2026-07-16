package com.denfense.server.balance;

import java.util.List;

public record WaveSpecBalanceDocument(List<WaveSpecBalance> waves) {
    public WaveSpecBalanceDocument {
        waves = waves == null ? null : List.copyOf(waves);
    }
}
