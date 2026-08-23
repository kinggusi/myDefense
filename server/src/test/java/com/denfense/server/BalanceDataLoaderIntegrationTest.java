package com.denfense.server;

import com.denfense.server.service.UpgradeCost;
import com.denfense.server.service.UpgradeCostPolicy;
import com.denfense.server.service.balance.BalanceDataLoader;
import com.denfense.server.service.balance.BalanceRegistry;
import com.denfense.server.service.balance.GameRewardBalance;
import com.denfense.server.service.reward.GameReward;
import com.denfense.server.service.reward.GameRewardContext;
import com.denfense.server.service.reward.MvpGameRewardPolicy;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import org.springframework.test.util.ReflectionTestUtils;

import static org.assertj.core.api.Assertions.assertThat;
import static org.junit.jupiter.api.Assertions.assertThrows;

@SpringBootTest
class BalanceDataLoaderIntegrationTest {

    @Autowired
    private BalanceRegistry registry;

    @Autowired
    private MvpGameRewardPolicy rewardPolicy;

    @Autowired
    private UpgradeCostPolicy upgradeCostPolicy;

    @Autowired
    private BalanceDataLoader loader;

    @Test
    @DisplayName("정상 로딩 및 정책 연동 확인")
    void loadAndPolicyIntegration() {
        // ApplicationRunner가 이미 정상 데이터를 로드한 상태
        
        // 1. Registry 적재 확인
        GameRewardBalance rewardBalance = registry.getGameRewardBalance();
        assertThat(rewardBalance.baseRewardGold()).isEqualTo(100);
        
        assertThat(upgradeCostPolicy.getMaxLevel()).isEqualTo(50);
        
        // 2. MvpGameRewardPolicy 연동 확인
        GameReward reward = rewardPolicy.calculate(new GameRewardContext(1)); // clearedWave=1
        assertThat(reward.accountGold()).isEqualTo(110); // 100 + 1*10
        
        // 3. UpgradeCostPolicy 연동 확인
        UpgradeCost cost1 = upgradeCostPolicy.calculate(1);
        assertThat(cost1.getRequiredPieces()).isEqualTo(5);
        assertThat(cost1.getRequiredGold()).isEqualTo(500);
        assertThat(cost1.getRequiredGrowthCell()).isEqualTo(0);
        
        UpgradeCost cost10 = upgradeCostPolicy.calculate(10);
        assertThat(cost10.getRequiredPieces()).isEqualTo(50);
        assertThat(cost10.getRequiredGold()).isEqualTo(3000);
        assertThat(cost10.getRequiredGrowthCell()).isEqualTo(10);
    }

    @Test
    @DisplayName("파일 누락 시 예외 발생 (Fail-fast)")
    void load_missingFile() {
        ReflectionTestUtils.setField(loader, "rewardFilePath", "classpath:balance/invalid/not-exists.json");
        assertThrows(IllegalStateException.class, loader::loadData);
        // 원상복구
        ReflectionTestUtils.setField(loader, "rewardFilePath", "classpath:balance/generated/game-reward.json");
    }

    @Test
    @DisplayName("문법 에러 시 예외 발생")
    void load_syntaxError() {
        ReflectionTestUtils.setField(loader, "rewardFilePath", "classpath:balance/invalid/reward-syntax.json");
        assertThrows(Exception.class, loader::loadData);
        // 원상복구
        ReflectionTestUtils.setField(loader, "rewardFilePath", "classpath:balance/generated/game-reward.json");
    }
}
