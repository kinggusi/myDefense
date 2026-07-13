package com.denfense.server.service.balance;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.assertDoesNotThrow;
import static org.junit.jupiter.api.Assertions.assertThrows;

class BalanceDataValidatorTest {

    private final BalanceDataValidator validator = new BalanceDataValidator();

    @Test
    @DisplayName("GameReward ?•ìƒ")
    void validateGameReward_valid() {
        GameRewardBalance balance = new GameRewardBalance(100, 10, 1000);
        assertDoesNotThrow(() -> validator.validateGameReward(balance));
    }

    @Test
    @DisplayName("GameReward ?Œìˆ˜ ?ˆì™¸")
    void validateGameReward_negative() {
        GameRewardBalance balance = new GameRewardBalance(-100, 10, 1000);
        assertThrows(IllegalStateException.class, () -> validator.validateGameReward(balance));
    }

    @Test
    @DisplayName("GameReward max < base ?ˆì™¸")
    void validateGameReward_maxLess() {
        GameRewardBalance balance = new GameRewardBalance(100, 10, 50);
        assertThrows(IllegalStateException.class, () -> validator.validateGameReward(balance));
    }

    @Test
    @DisplayName("AlienUpgrade ?•ìƒ")
    void validateAlienUpgrade_valid() {
        List<AlienUpgradeCostBalance> costs = List.of(
                new AlienUpgradeCostBalance(1, 5, 100, 0),
                new AlienUpgradeCostBalance(2, 10, 200, 0)
        );
        AlienUpgradeBalanceFile file = new AlienUpgradeBalanceFile(3, costs);
        assertDoesNotThrow(() -> validator.validateAlienUpgrade(file));
    }

