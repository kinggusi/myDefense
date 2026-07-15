package com.denfense.server;

import com.denfense.server.service.balance.BalanceDataLoader;
import com.denfense.server.service.balance.BalanceDataValidator;
import com.denfense.server.service.balance.BalanceRegistry;
import com.denfense.server.service.balance.AlienUpgradeBalanceRegistry;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.runner.ApplicationContextRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.io.DefaultResourceLoader;

import static org.assertj.core.api.Assertions.assertThat;

class BalanceContextFailFastTest {

    private final ApplicationContextRunner contextRunner = new ApplicationContextRunner()
            .withUserConfiguration(TestConfig.class);

    @Configuration
    static class TestConfig {
        @Bean
        public BalanceRegistry balanceRegistry() {
            return new BalanceRegistry();
        }

        @Bean
        public AlienUpgradeBalanceRegistry alienUpgradeBalanceRegistry() {
            return new AlienUpgradeBalanceRegistry();
        }

        @Bean
        public BalanceDataValidator balanceDataValidator() {
            return new BalanceDataValidator();
        }

        @Bean
        public ObjectMapper objectMapper() {
            return new ObjectMapper();
        }

        @Bean
        public BalanceDataLoader balanceDataLoader(ObjectMapper mapper, BalanceDataValidator validator,
                                                   BalanceRegistry registry, AlienUpgradeBalanceRegistry upgradeRegistry) {
            return new BalanceDataLoader(new DefaultResourceLoader(), mapper, validator, registry, upgradeRegistry);
        }
    }

    @Test
    @DisplayName("정상 JSON 시 컨텍스트 시작 성공")
    void contextLoads_withValidJson() {
        contextRunner
                .withPropertyValues(
                        "balance.reward.path=classpath:balance/valid/game-reward.json",
                        "balance.upgrade-cost.path=classpath:balance/valid/alien-upgrade-cost.json",
                        "balance.level-stat.path=classpath:balance/generated/alien-level-stat.json"
                )
                .run(context -> {
                    assertThat(context).hasNotFailed();
                    BalanceDataLoader loader = context.getBean(BalanceDataLoader.class);
                    loader.loadData();
                    BalanceRegistry registry = context.getBean(BalanceRegistry.class);
                    assertThat(registry.getGameRewardBalance().baseRewardGold()).isEqualTo(100);
                });
    }

    @Test
    @DisplayName("reward 음수일 때 시작 실패")
    void contextFails_withNegativeReward() {
        contextRunner
                .withPropertyValues(
                        "balance.reward.path=classpath:balance/invalid/reward-negative.json",
                        "balance.upgrade-cost.path=classpath:balance/valid/alien-upgrade-cost.json",
                        "balance.level-stat.path=classpath:balance/generated/alien-level-stat.json"
                )
                .run(context -> {
                    assertThat(context).hasNotFailed();
                    BalanceDataLoader loader = context.getBean(BalanceDataLoader.class);
                    Exception exception = org.junit.jupiter.api.Assertions.assertThrows(Exception.class, loader::loadData);
                    assertThat(exception).hasMessageContaining("baseRewardGold는 0 이상이어야 합니다");
                });
    }

    @Test
    @DisplayName("파일 없음일 때 시작 실패")
    void contextFails_withMissingFile() {
        contextRunner
                .withPropertyValues(
                        "balance.reward.path=classpath:balance/invalid/not-exists.json",
                        "balance.upgrade-cost.path=classpath:balance/valid/alien-upgrade-cost.json",
                        "balance.level-stat.path=classpath:balance/generated/alien-level-stat.json"
                )
                .run(context -> {
                    assertThat(context).hasNotFailed();
                    BalanceDataLoader loader = context.getBean(BalanceDataLoader.class);
                    Exception exception = org.junit.jupiter.api.Assertions.assertThrows(Exception.class, loader::loadData);
                    assertThat(exception).hasMessageContaining("파일을 찾을 수 없습니다");
                });
    }

    @Test
    @DisplayName("강화 비용 JSON 누락 시 fail-fast")
    void contextFails_withMissingUpgradeCostFile() {
        contextRunner
                .withPropertyValues(
                        "balance.reward.path=classpath:balance/valid/game-reward.json",
                        "balance.upgrade-cost.path=classpath:balance/invalid/not-exists-upgrade-cost.json",
                        "balance.level-stat.path=classpath:balance/generated/alien-level-stat.json"
                )
                .run(context -> {
                    BalanceDataLoader loader = context.getBean(BalanceDataLoader.class);
                    Exception exception = org.junit.jupiter.api.Assertions.assertThrows(Exception.class, loader::loadData);
                    assertThat(exception).hasMessageContaining("파일을 찾을 수 없습니다");
                });
    }

    @Test
    @DisplayName("레벨 능력치 JSON 누락 시 fail-fast")
    void contextFails_withMissingLevelStatFile() {
        contextRunner
                .withPropertyValues(
                        "balance.reward.path=classpath:balance/valid/game-reward.json",
                        "balance.upgrade-cost.path=classpath:balance/valid/alien-upgrade-cost.json",
                        "balance.level-stat.path=classpath:balance/invalid/not-exists-level-stat.json"
                )
                .run(context -> {
                    BalanceDataLoader loader = context.getBean(BalanceDataLoader.class);
                    Exception exception = org.junit.jupiter.api.Assertions.assertThrows(Exception.class, loader::loadData);
                    assertThat(exception).hasMessageContaining("파일을 찾을 수 없습니다");
                });
    }
}
