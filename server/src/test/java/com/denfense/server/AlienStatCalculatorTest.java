package com.denfense.server;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.service.AlienCurrentStat;
import com.denfense.server.service.AlienStatCalculator;
import com.denfense.server.service.balance.AlienLevelStatBalance;
import com.denfense.server.service.balance.AlienUpgradeBalanceRegistry;
import com.denfense.server.service.balance.AlienUpgradeCostBalance;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class AlienStatCalculatorTest {

    @Test
    void calculatesBaseStatsUsingOnlyRegistryMultipliers() {
        AlienUpgradeBalanceRegistry registry = new AlienUpgradeBalanceRegistry();
        registry.init(costs(), stats());
        AlienStatCalculator calculator = new AlienStatCalculator(registry);
        AlienSpec spec = new AlienSpec();
        spec.setBaseAtk(100);
        spec.setBaseMp(80);
        spec.setAtkSpeed(1.25);
        spec.setRange(4.5);

        AlienCurrentStat level20 = calculator.calculate(spec, 20);
        assertThat(level20.currentAtk()).isEqualByComparingTo("201.50");
        assertThat(level20.currentMp()).isEqualByComparingTo("125.60");
        assertThat(level20.currentAtkSpeed()).isEqualByComparingTo("1.3688");
        assertThat(level20.currentRange()).isEqualByComparingTo("4.5000");

        AlienSpec mythicWithSameBaseStats = new AlienSpec();
        mythicWithSameBaseStats.setGrade(AlienSpec.Grade.MYTHIC);
        mythicWithSameBaseStats.setBaseAtk(100);
        mythicWithSameBaseStats.setBaseMp(80);
        mythicWithSameBaseStats.setAtkSpeed(1.25);
        mythicWithSameBaseStats.setRange(4.5);
        assertThat(calculator.calculate(mythicWithSameBaseStats, 20)).isEqualTo(level20);
    }

    private List<AlienUpgradeCostBalance> costs() {
        List<AlienUpgradeCostBalance> result = new ArrayList<>();
        for (int level = 1; level <= 49; level++) result.add(new AlienUpgradeCostBalance(level, level + 1, 1, 1, 0));
        return result;
    }

    private List<AlienLevelStatBalance> stats() {
        List<AlienLevelStatBalance> result = new ArrayList<>();
        for (int level = 1; level <= 50; level++) {
            result.add(new AlienLevelStatBalance(level,
                    BigDecimal.ONE.add(BigDecimal.valueOf(level - 1).multiply(new BigDecimal("0.045")))
                            .add(BigDecimal.valueOf(Math.min(level / 10, 4)).multiply(new BigDecimal("0.08"))),
                    BigDecimal.ONE.add(BigDecimal.valueOf(level - 1).multiply(new BigDecimal("0.03"))),
                    BigDecimal.ONE.add(BigDecimal.valueOf(level - 1).multiply(new BigDecimal("0.005"))),
                    BigDecimal.ONE));
        }
        return result;
    }
}
