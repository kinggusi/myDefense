package com.denfense.server.service.balance;

import com.denfense.server.balance.ResonanceBalance;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class ResonanceBalanceRegistryTest {

    @Test
    void initializesAndReturnsImmutableCanonicalRows() {
        ResonanceBalanceRegistry registry = new ResonanceBalanceRegistry();
        registry.init(List.of(
                row("NORMAL", 1, 400, "1.05", "1.01"),
                row("MYTHIC", 1, 800, "1.08", "1.01")
        ));

        assertThat(registry.get("NORMAL", 1).requiredGold()).isEqualTo(400);
        assertThat(registry.get("MYTHIC", 1).attackMultiplier()).isEqualByComparingTo("1.08");
        assertThatThrownBy(() -> registry.getAll().add(row("NORMAL", 2, 800, "1.10", "1.02")))
                .isInstanceOf(UnsupportedOperationException.class);
    }

    @Test
    void rejectsUnknownOrDisabledRowsAndRepeatedInitialization() {
        ResonanceBalanceRegistry registry = new ResonanceBalanceRegistry();
        registry.init(List.of(new ResonanceBalance("NORMAL", 1, 400,
                new BigDecimal("1.05"), new BigDecimal("1.01"), BigDecimal.ONE, false)));

        assertThatThrownBy(() -> registry.get("NORMAL", 1)).isInstanceOf(IllegalArgumentException.class);
        assertThatThrownBy(() -> registry.get("MYTHIC", 1)).isInstanceOf(IllegalArgumentException.class);
        assertThatThrownBy(() -> registry.init(List.of(row("NORMAL", 1, 400, "1.05", "1.01"))))
                .isInstanceOf(IllegalStateException.class);
    }

    private ResonanceBalance row(String track, int level, int cost, String attack, String speed) {
        return new ResonanceBalance(track, level, cost, new BigDecimal(attack), new BigDecimal(speed), BigDecimal.ONE, true);
    }
}
