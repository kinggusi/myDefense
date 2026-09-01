package com.denfense.server.service.balance;

import com.denfense.server.balance.DailyContentBalance;
import com.denfense.server.domain.DailyContentType;
import org.springframework.stereotype.Component;

import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

@Component
public class DailyContentBalanceRegistry {
    private Map<String, DailyContentBalance> byKey = Map.of();

    public synchronized void init(List<DailyContentBalance> contents) {
        if (!byKey.isEmpty()) throw new IllegalStateException("Daily content balance already initialized.");
        byKey = contents.stream().collect(Collectors.toUnmodifiableMap(
                value -> key(value.contentType(), value.stage()), Function.identity()));
    }

    public DailyContentBalance get(DailyContentType type, int stage) {
        DailyContentBalance balance = byKey.get(key(type.name(), stage));
        if (balance == null || !balance.enabled()) throw new IllegalArgumentException("Unknown daily content stage.");
        return balance;
    }

    public List<DailyContentBalance> getAll() {
        return byKey.values().stream()
                .sorted(Comparator.comparing(DailyContentBalance::contentType).thenComparingInt(DailyContentBalance::stage))
                .toList();
    }

    private static String key(String type, int stage) {
        return type + ":" + stage;
    }
}
