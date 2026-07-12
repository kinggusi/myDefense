package com.denfense.server.service.balance;

import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import org.springframework.stereotype.Component;

import com.denfense.server.balance.AlienSpecBalance;
import java.util.Collections;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

@Component
public class BalanceRegistry {

    private boolean initialized = false;
    private GameRewardBalance gameRewardBalance;
    private int maxAlienLevel;
    private Map<Integer, AlienUpgradeCostBalance> upgradeCostMap = Collections.emptyMap();
    private Map<Long, AlienSpecBalance> alienSpecMap = Collections.emptyMap();

    public synchronized void init(GameRewardBalance rewardBalance, int maxAlienLevel, Map<Integer, AlienUpgradeCostBalance> costMap, List<AlienSpecBalance> specs) {
        if (this.initialized) {
            throw new IllegalStateException("BalanceRegistry는 이미 초기화되었습니다.");
        }
        if (rewardBalance == null || costMap == null || specs == null) {
            throw new IllegalArgumentException("적재할 데이터가 null입니다.");
        }
        this.gameRewardBalance = rewardBalance;
        this.maxAlienLevel = maxAlienLevel;
        this.upgradeCostMap = Map.copyOf(costMap);
        this.alienSpecMap = specs.stream().collect(Collectors.toUnmodifiableMap(AlienSpecBalance::alienId, Function.identity()));
        this.initialized = true;
    }

    public GameRewardBalance getGameRewardBalance() {
        if (gameRewardBalance == null) {
            throw new IllegalStateException("GameRewardBalance가 로드되지 않았습니다.");
        }
        return gameRewardBalance;
    }

    public int getMaxAlienLevel() {
        if (maxAlienLevel < 2) {
            throw new IllegalStateException("AlienUpgradeBalance가 로드되지 않았습니다.");
        }
        return maxAlienLevel;
    }

    public AlienUpgradeCostBalance getUpgradeCost(int currentLevel) {
        if (currentLevel >= maxAlienLevel) {
            throw new BusinessException(ErrorCode.MAX_ALIEN_LEVEL_REACHED, "최대 레벨에 도달했습니다.");
        }
        AlienUpgradeCostBalance cost = upgradeCostMap.get(currentLevel);
        if (cost == null) {
            throw new IllegalStateException("해당 레벨의 비용 데이터가 없습니다: " + currentLevel);
        }
        return cost;
    }

    public AlienSpecBalance getAlienSpec(long alienId) {
        if (alienSpecMap.isEmpty()) {
            throw new IllegalStateException("AlienSpec 데이터가 로드되지 않았습니다.");
        }
        AlienSpecBalance spec = alienSpecMap.get(alienId);
        if (spec == null) {
            throw new IllegalArgumentException("존재하지 않는 alienId입니다: " + alienId);
        }
        return spec;
    }

    public List<AlienSpecBalance> getAllAlienSpecs() {
        if (alienSpecMap.isEmpty()) {
            throw new IllegalStateException("AlienSpec 데이터가 로드되지 않았습니다.");
        }
        return alienSpecMap.values().stream()
                .sorted(java.util.Comparator.comparingLong(AlienSpecBalance::alienId))
                .toList();
    }
}
