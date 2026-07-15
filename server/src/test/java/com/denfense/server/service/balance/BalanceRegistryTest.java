package com.denfense.server.service.balance;

import com.denfense.server.balance.AlienSpecBalance;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.jupiter.api.Assertions.assertThrows;

class BalanceRegistryTest {
    private BalanceRegistry registry;
    private List<AlienSpecBalance> specs;

    @BeforeEach
    void setUp() {
        registry = new BalanceRegistry();
        specs = List.of(
                new AlienSpecBalance(2, "B", "", "NORMAL", 10, 10, 1.0, 1.0, null, false),
                new AlienSpecBalance(1, "A", "", "NORMAL", 10, 10, 1.0, 1.0, null, false));
    }

    @Test
    void returnsAlienSpecsSortedAndImmutable() {
        registry.init(new GameRewardBalance(100, 10, 1000), specs, List.of(), List.of());
        assertThat(registry.getAlienSpec(1).name()).isEqualTo("A");
        assertThat(registry.getAllAlienSpecs()).extracting(AlienSpecBalance::alienId).containsExactly(1L, 2L);
        assertThrows(UnsupportedOperationException.class, () -> registry.getAllAlienSpecs().add(specs.get(0)));
        assertThrows(IllegalArgumentException.class, () -> registry.getAlienSpec(99));
    }

    @Test
    void rejectsDuplicateInitialization() {
        registry.init(new GameRewardBalance(100, 10, 1000), specs, List.of(), List.of());
        assertThrows(IllegalStateException.class,
                () -> registry.init(new GameRewardBalance(100, 10, 1000), specs, List.of(), List.of()));
    }

    @Test
    void alienSpecBalanceRemainsAnImmutableRecord() {
        assertThat(AlienSpecBalance.class.isRecord()).isTrue();
    }

    @Test
    void returnsShopProductsAndPoolsAsImmutableCollections() {
        com.denfense.server.balance.ShopProductBalance product = new com.denfense.server.balance.ShopProductBalance("S1", "Shop", "DIAMOND", 500, 1, "P1", true);
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance("P1", "Pool", true, List.of(
                new com.denfense.server.balance.GachaGradeEntryBalance("NORMAL", 10000, List.of(1L))));
        registry.init(new GameRewardBalance(100, 10, 1000), specs, List.of(product), List.of(pool));
        assertThat(registry.getShopProduct("S1")).isEqualTo(product);
        assertThat(registry.getGachaPool("P1")).isEqualTo(pool);
        assertThrows(IllegalArgumentException.class, () -> registry.getShopProduct("S99"));
        assertThrows(IllegalArgumentException.class, () -> registry.getGachaPool("P99"));
        assertThrows(UnsupportedOperationException.class, () -> registry.getAllShopProducts().clear());
        assertThrows(UnsupportedOperationException.class, () -> registry.getAllGachaPools().clear());
        assertThrows(UnsupportedOperationException.class,
                () -> registry.getAllGachaPools().get(0).gradeEntries().clear());
        assertThrows(UnsupportedOperationException.class,
                () -> registry.getAllGachaPools().get(0).gradeEntries().get(0).alienIds().clear());
    }
}
