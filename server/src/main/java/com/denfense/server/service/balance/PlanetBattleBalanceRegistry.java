package com.denfense.server.service.balance;

import com.denfense.server.balance.PlanetBattleBalance;
import org.springframework.stereotype.Component;

import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

@Component
public class PlanetBattleBalanceRegistry {
    private Map<String, PlanetBattleBalance> byMapId = Map.of();

    public synchronized void init(List<PlanetBattleBalance> planets) {
        if (!byMapId.isEmpty()) throw new IllegalStateException("Planet Battle balance already initialized.");
        byMapId = planets.stream().collect(Collectors.toUnmodifiableMap(PlanetBattleBalance::mapId, Function.identity()));
    }

    public PlanetBattleBalance get(String mapId) {
        PlanetBattleBalance balance = byMapId.get(mapId);
        if (balance == null || !balance.enabled()) throw new IllegalArgumentException("Unknown or disabled mapId: " + mapId);
        return balance;
    }

    public List<PlanetBattleBalance> getAll() {
        return byMapId.values().stream().sorted(Comparator.comparingInt(PlanetBattleBalance::order)).toList();
    }
}
