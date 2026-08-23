package com.denfense.server.service.reward;

import com.denfense.server.balance.BattleRewardBalance;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class BattleRewardCalculatorTest {
    private final BattleRewardCalculator calculator = new BattleRewardCalculator();

    @Test
    void failedWaveUsesHighestFullyClearedWave() {
        var reward = calculator.calculate(balance(), "DEFEAT", 69);
        assertThat(reward.highestClearedWave()).isEqualTo(69);
        assertThat(reward.settlementGold()).isEqualTo((int) Math.floor(10000 * Math.min(.8, Math.pow(69d / 80d, 1.5))));
        assertThat(reward.reachedCheckpoints()).extracting(BattleRewardBalance.Checkpoint::wave)
                .containsExactly(10, 20, 30, 40, 50, 60);
    }

    @Test
    void wave70ClearThenWave71FailureKeepsWave70Checkpoint() {
        var reward = calculator.calculate(balance(), "DEFEAT", 70);
        assertThat(reward.reachedCheckpoints()).extracting(BattleRewardBalance.Checkpoint::wave)
                .containsExactly(10, 20, 30, 40, 50, 60, 70);
    }

    @Test
    void victoryAtWave80UsesRepeatableVictoryGold() {
        var reward = calculator.calculate(balance(), "VICTORY", 80);
        assertThat(reward.settlementGold()).isEqualTo(10000);
        assertThat(reward.reachedCheckpoints()).hasSize(8);
    }

    @Test
    void belowMinimumWaveHasNoPermanentGold() {
        assertThat(calculator.calculate(balance(), "DEFEAT", 9).settlementGold()).isZero();
    }

    @Test
    void waveIsClampedToCanonicalMaximum() {
        assertThat(calculator.calculate(balance(), "DEFEAT", 999).highestClearedWave()).isEqualTo(80);
    }

    private BattleRewardBalance balance() {
        return new BattleRewardBalance(80, 10, 10000, 80,
                List.of(
                        new BattleRewardBalance.Checkpoint(10, 500, 10),
                        new BattleRewardBalance.Checkpoint(20, 750, 15),
                        new BattleRewardBalance.Checkpoint(30, 1000, 20),
                        new BattleRewardBalance.Checkpoint(40, 1500, 25),
                        new BattleRewardBalance.Checkpoint(50, 2000, 30),
                        new BattleRewardBalance.Checkpoint(60, 2500, 35),
                        new BattleRewardBalance.Checkpoint(70, 3000, 40),
                        new BattleRewardBalance.Checkpoint(80, 4000, 50)),
                List.of(new BattleRewardBalance.MapFirstClear("EARTH", 80, 8000)));
    }
}
