package com.denfense.server.service.balance;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.assertDoesNotThrow;
import static org.junit.jupiter.api.Assertions.assertThrows;

class BalanceDataValidatorTest {

    private final BalanceDataValidator validator = new BalanceDataValidator();

    @Test
    @DisplayName("GameReward 정상")
    void validateGameReward_valid() {
        GameRewardBalance balance = new GameRewardBalance(100, 10, 1000);
        assertDoesNotThrow(() -> validator.validateGameReward(balance));
    }

    @Test
    @DisplayName("GameReward 음수 예외")
    void validateGameReward_negative() {
        GameRewardBalance balance = new GameRewardBalance(-100, 10, 1000);
        assertThrows(IllegalStateException.class, () -> validator.validateGameReward(balance));
    }

    @Test
    @DisplayName("GameReward max < base 예외")
    void validateGameReward_maxLess() {
        GameRewardBalance balance = new GameRewardBalance(100, 10, 50);
        assertThrows(IllegalStateException.class, () -> validator.validateGameReward(balance));
    }

    @Test
    @DisplayName("AlienUpgrade 정상")
    void validateAlienUpgrade_valid() {
        List<AlienUpgradeCostBalance> costs = List.of(
                new AlienUpgradeCostBalance(1, 5, 100, 0),
                new AlienUpgradeCostBalance(2, 10, 200, 0)
        );
        AlienUpgradeBalanceFile file = new AlienUpgradeBalanceFile(3, costs);
        assertDoesNotThrow(() -> validator.validateAlienUpgrade(file));
    }

    @Test
    @DisplayName("AlienUpgrade 중복 레벨 예외")
    void validateAlienUpgrade_dupLevel() {
        List<AlienUpgradeCostBalance> costs = List.of(
                new AlienUpgradeCostBalance(1, 5, 100, 0),
                new AlienUpgradeCostBalance(1, 10, 200, 0)
        );
        AlienUpgradeBalanceFile file = new AlienUpgradeBalanceFile(3, costs);
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgrade(file));
    }

    @Test
    @DisplayName("AlienUpgrade 누락 레벨 예외")
    void validateAlienUpgrade_missingLevel() {
        List<AlienUpgradeCostBalance> costs = List.of(
                new AlienUpgradeCostBalance(1, 5, 100, 0),
                new AlienUpgradeCostBalance(3, 10, 200, 0)
        );
        AlienUpgradeBalanceFile file = new AlienUpgradeBalanceFile(4, costs);
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgrade(file));
    }

    @Test
    @DisplayName("AlienUpgrade 음수 비용 예외")
    void validateAlienUpgrade_negativeCost() {
        List<AlienUpgradeCostBalance> costs = List.of(
                new AlienUpgradeCostBalance(1, -5, 100, 0)
        );
        AlienUpgradeBalanceFile file = new AlienUpgradeBalanceFile(2, costs);
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgrade(file));
    }

    @Test
    @DisplayName("AlienUpgrade 배열 크기 불일치 예외")
    void validateAlienUpgrade_sizeMismatch() {
        List<AlienUpgradeCostBalance> costs = List.of(
                new AlienUpgradeCostBalance(1, 5, 100, 0)
        );
        AlienUpgradeBalanceFile file = new AlienUpgradeBalanceFile(3, costs); // expects 2 elements
        assertThrows(IllegalStateException.class, () -> validator.validateAlienUpgrade(file));
    }
}
