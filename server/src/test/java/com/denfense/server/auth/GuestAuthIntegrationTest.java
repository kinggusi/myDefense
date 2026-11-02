package com.denfense.server.auth;

import com.denfense.server.repository.UserRepository;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;
import java.util.*;
import java.util.concurrent.*;
import static org.assertj.core.api.Assertions.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest(properties = "spring.datasource.url=jdbc:h2:mem:auth-flow;MODE=MySQL")
@AutoConfigureMockMvc @ActiveProfiles("dev")
class GuestAuthIntegrationTest {
    @Autowired AuthService auth;
    @Autowired UserRepository users;
    @Autowired MockMvc mvc;
    @Autowired ObjectMapper json;

    @Test void guestIsIdempotentStartsWithPolicyHeartAndPublicDto() throws Exception {
        String secret = AuthSecrets.randomToken();
        var response = mvc.perform(post("/api/auth/guest").contentType("application/json")
                        .content(json.writeValueAsString(Map.of("guestSecret", secret))))
                .andExpect(status().isOk()).andExpect(header().string("Cache-Control", "no-store"))
                .andExpect(header().string("Pragma", "no-cache")).andReturn().getResponse().getContentAsString();
        var session = json.readValue(response, AuthDtos.SessionResponse.class);
        assertThat(auth.guest(secret).user()).isEqualTo(session.user());
        var user = users.findById(session.user().userId()).orElseThrow();
        assertThat(user.getHeart()).isEqualTo(100);
        assertThat(user.getGold()).isZero(); assertThat(user.getDiamond()).isZero();
        mvc.perform(get("/api/auth/me").header("Authorization", "Bearer " + session.accessToken()))
                .andExpect(status().isOk()).andExpect(jsonPath("$.userId").value(user.getId()));
        mvc.perform(get("/api/lobby/info/" + user.getUsername()).header("Authorization", "Bearer " + session.accessToken()))
                .andExpect(status().isOk());
        mvc.perform(get("/api/users/" + user.getUsername()).header("Authorization", "Bearer " + session.accessToken()))
                .andExpect(status().isOk()).andExpect(jsonPath("$.password").doesNotExist())
                .andExpect(jsonPath("$.gold").doesNotExist());
    }

    @Test void authenticatedCrossAccountAndInvalidAuthorizationNeverUseLegacyFallback() throws Exception {
        var session = auth.guest(AuthSecrets.randomToken());
        for (String path : List.of("/api/lobby/info/sh1", "/api/users/sh1", "/api/quests", "/api/daily-contents")) {
            mvc.perform(get(path).param("username", "sh1").header("Authorization", "Bearer " + session.accessToken()))
                    .andExpect(status().isForbidden());
        }
        mvc.perform(get("/api/lobby/info/sh1").header("Authorization", "Bearer invalid"))
                .andExpect(status().isUnauthorized());
        mvc.perform(get("/api/lobby/info/sh1").with(r -> { r.setRemoteAddr("203.0.113.1"); return r; }))
                .andExpect(status().isUnauthorized());
    }

    @Test void refreshReplayRevokesWholeFamilyAndSecretRecoversSameAccount() {
        String secret = AuthSecrets.randomToken(); var first = auth.guest(secret);
        var rotated = auth.refresh(first.refreshToken());
        assertThat(rotated.refreshToken()).isNotEqualTo(first.refreshToken());
        assertThatThrownBy(() -> auth.refresh(first.refreshToken())).isInstanceOf(AuthException.class);
        assertThatThrownBy(() -> auth.refresh(rotated.refreshToken())).isInstanceOf(AuthException.class);
        assertThatThrownBy(() -> auth.authenticate(rotated.accessToken())).isInstanceOf(AuthException.class);
        var recovered = auth.guest(secret);
        assertThat(recovered.user()).isEqualTo(first.user());
        assertThat(auth.authenticate(recovered.accessToken()).userId()).isEqualTo(first.user().userId());
    }

    @Test void logoutOnlyRevokesItsFamily() {
        String secret = AuthSecrets.randomToken(); var first = auth.guest(secret); var second = auth.guest(secret);
        auth.logout(first.refreshToken());
        assertThatThrownBy(() -> auth.authenticate(first.accessToken())).isInstanceOf(AuthException.class);
        assertThat(auth.authenticate(second.accessToken()).userId()).isEqualTo(first.user().userId());
    }

