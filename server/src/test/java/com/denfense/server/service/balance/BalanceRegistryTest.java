package com.denfense.server.service.balance;

import com.denfense.server.balance.AlienSpecBalance;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.jupiter.api.Assertions.assertThrows;

public class BalanceRegistryTest {

    private BalanceRegistry registry;
    private GameRewardBalance rewardBalance;
    private Map<Integer, AlienUpgradeCostBalance> costMap;
    private List<AlienSpecBalance> specs;

    @BeforeEach
    void setUp() {
        registry = new BalanceRegistry();
        rewardBalance = new GameRewardBalance(100, 10, 1000);
        costMap = new HashMap<>();
        costMap.put(1, new AlienUpgradeCostBalance(1, 10, 100, 0));

        specs = List.of(
            new AlienSpecBalance(2, "B", "", "NORMAL", 10, 10, 1.0, 1.0, null, false),
            new AlienSpecBalance(1, "A", "", "NORMAL", 10, 10, 1.0, 1.0, null, false)
        );
    }

    @Test
    @DisplayName("getAlienSpec ?•ìƒ ì¡°íšŒ")
    void getAlienSpec() {
        registry.init(rewardBalance, 2, costMap, specs, List.of(), List.of());
        AlienSpecBalance spec = registry.getAlienSpec(1);
        assertThat(spec.name()).isEqualTo("A");
    }

    @Test
    @DisplayName("?†ëŠ” ID ì¡°íšŒ ??ëª…ì‹œ???ˆì™¸")
    void getAlienSpecNotFound() {
        registry.init(rewardBalance, 2, costMap, specs, List.of(), List.of());
        assertThrows(IllegalArgumentException.class, () -> registry.getAlienSpec(99));
    }

    @Test
    @DisplayName("getAllAlienSpecs alienId ?¤ë¦„ì°¨ìˆœ ë°?ë°˜í™˜ List ?˜ì • ë¶ˆê?")
    void getAllAlienSpecsSortedAndImmutable() {
        registry.init(rewardBalance, 2, costMap, specs, List.of(), List.of());
        List<AlienSpecBalance> all = registry.getAllAlienSpecs();

        // ?¤ë¦„ì°¨ìˆœ ê²€ì¦?        assertThat(all.get(0).alienId()).isEqualTo(1);
        assertThat(all.get(1).alienId()).isEqualTo(2);

        // ?˜ì • ë¶ˆê? ê²€ì¦?(UnsupportedOperationException)
        assertThrows(UnsupportedOperationException.class, () -> all.add(new AlienSpecBalance(3, "C", "", "NORMAL", 10, 10, 1.0, 1.0, null, false)));
    }

    @Test
    @DisplayName("AlienSpecBalanceê°€ record ?ëŠ” ë¶ˆë? ê°ì²´?¸ì? ?•ì¸")
    void checkRecord() {
        assertThat(AlienSpecBalance.class.isRecord()).isTrue();
    }

    @Test
    @DisplayName("Registry ì¤‘ë³µ ì´ˆê¸°??ê±°ì ˆ")
    void duplicateInit() {
        registry.init(rewardBalance, 2, costMap, specs, List.of(), List.of());
        assertThrows(IllegalStateException.class, () -> registry.init(rewardBalance, 2, costMap, specs, List.of(), List.of()));
    }

    @Test
    @DisplayName("getShopProduct ?•ìƒ ì¡°íšŒ ë°??†ëŠ” ID ?ˆì™¸")
    void getShopProduct() {
        com.denfense.server.balance.ShopProductBalance product = new com.denfense.server.balance.ShopProductBalance("S1", "??", "DIAMOND", 500, 1, "P1", true);
        registry.init(rewardBalance, 2, costMap, specs, List.of(product), List.of());

        assertThat(registry.getShopProduct("S1").name()).isEqualTo("??");
        assertThrows(IllegalArgumentException.class, () -> registry.getShopProduct("S99"));
    }

    @Test
    @DisplayName("getAllShopProducts ì»¬ë ‰??ë³€ê²?ë¶ˆê?")
    void getAllShopProductsImmutable() {
        com.denfense.server.balance.ShopProductBalance product = new com.denfense.server.balance.ShopProductBalance("S1", "??", "DIAMOND", 500, 1, "P1", true);
        registry.init(rewardBalance, 2, costMap, specs, List.of(product), List.of());

        List<com.denfense.server.balance.ShopProductBalance> all = registry.getAllShopProducts();
        assertThrows(UnsupportedOperationException.class, () -> all.clear());
    }

    @Test
    @DisplayName("getGachaPool ?•ìƒ ì¡°íšŒ ë°??†ëŠ” ID ?ˆì™¸")
    void getGachaPool() {
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance("P1", "?€1", true, List.of());
        registry.init(rewardBalance, 2, costMap, specs, List.of(), List.of(pool));

        assertThat(registry.getGachaPool("P1").name()).isEqualTo("?€1");
        assertThrows(IllegalArgumentException.class, () -> registry.getGachaPool("P99"));
    }

    @Test
    @DisplayName("getAllGachaPools ë°??´ë? ì»¬ë ‰??ë³€ê²?ë¶ˆê?")
    void getAllGachaPoolsImmutable() {
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance("P1", "?€1", true, List.of(
            new com.denfense.server.balance.GachaGradeEntryBalance("NORMAL", 10000, List.of(1L))
        ));
        registry.init(rewardBalance, 2, costMap, specs, List.of(), List.of(pool));

        List<com.denfense.server.balance.GachaPoolBalance> all = registry.getAllGachaPools();
        assertThrows(UnsupportedOperationException.class, () -> all.clear());

        com.denfense.server.balance.GachaPoolBalance fetchedPool = all.get(0);
        assertThrows(UnsupportedOperationException.class, () -> fetchedPool.gradeEntries().clear());

        com.denfense.server.balance.GachaGradeEntryBalance entry = fetchedPool.gradeEntries().get(0);
        assertThrows(UnsupportedOperationException.class, () -> entry.alienIds().clear());
    }
}
