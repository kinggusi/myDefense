package com.denfense.server.service.reward;

import org.springframework.stereotype.Component;

/**
 * MVP 동작 검증용 임시 보상 정책
 * 차후 확정 밸런스가 적용되거나 Excel/DB 정책으로 교체될 예정입니다.
 */
@Component
public class MvpGameRewardPolicy implements GameRewardPolicy {

    private static final int BASE_REWARD_GOLD = 100;
    private static final int GOLD_PER_WAVE = 10;
    private static final int MAX_REWARD_GOLD = 1000;

    @Override
    public GameReward calculate(GameRewardContext context) {
        int clearedWave = Math.max(0, context.clearedWave()); // 음수 웨이브 방어
        // 비정상적인 큰 웨이브 방어를 위해 MAX 캡을 최종 결과에 적용

        long calculated = (long) BASE_REWARD_GOLD + ((long) clearedWave * GOLD_PER_WAVE);
        
        int finalReward = (int) Math.min(calculated, MAX_REWARD_GOLD);
        finalReward = Math.max(0, finalReward); // 음수 보상 최종 방어

        return new GameReward(finalReward);
    }
}
