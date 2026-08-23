package com.denfense.server.service.balance;

import com.denfense.server.balance.ResonanceBalance;
import org.springframework.stereotype.Component;

import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

@Component
public class ResonanceBalanceRegistry {
    private Map<Key, ResonanceBalance> byKey = Map.of();

    public synchronized void init(List<ResonanceBalance> balances) {
        if (!byKey.isEmpty()) throw new IllegalStateException("Resonance balance already initialized.");
        byKey = balances.stream().collect(Collectors.toUnmodifiableMap(
                balance -> new Key(balance.track(), balance.level()), Function.identity()));
    }

    public ResonanceBalance get(String track, int level) {
        ResonanceBalance balance = byKey.get(new Key(track, level));
        if (balance == null || !balance.enabled()) {
            throw new IllegalArgumentException("Unknown or disabled resonance balance: " + track + " level " + level);
        }
        return balance;
    }

    public List<ResonanceBalance> getAll() {
        return byKey.values().stream()
                .sorted(Comparator.comparing(ResonanceBalance::track).thenComparingInt(ResonanceBalance::level))
                .toList();
    }

    private record Key(String track, int level) {
    }
}
