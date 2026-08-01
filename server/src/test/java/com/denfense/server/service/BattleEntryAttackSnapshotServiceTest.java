package com.denfense.server.service;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.balance.AlienSpecBalance;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.BalanceVersionRegistry;
import com.denfense.server.service.balance.BalanceRegistry;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

class BattleEntryAttackSnapshotServiceTest {

    @Test
    void usesPermanentLevelAndCanonicalCalculatorWithoutClientFormula() {
        UserRepository users = mock(UserRepository.class);
        UserAlienRepository userAliens = mock(UserAlienRepository.class);
        BalanceRegistry specs = mock(BalanceRegistry.class);
        AlienStatCalculator calculator = mock(AlienStatCalculator.class);
        BalanceVersionRegistry versions = mock(BalanceVersionRegistry.class);
        User user = new User();
        AlienSpec entitySpec = entitySpec(7L);
        AlienSpecBalance spec = spec(7L);
        UserAlien owned = new UserAlien(user, entitySpec);
        owned.setLevel(4);
        when(users.findByUsername("player-a")).thenReturn(Optional.of(user));
        when(userAliens.findAllByUser(user)).thenReturn(List.of(owned));
        when(specs.getAllAlienSpecs()).thenReturn(List.of(spec));
        when(calculator.calculate(spec, 4)).thenReturn(stat("25.00", "8.00", "1.2500", "4.5000"));
        when(versions.getBalanceVersion()).thenReturn("1-version");
        when(versions.getContentHash()).thenReturn("content-hash");

        var response = service(users, userAliens, specs, calculator, versions).getForPlayer(" player-a ");

        assertThat(response.playerId()).isEqualTo("player-a");
        assertThat(response.balanceVersion()).isEqualTo("1-version");
        assertThat(response.contentHash()).isEqualTo("content-hash");
        assertThat(response.aliens()).singleElement().satisfies(snapshot -> {
            assertThat(snapshot.alienId()).isEqualTo(7L);
            assertThat(snapshot.level()).isEqualTo(4);
            assertThat(snapshot.damage()).isEqualByComparingTo("25.00");
            assertThat(snapshot.attackRate()).isEqualByComparingTo("1.2500");
            assertThat(snapshot.range()).isEqualByComparingTo("4.5000");
        });
    }

    @Test
    void developmentIdentityWithoutAccountReceivesCanonicalLevelOneStats() {
        UserRepository users = mock(UserRepository.class);
        UserAlienRepository userAliens = mock(UserAlienRepository.class);
        BalanceRegistry specs = mock(BalanceRegistry.class);
        AlienStatCalculator calculator = mock(AlienStatCalculator.class);
        BalanceVersionRegistry versions = mock(BalanceVersionRegistry.class);
        AlienSpecBalance second = spec(2L);
        AlienSpecBalance first = spec(1L);
        when(users.findByUsername("dev-host")).thenReturn(Optional.empty());
        when(specs.getAllAlienSpecs()).thenReturn(List.of(second, first));
        when(calculator.calculate(first, 1)).thenReturn(stat("10", "1", "1", "3"));
        when(calculator.calculate(second, 1)).thenReturn(stat("20", "2", "2", "4"));

        BattleEntryAttackSnapshotService service = service(users, userAliens, specs, calculator, versions);
        service.allowAnonymousEntrySnapshotsForTest();
        var response = service.getForPlayer("dev-host");

        assertThat(response.aliens()).extracting(a -> a.alienId()).containsExactly(1L, 2L);
        assertThat(response.aliens()).extracting(a -> a.level()).containsOnly(1);
    }

    @Test
    void blankIdentityIsRejectedWhenAnonymousDevelopmentModeIsDisabled() {
        UserRepository users = mock(UserRepository.class);
        UserAlienRepository userAliens = mock(UserAlienRepository.class);
        BalanceRegistry specs = mock(BalanceRegistry.class);
        AlienStatCalculator calculator = mock(AlienStatCalculator.class);
        BalanceVersionRegistry versions = mock(BalanceVersionRegistry.class);

        org.assertj.core.api.Assertions.assertThatThrownBy(
                        () -> service(users, userAliens, specs, calculator, versions).getForPlayer(" "))
                .isInstanceOf(com.denfense.server.exception.BusinessException.class)
                .extracting("errorCode")
                .isEqualTo(com.denfense.server.exception.ErrorCode.USER_NOT_FOUND);
    }

    private static BattleEntryAttackSnapshotService service(
            UserRepository users,
            UserAlienRepository userAliens,
            BalanceRegistry specs,
            AlienStatCalculator calculator,
            BalanceVersionRegistry versions) {
        return new BattleEntryAttackSnapshotService(users, userAliens, specs, calculator, versions);
    }

    private static AlienSpec entitySpec(long id) {
        AlienSpec spec = new AlienSpec();
        spec.setId(id);
        return spec;
    }

    private static AlienSpecBalance spec(long id) {
        return new AlienSpecBalance(id, "Alien-" + id, "", "NORMAL", 10, 10, 1.0, 3.0, null, false);
    }

    private static AlienCurrentStat stat(String atk, String mp, String attackRate, String range) {
        return new AlienCurrentStat(
                new BigDecimal(atk),
                new BigDecimal(mp),
                new BigDecimal(attackRate),
                new BigDecimal(range));
    }
}
