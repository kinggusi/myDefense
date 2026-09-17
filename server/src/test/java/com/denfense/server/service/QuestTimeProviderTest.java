package com.denfense.server.service;

import com.denfense.server.balance.QuestCycleType;
import org.junit.jupiter.api.Test;

import java.time.*;

import static org.assertj.core.api.Assertions.assertThat;

class QuestTimeProviderTest {
    private final QuestTimeProvider provider = new QuestTimeProvider();

    @Test
    void dailyCycleChangesAtKstMidnight() {
        ZonedDateTime before = ZonedDateTime.of(2026, 9, 2, 23, 59, 59, 0, QuestTimeProvider.KST);
        ZonedDateTime after = before.plusSeconds(1);

        assertThat(provider.cycleKey(QuestCycleType.DAILY, before)).isEqualTo("2026-09-02");
        assertThat(provider.cycleKey(QuestCycleType.DAILY, after)).isEqualTo("2026-09-03");
        assertThat(provider.nextResetAt(QuestCycleType.DAILY, before))
                .isEqualTo(Instant.parse("2026-09-02T15:00:00Z"));
    }

    @Test
    void weeklyCycleUsesMondayKstAndResetsNextMonday() {
        ZonedDateTime sunday = ZonedDateTime.of(2026, 9, 6, 23, 59, 59, 0, QuestTimeProvider.KST);
        ZonedDateTime monday = sunday.plusSeconds(1);

        assertThat(provider.cycleKey(QuestCycleType.WEEKLY, sunday)).isEqualTo("2026-08-31");
        assertThat(provider.cycleKey(QuestCycleType.WEEKLY, monday)).isEqualTo("2026-09-07");
        assertThat(provider.nextResetAt(QuestCycleType.WEEKLY, sunday))
                .isEqualTo(Instant.parse("2026-09-06T15:00:00Z"));
    }
}
