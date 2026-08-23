package com.denfense.server;

import com.denfense.server.service.balance.AlienLevelStatBalance;
import com.denfense.server.service.balance.AlienUpgradeCostBalance;
import com.denfense.server.service.balance.BalanceDataValidator;
import com.denfense.server.service.balance.GameRewardBalance;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;

import static org.junit.jupiter.api.Assertions.assertDoesNotThrow;
import static org.junit.jupiter.api.Assertions.assertThrows;

class BalanceDataValidatorTest {
    private final BalanceDataValidator validator = new BalanceDataValidator();

    @Test
    void validatesGameReward() {
        assertDoesNotThrow(() -> validator.validateGameReward(new GameRewardBalance(100, 10, 1000)));
        assertThrows(IllegalStateException.class, () -> validator.validateGameReward(new GameRewardBalance(-1, 10, 1000)));
        assertThrows(IllegalStateException.class, () -> validator.validateGameReward(new GameRewardBalance(100, 10, 50)));
    }

    @Test
    void validatesUpgradeCostContinuityAndValues() {
        List<AlienUpgradeCostBalance> valid = List.of(cost(1, 2, 5, 100, 0), cost(2, 3, 10, 200, 0));
        assertDoesNotThrow(() -> validator.validateAlienUpgradeCosts(valid, 3));
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgradeCosts(List.of(cost(1, 2, 5, 100, 0), cost(1, 2, 10, 200, 0)), 3));
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgradeCosts(List.of(cost(1, 2, 5, 100, 0), cost(3, 4, 10, 200, 0)), 4));
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgradeCosts(List.of(cost(1, 3, 5, 100, 0)), 2));
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgradeCosts(List.of(cost(1, 2, 0, 100, 0)), 2));
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgradeCosts(List.of(cost(1, 2, 5, 0, 0)), 2));
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgradeCosts(List.of(cost(1, 2, 5, 100, -1)), 2));
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgradeCosts(valid, 2));
    }

    @Test
    void validatesLevelStats() {
        List<AlienLevelStatBalance> valid = validStats();
        assertDoesNotThrow(() -> validator.validateAlienLevelStats(valid));
        List<AlienLevelStatBalance> duplicate = new ArrayList<>(valid);
        duplicate.set(49, duplicate.get(48));
        assertThrows(IllegalStateException.class, () -> validator.validateAlienLevelStats(duplicate));
        List<AlienLevelStatBalance> zero = new ArrayList<>(valid);
        zero.set(9, stat(10, "0.00", "1.27", "1.02", "1.00"));
        assertThrows(IllegalStateException.class, () -> validator.validateAlienLevelStats(zero));
        List<AlienLevelStatBalance> invalidFirst = new ArrayList<>(valid);
        invalidFirst.set(0, stat(1, "1.05", "1.00", "1.00", "1.00"));
        assertThrows(IllegalStateException.class, () -> validator.validateAlienLevelStats(invalidFirst));
        List<AlienLevelStatBalance> invalidRange = new ArrayList<>(valid);
        invalidRange.set(1, stat(2, "1.05", "1.03", "1.00", "1.01"));
        assertThrows(IllegalStateException.class, () -> validator.validateAlienLevelStats(invalidRange));
        assertDoesNotThrow(() -> validator.validateAlienLevelStats(valid.subList(0, 49)));
    }

    @Test
    void validatesGachaPool() {
        List<com.denfense.server.balance.AlienSpecBalance> specs = List.of(
                spec(1L, "NORMAL"), spec(29L, "MYTHIC"));
        com.denfense.server.balance.GachaPoolBalance pool = pool("P1", true, List.of(
                entry("NORMAL", 9950, List.of(1L)),
                entry("MYTHIC", 50, List.of(29L))));

        assertDoesNotThrow(() -> validator.validateGachaPool(poolDocument(pool), specs));
    }

    @Test
    void rejectsDuplicateGachaPoolId() {
        com.denfense.server.balance.GachaPoolBalance pool = pool("P1", false, List.of());
        assertThrows(IllegalStateException.class,
                () -> validator.validateGachaPool(poolDocument(pool, pool), List.of()));
    }

    @Test
    void rejectsDuplicateGradeInGachaPool() {
        List<com.denfense.server.balance.AlienSpecBalance> specs = List.of(
                spec(1L, "NORMAL"), spec(2L, "NORMAL"));
        com.denfense.server.balance.GachaPoolBalance pool = pool("P1", false, List.of(
                entry("NORMAL", 5000, List.of(1L)),
                entry("NORMAL", 5000, List.of(2L))));

        assertThrows(IllegalStateException.class,
                () -> validator.validateGachaPool(poolDocument(pool), specs));
    }

    @Test
    void rejectsInvalidActiveGachaPoolWeightSum() {
        com.denfense.server.balance.GachaPoolBalance pool = pool("P1", true, List.of(
                entry("NORMAL", 9999, List.of(1L))));

        assertThrows(IllegalStateException.class,
                () -> validator.validateGachaPool(poolDocument(pool), List.of(spec(1L, "NORMAL"))));
    }

    @Test
    void rejectsMissingAlienInGachaPool() {
        com.denfense.server.balance.GachaPoolBalance pool = pool("P1", false, List.of(
                entry("NORMAL", 10000, List.of(99L))));

        assertThrows(IllegalStateException.class,
                () -> validator.validateGachaPool(poolDocument(pool), List.of()));
    }

    @Test
    void rejectsAlienGradeMismatchInGachaPool() {
        com.denfense.server.balance.GachaPoolBalance pool = pool("P1", false, List.of(
                entry("NORMAL", 10000, List.of(1L))));

        assertThrows(IllegalStateException.class,
                () -> validator.validateGachaPool(poolDocument(pool), List.of(spec(1L, "EPIC"))));
    }

    @Test
    void rejectsDuplicateAlienInGachaEntry() {
        com.denfense.server.balance.GachaPoolBalance pool = pool("P1", false, List.of(
                entry("NORMAL", 10000, List.of(1L, 1L))));

        assertThrows(IllegalStateException.class,
                () -> validator.validateGachaPool(poolDocument(pool), List.of(spec(1L, "NORMAL"))));
    }

    @Test
    void rejectsDuplicateAlienAcrossGachaPoolEntries() {
        com.denfense.server.balance.GachaPoolBalance pool = pool("P1", false, List.of(
                entry("NORMAL", 5000, List.of(1L)),
                entry("EPIC", 5000, List.of(1L))));

        assertThrows(IllegalStateException.class,
                () -> validator.validateGachaPool(poolDocument(pool), List.of(spec(1L, "NORMAL"))));
    }

    @Test
    void validatesShopProduct() {
        com.denfense.server.balance.GachaPoolBalanceDocument pools = poolDocument(pool("P1", false, List.of()));
        com.denfense.server.balance.ShopProductBalance product = product("S1", "DIAMOND", "P1");

        assertDoesNotThrow(() -> validator.validateShopProduct(productDocument(product), pools));
    }

    @Test
    void rejectsDuplicateShopProductId() {
        com.denfense.server.balance.GachaPoolBalanceDocument pools = poolDocument(pool("P1", false, List.of()));
        com.denfense.server.balance.ShopProductBalance product = product("S1", "DIAMOND", "P1");

        assertThrows(IllegalStateException.class,
                () -> validator.validateShopProduct(productDocument(product, product), pools));
    }

    @Test
    void rejectsInvalidShopCurrency() {
        com.denfense.server.balance.GachaPoolBalanceDocument pools = poolDocument(pool("P1", false, List.of()));

        assertThrows(IllegalStateException.class,
                () -> validator.validateShopProduct(productDocument(product("S1", "INVALID", "P1")), pools));
    }

    @Test
    void rejectsMissingShopGachaPool() {
        assertThrows(IllegalStateException.class,
                () -> validator.validateShopProduct(
                        productDocument(product("S1", "DIAMOND", "P1")), poolDocument()));
    }

    private AlienUpgradeCostBalance cost(int current, int target, int pieces, int gold, int cell) {
        return new AlienUpgradeCostBalance(current, target, pieces, gold, cell);
    }

    private List<AlienLevelStatBalance> validStats() {
        List<AlienLevelStatBalance> stats = new ArrayList<>();
        for (int level = 1; level <= 50; level++) {
            BigDecimal atk = BigDecimal.ONE
                    .add(BigDecimal.valueOf(level - 1).multiply(new BigDecimal("0.045")))
                    .add(BigDecimal.valueOf(Math.min(level / 10, 4)).multiply(new BigDecimal("0.08")));
            BigDecimal mp = BigDecimal.ONE.add(BigDecimal.valueOf(level - 1).multiply(new BigDecimal("0.03")));
            BigDecimal speed = BigDecimal.ONE.add(BigDecimal.valueOf(level - 1).multiply(new BigDecimal("0.005")));
            stats.add(new AlienLevelStatBalance(level, atk, mp, speed, new BigDecimal("1.00")));
        }
        return stats;
    }

    private AlienLevelStatBalance stat(int level, String atk, String mp, String speed, String range) {
        return new AlienLevelStatBalance(level, new BigDecimal(atk), new BigDecimal(mp), new BigDecimal(speed), new BigDecimal(range));
    }

    private com.denfense.server.balance.AlienSpecBalance spec(long id, String grade) {
        return new com.denfense.server.balance.AlienSpecBalance(
                id, "Alien-" + id, "", grade, 10, 10, 1.0, 1.0, null, false);
    }

    private com.denfense.server.balance.GachaGradeEntryBalance entry(
            String grade, int weight, List<Long> alienIds) {
        return new com.denfense.server.balance.GachaGradeEntryBalance(grade, weight, alienIds);
    }

    private com.denfense.server.balance.GachaPoolBalance pool(
            String id, boolean active, List<com.denfense.server.balance.GachaGradeEntryBalance> entries) {
        return new com.denfense.server.balance.GachaPoolBalance(id, "Pool", active, entries);
    }

    private com.denfense.server.balance.GachaPoolBalanceDocument poolDocument(
            com.denfense.server.balance.GachaPoolBalance... pools) {
        return new com.denfense.server.balance.GachaPoolBalanceDocument(List.of(pools));
    }

    private com.denfense.server.balance.ShopProductBalance product(
            String id, String currency, String poolId) {
        return new com.denfense.server.balance.ShopProductBalance(
                id, "Product", currency, 500, 1, poolId, true);
    }

    private com.denfense.server.balance.ShopProductBalanceDocument productDocument(
            com.denfense.server.balance.ShopProductBalance... products) {
        return new com.denfense.server.balance.ShopProductBalanceDocument(List.of(products));
    }
}
