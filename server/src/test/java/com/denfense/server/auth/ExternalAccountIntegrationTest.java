package com.denfense.server.auth;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Import;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.test.web.servlet.MockMvc;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.util.*;
import java.util.concurrent.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.assertj.core.api.Assertions.*;

@SpringBootTest(properties = "spring.datasource.url=jdbc:h2:mem:external-auth;MODE=MySQL")
@ActiveProfiles("dev") @Import(ExternalAccountIntegrationTest.Fixture.class) @AutoConfigureMockMvc
class ExternalAccountIntegrationTest {
    @Autowired AuthService auth;
    @Autowired ExternalAccountService external;
    @Autowired MockMvc mvc;
    @Autowired ObjectMapper json;

    @TestConfiguration static class Fixture {
        // Explicit deterministic adapter fixture, NOT live Google identity validation.
        @Bean ExternalIdentityVerifier fakeVerifier() {
            return new ExternalIdentityVerifier() {
                public String provider() { return "GOOGLE"; }
                public VerifiedIdentity verify(String token, String nonce) {
                    String[] parts = token.split(":", -1);
                    if (parts.length < 2 || !parts[0].equals("valid") || !parts[1].equals(nonce))
                        throw AuthException.unauthorized();
                    return new VerifiedIdentity("test-only-issuer", parts.length == 3 ? parts[2] : "stable-test-subject");
                }
            };
        }
    }

    @Test void linkingKeepsAccountAndConflictsNeverMergeAccounts() {
        var first = auth.guest(AuthSecrets.randomToken());
        var second = auth.guest(AuthSecrets.randomToken());
        var link = external.challenge("GOOGLE", "LINK", first.user().userId());
        var request = new AuthDtos.ExternalRequest("GOOGLE", "valid:" + link.nonce(), link.challengeId());
        assertThat(external.link(first.user().userId(), request).isGuest()).isFalse();
        assertThat(auth.userInfo(first.user().userId()).userId()).isEqualTo(first.user().userId());
        assertThatThrownBy(() -> external.link(first.user().userId(), request)).isInstanceOf(AuthException.class);
        var other = external.challenge("GOOGLE", "LINK", second.user().userId());
        assertThatThrownBy(() -> external.link(second.user().userId(),
                new AuthDtos.ExternalRequest("GOOGLE", "valid:" + other.nonce(), other.challengeId())))
                .isInstanceOfSatisfying(AuthException.class, e -> assertThat(e.status().value()).isEqualTo(409));
        assertThat(auth.userInfo(second.user().userId()).isGuest()).isTrue();
        var login = external.challenge("GOOGLE", "LOGIN", null);
        var loggedIn = external.login(new AuthDtos.ExternalRequest("GOOGLE", "valid:" + login.nonce(), login.challengeId()));
        assertThat(loggedIn.user().userId()).isEqualTo(first.user().userId());
        assertThatThrownBy(() -> external.login(new AuthDtos.ExternalRequest("GOOGLE", "valid:" + login.nonce(), login.challengeId())))
                .isInstanceOf(AuthException.class);
    }

    @Test void challengeIsBoundToNoncePurposeAndAccount() {
        var first = auth.guest(AuthSecrets.randomToken()); var second = auth.guest(AuthSecrets.randomToken());
        var challenge = external.challenge("GOOGLE", "LINK", first.user().userId());
        var valid = new AuthDtos.ExternalRequest("GOOGLE", "valid:" + challenge.nonce(), challenge.challengeId());
        assertThatThrownBy(() -> external.link(second.user().userId(), valid)).isInstanceOf(AuthException.class);
        assertThatThrownBy(() -> external.login(valid)).isInstanceOf(AuthException.class);
        assertThatThrownBy(() -> external.link(first.user().userId(),
                new AuthDtos.ExternalRequest("GOOGLE", "invalid", challenge.challengeId()))).isInstanceOf(AuthException.class);
    }

    @Test void simultaneousHttpLinksReturnOneSuccessAndOneConflictWithoutMerging() throws Exception {
        String subject = UUID.randomUUID().toString();
        var sessions = List.of(auth.guest(AuthSecrets.randomToken()), auth.guest(AuthSecrets.randomToken()));
        var pool = Executors.newFixedThreadPool(2);
        try {
            var start = new CountDownLatch(1); List<Future<Integer>> futures = new ArrayList<>();
            for (var session : sessions) {
                var challenge = external.challenge("GOOGLE", "LINK", session.user().userId());
                String body = json.writeValueAsString(new AuthDtos.ExternalRequest("GOOGLE",
                        "valid:" + challenge.nonce() + ":" + subject, challenge.challengeId()));
                futures.add(pool.submit(() -> { start.await();
                    return mvc.perform(post("/api/auth/links").header("Authorization", "Bearer " + session.accessToken())
                            .contentType("application/json").content(body)).andReturn().getResponse().getStatus(); }));
            }
            start.countDown(); List<Integer> statuses = new ArrayList<>();
            for (var future : futures) statuses.add(future.get(20, TimeUnit.SECONDS));
            assertThat(statuses).containsExactlyInAnyOrder(200, 409);
            assertThat(sessions.stream().filter(s -> auth.userInfo(s.user().userId()).isGuest()).count()).isEqualTo(1);
            assertThat(sessions.get(0).user().userId()).isNotEqualTo(sessions.get(1).user().userId());
        } finally { pool.shutdownNow(); }
    }
}
