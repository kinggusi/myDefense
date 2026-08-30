package com.denfense.server.controller;

import com.denfense.server.domain.User;
import com.denfense.server.dto.battle.BattleSessionRosterDtos;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.repository.BattleEntryReservationRepository;
import com.denfense.server.service.BattleSessionRosterRegistry;
import com.denfense.server.service.balance.BalanceVersionRegistry;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.context.ActiveProfiles;

import java.util.List;
import java.util.UUID;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@AutoConfigureMockMvc
@ActiveProfiles("local")
class LocalBattleSessionRosterControllerIntegrationTest {
    @Autowired MockMvc mockMvc;
    @Autowired ObjectMapper objectMapper;
    @Autowired UserRepository users;
    @Autowired BalanceVersionRegistry versions;
    @Autowired BattleSessionRosterRegistry rosters;
    @Autowired BattleEntryReservationRepository entryReservations;

    @Test
    void loopbackFusionAuthorityRegistersExactlyTwoExistingPlayers() throws Exception {
        String suffix = UUID.randomUUID().toString().replace("-", "");
        User one = users.save(new User("roster-p1-" + suffix, "pw"));
        User two = users.save(new User("roster-p2-" + suffix, "pw"));
        String sessionId = "roster-session-" + suffix;

        mockMvc.perform(post("/api/dev/battle/session-rosters")
                        .with(request -> { request.setRemoteAddr("127.0.0.1"); return request; })
                        .contentType("application/json")
                        .content(objectMapper.writeValueAsString(validRequest(sessionId, one, two))))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.battleSessionId").value(sessionId))
                .andExpect(jsonPath("$.status").value("REGISTERED"))
                .andExpect(jsonPath("$.playerCount").value(2));

        rosters.requireComplete(sessionId);
    }

    @Test
    void nonLoopbackCallerIsRejectedWithoutPopulatingTrustedRoster() throws Exception {
        String suffix = UUID.randomUUID().toString().replace("-", "");
        User one = users.save(new User("remote-p1-" + suffix, "pw"));
        User two = users.save(new User("remote-p2-" + suffix, "pw"));
        String sessionId = "remote-session-" + suffix;

        mockMvc.perform(post("/api/dev/battle/session-rosters")
                        .with(request -> { request.setRemoteAddr("192.0.2.10"); return request; })
                        .contentType("application/json")
                        .content(objectMapper.writeValueAsString(validRequest(sessionId, one, two))))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.code").value("BATTLE_ROSTER_REGISTRATION_FORBIDDEN"));
    }

