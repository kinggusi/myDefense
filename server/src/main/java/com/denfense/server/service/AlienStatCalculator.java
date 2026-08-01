package com.denfense.server.service;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.balance.AlienSpecBalance;
import com.denfense.server.service.balance.AlienLevelStatBalance;
import com.denfense.server.service.balance.AlienUpgradeBalanceRegistry;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

import java.math.BigDecimal;
import java.math.RoundingMode;

@Component
@RequiredArgsConstructor
public class AlienStatCalculator {

    private final AlienUpgradeBalanceRegistry balanceRegistry;

    public AlienCurrentStat calculate(AlienSpec spec, int level) {
        if (spec == null) {
            throw new IllegalArgumentException("AlienSpec은 null일 수 없습니다.");
        }
        AlienLevelStatBalance multiplier = balanceRegistry.getLevelStat(level);
        return new AlienCurrentStat(
                multiply(spec.getBaseAtk(), multiplier.atkMultiplier(), 2),
                multiply(spec.getBaseMp(), multiplier.mpMultiplier(), 2),
                multiply(spec.getAtkSpeed(), multiplier.atkSpeedMultiplier(), 4),
                multiply(spec.getRange(), multiplier.rangeMultiplier(), 4)
        );
    }

    public AlienCurrentStat calculate(AlienSpecBalance spec, int level) {
        if (spec == null) {
            throw new IllegalArgumentException("AlienSpecBalance must not be null.");
        }
        AlienLevelStatBalance multiplier = balanceRegistry.getLevelStat(level);
        return new AlienCurrentStat(
                multiply(spec.baseAttack(), multiplier.atkMultiplier(), 2),
                multiply(spec.baseMp(), multiplier.mpMultiplier(), 2),
                multiply(spec.attackSpeed(), multiplier.atkSpeedMultiplier(), 4),
                multiply(spec.attackRange(), multiplier.rangeMultiplier(), 4)
        );
    }

    private BigDecimal multiply(int base, BigDecimal multiplier, int scale) {
        return BigDecimal.valueOf(base).multiply(multiplier).setScale(scale, RoundingMode.HALF_UP);
    }

    private BigDecimal multiply(double base, BigDecimal multiplier, int scale) {
        return BigDecimal.valueOf(base).multiply(multiplier).setScale(scale, RoundingMode.HALF_UP);
    }
}
