package com.denfense.server.service.reward;

import com.denfense.server.balance.BattleRewardBalance;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
public class BattleRewardCalculator {
    public RewardCalculation calculate(BattleRewardBalance balance, String result, int highestClearedWave) {
        int wave = Math.max(0, Math.min(balance.maxWave(), highestClearedWave));
        int gold;
        if ("VICTORY".equals(result) && wave >= balance.maxWave()) {
            gold = balance.failureRewardBaseGold();
        } else if (wave < balance.minimumRewardWave()) {
            gold = 0;
        } else {
            double ratio = (double) wave / balance.maxWave();
            double rewardRate = Math.min(balance.failureRewardCapPercent() / 100.0, Math.pow(ratio, 1.5));
            gold = (int) Math.floor(balance.failureRewardBaseGold() * rewardRate);
        }
        List<BattleRewardBalance.Checkpoint> checkpoints = balance.checkpoints().stream()
                .filter(c -> c.wave() <= wave)
                .toList();
        return new RewardCalculation(wave, gold, checkpoints);
    }

    public record RewardCalculation(int highestClearedWave, int settlementGold,
                                     List<BattleRewardBalance.Checkpoint> reachedCheckpoints) {}
}
