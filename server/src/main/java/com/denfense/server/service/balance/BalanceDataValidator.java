package com.denfense.server.service.balance;

import com.denfense.server.balance.*;
import org.springframework.stereotype.Component;

import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.math.BigDecimal;
import java.util.stream.Collectors;

@Component
public class BalanceDataValidator {

    public void validateDailyContents(DailyContentBalanceDocument document) {
        if (document == null || document.contents() == null || document.contents().size() != 10) {
            throw new IllegalStateException("DailyContent must contain exactly 10 rows.");
        }
        Set<String> types = Set.of("CULTIVATION_ZONE", "MUTATION_LAB");
        Set<String> keys = new HashSet<>();
        for (DailyContentBalance row : document.contents()) {
            if (row == null || !types.contains(row.contentType()) || row.stage() < 1 || row.stage() > 5
                    || row.repeatReward() <= 0 || row.firstClearReward() < 0 || !row.enabled()
                    || !keys.add(row.contentType() + ":" + row.stage())) {
                throw new IllegalStateException("Invalid DailyContent row: " + row);
            }
        }
        for (String type : types) {
            for (int stage = 1; stage <= 5; stage++) {
                if (!keys.contains(type + ":" + stage)) {
                    throw new IllegalStateException("Missing DailyContent row: " + type + ":" + stage);
                }
            }
        }
    }

    public void validateBattleReward(com.denfense.server.balance.BattleRewardBalance balance) {
        if (balance == null || balance.maxWave() != 80 || balance.minimumRewardWave() < 1
                || balance.failureRewardBaseGold() <= 0 || balance.failureRewardCapPercent() <= 0
                || balance.failureRewardCapPercent() > 100) {
            throw new IllegalStateException("Invalid BattleReward config.");
        }
        if (balance.checkpoints() == null || balance.checkpoints().size() != 8
                || balance.checkpoints().stream().anyMatch(c -> c.wave() <= 0 || c.wave() > balance.maxWave()
                || c.wave() % 10 != 0 || c.gold() < 0 || c.universalPiece() < 0)
                || balance.checkpoints().stream().map(com.denfense.server.balance.BattleRewardBalance.Checkpoint::wave).distinct().count() != 8) {
            throw new IllegalStateException("BattleReward checkpoints must define 10..80 exactly once.");
        }
        Set<String> expectedMaps = Set.of("NEPTUNE", "URANUS", "SATURN", "JUPITER", "MARS", "EARTH", "VENUS", "MERCURY", "SUN");
        if (balance.mapFirstClears() == null || balance.mapFirstClears().size() != expectedMaps.size()
                || balance.mapFirstClears().stream().anyMatch(c -> c.wave() != balance.maxWave() || c.diamond() <= 0
                || c.mapId() == null || c.mapId().isBlank())
                || !balance.mapFirstClears().stream().map(com.denfense.server.balance.BattleRewardBalance.MapFirstClear::mapId).collect(Collectors.toSet()).equals(expectedMaps)) {
            throw new IllegalStateException("BattleReward must define nine unique planet first-clear rewards.");
        }
    }

    private static final String EACH_FIELD = "EACH_FIELD";
    private static final String BOSS_SHARED = "BOSS_SHARED";
    private static final Set<String> ALLOWED_LANE_POLICIES = Set.of(EACH_FIELD, BOSS_SHARED);

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

