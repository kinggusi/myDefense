package com.denfense.server.service.balance;

import org.springframework.stereotype.Component;

import java.util.HashSet;
import java.util.List;
import java.util.Set;

@Component
public class BalanceDataValidator {

    public void validateGameReward(GameRewardBalance balance) {
        if (balance == null) {
            throw new IllegalStateException("GameRewardBalance가 null입니다.");
        }
        if (balance.baseRewardGold() < 0) {
            throw new IllegalStateException("baseRewardGold는 0 이상이어야 합니다.");
        }
        if (balance.goldPerWave() < 0) {
            throw new IllegalStateException("goldPerWave는 0 이상이어야 합니다.");
        }
        if (balance.maxRewardGold() < 0) {
            throw new IllegalStateException("maxRewardGold는 0 이상이어야 합니다.");
        }
        if (balance.maxRewardGold() < balance.baseRewardGold()) {
            throw new IllegalStateException("maxRewardGold는 baseRewardGold 이상이어야 합니다.");
        }
    }

    public void validateAlienUpgrade(AlienUpgradeBalanceFile file) {
        if (file == null) {
            throw new IllegalStateException("AlienUpgradeBalanceFile이 null입니다.");
        }
        if (file.maxLevel() < 2) {
            throw new IllegalStateException("maxLevel은 2 이상이어야 합니다.");
        }
        List<AlienUpgradeCostBalance> costs = file.costs();
        if (costs == null || costs.isEmpty()) {
            throw new IllegalStateException("costs 배열이 null이거나 비어 있습니다.");
        }
        if (costs.size() != file.maxLevel() - 1) {
            throw new IllegalStateException("costs 배열 크기는 maxLevel - 1 이어야 합니다. 기대: " + (file.maxLevel() - 1) + ", 실제: " + costs.size());
        }

        Set<Integer> levels = new HashSet<>();
        for (AlienUpgradeCostBalance cost : costs) {
            if (cost.currentLevel() < 1 || cost.currentLevel() >= file.maxLevel()) {
                throw new IllegalStateException("currentLevel은 1부터 maxLevel - 1 사이여야 합니다: " + cost.currentLevel());
            }
            if (!levels.add(cost.currentLevel())) {
                throw new IllegalStateException("중복된 currentLevel이 존재합니다: " + cost.currentLevel());
            }
            if (cost.requiredPieces() < 0 || cost.requiredGold() < 0 || cost.requiredGrowthCell() < 0) {
                throw new IllegalStateException("요구 비용은 0 이상이어야 합니다. 레벨: " + cost.currentLevel());
            }
        }

        for (int i = 1; i < file.maxLevel(); i++) {
            if (!levels.contains(i)) {
                throw new IllegalStateException("누락된 currentLevel이 존재합니다: " + i);
            }
        }
    }
}
