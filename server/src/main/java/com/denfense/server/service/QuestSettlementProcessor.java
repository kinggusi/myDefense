package com.denfense.server.service;

import com.denfense.server.domain.*;
import com.denfense.server.repository.*;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Propagation;
import org.springframework.transaction.annotation.Transactional;

import java.util.LinkedHashMap;
import java.util.Map;

/**
 * Converts an accepted production Settlement into permanent quest fact
 * counters. Rewards and daily/weekly reset policy intentionally remain outside
 * this processor. The application table makes every
 * (settlement, user, condition) contribution exactly-once.
 */
@Service
@RequiredArgsConstructor
public class QuestSettlementProcessor {
    public static final String MATCH_PARTICIPATION = "BATTLE_MATCH_PARTICIPATION";
    public static final String MATCH_VICTORY = "BATTLE_MATCH_VICTORY";
    public static final String WAVE_CLEARED = "BATTLE_WAVE_CLEARED";
    public static final String MONSTER_KILL = "BATTLE_MONSTER_KILL";
    public static final String SUPPORT_KILL = "BATTLE_SUPPORT_KILL";
    public static final String BOSS_KILL = "BATTLE_BOSS_KILL";
    public static final String PLANET_VICTORY_PREFIX = "BATTLE_PLANET_VICTORY:";

    private final BattleSettlementRepository settlements;
    private final BattlePlayerSettlementRepository playerSettlements;
    private final UserRepository users;
    private final QuestProgressRepository progresses;
    private final QuestSettlementApplicationRepository applications;

    @Transactional(propagation = Propagation.REQUIRES_NEW)
    public ProcessResult process(Long settlementId) {
        BattleSettlement settlement = settlements.findById(settlementId).orElseThrow();
        if (settlement.getSessionSource() != SessionSource.PRODUCTION) {
            return new ProcessResult(true, 0, 0);
        }

        int applicationCount = 0;
        long appliedAmount = 0;
        var storedPlayers = playerSettlements.findByBattleSettlementId(settlementId).stream()
                .sorted(java.util.Comparator.comparing(player -> player.getUser().getId()))
                .toList();
        for (BattlePlayerSettlement storedPlayer : storedPlayers) {
            if (storedPlayer.isAbandoned()) continue;
            User user = users.findByIdForUpdate(storedPlayer.getUser().getId()).orElseThrow();
            for (Map.Entry<String, Long> contribution : contributions(settlement, storedPlayer).entrySet()) {
                String conditionId = contribution.getKey();
                long amount = contribution.getValue();
                if (amount <= 0 || applications.existsByBattleSettlementIdAndUserIdAndQuestConditionId(
                        settlementId, user.getId(), conditionId)) {
                    continue;
                }
                QuestProgress progress = progresses.findForUpdate(user.getId(), conditionId)
                        .orElseGet(() -> new QuestProgress(user, conditionId));
                progress.add(amount);
                progresses.save(progress);
                applications.save(new QuestSettlementApplication(settlement, user, conditionId, amount));
                applicationCount++;
                appliedAmount = Math.addExact(appliedAmount, amount);
            }
        }
        applications.flush();
        progresses.flush();
        return new ProcessResult(false, applicationCount, appliedAmount);
    }

    private Map<String, Long> contributions(BattleSettlement settlement, BattlePlayerSettlement player) {
        Map<String, Long> result = new LinkedHashMap<>();
        result.put(MATCH_PARTICIPATION, 1L);
        if (settlement.getResult() == BattleResult.VICTORY) {
            result.put(MATCH_VICTORY, 1L);
            result.put(PLANET_VICTORY_PREFIX + settlement.getMapId(), 1L);
        }
        result.put(WAVE_CLEARED, (long) settlement.getFinalWave());
        result.put(MONSTER_KILL, (long) player.getKills());
        result.put(SUPPORT_KILL, (long) player.getSupportKills());
        result.put(BOSS_KILL, (long) player.getBossKills());
        return result;
    }

    public record ProcessResult(boolean excludedBySource, int applicationCount, long appliedAmount) {
    }
}
