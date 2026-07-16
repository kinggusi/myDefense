package com.denfense.server.service.balance;

import com.denfense.server.balance.MonsterSpecBalance;
import org.springframework.stereotype.Component;

import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

@Component
public class MonsterBalanceRegistry {
    private volatile Map<String, MonsterSpecBalance> monsters;

    public synchronized void init(List<MonsterSpecBalance> source) {
        if (monsters != null) throw new IllegalStateException("MonsterBalanceRegistry is already initialized.");
        monsters = source.stream().collect(Collectors.toUnmodifiableMap(MonsterSpecBalance::monsterId, Function.identity()));
    }

    public MonsterSpecBalance getById(String monsterId) {
        MonsterSpecBalance result = requireState().get(monsterId);
        if (result == null) throw new IllegalArgumentException("Unknown monsterId: " + monsterId);
        return result;
    }

    public List<MonsterSpecBalance> getAll() {
        return requireState().values().stream().sorted(Comparator.comparing(MonsterSpecBalance::monsterId)).toList();
    }

    private Map<String, MonsterSpecBalance> requireState() {
        Map<String, MonsterSpecBalance> current = monsters;
        if (current == null) throw new IllegalStateException("Monster balance is not initialized.");
        return current;
    }
}
