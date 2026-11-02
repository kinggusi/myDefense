package com.denfense.server.service;

import com.denfense.server.balance.QuestCycleType;
import org.springframework.stereotype.Component;

import java.time.*;
import java.time.temporal.TemporalAdjusters;

@Component
public class QuestTimeProvider {
    public static final ZoneId KST = ZoneId.of("Asia/Seoul");

    public ZonedDateTime now() {
        return ZonedDateTime.now(KST);
    }

    public String cycleKey(QuestCycleType type) {
        return cycleKey(type, now());
    }

    String cycleKey(QuestCycleType type, ZonedDateTime current) {
        LocalDate today = current.withZoneSameInstant(KST).toLocalDate();
        return switch (type) {
            case DAILY -> today.toString();
            case WEEKLY -> today.with(TemporalAdjusters.previousOrSame(DayOfWeek.MONDAY)).toString();
        };
    }

    public Instant nextResetAt(QuestCycleType type) {
        return nextResetAt(type, now());
    }

    Instant nextResetAt(QuestCycleType type, ZonedDateTime current) {
        current = current.withZoneSameInstant(KST);
        return switch (type) {
            case DAILY -> current.toLocalDate().plusDays(1).atStartOfDay(KST).toInstant();
            case WEEKLY -> current.toLocalDate()
                    .with(TemporalAdjusters.next(DayOfWeek.MONDAY)).atStartOfDay(KST).toInstant();
        };
    }
}
