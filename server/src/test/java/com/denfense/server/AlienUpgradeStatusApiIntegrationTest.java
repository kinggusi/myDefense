package com.denfense.server;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.web.servlet.MockMvc;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@AutoConfigureMockMvc
class AlienUpgradeStatusApiIntegrationTest {

    @Autowired MockMvc mockMvc;
    @Autowired UserRepository userRepository;
    @Autowired AlienSpecRepository alienSpecRepository;
    @Autowired UserAlienRepository userAlienRepository;

    private User user;
    private AlienSpec unlockedSpec;
    private AlienSpec lockedSpec;
    private UserAlien userAlien;

    @BeforeEach
    void setUp() {
        userAlienRepository.deleteAll();
        userRepository.deleteAll();
        alienSpecRepository.deleteAll();

        unlockedSpec = spec(100L, false);
        lockedSpec = spec(101L, true);
        alienSpecRepository.save(unlockedSpec);
        alienSpecRepository.save(lockedSpec);

        user = new User("upgrade-status-user", "pw");
        user.setGold(100_000);
        user.setUniversalPiece(1_000);
        user.setGrowthCell(1_000);
        user = userRepository.save(user);

        userAlien = new UserAlien(user, unlockedSpec);
        userAlien.setLevel(1);
        userAlien.setPieces(5);
        userAlien = userAlienRepository.save(userAlien);
    }

    @Test
    void returnsOwnedUpgradeStatusWithCostsAndCurrentStats() throws Exception {
        mockMvc.perform(get("/api/aliens/100/upgrade-status").param("username", user.getUsername()))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.owned").value(true))
                .andExpect(jsonPath("$.specLocked").value(false))
                .andExpect(jsonPath("$.currentLevel").value(1))
                .andExpect(jsonPath("$.requiredPieces").value(5))
                .andExpect(jsonPath("$.requiredUniversalPiece").value(0))
                .andExpect(jsonPath("$.requiredGold").value(100))
                .andExpect(jsonPath("$.requiredGrowthCell").value(0))
                .andExpect(jsonPath("$.maxLevel").value(50))
                .andExpect(jsonPath("$.canUpgrade").value(true))
                .andExpect(jsonPath("$.cannotUpgradeReason").value("NONE"))
                .andExpect(jsonPath("$.currentAtk").value(100.0))
                .andExpect(jsonPath("$.currentMp").value(80.0))
                .andExpect(jsonPath("$.currentAtkSpeed").value(1.25))
                .andExpect(jsonPath("$.currentRange").value(4.5));
    }

    @Test
    void returnsNotOwnedAndSpecLockedStates() throws Exception {
        mockMvc.perform(get("/api/aliens/101/upgrade-status").param("username", user.getUsername()))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.owned").value(false))
                .andExpect(jsonPath("$.specLocked").value(true))
                .andExpect(jsonPath("$.canUpgrade").value(false))
                .andExpect(jsonPath("$.cannotUpgradeReason").value("NOT_OWNED"))
                .andExpect(jsonPath("$.requiredPieces").value(0));

        UserAlien lockedOwned = new UserAlien(user, lockedSpec);
        lockedOwned.setPieces(100);
        userAlienRepository.save(lockedOwned);
        mockMvc.perform(get("/api/aliens/101/upgrade-status").param("username", user.getUsername()))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.owned").value(true))
                .andExpect(jsonPath("$.cannotUpgradeReason").value("SPEC_LOCKED"));
    }

    @Test
    void reportsResourceShortagesAndUniversalPieceRequirement() throws Exception {
        userAlien.setPieces(2);
        userAlienRepository.save(userAlien);
        user.setUniversalPiece(3);
        user.setGold(99);
        userRepository.save(user);
        mockMvc.perform(get("/api/aliens/100/upgrade-status").param("username", user.getUsername()))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.requiredUniversalPiece").value(3))
                .andExpect(jsonPath("$.cannotUpgradeReason").value("INSUFFICIENT_GOLD"));

        user.setUniversalPiece(2);
        user.setGold(100_000);
        userRepository.save(user);
        mockMvc.perform(get("/api/aliens/100/upgrade-status").param("username", user.getUsername()))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.requiredUniversalPiece").value(3))
                .andExpect(jsonPath("$.cannotUpgradeReason").value("INSUFFICIENT_PIECES"));

        userAlien.setLevel(9);
        userAlien.setPieces(45);
        userAlienRepository.save(userAlien);
        user.setUniversalPiece(0);
        user.setGold(100_000);
        user.setGrowthCell(9);
        userRepository.save(user);
        mockMvc.perform(get("/api/aliens/100/upgrade-status").param("username", user.getUsername()))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.requiredGrowthCell").value(10))
                .andExpect(jsonPath("$.cannotUpgradeReason").value("INSUFFICIENT_GROWTH_CELL"));
    }

