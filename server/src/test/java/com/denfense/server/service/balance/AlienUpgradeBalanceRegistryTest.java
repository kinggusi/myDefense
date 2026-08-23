package com.denfense.server.service.balance;

import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.jupiter.api.Assertions.assertThrows;

class AlienUpgradeBalanceRegistryTest {

    @Test
    void initializesAtomicallyAndReturnsImmutableData() {
        AlienUpgradeBalanceRegistry registry = new AlienUpgradeBalanceRegistry();
        registry.init(costs(), stats());

        assertThat(registry.getMaxLevel()).isEqualTo(50);
        assertThat(registry.getUpgradeCost(9).requiredGrowthCell()).isEqualTo(10);
        assertThat(registry.getLevelStat(50).atkMultiplier()).isEqualByComparingTo("3.525");
        assertThrows(UnsupportedOperationException.class, () -> registry.getAllUpgradeCosts().clear());
        assertThrows(UnsupportedOperationException.class, () -> registry.getAllLevelStats().clear());
        assertThrows(IllegalStateException.class, () -> registry.init(costs(), stats()));
    }

    @Test
    void rejectsOutOfRangeLookups() {
        AlienUpgradeBalanceRegistry registry = new AlienUpgradeBalanceRegistry();
        registry.init(costs(), stats());
        BusinessException max = assertThrows(BusinessException.class, () -> registry.getUpgradeCost(50));
        assertThat(max.getErrorCode()).isEqualTo(ErrorCode.MAX_ALIEN_LEVEL_REACHED);
        assertThrows(IllegalArgumentException.class, () -> registry.getLevelStat(0));
        assertThrows(IllegalArgumentException.class, () -> registry.getLevelStat(51));
    }

    @Test
    void missingBalanceDataFailsFastWithoutPartiallyInitializing() {
        AlienUpgradeBalanceRegistry registry = new AlienUpgradeBalanceRegistry();
        List<AlienUpgradeCostBalance> missingCosts = new ArrayList<>(costs());
        missingCosts.remove(9);

        assertThrows(IllegalStateException.class, () -> registry.init(missingCosts, stats()));
        assertThrows(IllegalStateException.class, registry::getMaxLevel);

        registry.init(costs(), stats());
        assertThat(registry.getMaxLevel()).isEqualTo(50);
    }

    private List<AlienUpgradeCostBalance> costs() {
        List<AlienUpgradeCostBalance> costs = new ArrayList<>();
        for (int level = 1; level <= 49; level++) {
            costs.add(new AlienUpgradeCostBalance(level, level + 1, level * 5, level * 100,
                    level < 9 ? 0 : Math.min(50, ((level - 9) / 10 + 1) * 10)));
        }
        return costs;
    }

    private List<AlienLevelStatBalance> stats() {
        List<AlienLevelStatBalance> stats = new ArrayList<>();
        for (int level = 1; level <= 50; level++) {
            stats.add(new AlienLevelStatBalance(level,
                    BigDecimal.ONE.add(BigDecimal.valueOf(level - 1).multiply(new BigDecimal("0.045")))
                            .add(BigDecimal.valueOf(Math.min(level / 10, 4)).multiply(new BigDecimal("0.08"))),
                    BigDecimal.ONE.add(BigDecimal.valueOf(level - 1).multiply(new BigDecimal("0.03"))),
                    BigDecimal.ONE.add(BigDecimal.valueOf(level - 1).multiply(new BigDecimal("0.005"))),
                    new BigDecimal("1.00")));
        }
        return stats;
    }
}
