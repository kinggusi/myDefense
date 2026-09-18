package com.denfense.server.auth;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;
import com.denfense.server.repository.UserRepository;
import static org.assertj.core.api.Assertions.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest(properties = {"spring.datasource.url=jdbc:h2:mem:prod-auth;MODE=MySQL",
        "mydefense.auth.local-accounts.enabled=true", "mydefense.auth.local-accounts.bootstrap-enabled=true"})
@ActiveProfiles({"local", "prod"}) @AutoConfigureMockMvc
class ProductionAuthBoundaryTest {
    @Autowired MockMvc mvc;
    @Autowired UserRepository users;
    @Autowired org.springframework.context.ApplicationContext context;
    @Test void mixedProfileNeverSeedsOrEnablesDevelopmentAuthentication() throws Exception {
        assertThat(users.findByUsername("sh1")).isEmpty();
        assertThat(context.getBeansOfType(LocalDeveloperAccountService.class)).isEmpty();
        assertThat(context.getBeansOfType(LocalDeveloperAccountBootstrap.class)).isEmpty();
        assertThat(context.getBeansOfType(LocalDeveloperLoginController.class)).isEmpty();
        mvc.perform(post("/api/auth/guest").contentType("application/json")
                .content("{\"guestSecret\":\"" + AuthSecrets.randomToken() + "\"}"))
                .andExpect(status().isServiceUnavailable());
        mvc.perform(get("/api/lobby/info/sh1")).andExpect(status().isUnauthorized());
        mvc.perform(get("/h2-console/")).andExpect(status().isForbidden());
        mvc.perform(get("/api/quests").param("username", "sh1")).andExpect(status().isNotFound());
    }
}