    @Test
    void duplicateSlotOrUnknownUserIsRejected() throws Exception {
        String suffix = UUID.randomUUID().toString().replace("-", "");
        User one = users.save(new User("invalid-p1-" + suffix, "pw"));
        var request = new BattleSessionRosterDtos.RegisterRequest(
                "invalid-session-" + suffix,
                "NEPTUNE",
                versions.getBalanceVersion(),
                versions.getContentHash(),
                List.of(
                        new BattleSessionRosterDtos.Player(1, one.getUsername()),
                        new BattleSessionRosterDtos.Player(1, "missing-" + suffix)));

        mockMvc.perform(post("/api/dev/battle/session-rosters")
                        .with(http -> { http.setRemoteAddr("127.0.0.1"); return http; })
                        .contentType("application/json")
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.code").value("BATTLE_ROSTER_REGISTRATION_INVALID"));
    }

    @Test
    void localDevIdentityIsProvisionedOnlyForExplicitDevPrefix() throws Exception {
        String suffix = UUID.randomUUID().toString().replace("-", "");
        String one = "dev-host-" + suffix;
        String two = "dev-client-" + suffix;
        var request = new BattleSessionRosterDtos.RegisterRequest(
                "dev-session-" + suffix,
                "NEPTUNE",
                versions.getBalanceVersion(),
                versions.getContentHash(),
                List.of(
                        new BattleSessionRosterDtos.Player(1, one),
                        new BattleSessionRosterDtos.Player(2, two)));

        mockMvc.perform(post("/api/dev/battle/session-rosters")
                        .with(http -> { http.setRemoteAddr("127.0.0.1"); return http; })
                        .contentType("application/json")
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.status").value("REGISTERED"));

        org.assertj.core.api.Assertions.assertThat(users.findByUsername(one)).isPresent();
        org.assertj.core.api.Assertions.assertThat(users.findByUsername(two)).isPresent();
    }

    @Test
    void registrationChargesHeartsAndTrustedRefundEndpointIsIdempotent() throws Exception {
        String suffix = UUID.randomUUID().toString().replace("-", "");
        User one = new User("refund-p1-" + suffix, "pw");
        User two = new User("refund-p2-" + suffix, "pw");
        one.setHeart(5);
        two.setHeart(5);
        one.setLastHeartUpdateTime(java.time.LocalDateTime.now());
        two.setLastHeartUpdateTime(java.time.LocalDateTime.now());
        users.saveAllAndFlush(List.of(one, two));
        String sessionId = "refund-session-" + suffix;

        mockMvc.perform(post("/api/dev/battle/session-rosters")
                        .with(http -> { http.setRemoteAddr("127.0.0.1"); return http; })
                        .contentType("application/json")
                        .content(objectMapper.writeValueAsString(validRequest(sessionId, one, two))))
                .andExpect(status().isOk());
        org.assertj.core.api.Assertions.assertThat(users.findById(one.getId()).orElseThrow().getHeart()).isEqualTo(4);

        String refund = objectMapper.writeValueAsString(new BattleSessionRosterDtos.RefundRequest("SERVER_ABORTED"));
        mockMvc.perform(post("/api/dev/battle/session-rosters/{battleSessionId}/refund", sessionId)
                        .with(http -> { http.setRemoteAddr("127.0.0.1"); return http; })
                        .contentType("application/json").content(refund))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.status").value("REFUNDED"))
                .andExpect(jsonPath("$.alreadyProcessed").value(false));
        mockMvc.perform(post("/api/dev/battle/session-rosters/{battleSessionId}/refund", sessionId)
                        .with(http -> { http.setRemoteAddr("127.0.0.1"); return http; })
                        .contentType("application/json").content(refund))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.alreadyProcessed").value(true));
        org.assertj.core.api.Assertions.assertThat(users.findById(one.getId()).orElseThrow().getHeart()).isEqualTo(5);
        org.assertj.core.api.Assertions.assertThat(entryReservations.findByBattleSessionId(sessionId)).isPresent();
    }

    @Test
    void planetProgressionHttpResponseShowsOnlyDefaultPlanetUnlocked() throws Exception {
        User user = users.save(new User("progress-" + UUID.randomUUID(), "pw"));

        mockMvc.perform(get("/api/planet-progressions").param("username", user.getUsername()))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.userId").value(user.getId()))
                .andExpect(jsonPath("$.planets.length()").value(9))
                .andExpect(jsonPath("$.planets[0].mapId").value("NEPTUNE"))
                .andExpect(jsonPath("$.planets[0].unlocked").value(true))
                .andExpect(jsonPath("$.planets[1].unlocked").value(false));
    }

    @Test
    void trustedRosterPublishConflictCompensatesNewHeartReservation() throws Exception {
        String suffix = UUID.randomUUID().toString().replace("-", "");
        User one = heartUser("publish-p1-" + suffix, 5);
        User two = heartUser("publish-p2-" + suffix, 5);
        String session = "publish-conflict-" + suffix;
        rosters.registerComplete(session, "NEPTUNE", versions.getBalanceVersion(), versions.getContentHash(),
                List.of(new BattleSessionRosterRegistry.Player(1, "different-one-" + suffix),
                        new BattleSessionRosterRegistry.Player(2, "different-two-" + suffix)));

        mockMvc.perform(post("/api/dev/battle/session-rosters")
                        .with(http -> { http.setRemoteAddr("127.0.0.1"); return http; })
                        .contentType("application/json")
                        .content(objectMapper.writeValueAsString(validRequest(session, one, two))))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.code").value("BATTLE_PARTICIPANT_MISMATCH"));

        org.assertj.core.api.Assertions.assertThat(users.findById(one.getId()).orElseThrow().getHeart()).isEqualTo(5);
        org.assertj.core.api.Assertions.assertThat(users.findById(two.getId()).orElseThrow().getHeart()).isEqualTo(5);
        org.assertj.core.api.Assertions.assertThat(entryReservations.findByBattleSessionId(session).orElseThrow().getStatus())
                .isEqualTo(com.denfense.server.domain.BattleEntryStatus.REFUNDED);
    }

    @Test
    void invalidRosterDoesNotPartiallyProvisionDevelopmentUser() throws Exception {
        String suffix = UUID.randomUUID().toString().replace("-", "");
        String leakedCandidate = "dev-should-not-exist-" + suffix;
        var invalid = new BattleSessionRosterDtos.RegisterRequest(
                "invalid-provision-" + suffix, "NEPTUNE", versions.getBalanceVersion(), versions.getContentHash(),
                List.of(new BattleSessionRosterDtos.Player(1, leakedCandidate),
                        new BattleSessionRosterDtos.Player(1, "dev-duplicate-slot-" + suffix)));

        mockMvc.perform(post("/api/dev/battle/session-rosters")
                        .with(http -> { http.setRemoteAddr("127.0.0.1"); return http; })
                        .contentType("application/json")
                        .content(objectMapper.writeValueAsString(invalid)))
                .andExpect(status().isBadRequest());
        org.assertj.core.api.Assertions.assertThat(users.findByUsername(leakedCandidate)).isEmpty();
    }

    private User heartUser(String username, int heart) {
        User user = new User(username, "pw");
        user.setHeart(heart);
        user.setLastHeartUpdateTime(java.time.LocalDateTime.now());
        return users.saveAndFlush(user);
    }

    private BattleSessionRosterDtos.RegisterRequest validRequest(String sessionId, User one, User two) {
        return new BattleSessionRosterDtos.RegisterRequest(
                sessionId,
                "NEPTUNE",
                versions.getBalanceVersion(),
                versions.getContentHash(),
                List.of(
                        new BattleSessionRosterDtos.Player(1, one.getUsername()),
                        new BattleSessionRosterDtos.Player(2, two.getUsername())));
    }
}
