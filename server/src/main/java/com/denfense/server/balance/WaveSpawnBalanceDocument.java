package com.denfense.server.balance;

import java.util.List;

public record WaveSpawnBalanceDocument(List<WaveSpawnBalance> spawns) {
    public WaveSpawnBalanceDocument {
        spawns = spawns == null ? null : List.copyOf(spawns);
    }
}