    @Test
    void maxLevelStatusHasNoNextCost() throws Exception {
        userAlien.setLevel(50);
        userAlienRepository.save(userAlien);

        mockMvc.perform(get("/api/aliens/100/upgrade-status").param("username", user.getUsername()))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.maxLevelReached").value(true))
                .andExpect(jsonPath("$.canUpgrade").value(false))
                .andExpect(jsonPath("$.cannotUpgradeReason").value("MAX_LEVEL"))
                .andExpect(jsonPath("$.requiredPieces").value(0))
                .andExpect(jsonPath("$.requiredUniversalPiece").value(0))
                .andExpect(jsonPath("$.requiredGold").value(0))
                .andExpect(jsonPath("$.requiredGrowthCell").value(0));
    }

    @Test
    void upgradeResponseContainsNextCostsAndCurrentStats() throws Exception {
        userAlien.setPieces(2);
        userAlienRepository.save(userAlien);
        mockMvc.perform(post("/api/aliens/100/upgrade").param("username", user.getUsername()))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.requiredPieces").value(5))
                .andExpect(jsonPath("$.usedPieces").value(2))
                .andExpect(jsonPath("$.usedUniversalPiece").value(3))
                .andExpect(jsonPath("$.afterLevel").value(2))
                .andExpect(jsonPath("$.nextRequiredPieces").value(10))
                .andExpect(jsonPath("$.nextRequiredUniversalPiece").value(10))
                .andExpect(jsonPath("$.nextRequiredGold").value(200))
                .andExpect(jsonPath("$.currentAtk").value(105.0))
                .andExpect(jsonPath("$.currentMp").value(82.4))
                .andExpect(jsonPath("$.canUpgrade").value(true))
                .andExpect(jsonPath("$.cannotUpgradeReason").value("NONE"));
    }

    @Test
    void level49UpgradeReturnsMaxAndZeroNextCosts() throws Exception {
        userAlien.setLevel(49);
        userAlien.setPieces(245);
        userAlienRepository.save(userAlien);
        mockMvc.perform(post("/api/aliens/100/upgrade").param("username", user.getUsername()))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.afterLevel").value(50))
                .andExpect(jsonPath("$.usedGrowthCell").value(50))
                .andExpect(jsonPath("$.maxLevelReached").value(true))
                .andExpect(jsonPath("$.canUpgrade").value(false))
                .andExpect(jsonPath("$.cannotUpgradeReason").value("MAX_LEVEL"))
                .andExpect(jsonPath("$.nextRequiredPieces").value(0))
                .andExpect(jsonPath("$.nextRequiredGold").value(0));
    }

    @Test
    void lockedUpgradeAndMissingSpecReturnStableErrorsWithoutMutation() throws Exception {
        UserAlien lockedOwned = new UserAlien(user, lockedSpec);
        lockedOwned.setPieces(100);
        lockedOwned = userAlienRepository.save(lockedOwned);
        int beforeGold = user.getGold();
        int beforeUniversalPiece = user.getUniversalPiece();
        int beforeGrowthCell = user.getGrowthCell();

        mockMvc.perform(post("/api/aliens/101/upgrade").param("username", user.getUsername()))
                .andExpect(status().isConflict())
                .andExpect(jsonPath("$.code").value("ALIEN_SPEC_LOCKED"));
        UserAlien unchanged = userAlienRepository.findById(lockedOwned.getId()).orElseThrow();
        assertThat(unchanged.getLevel()).isEqualTo(1);
        assertThat(unchanged.getPieces()).isEqualTo(100);
        User unchangedUser = userRepository.findById(user.getId()).orElseThrow();
        assertThat(unchangedUser.getGold()).isEqualTo(beforeGold);
        assertThat(unchangedUser.getUniversalPiece()).isEqualTo(beforeUniversalPiece);
        assertThat(unchangedUser.getGrowthCell()).isEqualTo(beforeGrowthCell);

        mockMvc.perform(post("/api/aliens/999/upgrade").param("username", user.getUsername()))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath("$.code").value("ALIEN_SPEC_NOT_FOUND"));
    }

    private AlienSpec spec(long id, boolean locked) {
        AlienSpec spec = new AlienSpec();
        spec.setId(id);
        spec.setName("Alien-" + id);
        spec.setGrade(AlienSpec.Grade.NORMAL);
        spec.setBaseAtk(100);
        spec.setBaseMp(80);
        spec.setAtkSpeed(1.25);
        spec.setRange(4.5);
        spec.setLocked(locked);
        return spec;
    }
}
