package com.denfense.server.auth;

import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.context.bean.override.mockito.MockitoSpyBean;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.transaction.support.TransactionTemplate;
import java.util.Optional;
import java.util.UUID;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicInteger;
import static org.assertj.core.api.Assertions.*;
import static org.mockito.Mockito.*;

@SpringBootTest(properties = "spring.datasource.url=jdbc:h2:mem:credential-insert-race;MODE=MySQL")
@ActiveProfiles("dev")
class CredentialInsertConcurrencyTest {
    @Autowired AuthService auth;
    @Autowired UserRepository users;
    @Autowired LocalDeveloperCredentialRepository locals;
    @Autowired CredentialInsertRepository inserts;
    @Autowired PlatformTransactionManager transactions;
    @MockitoSpyBean GuestCredentialRepository guests;

    @Test void twoEmptyReadsThenWinnerCommitCannotRebindGuestToLoserAccount() throws Exception {
        String secret = AuthSecrets.randomToken(); String hash = AuthSecrets.hash(secret);
        assertThat(guests.findById(hash)).isEmpty();
        long usersBefore = users.count();
        CountDownLatch firstRead = new CountDownLatch(1), secondRead = new CountDownLatch(1), winnerCommitted = new CountDownLatch(1);
        AtomicInteger reads = new AtomicInteger();
        // Both initial queries observe absence. The loser resumes only after the winner COMMIT,
        // reproducing the exact interleaving where save/merge would overwrite the winner's row.
        doAnswer(invocation -> {
            int read = reads.incrementAndGet();
            if (read == 1) {
                firstRead.countDown();
                if (!secondRead.await(15, TimeUnit.SECONDS)) throw new AssertionError("Second request did not reach absent read");
                return Optional.empty();
            }
            if (read == 2) {
                secondRead.countDown();
                if (!winnerCommitted.await(15, TimeUnit.SECONDS)) throw new AssertionError("Winner did not commit");
                return Optional.empty();
            }
            // Spring Data's repository method is abstract on the spy interface;
            // use a separate real repository query for the retry after rollback.
            return guests.findAll().stream().filter(c -> c.getSecretHash().equals(hash)).findFirst();
        }).when(guests).findById(hash);
        ExecutorService pool = Executors.newFixedThreadPool(2);
        try {
            Future<AuthDtos.SessionResponse> first = pool.submit(() -> {
                try { return auth.guest(secret); } finally { winnerCommitted.countDown(); }
            });
            assertThat(firstRead.await(15, TimeUnit.SECONDS)).isTrue();
            Future<AuthDtos.SessionResponse> second = pool.submit(() -> auth.guest(secret));
            var winner = first.get(30, TimeUnit.SECONDS);
            var loser = second.get(30, TimeUnit.SECONDS);
            assertThat(loser.user().userId()).isEqualTo(winner.user().userId());
            assertThat(guests.findById(hash).orElseThrow().getUser().getId()).isEqualTo(winner.user().userId());
            assertThat(users.count()).isEqualTo(usersBefore + 1); // Loser's transient User rolls back.
            assertThat(auth.authenticate(winner.accessToken()).userId()).isEqualTo(winner.user().userId());
            assertThat(auth.authenticate(loser.accessToken()).userId()).isEqualTo(winner.user().userId());
        } finally {
            winnerCommitted.countDown(); secondRead.countDown(); pool.shutdownNow(); reset(guests);
        }
    }

    @Test void duplicateLocalCredentialInsertNeverOverwritesOwnerOrHashAndRollsBackNewUser() {
        String name = "insert-" + UUID.randomUUID();
        var tx = new TransactionTemplate(transactions);
        Long winner = tx.execute(status -> {
            User user = users.saveAndFlush(new User(name, null));
            inserts.insertLocal(new LocalDeveloperCredential(name, user, "winner-test-hash"));
            return user.getId();
        });
        long usersBefore = users.count();
        assertThatThrownBy(() -> tx.executeWithoutResult(status -> {
            User loser = users.saveAndFlush(new User("loser-" + UUID.randomUUID(), null));
            inserts.insertLocal(new LocalDeveloperCredential(name, loser, "loser-test-hash"));
        })).isInstanceOf(DataIntegrityViolationException.class);
        var credential = locals.findById(name).orElseThrow();
        assertThat(credential.getUser().getId()).isEqualTo(winner);
        assertThat(credential.getPasswordHash()).isEqualTo("winner-test-hash");
        assertThat(users.count()).isEqualTo(usersBefore);
    }
}
