package com.denfense.server.service.balance;

import com.denfense.server.balance.DailyBattleStageBalance;
import com.denfense.server.domain.DailyContentType;
import org.springframework.stereotype.Component;

import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Component
public class DailyBattleStageBalanceRegistry {
    private Map<String, List<DailyBattleStageBalance>> byStage = Map.of();

    public synchronized void init(List<DailyBattleStageBalance> rows) {
        if (!byStage.isEmpty()) throw new IllegalStateException("Daily battle stage balance already initialized.");
        byStage = rows.stream()
                .collect(Collectors.groupingBy(
                        row -> key(row.contentType(), row.stage()),
                        Collectors.collectingAndThen(Collectors.toList(), values -> values.stream()
                                .sorted(Comparator.comparingInt(DailyBattleStageBalance::wave))
                                .toList())));
        byStage = Map.copyOf(byStage);
    }

    public List<DailyBattleStageBalance> get(DailyContentType type, int stage) {
        List<DailyBattleStageBalance> rows = byStage.get(key(type.name(), stage));
        if (rows == null || rows.isEmpty()) throw new IllegalArgumentException("Unknown daily battle stage.");
        return rows;
    }

    public List<DailyBattleStageBalance> getAll() {
        return byStage.values().stream().flatMap(List::stream)
                .sorted(Comparator.comparing(DailyBattleStageBalance::contentType)
                        .thenComparingInt(DailyBattleStageBalance::stage)
                        .thenComparingInt(DailyBattleStageBalance::wave))
                .toList();
    }

    private static String key(String contentType, int stage) {
        return contentType + ":" + stage;
    }
}
