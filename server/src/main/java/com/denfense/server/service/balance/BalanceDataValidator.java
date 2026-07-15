package com.denfense.server.service.balance;

import org.springframework.stereotype.Component;

import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.math.BigDecimal;

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

    public void validateAlienUpgradeCosts(List<AlienUpgradeCostBalance> costs, int maxLevel) {
        if (costs == null || costs.isEmpty()) {
            throw new IllegalStateException("AlienUpgradeCost 데이터가 비어 있습니다.");
        }
        if (costs.size() != maxLevel - 1) {
            throw new IllegalStateException("AlienUpgradeCost 행 수는 maxLevel - 1 이어야 합니다.");
        }

        Set<Integer> levels = new HashSet<>();
        for (AlienUpgradeCostBalance cost : costs) {
            if (cost.currentLevel() < 1 || cost.currentLevel() >= maxLevel) {
                throw new IllegalStateException("currentLevel은 1부터 maxLevel - 1 사이여야 합니다: " + cost.currentLevel());
            }
            if (!levels.add(cost.currentLevel())) {
                throw new IllegalStateException("중복된 currentLevel이 존재합니다: " + cost.currentLevel());
            }
            if (cost.targetLevel() != cost.currentLevel() + 1) {
                throw new IllegalStateException("targetLevel은 currentLevel + 1 이어야 합니다: " + cost.currentLevel());
            }
            if (cost.requiredPieces() <= 0 || cost.requiredGold() <= 0 || cost.requiredGrowthCell() < 0) {
                throw new IllegalStateException("조각/Gold는 양수이고 GrowthCell은 0 이상이어야 합니다: " + cost.currentLevel());
            }
        }

        for (int i = 1; i < maxLevel; i++) {
            if (!levels.contains(i)) {
                throw new IllegalStateException("누락된 currentLevel이 존재합니다: " + i);
            }
        }
    }

    public void validateAlienLevelStats(List<AlienLevelStatBalance> stats) {
        if (stats == null || stats.isEmpty()) {
            throw new IllegalStateException("AlienLevelStat 데이터가 비어 있습니다.");
        }
        Set<Integer> levels = new HashSet<>();
        int maxLevel = stats.stream().mapToInt(AlienLevelStatBalance::level).max().orElseThrow();
        if (maxLevel < 1 || stats.size() != maxLevel) {
            throw new IllegalStateException("AlienLevelStat은 level 1부터 최대 level까지 연속이어야 합니다.");
        }
        for (AlienLevelStatBalance stat : stats) {
            if (stat.level() < 1 || stat.level() > maxLevel || !levels.add(stat.level())) {
                throw new IllegalStateException("level 범위 또는 중복 오류: " + stat.level());
            }
            if (!positive(stat.atkMultiplier()) || !positive(stat.mpMultiplier())
                    || !positive(stat.atkSpeedMultiplier()) || !positive(stat.rangeMultiplier())) {
                throw new IllegalStateException("모든 multiplier는 0보다 커야 합니다: " + stat.level());
            }
            if (stat.rangeMultiplier().compareTo(new BigDecimal("1.00")) != 0) {
                throw new IllegalStateException("rangeMultiplier는 항상 1.00이어야 합니다: " + stat.level());
            }
            if (stat.level() == 1 && (stat.atkMultiplier().compareTo(BigDecimal.ONE) != 0
                    || stat.mpMultiplier().compareTo(BigDecimal.ONE) != 0
                    || stat.atkSpeedMultiplier().compareTo(BigDecimal.ONE) != 0
                    || stat.rangeMultiplier().compareTo(BigDecimal.ONE) != 0)) {
                throw new IllegalStateException("level 1 multiplier는 모두 1.00이어야 합니다.");
            }
        }
        for (int level = 1; level <= maxLevel; level++) {
            if (!levels.contains(level)) {
                throw new IllegalStateException("누락된 level이 존재합니다: " + level);
            }
        }
    }

    private boolean positive(BigDecimal value) {
        return value != null && value.compareTo(BigDecimal.ZERO) > 0;
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

    public void validateGachaPool(com.denfense.server.balance.GachaPoolBalanceDocument document, List<com.denfense.server.balance.AlienSpecBalance> specs) {
        if (document == null) {
            throw new IllegalStateException("GachaPoolBalanceDocument가 null입니다.");
        }
        if (document.pools() == null) {
            throw new IllegalStateException("GachaPool 목록(pools)이 null입니다.");
        }

        Set<String> poolIds = new HashSet<>();
        java.util.Map<Long, String> alienSpecGradeMap = specs.stream()
                .collect(java.util.stream.Collectors.toMap(
                        com.denfense.server.balance.AlienSpecBalance::alienId,
                        com.denfense.server.balance.AlienSpecBalance::grade
                ));

        for (com.denfense.server.balance.GachaPoolBalance pool : document.pools()) {
            if (pool.poolId() == null || pool.poolId().trim().isEmpty()) {
                throw new IllegalStateException("poolId는 null이거나 공백일 수 없습니다.");
            }
            if (!poolIds.add(pool.poolId())) {
                throw new IllegalStateException("중복된 poolId가 존재합니다: " + pool.poolId());
            }
            if (pool.name() == null || pool.name().trim().isEmpty()) {
                throw new IllegalStateException("name은 null이거나 공백일 수 없습니다: " + pool.poolId());
            }

            if (pool.active() && (pool.gradeEntries() == null || pool.gradeEntries().isEmpty())) {
                throw new IllegalStateException("활성 상태인 GachaPool의 gradeEntries는 비어 있을 수 없습니다: " + pool.poolId());
            }

            if (pool.gradeEntries() != null) {
                Set<String> entryGrades = new HashSet<>();
                Set<Long> poolAlienIds = new HashSet<>();
                int totalWeight = 0;

                for (com.denfense.server.balance.GachaGradeEntryBalance entry : pool.gradeEntries()) {
                    if (entry.grade() == null || entry.grade().trim().isEmpty()) {
                        throw new IllegalStateException("grade는 null이거나 공백일 수 없습니다: " + pool.poolId());
                    }
                    try {
                        com.denfense.server.domain.AlienSpec.Grade.valueOf(entry.grade());
                    } catch (IllegalArgumentException e) {
                        throw new IllegalStateException("유효하지 않은 grade입니다: " + entry.grade());
                    }
                    if (!entryGrades.add(entry.grade())) {
                        throw new IllegalStateException("동일 Pool 내 중복된 grade가 존재합니다: " + pool.poolId() + ", " + entry.grade());
                    }
                    if (entry.weight() <= 0) {
                        throw new IllegalStateException("weight는 0보다 커야 합니다: " + pool.poolId() + ", " + entry.grade());
                    }
                    totalWeight += entry.weight();

                    if (entry.alienIds() == null || entry.alienIds().isEmpty()) {
                        throw new IllegalStateException("alienIds는 비어 있을 수 없습니다: " + pool.poolId() + ", " + entry.grade());
                    }

                    Set<Long> entryAlienIds = new HashSet<>();
                    for (Long alienId : entry.alienIds()) {
                        if (!entryAlienIds.add(alienId)) {
                            throw new IllegalStateException("동일 entry 내 중복된 alienId가 존재합니다: " + pool.poolId() + ", " + alienId);
                        }
                        if (!poolAlienIds.add(alienId)) {
                            throw new IllegalStateException("동일 Pool 전체에서 중복된 alienId가 존재합니다: " + pool.poolId() + ", " + alienId);
                        }

                        String specGrade = alienSpecGradeMap.get(alienId);
                        if (specGrade == null) {
                            throw new IllegalStateException("AlienSpec에 존재하지 않는 alienId입니다: " + pool.poolId() + ", " + alienId);
                        }
                        if (!specGrade.equals(entry.grade())) {
                            throw new IllegalStateException("AlienSpec.grade와 entry.grade가 일치하지 않습니다. pool: " + pool.poolId() + ", alienId: " + alienId + ", 예상: " + entry.grade() + ", 실제: " + specGrade);
                        }
                    }
                }

                if (pool.active() && totalWeight != 10000) {
                    throw new IllegalStateException("활성 Pool의 weight 총합은 10000이어야 합니다. pool: " + pool.poolId() + ", 현재 총합: " + totalWeight);
                }
            }
        }
    }

    public void validateShopProduct(com.denfense.server.balance.ShopProductBalanceDocument document, com.denfense.server.balance.GachaPoolBalanceDocument poolDocument) {
        if (document == null) {
            throw new IllegalStateException("ShopProductBalanceDocument가 null입니다.");
        }
        if (document.products() == null) {
            throw new IllegalStateException("products 목록이 null입니다.");
        }

        Set<String> poolIds = new HashSet<>();
        if (poolDocument != null && poolDocument.pools() != null) {
            for (com.denfense.server.balance.GachaPoolBalance pool : poolDocument.pools()) {
                poolIds.add(pool.poolId());
            }
        }

        Set<String> productIds = new HashSet<>();
        for (com.denfense.server.balance.ShopProductBalance product : document.products()) {
            if (product.productId() == null || product.productId().trim().isEmpty()) {
                throw new IllegalStateException("productId는 null이거나 공백일 수 없습니다.");
            }
            if (!productIds.add(product.productId())) {
                throw new IllegalStateException("중복된 productId가 존재합니다: " + product.productId());
            }
            if (product.name() == null || product.name().trim().isEmpty()) {
                throw new IllegalStateException("name은 null이거나 공백일 수 없습니다: " + product.productId());
            }
            if (product.currencyType() == null || product.currencyType().trim().isEmpty()) {
                throw new IllegalStateException("currencyType은 null이거나 공백일 수 없습니다: " + product.productId());
            }
            // 임시로 DIAMOND만 사용하지만 enum 검증을 위해 체크
            if (!"DIAMOND".equals(product.currencyType()) && !"GOLD".equals(product.currencyType())) {
                throw new IllegalStateException("유효하지 않은 currencyType입니다: " + product.currencyType());
            }
            if (product.price() <= 0) {
                throw new IllegalStateException("price는 0보다 커야 합니다: " + product.productId());
            }
            if (product.drawCount() <= 0) {
                throw new IllegalStateException("drawCount는 0보다 커야 합니다: " + product.productId());
            }
            if (product.gachaPoolId() == null || product.gachaPoolId().trim().isEmpty()) {
                throw new IllegalStateException("gachaPoolId는 null이거나 공백일 수 없습니다: " + product.productId());
            }
            if (!poolIds.contains(product.gachaPoolId())) {
                throw new IllegalStateException("연결된 GachaPool이 존재하지 않습니다. productId: " + product.productId() + ", gachaPoolId: " + product.gachaPoolId());
            }
        }
    }
}