    @Test
    @DisplayName("AlienUpgrade ì¤‘ë³µ ?ˆë²¨ ?ˆì™¸")
    void validateAlienUpgrade_dupLevel() {
        List<AlienUpgradeCostBalance> costs = List.of(
                new AlienUpgradeCostBalance(1, 5, 100, 0),
                new AlienUpgradeCostBalance(1, 10, 200, 0)
        );
        AlienUpgradeBalanceFile file = new AlienUpgradeBalanceFile(3, costs);
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgrade(file));
    }

    @Test
    @DisplayName("AlienUpgrade ?„ë½ ?ˆë²¨ ?ˆì™¸")
    void validateAlienUpgrade_missingLevel() {
        List<AlienUpgradeCostBalance> costs = List.of(
                new AlienUpgradeCostBalance(1, 5, 100, 0),
                new AlienUpgradeCostBalance(3, 10, 200, 0)
        );
        AlienUpgradeBalanceFile file = new AlienUpgradeBalanceFile(4, costs);
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgrade(file));
    }

    @Test
    @DisplayName("AlienUpgrade ?Œìˆ˜ ë¹„ìš© ?ˆì™¸")
    void validateAlienUpgrade_negativeCost() {
        List<AlienUpgradeCostBalance> costs = List.of(
                new AlienUpgradeCostBalance(1, -5, 100, 0)
        );
        AlienUpgradeBalanceFile file = new AlienUpgradeBalanceFile(2, costs);
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgrade(file));
    }

    @Test
    @DisplayName("AlienUpgrade ë°°ì—´ ?¬ê¸° ë¶ˆì¼ì¹??ˆì™¸")
    void validateAlienUpgrade_sizeMismatch() {
        List<AlienUpgradeCostBalance> costs = List.of(
                new AlienUpgradeCostBalance(1, 5, 100, 0)
        );
        AlienUpgradeBalanceFile file = new AlienUpgradeBalanceFile(3, costs); // expects 2 elements
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgrade(file));
    }

    @Test
    @DisplayName("GachaPool ?•ìƒ")
    void validateGachaPool_valid() {
        List<com.denfense.server.balance.AlienSpecBalance> specs = List.of(
            new com.denfense.server.balance.AlienSpecBalance(1L, "A", "", "NORMAL", 10, 10, 1.0, 1.0, null, false),
            new com.denfense.server.balance.AlienSpecBalance(29L, "Mythic", "", "MYTHIC", 100, 100, 1.0, 1.0, null, true)
        );
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance(
            "P1", "?€1", true, List.of(
                new com.denfense.server.balance.GachaGradeEntryBalance("NORMAL", 9950, List.of(1L)),
                new com.denfense.server.balance.GachaGradeEntryBalance("MYTHIC", 50, List.of(29L))
            )
        );
        com.denfense.server.balance.GachaPoolBalanceDocument doc = new com.denfense.server.balance.GachaPoolBalanceDocument(List.of(pool));
        assertDoesNotThrow(() -> validator.validateGachaPool(doc, specs));
    }

    @Test
    @DisplayName("GachaPool ID ì¤‘ë³µ ?¤íŒ¨")
    void validateGachaPool_dupPoolId() {
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance("P1", "?€1", false, List.of());
        com.denfense.server.balance.GachaPoolBalanceDocument doc = new com.denfense.server.balance.GachaPoolBalanceDocument(List.of(pool, pool));
        assertThrows(IllegalStateException.class, () -> validator.validateGachaPool(doc, List.of()));
    }

    @Test
    @DisplayName("GachaPool grade ì¤‘ë³µ ?¤íŒ¨")
    void validateGachaPool_dupGrade() {
        List<com.denfense.server.balance.AlienSpecBalance> specs = List.of(
            new com.denfense.server.balance.AlienSpecBalance(1L, "A", "", "NORMAL", 10, 10, 1.0, 1.0, null, false),
            new com.denfense.server.balance.AlienSpecBalance(2L, "B", "", "NORMAL", 10, 10, 1.0, 1.0, null, false)
        );
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance(
            "P1", "?€", false, List.of(
                new com.denfense.server.balance.GachaGradeEntryBalance("NORMAL", 5000, List.of(1L)),
                new com.denfense.server.balance.GachaGradeEntryBalance("NORMAL", 5000, List.of(2L))
            )
        );
        com.denfense.server.balance.GachaPoolBalanceDocument doc = new com.denfense.server.balance.GachaPoolBalanceDocument(List.of(pool));
        assertThrows(IllegalStateException.class, () -> validator.validateGachaPool(doc, specs));
    }

    @Test
    @DisplayName("GachaPool weight ì´í•© ë¶ˆì¼ì¹??¤íŒ¨")
    void validateGachaPool_weightSum() {
        List<com.denfense.server.balance.AlienSpecBalance> specs = List.of(
            new com.denfense.server.balance.AlienSpecBalance(1L, "A", "", "NORMAL", 10, 10, 1.0, 1.0, null, false)
        );
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance(
            "P1", "?€", true, List.of(
                new com.denfense.server.balance.GachaGradeEntryBalance("NORMAL", 9999, List.of(1L))
            )
        );
        com.denfense.server.balance.GachaPoolBalanceDocument doc = new com.denfense.server.balance.GachaPoolBalanceDocument(List.of(pool));
        assertThrows(IllegalStateException.class, () -> validator.validateGachaPool(doc, specs));
    }

    @Test
    @DisplayName("GachaPool ?†ëŠ” alienId ?¤íŒ¨")
    void validateGachaPool_missingAlien() {
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance(
            "P1", "?€", false, List.of(
                new com.denfense.server.balance.GachaGradeEntryBalance("NORMAL", 10000, List.of(99L))
            )
        );
        com.denfense.server.balance.GachaPoolBalanceDocument doc = new com.denfense.server.balance.GachaPoolBalanceDocument(List.of(pool));
        assertThrows(IllegalStateException.class, () -> validator.validateGachaPool(doc, List.of()));
    }

    @Test
    @DisplayName("GachaPool AlienSpec grade ë¶ˆì¼ì¹??¤íŒ¨")
    void validateGachaPool_gradeMismatch() {
        List<com.denfense.server.balance.AlienSpecBalance> specs = List.of(
            new com.denfense.server.balance.AlienSpecBalance(1L, "A", "", "EPIC", 10, 10, 1.0, 1.0, null, false)
        );
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance(
            "P1", "?€", false, List.of(
                new com.denfense.server.balance.GachaGradeEntryBalance("NORMAL", 10000, List.of(1L))
            )
        );
        com.denfense.server.balance.GachaPoolBalanceDocument doc = new com.denfense.server.balance.GachaPoolBalanceDocument(List.of(pool));
        assertThrows(IllegalStateException.class, () -> validator.validateGachaPool(doc, specs));
    }

    @Test
    @DisplayName("GachaPool ?™ì¼ entry alienId ì¤‘ë³µ ?¤íŒ¨")
    void validateGachaPool_dupAlienInEntry() {
        List<com.denfense.server.balance.AlienSpecBalance> specs = List.of(
            new com.denfense.server.balance.AlienSpecBalance(1L, "A", "", "NORMAL", 10, 10, 1.0, 1.0, null, false)
        );
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance(
            "P1", "?€", false, List.of(
                new com.denfense.server.balance.GachaGradeEntryBalance("NORMAL", 10000, List.of(1L, 1L))
            )
        );
        com.denfense.server.balance.GachaPoolBalanceDocument doc = new com.denfense.server.balance.GachaPoolBalanceDocument(List.of(pool));
        assertThrows(IllegalStateException.class, () -> validator.validateGachaPool(doc, specs));
    }

    @Test
    @DisplayName("GachaPool ?™ì¼ Pool ?„ì²´ alienId ì¤‘ë³µ ?¤íŒ¨")
    void validateGachaPool_dupAlienInPool() {
        List<com.denfense.server.balance.AlienSpecBalance> specs = List.of(
            new com.denfense.server.balance.AlienSpecBalance(1L, "A", "", "NORMAL", 10, 10, 1.0, 1.0, null, false)
        );
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance(
            "P1", "?€", false, List.of(
                new com.denfense.server.balance.GachaGradeEntryBalance("NORMAL", 5000, List.of(1L)),
                new com.denfense.server.balance.GachaGradeEntryBalance("EPIC", 5000, List.of(1L)) // grade validation will fail first or alien validation
            )
        );
        com.denfense.server.balance.GachaPoolBalanceDocument doc = new com.denfense.server.balance.GachaPoolBalanceDocument(List.of(pool));
        assertThrows(IllegalStateException.class, () -> validator.validateGachaPool(doc, specs));
    }

    @Test
    @DisplayName("ShopProduct ?•ìƒ")
    void validateShopProduct_valid() {
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance("P1", "?€1", false, List.of());
        com.denfense.server.balance.GachaPoolBalanceDocument poolDoc = new com.denfense.server.balance.GachaPoolBalanceDocument(List.of(pool));

        com.denfense.server.balance.ShopProductBalance product = new com.denfense.server.balance.ShopProductBalance(
            "S1", "??", "DIAMOND", 500, 1, "P1", true
        );
        com.denfense.server.balance.ShopProductBalanceDocument doc = new com.denfense.server.balance.ShopProductBalanceDocument(List.of(product));
        assertDoesNotThrow(() -> validator.validateShopProduct(doc, poolDoc));
    }

    @Test
    @DisplayName("ShopProduct ?í’ˆ ID ì¤‘ë³µ ?¤íŒ¨")
    void validateShopProduct_dupId() {
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance("P1", "?€1", false, List.of());
        com.denfense.server.balance.GachaPoolBalanceDocument poolDoc = new com.denfense.server.balance.GachaPoolBalanceDocument(List.of(pool));

        com.denfense.server.balance.ShopProductBalance product = new com.denfense.server.balance.ShopProductBalance("S1", "??", "DIAMOND", 500, 1, "P1", true);
        com.denfense.server.balance.ShopProductBalanceDocument doc = new com.denfense.server.balance.ShopProductBalanceDocument(List.of(product, product));
        assertThrows(IllegalStateException.class, () -> validator.validateShopProduct(doc, poolDoc));
    }

    @Test
    @DisplayName("ShopProduct ?˜ëª»??currencyType ?¤íŒ¨")
    void validateShopProduct_invalidCurrency() {
        com.denfense.server.balance.GachaPoolBalance pool = new com.denfense.server.balance.GachaPoolBalance("P1", "?€1", false, List.of());
        com.denfense.server.balance.GachaPoolBalanceDocument poolDoc = new com.denfense.server.balance.GachaPoolBalanceDocument(List.of(pool));

        com.denfense.server.balance.ShopProductBalance product = new com.denfense.server.balance.ShopProductBalance("S1", "??", "INVALID", 500, 1, "P1", true);
        com.denfense.server.balance.ShopProductBalanceDocument doc = new com.denfense.server.balance.ShopProductBalanceDocument(List.of(product));
        assertThrows(IllegalStateException.class, () -> validator.validateShopProduct(doc, poolDoc));
    }

    @Test
    @DisplayName("ShopProduct ?†ëŠ” gachaPoolId ?¤íŒ¨")
    void validateShopProduct_missingPool() {
        com.denfense.server.balance.GachaPoolBalanceDocument poolDoc = new com.denfense.server.balance.GachaPoolBalanceDocument(List.of());
        com.denfense.server.balance.ShopProductBalance product = new com.denfense.server.balance.ShopProductBalance("S1", "??", "DIAMOND", 500, 1, "P1", true);
        com.denfense.server.balance.ShopProductBalanceDocument doc = new com.denfense.server.balance.ShopProductBalanceDocument(List.of(product));
        assertThrows(IllegalStateException.class, () -> validator.validateShopProduct(doc, poolDoc));
    }
}
