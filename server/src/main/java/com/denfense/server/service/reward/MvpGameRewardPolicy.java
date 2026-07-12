package com.denfense.server.service.reward;

import com.denfense.server.service.balance.BalanceRegistry;
import com.denfense.server.service.balance.GameRewardBalance;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

/**
 * MVP 동작 검증용 임시 보상 정책
 * 차후 확정 밸런스가 적용되거나 Excel/DB 정책으로 교체될 예정입니다.
 */
@Component
@RequiredArgsConstructor
public class MvpGameRewardPolicy implements GameRewardPolicy {

    private final BalanceRegistry registry;

    @Override
    public GameReward calculate(GameRewardContext context) {
        GameRewardBalance balance = registry.getGameRewardBalance();
        
        int clearedWave = Math.max(0, context.clearedWave()); // 음수 웨이브 방어
        // 비정상적인 큰 웨이브 방어를 위해 MAX 캡을 최종 결과에 적용

        long calculated = (long) balance.baseRewardGold() + ((long) clearedWave * balance.goldPerWave());
        
        int finalReward = (int) Math.min(calculated, balance.maxRewardGold());
        finalReward = Math.max(0, finalReward); // 음수 보상 최종 방어

        return new GameReward(finalReward);
    }
}
