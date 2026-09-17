package com.denfense.server.service;

import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.CsvSource;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.mock.env.MockEnvironment;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.transaction.annotation.Transactional;

import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@AutoConfigureMockMvc
@ActiveProfiles("local")
@Transactional
class StarterMythicCollectionIntegrationTest {
    @Autowired private UserRepository users;
    @Autowired private UserAlienRepository inventory;
    @Autowired private AlienSpecRepository specs;
    @Autowired private StarterAlienCollectionService service;
    @Autowired private MockMvc mvc;

    @Test
    void localLobbyGrantsOnlyTwoPreviewMythicsAndRepeatedReadsPreserveProgress() throws Exception {
        User user = users.saveAndFlush(new User("starter-" + UUID.randomUUID(), "test"));
        UserAlien existing = new UserAlien(user, specs.findById(29L).orElseThrow());
        existing.setLevel(4);
        existing.setPieces(17);
        inventory.saveAndFlush(existing);

        for (int i = 0; i < 2; i++) {
            mvc.perform(get("/api/lobby/info/{username}", user.getUsername()))
                    .andExpect(status().isOk())
                    .andExpect(jsonPath("$.aliens[28].id").value(29))
                    .andExpect(jsonPath("$.aliens[28].owned").value(true))
                    .andExpect(jsonPath("$.aliens[28].level").value(4))
                    .andExpect(jsonPath("$.aliens[28].pieces").value(17))
                    .andExpect(jsonPath("$.aliens[29].id").value(30))
                    .andExpect(jsonPath("$.aliens[29].owned").value(true))
                    .andExpect(jsonPath("$.aliens[29].level").value(1))
                    .andExpect(jsonPath("$.aliens[29].pieces").value(0))
                    .andExpect(jsonPath("$.aliens[30].owned").value(false))
                    .andExpect(jsonPath("$.aliens[47].owned").value(false));
        }
        assertThat(inventory.findAllByUser(user)).hasSize(30);
    }

    @Test
    void freshLocalAccountReceivesBothAtLevelOneWithoutPieces() {
        User user = users.saveAndFlush(new User("starter-" + UUID.randomUUID(), "test"));
        service.ensureStarterCollection(user.getUsername());
        assertThat(inventory.findAllByUser(user)).hasSize(30);
        for (long id : new long[] {29L, 30L}) {
            UserAlien owned = inventory.findByUserAndAlienSpec(user, specs.findById(id).orElseThrow()).orElseThrow();
            assertThat(owned.getLevel()).isEqualTo(1);
            assertThat(owned.getPieces()).isZero();
        }
    }

    @ParameterizedTest
    @CsvSource({
            "none, true, 28", "none, missing, 28", "prod, true, 28", "production, true, 28",
            "local+prod, true, 28", "dev+production, true, 28", "local+production, true, 28",
            "dev+prod, true, 28", "dev, missing, 28", "dev, false, 28", "local, false, 28",
            "local, true, 30", "dev, true, 30"
    })
    void profileAndOptInBoundariesAreFailClosed(String profiles, String flag, int expectedCount) {
        MockEnvironment environment = new MockEnvironment();
        if (!profiles.equals("none")) environment.setActiveProfiles(profiles.split("\\+"));
        if (!flag.equals("missing")) environment.setProperty("mydefense.collection.tutorial-mythic-preview-enabled", flag);
        StarterAlienCollectionService isolated = new StarterAlienCollectionService(users, specs, inventory, environment);
        User user = users.saveAndFlush(new User("starter-" + UUID.randomUUID(), "test"));
        isolated.ensureStarterCollection(user.getUsername());
        isolated.ensureStarterCollection(user.getUsername());
        assertThat(inventory.findAllByUser(user)).hasSize(expectedCount);
        assertThat(inventory.findAllByUser(user).stream().filter(a -> a.getAlienSpec().getId() >= 29)
                .map(a -> a.getAlienSpec().getId()).toList())
                .containsExactlyInAnyOrderElementsOf(expectedCount == 30 ? java.util.List.of(29L, 30L) : java.util.List.of());
    }
}
