package com.denfense.server.auth;

import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import static org.assertj.core.api.Assertions.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest(properties = {"spring.datasource.url=jdbc:h2:mem:local-account-tests;MODE=MySQL",
        "mydefense.auth.local-accounts.enabled=true"})
@ActiveProfiles("local") @AutoConfigureMockMvc
class LocalDeveloperAccountIntegrationTest {
    @Autowired LocalDeveloperAccountService service;
    @Autowired LocalDeveloperCredentialRepository credentials;
    @Autowired UserRepository users;
    @Autowired MockMvc mvc;
    @Autowired ObjectMapper json;

    @Test void passwordIsHashedLoginUsesJwtAndBootstrapNeverResetsProgress() throws Exception {
        String name = "local-" + UUID.randomUUID(); String password = UUID.randomUUID().toString();
        service.provision(List.of(name), password);
        var credential = credentials.findById(name).orElseThrow();
        assertThat(credential.getPasswordHash()).isNotEqualTo(password);
        assertThat(new BCryptPasswordEncoder().matches(password, credential.getPasswordHash())).isTrue();
        User user = users.findByUsername(name).orElseThrow();
        assertThat(user.getPassword()).isNull(); assertThat(user.getGold()).isZero();
        user.setGold(3456); users.saveAndFlush(user);
        service.provision(List.of(name), UUID.randomUUID().toString());
        assertThat(users.findByUsername(name).orElseThrow().getGold()).isEqualTo(3456);
        assertThat(credentials.findById(name).orElseThrow().getPasswordHash()).isEqualTo(credential.getPasswordHash());
        String result = mvc.perform(post("/api/dev/auth/login").contentType("application/json")
                        .content(json.writeValueAsString(Map.of("username", name, "password", password))))
                .andExpect(status().isOk()).andExpect(jsonPath("$.user.isGuest").value(false))
                .andExpect(header().string("Cache-Control", "no-store"))
                .andReturn().getResponse().getContentAsString();
        var session = json.readValue(result, AuthDtos.SessionResponse.class);
        mvc.perform(get("/api/auth/me").header("Authorization", "Bearer " + session.accessToken()))
                .andExpect(status().isOk()).andExpect(jsonPath("$.username").value(name))
                .andExpect(jsonPath("$.isGuest").value(false));
        mvc.perform(post("/api/dev/auth/login").contentType("application/json")
                        .content(json.writeValueAsString(Map.of("username", name, "password", "incorrect-value"))))
                .andExpect(status().isUnauthorized());
    }

    @Test void remoteRequestsAndExistingAccountAdoptionAreRejected() throws Exception {
        mvc.perform(post("/api/dev/auth/login").with(r -> { r.setRemoteAddr("203.0.113.8"); return r; })
                        .contentType("application/json").content("{\"username\":\"some-user\",\"password\":\"some-password\"}"))
                .andExpect(status().isForbidden());
        String existing = "taken-" + UUID.randomUUID();
        users.saveAndFlush(new User(existing, null));
        assertThatThrownBy(() -> service.provision(List.of(existing), UUID.randomUUID().toString()))
                .isInstanceOf(IllegalStateException.class);
        assertThat(credentials.existsById(existing)).isFalse();
    }

    @Test void invalidBootstrapIsAtomicAndGuestAuthStillWorks() {
        String first = "atomic-" + UUID.randomUUID();
        assertThatThrownBy(() -> service.provision(List.of(first, "bad/name"), UUID.randomUUID().toString()))
                .isInstanceOf(IllegalArgumentException.class);
        assertThat(users.findByUsername(first)).isEmpty();
        assertThatThrownBy(() -> service.login("unknown", "한".repeat(30)))
                .isInstanceOf(AuthException.class);
    }
}
