package com.denfense.server.controller;

import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.*;
import com.denfense.server.service.balance.*;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;
import org.springframework.boot.test.context.runner.ApplicationContextRunner;

import java.util.List;
import java.util.stream.Stream;

import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.mock;

class DevelopmentProfileBoundaryTest {
    private final ApplicationContextRunner runner = new ApplicationContextRunner()
            .withUserConfiguration(QuestController.class, DailyContentController.class,
                    LocalDailyContentResultController.class, LocalBattleSessionRosterController.class,
                    LocalFusionSessionRosterAdapter.class, BattleEntryAttackSnapshotService.class)
            .withBean(QuestService.class, () -> mock(QuestService.class))
            .withBean(com.denfense.server.auth.AccountAccess.class, () -> mock(com.denfense.server.auth.AccountAccess.class))
            .withBean(DailyContentService.class, () -> mock(DailyContentService.class))
            .withBean(BattleSessionRosterRegistry.class, () -> mock(BattleSessionRosterRegistry.class))
            .withBean(BattlePlanetEntryService.class, () -> mock(BattlePlanetEntryService.class))
            .withBean(UserRepository.class, () -> mock(UserRepository.class))
            .withBean(UserAlienRepository.class, () -> mock(UserAlienRepository.class))
            .withBean(BalanceVersionRegistry.class, () -> mock(BalanceVersionRegistry.class))
            .withBean(PlanetBattleBalanceRegistry.class, () -> mock(PlanetBattleBalanceRegistry.class))
            .withBean(BalanceRegistry.class, () -> mock(BalanceRegistry.class))
            .withBean(AlienStatCalculator.class, () -> mock(AlienStatCalculator.class));

    static Stream<Arguments> profiles() {
        return Stream.of(
                Arguments.of("", false),
                Arguments.of("local", true),
                Arguments.of("dev", true),
                Arguments.of("local,dev", true),
                Arguments.of("prod", false),
                Arguments.of("production", false),
                Arguments.of("local,prod", false),
                Arguments.of("local,production", false),
                Arguments.of("dev,prod", false),
                Arguments.of("dev,production", false),
                Arguments.of("local,dev,prod,production", false));
    }

    @ParameterizedTest(name = "profiles={0}, developmentEnabled={1}")
    @MethodSource("profiles")
    void developmentEndpointsAndAnonymousSnapshotsRespectProfileBoundary(String profiles, boolean enabled) {
        runCase(profiles, enabled, true);
        runCase(profiles, enabled, false);
    }

    private void runCase(String profiles, boolean developmentEnabled, boolean anonymousConfigured) {
        runner.withInitializer(context -> context.getEnvironment().setActiveProfiles(
                        profiles.isEmpty() ? new String[0] : profiles.split(",")))
                .withPropertyValues("mydefense.battle.allow-anonymous-entry-snapshots=" + anonymousConfigured)
                .run(context -> {
                    assertThat(context).hasNotFailed();
                    for (Class<?> developmentType : List.of(QuestController.class, DailyContentController.class,
                            LocalDailyContentResultController.class, LocalBattleSessionRosterController.class,
                            LocalFusionSessionRosterAdapter.class)) {
                        assertThat(context.getBeansOfType(developmentType)).hasSize(developmentEnabled ? 1 : 0);
                    }
                    var snapshots = context.getBean(BattleEntryAttackSnapshotService.class);
                    for (String playerId : List.of("", "unknown-development-player")) {
                        if (developmentEnabled && anonymousConfigured) {
                            assertThat(snapshots.getForPlayer(playerId).aliens()).isEmpty();
                        } else {
                            assertThatThrownBy(() -> snapshots.getForPlayer(playerId))
                                    .isInstanceOf(BusinessException.class)
                                    .extracting("errorCode").isEqualTo(ErrorCode.USER_NOT_FOUND);
                        }
                    }
                });
    }
}