    @Test void concurrentGuestRequestsResolveOneAccount() throws Exception {
        String secret = AuthSecrets.randomToken(); var pool = Executors.newFixedThreadPool(4);
        try {
            var start = new CountDownLatch(1); List<Future<Long>> futures = new ArrayList<>();
            for (int i = 0; i < 4; i++) futures.add(pool.submit(() -> { start.await(); return auth.guest(secret).user().userId(); }));
            start.countDown(); Set<Long> ids = new HashSet<>();
            for (var future : futures) ids.add(future.get(20, TimeUnit.SECONDS));
            assertThat(ids).hasSize(1);
        } finally { pool.shutdownNow(); }
    }

    @Test void simultaneousRefreshAllowsOneRotationThenRevokesFamilyOnReplay() throws Exception {
        var first = auth.guest(AuthSecrets.randomToken()); var pool = Executors.newFixedThreadPool(2);
        try {
            var start = new CountDownLatch(1); List<Future<AuthDtos.SessionResponse>> futures = new ArrayList<>();
            for (int i = 0; i < 2; i++) futures.add(pool.submit(() -> { start.await();
                try { return auth.refresh(first.refreshToken()); } catch (AuthException expected) { return null; } }));
            start.countDown(); List<AuthDtos.SessionResponse> successes = new ArrayList<>();
            for (var future : futures) { var result = future.get(20, TimeUnit.SECONDS); if (result != null) successes.add(result); }
            assertThat(successes).hasSize(1);
            assertThatThrownBy(() -> auth.authenticate(successes.get(0).accessToken())).isInstanceOf(AuthException.class);
        } finally { pool.shutdownNow(); }
    }

    @Test void unconfiguredProvidersAreExplicitlyUnavailable() throws Exception {
        mvc.perform(get("/api/auth/providers")).andExpect(status().isOk())
                .andExpect(jsonPath("$.providers[0].available").value(false))
                .andExpect(jsonPath("$.providers[1].available").value(false));
        mvc.perform(post("/api/auth/challenges").contentType("application/json")
                .content("{\"provider\":\"GOOGLE\",\"purpose\":\"LOGIN\"}"))
                .andExpect(status().isServiceUnavailable());
    }

    @Test void allAccountEconomyWritesRejectAnotherUsernameBeforeMutating() throws Exception {
        var session = auth.guest(AuthSecrets.randomToken()); String bearer = "Bearer " + session.accessToken();
        mvc.perform(post("/api/aliens/1/upgrade").param("username", "sh1").header("Authorization", bearer))
                .andExpect(status().isForbidden());
        mvc.perform(post("/api/shop/gacha/purchase").param("username", "sh1").param("productId", "any")
                .param("purchaseRequestId", UUID.randomUUID().toString()).header("Authorization", bearer))
                .andExpect(status().isForbidden());
        for (String action : List.of("unlock", "start", "claim", "accelerate")) {
            mvc.perform(post("/api/mythic-breeding/slots/1/" + action).param("username", "sh1")
                    .header("Authorization", bearer).contentType("application/json")
                    .content("{\"requestId\":\"auth-cross-account-test\"}"))
                    .andExpect(status().isForbidden());
        }
        Map<String, Object> claims = new HashMap<>(Map.of("requestId", "auth-cross-account-test", "username", "sh1",
                "questId", "any", "cycleType", "DAILY", "achievementId", "any"));
        for (String path : List.of("/api/quests/claims", "/api/quests/milestone-claims", "/api/quests/achievement-claims")) {
            mvc.perform(post(path).header("Authorization", bearer).contentType("application/json")
                    .content(json.writeValueAsString(claims))).andExpect(status().isForbidden());
        }
        for (String action : List.of("entries", "sweeps")) {
            mvc.perform(post("/api/daily-contents/" + action).header("Authorization", bearer).contentType("application/json")
                    .content("{\"requestId\":\"auth-cross-account-test\",\"username\":\"sh1\",\"contentType\":\"CULTIVATION_ZONE\",\"stage\":1}"))
                    .andExpect(status().isForbidden());
        }
    }
}
