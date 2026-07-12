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

    public void validateAlienSpec(List<com.denfense.server.balance.AlienSpecBalance> specs) {
        if (specs == null || specs.isEmpty()) {
            throw new IllegalStateException("alienSpecs 배열이 null이거나 비어 있습니다.");
        }

        Set<Long> ids = new HashSet<>();
        for (com.denfense.server.balance.AlienSpecBalance spec : specs) {
            if (spec.alienId() <= 0) {
                throw new IllegalStateException("alienId는 1 이상이어야 합니다: " + spec.alienId());
            }
            if (!ids.add(spec.alienId())) {
                throw new IllegalStateException("중복된 alienId가 존재합니다: " + spec.alienId());
            }
            if (spec.grade() == null) {
                throw new IllegalStateException("grade가 null입니다: " + spec.alienId());
            }
            try {
                com.denfense.server.domain.AlienSpec.Grade.valueOf(spec.grade());
            } catch (IllegalArgumentException e) {
                throw new IllegalStateException("유효하지 않은 grade입니다: " + spec.grade());
            }
            if (spec.baseAttack() < 0) {
                throw new IllegalStateException("baseAttack은 0 이상이어야 합니다: " + spec.alienId());
            }
            if (spec.baseMp() < 0) {
                throw new IllegalStateException("baseMp는 0 이상이어야 합니다: " + spec.alienId());
            }
            if (spec.attackSpeed() <= 0) {
                throw new IllegalStateException("attackSpeed는 0보다 커야 합니다: " + spec.alienId());
            }
            if (spec.attackRange() <= 0) {
                throw new IllegalStateException("attackRange는 0보다 커야 합니다: " + spec.alienId());
            }
        }

        java.util.Map<Long, Long> evolutionMap = specs.stream()
                .filter(s -> s.evolutionTargetId() != null)
                .collect(java.util.stream.Collectors.toMap(com.denfense.server.balance.AlienSpecBalance::alienId, com.denfense.server.balance.AlienSpecBalance::evolutionTargetId));

        for (Long targetId : evolutionMap.values()) {
            if (!ids.contains(targetId)) {
                throw new IllegalStateException("evolutionTargetId가 존재하지 않는 alienId입니다. Target: " + targetId);
            }
        }

        java.util.Map<Long, Integer> states = new java.util.HashMap<>();
        for (Long id : ids) {
            states.put(id, 0); // UNVISITED
        }

        for (Long id : ids) {
            if (states.get(id) == 0) {
                if (hasCycle(id, evolutionMap, states)) {
                    throw new IllegalStateException("진화 트리에 순환(Cycle)이 발생했습니다. 관련된 ID: " + id);
                }
            }
        }
    }

    private boolean hasCycle(Long current, java.util.Map<Long, Long> evolutionMap, java.util.Map<Long, Integer> states) {
        states.put(current, 1); // VISITING

        Long next = evolutionMap.get(current);
        if (next != null) {
            Integer nextState = states.get(next);
            if (nextState != null && nextState == 1) {
                return true; // Cycle detected
            } else if (nextState == null || nextState == 0) {
                if (hasCycle(next, evolutionMap, states)) {
                    return true;
                }
            }
        }

        states.put(current, 2); // VISITED
        return false;
    }
}
