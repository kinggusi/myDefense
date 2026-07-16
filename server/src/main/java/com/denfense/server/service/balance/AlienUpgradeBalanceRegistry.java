package com.denfense.server.service.balance;

import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import org.springframework.stereotype.Component;

import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

@Component
public class AlienUpgradeBalanceRegistry {

    private volatile State state;

    public synchronized void init(List<AlienUpgradeCostBalance> costs, List<AlienLevelStatBalance> levelStats) {
        if (state != null) {
            throw new IllegalStateException("AlienUpgradeBalanceRegistry is already initialized.");
        }
        if (costs == null || levelStats == null) {
            throw new IllegalArgumentException("Alien upgrade balance data must not be null.");
        }

        BalanceDataValidator validator = new BalanceDataValidator();
        validator.validateAlienLevelStats(levelStats);
        int maxLevel = levelStats.stream()
                .mapToInt(AlienLevelStatBalance::level)
                .max()
                .orElseThrow(() -> new IllegalStateException("AlienLevelStat data is empty."));
        validator.validateAlienUpgradeCosts(costs, maxLevel);

        Map<Integer, AlienUpgradeCostBalance> costMap = costs.stream()
                .collect(Collectors.toUnmodifiableMap(AlienUpgradeCostBalance::currentLevel, Function.identity()));
        Map<Integer, AlienLevelStatBalance> statMap = levelStats.stream()
                .collect(Collectors.toUnmodifiableMap(AlienLevelStatBalance::level, Function.identity()));

        state = new State(costMap, statMap, maxLevel);
    }

    public AlienUpgradeCostBalance getUpgradeCost(int currentLevel) {
        State current = requireState();
        if (currentLevel >= current.maxLevel()) {
            throw new BusinessException(ErrorCode.MAX_ALIEN_LEVEL_REACHED, "Maximum Alien level reached.");
        }
        AlienUpgradeCostBalance cost = current.costMap().get(currentLevel);
        if (cost == null) {
            throw new IllegalStateException("Missing Alien upgrade cost for level: " + currentLevel);
        }
        return cost;
    }

    public AlienLevelStatBalance getLevelStat(int level) {
        AlienLevelStatBalance stat = requireState().statMap().get(level);
        if (stat == null) {
            throw new IllegalArgumentException("Missing Alien level stat for level: " + level);
        }
        return stat;
    }

    public int getMaxLevel() {
        return requireState().maxLevel();
    }

    public List<AlienUpgradeCostBalance> getAllUpgradeCosts() {
        return requireState().costMap().values().stream()
                .sorted(Comparator.comparingInt(AlienUpgradeCostBalance::currentLevel))
                .toList();
    }

    public List<AlienLevelStatBalance> getAllLevelStats() {
        return requireState().statMap().values().stream()
                .sorted(Comparator.comparingInt(AlienLevelStatBalance::level))
                .toList();
    }

    private State requireState() {
        State current = state;
        if (current == null) {
            throw new IllegalStateException("Alien upgrade balance is not initialized.");
        }
        return current;
    }

    private record State(
            Map<Integer, AlienUpgradeCostBalance> costMap,
            Map<Integer, AlienLevelStatBalance> statMap,
            int maxLevel
    ) {
    }
}
