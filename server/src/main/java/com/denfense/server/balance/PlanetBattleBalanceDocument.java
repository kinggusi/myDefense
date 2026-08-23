package com.denfense.server.balance;

import java.util.List;

public record PlanetBattleBalanceDocument(List<PlanetBattleBalance> planets) {
    public PlanetBattleBalanceDocument {
        planets = planets == null ? List.of() : List.copyOf(planets);
    }
}
