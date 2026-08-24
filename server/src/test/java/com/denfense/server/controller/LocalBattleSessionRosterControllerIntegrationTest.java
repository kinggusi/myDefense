package com.denfense.server.controller;

import com.denfense.server.domain.User;
import com.denfense.server.dto.battle.BattleSessionRosterDtos;
import com.denfense.server.repository.UserRepository;
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
