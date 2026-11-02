package com.denfense.server.auth;

import com.denfense.server.DefenseServerApplication;
import com.denfense.server.domain.*;
import com.denfense.server.repository.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import org.springframework.boot.WebApplicationType;
import org.springframework.boot.builder.SpringApplicationBuilder;
import org.springframework.context.ConfigurableApplicationContext;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.transaction.support.TransactionTemplate;
import java.nio.file.Path;
import java.time.Instant;
import java.util.*;
import static org.assertj.core.api.Assertions.*;

class LocalPersistenceRestartTest {
    @TempDir Path directory;

    @Test void closingAndReopeningFileDatabasePreservesAccountWalletInventoryBreedingAndRefresh() {
        String url = "jdbc:h2:file:" + directory.resolve("restart").toAbsolutePath().toString().replace('\\', '/') + ";MODE=MySQL";
        String key = Base64.getEncoder().encodeToString(new byte[32]); // Isolated test key, never production configuration.
        String password = UUID.randomUUID().toString();
        AuthDtos.SessionResponse first;
        Map<String, Object> wallet, inventory, breeding;
        try (var context = start(url, key)) {
            context.getBean(LocalDeveloperAccountService.class).provision(List.of("restart-account"), password);
            first = context.getBean(LocalDeveloperAccountService.class).login("restart-account", password);
            new TransactionTemplate(context.getBean(PlatformTransactionManager.class)).executeWithoutResult(status -> {
                var users = context.getBean(UserRepository.class);
                User user = users.findById(first.user().userId()).orElseThrow();
                user.setGold(2501); user.setDiamond(370); user.setUniversalPiece(17); user.setGrowthCell(9);
                UserAlien alien = new UserAlien(user, context.getBean(AlienSpecRepository.class).findById(29L).orElseThrow());
                alien.setLevel(4); alien.setPieces(23);
                context.getBean(UserAlienRepository.class).saveAndFlush(alien);
                MythicBreedingSlot slot = new MythicBreedingSlot(user, 1, MythicBreedingSlotStatus.AVAILABLE, MythicBreedingUnlockSource.DEFAULT);
                slot.start(30L, Instant.now(), Instant.now().plusSeconds(86400), "restart-breeding");
                context.getBean(MythicBreedingSlotRepository.class).saveAndFlush(slot);
            });
            var jdbc = context.getBean(JdbcTemplate.class);
            wallet = jdbc.queryForMap("select id, gold, diamond, universal_piece, growth_cell from users where username='restart-account'");
            inventory = jdbc.queryForMap("select alien_id, level, pieces from user_aliens where user_id=?", first.user().userId());
            breeding = jdbc.queryForMap("select status, result_alien_id, ready_at, start_request_id from mythic_breeding_slots where user_id=?", first.user().userId());
        }
        assertThat(directory.resolve("restart.mv.db")).exists();
        try (var context = start(url, key)) {
            context.getBean(LocalDeveloperAccountService.class).provision(List.of("restart-account"), UUID.randomUUID().toString());
            var again = context.getBean(LocalDeveloperAccountService.class).login("restart-account", password);
            assertThat(again.user()).isEqualTo(first.user());
            var jdbc = context.getBean(JdbcTemplate.class);
            assertThat(jdbc.queryForMap("select id, gold, diamond, universal_piece, growth_cell from users where username='restart-account'")).isEqualTo(wallet);
            assertThat(jdbc.queryForMap("select alien_id, level, pieces from user_aliens where user_id=?", first.user().userId())).isEqualTo(inventory);
            assertThat(jdbc.queryForMap("select status, result_alien_id, ready_at, start_request_id from mythic_breeding_slots where user_id=?", first.user().userId())).isEqualTo(breeding);
            var auth = context.getBean(AuthService.class);
            assertThat(auth.authenticate(first.accessToken()).userId()).isEqualTo(first.user().userId());
            assertThat(auth.refresh(first.refreshToken()).user()).isEqualTo(first.user());
        }
    }

    private ConfigurableApplicationContext start(String url, String key) {
        return new SpringApplicationBuilder(DefenseServerApplication.class).web(WebApplicationType.NONE).profiles("local").run(
                "--spring.datasource.url=" + url, "--spring.jpa.hibernate.ddl-auto=update",
                "--spring.jpa.show-sql=false", "--spring.sql.init.mode=never",
                "--mydefense.auth.local-accounts.enabled=true", "--mydefense.auth.local-accounts.bootstrap-enabled=false",
                "--mydefense.auth.signing-key-base64=" + key);
    }
}
