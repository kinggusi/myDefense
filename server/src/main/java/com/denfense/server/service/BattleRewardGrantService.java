package com.denfense.server.service;

import com.denfense.server.balance.BattleRewardBalance;
import com.denfense.server.domain.*;
import com.denfense.server.dto.battle.BattleSettlementDtos;
import com.denfense.server.repository.BattleRewardClaimRepository;
import com.denfense.server.repository.BattlePlayerSettlementRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.BalanceRegistry;
import com.denfense.server.service.reward.BattleRewardCalculator;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.List;

@Service
@RequiredArgsConstructor
public class BattleRewardGrantService {
    private final BalanceRegistry balanceRegistry;
    private final BattleRewardClaimRepository claims;
    private final BattlePlayerSettlementRepository playerSettlements;
    private final UserRepository users;
    private final BattleRewardCalculator calculator;
    private final PlanetProgressionService planetProgression;

    @Transactional
    public List<BattleSettlementDtos.Reward> grant(BattleSettlement settlement, BattleSettlementDtos.Request request) {
        BattleRewardBalance balance = balanceRegistry.getBattleRewardBalance();
        BattleRewardCalculator.RewardCalculation calculation = calculator.calculate(balance, settlement.getResult().name(), settlement.getFinalWave());
        List<BattleSettlementDtos.Reward> result = new ArrayList<>();
        for (BattlePlayerSettlement storedPlayer : playerSettlements.findByBattleSettlementId(settlement.getId())) {
            if (storedPlayer.isAbandoned()) continue;
            User user = users.findByIdForUpdate(storedPlayer.getUser().getId()).orElseThrow();
            if (settlement.getResult() == BattleResult.VICTORY
                    && settlement.getFinalWave() >= balance.maxWave()
                    && settlement.getMapId() != null
                    && !settlement.getMapId().isBlank()) {
                planetProgression.unlockNext(user, settlement.getMapId(), settlement.getBattleSessionId());
            }
            List<BattleRewardClaim> existingClaims = claims.findByBattleSessionIdOrderByIdAsc(settlement.getBattleSessionId()).stream()
                    .filter(c -> c.getUser().getId().equals(user.getId()))
                    .toList();
            if (!existingClaims.isEmpty()) {
                existingClaims.stream()
                        .filter(c -> c.getGold() != 0 || c.getUniversalPiece() != 0 || c.getDiamond() != 0)
                        .map(c -> new BattleSettlementDtos.Reward(user.getId(), c.getRewardKey(), c.getRewardType(), c.getGold(), c.getUniversalPiece(), c.getDiamond()))
                        .forEach(result::add);
                continue;
            }
            addClaim(user, settlement, "SETTLEMENT:" + settlement.getBattleSessionId(), "SETTLEMENT",
                    calculation.settlementGold(), 0, 0, result);
            if (settlement.getMapId() != null && !settlement.getMapId().isBlank()
                    && calculation.highestClearedWave() >= balance.minimumRewardWave()) {
                for (BattleRewardBalance.Checkpoint checkpoint : calculation.reachedCheckpoints()) {
                    addClaimIfAbsent(user, settlement, "MAP:" + settlement.getMapId() + ":CHECKPOINT:" + checkpoint.wave(), "CHECKPOINT",
                            checkpoint.gold(), checkpoint.universalPiece(), 0, result);
                }
                if (settlement.getResult() == BattleResult.VICTORY && calculation.highestClearedWave() >= balance.maxWave()) {
                    balance.mapFirstClears().stream().filter(m -> m.mapId().equals(settlement.getMapId()) && m.wave() == balance.maxWave())
                            .findFirst().ifPresent(m -> addClaimIfAbsent(user, settlement, "MAP:" + m.mapId() + ":FIRST_CLEAR", "MAP_FIRST_CLEAR", 0, 0, m.diamond(), result));
                }
            }
        }
        return List.copyOf(result);
    }

    private void addClaimIfAbsent(User user, BattleSettlement settlement, String key, String type,
                                  int gold, int universal, int diamond, List<BattleSettlementDtos.Reward> result) {
        if (claims.findByUserIdAndRewardKey(user.getId(), key).isPresent()) return;
        addClaim(user, settlement, key, type, gold, universal, diamond, result);
    }

    private void addClaim(User user, BattleSettlement settlement, String key, String type,
                          int gold, int universal, int diamond, List<BattleSettlementDtos.Reward> result) {
        BattleRewardClaim claim = new BattleRewardClaim(user, key, settlement.getBattleSessionId(), type, gold, universal, diamond);
        claims.saveAndFlush(claim);
        user.earnGold(gold);
        user.earnUniversalPiece(universal);
        user.earnDiamond(diamond);
        if (gold != 0 || universal != 0 || diamond != 0) {
            result.add(new BattleSettlementDtos.Reward(user.getId(), key, type, gold, universal, diamond));
        }
    }
}