    public void validateResonanceBalance(List<ResonanceBalance> rows) {
        if (rows == null || rows.size() != 10) {
            throw new IllegalStateException("ResonanceBalance must contain exactly 10 rows.");
        }

        Set<String> allowedTracks = Set.of("NORMAL", "MYTHIC");
        Set<String> keys = new HashSet<>();
        Map<String, List<ResonanceBalance>> byTrack = rows.stream()
                .collect(Collectors.groupingBy(ResonanceBalance::track));
        if (!byTrack.keySet().equals(allowedTracks)) {
            throw new IllegalStateException("ResonanceBalance tracks must be NORMAL and MYTHIC.");
        }

        for (ResonanceBalance row : rows) {
            if (row == null || !allowedTracks.contains(row.track())
                    || row.level() < 1 || row.level() > 5
                    || !keys.add(row.track() + ":" + row.level())
                    || row.requiredGold() <= 0
                    || !row.enabled()
                    || row.attackMultiplier() == null || row.attackMultiplier().compareTo(BigDecimal.ONE) <= 0
                    || row.attackSpeedMultiplier() == null || row.attackSpeedMultiplier().compareTo(BigDecimal.ONE) <= 0
                    || row.rangeMultiplier() == null || row.rangeMultiplier().compareTo(BigDecimal.ONE) != 0) {
                throw new IllegalStateException("Invalid ResonanceBalance row: " + row);
            }
        }

        for (String track : allowedTracks) {
            List<ResonanceBalance> ordered = byTrack.get(track).stream()
                    .sorted(java.util.Comparator.comparingInt(ResonanceBalance::level))
                    .toList();
            int previousGold = 0;
            BigDecimal previousAttack = BigDecimal.ONE;
            BigDecimal previousSpeed = BigDecimal.ONE;
            for (int index = 0; index < ordered.size(); index++) {
                ResonanceBalance row = ordered.get(index);
                if (row.level() != index + 1
                        || row.requiredGold() <= previousGold
                        || row.attackMultiplier().compareTo(previousAttack) <= 0
                        || row.attackSpeedMultiplier().compareTo(previousSpeed) <= 0) {
                    throw new IllegalStateException("ResonanceBalance must increase continuously: " + track);
                }
                previousGold = row.requiredGold();
                previousAttack = row.attackMultiplier();
                previousSpeed = row.attackSpeedMultiplier();
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
            int completedMilestones = Math.min(stat.level() / 10, 4);
            BigDecimal expectedAttack = BigDecimal.ONE
                    .add(BigDecimal.valueOf(stat.level() - 1L).multiply(new BigDecimal("0.045")))
                    .add(BigDecimal.valueOf(completedMilestones).multiply(new BigDecimal("0.08")));
            BigDecimal expectedMp = BigDecimal.ONE
                    .add(BigDecimal.valueOf(stat.level() - 1L).multiply(new BigDecimal("0.03")));
            BigDecimal expectedSpeed = BigDecimal.ONE
                    .add(BigDecimal.valueOf(stat.level() - 1L).multiply(new BigDecimal("0.005")));
            if (stat.atkMultiplier().compareTo(expectedAttack) != 0
                    || stat.mpMultiplier().compareTo(expectedMp) != 0
                    || stat.atkSpeedMultiplier().compareTo(expectedSpeed) != 0) {
                throw new IllegalStateException("AlienLevelStat multiplier does not match the canonical growth policy at level "
                        + stat.level() + ".");
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

      public void validateSummonPool(com.denfense.server.balance.SummonPoolBalanceDocument document,
                                     List<com.denfense.server.balance.AlienSpecBalance> specs) {
          if (document == null || document.pools() == null || document.pools().isEmpty())
              throw new IllegalStateException("SummonPool must not be empty.");
          var gradeById = specs.stream().collect(Collectors.toMap(com.denfense.server.balance.AlienSpecBalance::alienId,
                  com.denfense.server.balance.AlienSpecBalance::grade));
          Set<String> poolIds = new HashSet<>();
          for (var pool : document.pools()) {
              if (pool == null || pool.poolId() == null || pool.poolId().isBlank() || !poolIds.add(pool.poolId()))
                  throw new IllegalStateException("SummonPool poolId must be unique and non-blank.");
              if (pool.active() && (pool.entries() == null || pool.entries().isEmpty()))
                  throw new IllegalStateException("Active SummonPool must have entries: " + pool.poolId());
              int totalWeight = 0;
              Set<Long> ids = new HashSet<>();
              for (var entry : pool.entries()) {
                  if (!"NORMAL".equals(entry.grade()) || entry.weight() <= 0 || entry.alienIds() == null || entry.alienIds().isEmpty())
                      throw new IllegalStateException("Battle SummonPool permits NORMAL entries only: " + pool.poolId());
                  totalWeight += entry.weight();
                  for (Long id : entry.alienIds()) {
                      if (!ids.add(id) || !"NORMAL".equals(gradeById.get(id)))
                          throw new IllegalStateException("SummonPool contains duplicate or non-NORMAL alienId: " + id);
                  }
              }
              if (totalWeight != 10000) throw new IllegalStateException("SummonPool weights must total 10000: " + pool.poolId());
          }
          if (poolIds.stream().noneMatch("STANDARD_SUMMON_POOL"::equals))
              throw new IllegalStateException("STANDARD_SUMMON_POOL is required.");
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

    public void validateMonsterSpecs(MonsterSpecBalanceDocument document) {
        if (document == null || document.monsters() == null || document.monsters().isEmpty()) {
            throw new IllegalStateException("MonsterSpec data is empty.");
        }
        Set<String> ids = new HashSet<>();
        Set<String> types = new HashSet<>();
        Set<String> allowedTypes = Set.of("NORMAL", "ELITE", "WAVE_BOSS");
        for (MonsterSpecBalance monster : document.monsters()) {
            requireText(monster.monsterId(), "monsterId");
            requireText(monster.name(), "monster name");
            if (!ids.add(monster.monsterId())) {
                throw new IllegalStateException("Duplicate monsterId: " + monster.monsterId());
            }
            if (!allowedTypes.contains(monster.monsterType())) {
                throw new IllegalStateException("Unsupported monsterType: " + monster.monsterType());
            }
            types.add(monster.monsterType());
            if (!positive(monster.baseHp()) || !positive(monster.moveSpeed())) {
                throw new IllegalStateException("Monster HP and move speed must be positive: " + monster.monsterId());
            }
            if (monster.killGold() < 0) {
                throw new IllegalStateException("Monster killGold must be non-negative: " + monster.monsterId());
            }
        }
        if (!types.containsAll(allowedTypes)) {
            throw new IllegalStateException("MonsterSpec must contain NORMAL, ELITE, and WAVE_BOSS.");
        }
    }

    public void validateWaves(WaveSpecBalanceDocument waveDocument, WaveSpawnBalanceDocument spawnDocument) {
        if (waveDocument == null || waveDocument.waves() == null || waveDocument.waves().isEmpty()) {
            throw new IllegalStateException("WaveSpec data is empty.");
        }
        if (spawnDocument == null || spawnDocument.spawns() == null || spawnDocument.spawns().isEmpty()) {
            throw new IllegalStateException("WaveSpawn data is empty.");
        }
        Map<String, List<WaveSpawnBalance>> spawnsByGroup = spawnDocument.spawns().stream()
                .collect(Collectors.groupingBy(WaveSpawnBalance::spawnGroupId));
        Set<String> keys = new HashSet<>();
        Map<String, List<WaveSpecBalance>> byMode = waveDocument.waves().stream()
                .collect(Collectors.groupingBy(WaveSpecBalance::modeId));
        for (WaveSpecBalance wave : waveDocument.waves()) {
            requireText(wave.modeId(), "wave modeId");
            requireText(wave.spawnGroupId(), "spawnGroupId");
            if (!keys.add(wave.modeId() + "\0" + wave.wave())) {
                throw new IllegalStateException("Duplicate modeId+wave: " + wave.modeId() + "/" + wave.wave());
            }
            if (wave.wave() <= 0 || !positive(wave.hpMultiplier())
                    || wave.interWaveDelaySeconds() == null || wave.interWaveDelaySeconds().signum() < 0) {
                throw new IllegalStateException("Invalid WaveSpec numeric value: " + wave.modeId() + "/" + wave.wave());
            }
            if (wave.isBossWave()) {
                if (!positive(wave.bossTimeLimitSeconds())) {
                    throw new IllegalStateException("Boss wave time limit must be positive: " + wave.modeId() + "/" + wave.wave());
                }
            } else if (wave.bossTimeLimitSeconds() == null || wave.bossTimeLimitSeconds().signum() != 0) {
                throw new IllegalStateException("Normal wave bossTimeLimitSeconds must be zero: " + wave.modeId() + "/" + wave.wave());
            }
            List<WaveSpawnBalance> groupSpawns = spawnsByGroup.get(wave.spawnGroupId());
            if (groupSpawns == null) {
                throw new IllegalStateException("Wave references missing spawnGroupId: " + wave.spawnGroupId());
            }
            if (wave.isBossWave()) {
                if (groupSpawns.size() != 1
                        || !BOSS_SHARED.equals(groupSpawns.get(0).lanePolicy())
                        || groupSpawns.get(0).spawnCountPerField() != 1) {
                    throw new IllegalStateException("Boss wave requires exactly one BOSS_SHARED spawn: " + wave.spawnGroupId());
                }
            } else if (groupSpawns.stream().anyMatch(spawn -> !EACH_FIELD.equals(spawn.lanePolicy()))) {
                throw new IllegalStateException("Normal wave requires only EACH_FIELD spawns: " + wave.spawnGroupId());
            }
        }
        for (Map.Entry<String, List<WaveSpecBalance>> entry : byMode.entrySet()) {
            List<Integer> waves = entry.getValue().stream().map(WaveSpecBalance::wave).sorted().toList();
            if (waves.size() != 80) {
                throw new IllegalStateException("COOP wave template must define exactly 80 waves for mode: " + entry.getKey());
            }
            for (int expected = 1; expected <= waves.size(); expected++) {
                if (waves.get(expected - 1) != expected) {
                    throw new IllegalStateException("Waves must be continuous from 1 for mode: " + entry.getKey());
                }
            }
            if (entry.getValue().stream().noneMatch(WaveSpecBalance::isBossWave)) {
                throw new IllegalStateException("At least one boss wave is required for mode: " + entry.getKey());
            }
            for (WaveSpecBalance wave : entry.getValue()) {
                if (wave.isBossWave() != (wave.wave() % 10 == 0)) {
                    throw new IllegalStateException("Boss waves must be exactly waves 10,20,...,80: "
                            + entry.getKey() + "/" + wave.wave());
                }
            }
        }

        Set<String> bossSpawnGroups = waveDocument.waves().stream()
                .filter(WaveSpecBalance::isBossWave)
                .map(WaveSpecBalance::spawnGroupId)
                .collect(Collectors.toSet());
        for (WaveSpawnBalance spawn : spawnDocument.spawns()) {
            if (BOSS_SHARED.equals(spawn.lanePolicy()) && !bossSpawnGroups.contains(spawn.spawnGroupId())) {
                throw new IllegalStateException("BOSS_SHARED spawn must belong to a boss wave: " + spawn.spawnGroupId());
            }
        }
    }

    public void validateWaveSpawns(WaveSpawnBalanceDocument document, MonsterSpecBalanceDocument monsters) {
        if (document == null || document.spawns() == null || document.spawns().isEmpty()) {
            throw new IllegalStateException("WaveSpawn data is empty.");
        }
        Map<String, String> monsterTypes = monsters.monsters().stream()
                .collect(Collectors.toMap(MonsterSpecBalance::monsterId, MonsterSpecBalance::monsterType));
        Set<String> keys = new HashSet<>();
        for (WaveSpawnBalance spawn : document.spawns()) {
            requireText(spawn.spawnGroupId(), "spawnGroupId");
            requireText(spawn.monsterId(), "spawn monsterId");
            if (!keys.add(spawn.spawnGroupId() + "\0" + spawn.order())) {
                throw new IllegalStateException("Duplicate spawnGroupId+order: " + spawn.spawnGroupId() + "/" + spawn.order());
            }
            String monsterType = monsterTypes.get(spawn.monsterId());
            if (monsterType == null) {
                throw new IllegalStateException("WaveSpawn references missing monsterId: " + spawn.monsterId());
            }
            if (spawn.order() <= 0 || spawn.spawnCountPerField() <= 0
                    || spawn.startDelaySeconds() == null || spawn.startDelaySeconds().signum() < 0
                    || !positive(spawn.spawnIntervalSeconds())) {
                throw new IllegalStateException("Invalid WaveSpawn numeric value: " + spawn.spawnGroupId() + "/" + spawn.order());
            }
            if (!ALLOWED_LANE_POLICIES.contains(spawn.lanePolicy())) {
                throw new IllegalStateException("Unsupported lanePolicy: " + spawn.lanePolicy());
            }
            if (EACH_FIELD.equals(spawn.lanePolicy()) && "WAVE_BOSS".equals(monsterType)) {
                throw new IllegalStateException("EACH_FIELD cannot spawn WAVE_BOSS: " + spawn.spawnGroupId());
            }
            if (BOSS_SHARED.equals(spawn.lanePolicy())) {
                if (!"WAVE_BOSS".equals(monsterType)) {
                    throw new IllegalStateException("BOSS_SHARED requires WAVE_BOSS: " + spawn.spawnGroupId());
                }
                if (spawn.spawnCountPerField() != 1) {
                    throw new IllegalStateException("BOSS_SHARED spawn count must be exactly one: " + spawn.spawnGroupId());
                }
            }
        }


        Map<String, List<WaveSpawnBalance>> spawnsByGroup = document.spawns().stream()
                .collect(Collectors.groupingBy(WaveSpawnBalance::spawnGroupId));
        for (Map.Entry<String, List<WaveSpawnBalance>> entry : spawnsByGroup.entrySet()) {
            long bossSharedCount = entry.getValue().stream()
                    .filter(spawn -> BOSS_SHARED.equals(spawn.lanePolicy()))
                    .count();
            if (bossSharedCount > 0 && (bossSharedCount != 1 || entry.getValue().size() != 1)) {
                throw new IllegalStateException("Boss SpawnGroup must contain exactly one BOSS_SHARED row without mixed lanes: "
                        + entry.getKey());
            }
        }
    }

    public void validateFieldLimits(FieldLimitBalanceDocument document) {
        if (document == null || document.fieldLimits() == null || document.fieldLimits().isEmpty()) {
            throw new IllegalStateException("FieldLimitBalance data is empty.");
        }
        Set<String> modes = new HashSet<>();
        for (FieldLimitBalance limit : document.fieldLimits()) {
            requireText(limit.modeId(), "field limit modeId");
            if (!modes.add(limit.modeId())) {
                throw new IllegalStateException("Duplicate FieldLimit modeId: " + limit.modeId());
            }
            if (limit.playerCount() != 2 || limit.warningThreshold() <= 0
                    || limit.warningThreshold() >= limit.dangerThreshold()
                    || limit.dangerThreshold() >= limit.maxAliveMonsterCountPerField()) {
                throw new IllegalStateException("Invalid field limit thresholds for mode: " + limit.modeId());
            }
        }
    }

    public void validatePlanetBattles(PlanetBattleBalanceDocument document) {
        List<String> expectedOrder = List.of(
                "NEPTUNE", "URANUS", "SATURN", "JUPITER", "MARS",
                "EARTH", "VENUS", "MERCURY", "SUN");
        List<BigDecimal> expectedHp = List.of(
                new BigDecimal("1.00"), new BigDecimal("1.35"), new BigDecimal("1.80"),
                new BigDecimal("2.40"), new BigDecimal("3.20"), new BigDecimal("4.30"),
                new BigDecimal("5.80"), new BigDecimal("7.80"), new BigDecimal("11.00"));
        List<BigDecimal> expectedSpeed = List.of(
                new BigDecimal("1.00"), new BigDecimal("1.03"), new BigDecimal("1.06"),
                new BigDecimal("1.09"), new BigDecimal("1.12"), new BigDecimal("1.15"),
                new BigDecimal("1.18"), new BigDecimal("1.21"), new BigDecimal("1.25"));
        if (document == null || document.planets() == null || document.planets().size() != expectedOrder.size()) {
            throw new IllegalStateException("PlanetBattle must define exactly nine planets.");
        }
        List<PlanetBattleBalance> sorted = document.planets().stream()
                .sorted(java.util.Comparator.comparingInt(PlanetBattleBalance::order)).toList();
        Set<String> ids = new HashSet<>();
        Set<Integer> orders = new HashSet<>();
        for (int index = 0; index < sorted.size(); index++) {
            PlanetBattleBalance planet = sorted.get(index);
            requireText(planet.mapId(), "planet mapId");
            if (!planet.enabled() || !ids.add(planet.mapId()) || !orders.add(planet.order())
                    || planet.order() != index + 1 || !expectedOrder.get(index).equals(planet.mapId())
                    || !positive(planet.hpMultiplier()) || !positive(planet.speedMultiplier())
                    || planet.bossHpMultiplier() == null
                    || planet.bossHpMultiplier().compareTo(new BigDecimal("3.00")) != 0
                    || planet.hpMultiplier().compareTo(expectedHp.get(index)) != 0
                    || planet.speedMultiplier().compareTo(expectedSpeed.get(index)) != 0) {
                throw new IllegalStateException("Invalid PlanetBattle row: " + planet.mapId());
            }
        }
    }

    public void validateSummons(SummonBalanceDocument document) {
        if (document == null || document.summons() == null || document.summons().isEmpty()) {
            throw new IllegalStateException("SummonBalance data is empty.");
        }
        Set<String> keys = new HashSet<>();
        for (SummonBalance summon : document.summons()) {
            requireText(summon.modeId(), "summon modeId");
            requireText(summon.summonType(), "summonType");
            requireText(summon.resultPoolId(), "resultPoolId");
            if (!keys.add(summon.modeId() + "\0" + summon.summonType())) {
                throw new IllegalStateException("Duplicate SummonBalance modeId+summonType: " + summon.modeId() + "/" + summon.summonType());
            }
            if (summon.baseCost() < 0 || summon.costIncreasePerUse() < 0
                    || (summon.maxUses() != -1 && summon.maxUses() <= 0)) {
                throw new IllegalStateException("Invalid SummonBalance numeric value: " + summon.modeId() + "/" + summon.summonType());
            }
        }
    }

    public void validateMergeRules(MergeRuleBalanceDocument document) {
        if (document == null || document.mergeRules() == null || document.mergeRules().isEmpty()) {
            throw new IllegalStateException("MergeRule data is empty.");
        }
        Map<String, String> expectedResultGrade = Map.of(
                "NORMAL", "EPIC", "EPIC", "UNIQUE", "UNIQUE", "LEGEND", "LEGEND", "MYTHIC", "MYTHIC", "MYTHIC");
        Map<String, String> expectedResultType = Map.of(
                "NORMAL", "RANDOM_NEXT_GRADE", "EPIC", "RANDOM_NEXT_GRADE", "UNIQUE", "RANDOM_NEXT_GRADE",
                "LEGEND", "MYTHIC_CHOICE", "MYTHIC", "DISABLED");
        Set<String> grades = new HashSet<>();
        for (MergeRuleBalance rule : document.mergeRules()) {
            if (!expectedResultGrade.containsKey(rule.sourceGrade()) || !grades.add(rule.sourceGrade())) {
                throw new IllegalStateException("Invalid or duplicate MergeRule sourceGrade: " + rule.sourceGrade());
            }
            if (rule.requiredCount() != 2 || !rule.sameSpeciesRequired()) {
                throw new IllegalStateException("Merge requires two Aliens with the same grade and alienId: " + rule.sourceGrade());
            }
            if (!expectedResultGrade.get(rule.sourceGrade()).equals(rule.resultGrade())
                    || !expectedResultType.get(rule.sourceGrade()).equals(rule.resultType())) {
                throw new IllegalStateException("Invalid merge grade transition: " + rule.sourceGrade());
            }
        }
        if (!grades.equals(expectedResultGrade.keySet())) {
            throw new IllegalStateException("MergeRule must contain all Alien grades.");
        }
    }

    public void validateMythicChoices(MythicChoiceBalanceDocument document, List<AlienSpecBalance> alienSpecs) {
        if (document == null || document.mythicChoices() == null || document.mythicChoices().isEmpty()) {
            throw new IllegalStateException("MythicChoiceBalance data is empty.");
        }
        List<AlienSpecBalance> mythics = alienSpecs.stream()
                .filter(spec -> "MYTHIC".equals(spec.grade()))
                .toList();
        if (mythics.size() != 20) {
            throw new IllegalStateException("Mythic choice pool must derive exactly 20 MYTHIC AlienSpec rows.");
        }
        Set<String> modes = new HashSet<>();
        Set<String> policies = Set.of("FIRST", "RANDOM");
        for (MythicChoiceBalance choice : document.mythicChoices()) {
            requireText(choice.modeId(), "mythic choice modeId");
            if (!modes.add(choice.modeId())) {
                throw new IllegalStateException("Duplicate MythicChoice modeId: " + choice.modeId());
            }
            if (choice.candidateCount() != 3 || choice.candidateCount() > mythics.size()
                    || choice.freeRerollCount() < 0 || choice.paidRerollLimit() < 0
                    || choice.paidRerollCost() < 0 || !positive(choice.selectionTimeoutSeconds())
                    || !policies.contains(choice.autoSelectPolicy())) {
                throw new IllegalStateException("Invalid MythicChoiceBalance: " + choice.modeId());
            }
        }
    }

    public void validateBattleBalance(
            MonsterSpecBalanceDocument monsters,
            WaveSpecBalanceDocument waves,
            WaveSpawnBalanceDocument spawns,
            FieldLimitBalanceDocument fieldLimits,
            SummonBalanceDocument summons,
            MergeRuleBalanceDocument mergeRules,
            MythicChoiceBalanceDocument mythicChoices,
            List<AlienSpecBalance> alienSpecs
    ) {
        validateMonsterSpecs(monsters);
        validateWaveSpawns(spawns, monsters);
        validateWaves(waves, spawns);
        validateFieldLimits(fieldLimits);
        validateSummons(summons);
        validateMergeRules(mergeRules);
        validateMythicChoices(mythicChoices, alienSpecs);

        Set<String> waveModes = waves.waves().stream().map(WaveSpecBalance::modeId).collect(Collectors.toSet());
        Set<String> fieldModes = fieldLimits.fieldLimits().stream().map(FieldLimitBalance::modeId).collect(Collectors.toSet());
        Set<String> summonModes = summons.summons().stream().map(SummonBalance::modeId).collect(Collectors.toSet());
        Set<String> choiceModes = mythicChoices.mythicChoices().stream().map(MythicChoiceBalance::modeId).collect(Collectors.toSet());
        if (!waveModes.equals(fieldModes) || !waveModes.equals(summonModes) || !waveModes.equals(choiceModes)) {
            throw new IllegalStateException("Battle balance modeId sets must match across Wave, FieldLimit, Summon, and MythicChoice.");
        }
    }

    private void requireText(String value, String field) {
        if (value == null || value.isBlank()) {
            throw new IllegalStateException(field + " must not be blank.");
        }
    }
}
