package com.denfense.server.controller;

import com.denfense.server.domain.BattleSettlement;
import com.denfense.server.domain.User;
import com.denfense.server.dto.battle.BattleSettlementDtos;
import com.denfense.server.dto.battle.BattleSessionRosterDtos;
import com.denfense.server.repository.BattlePlayerSettlementRepository;
import com.denfense.server.repository.BattleSettlementRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.BattleSettlementSummaryHasher;
import com.denfense.server.service.balance.BalanceVersionRegistry;
import com.denfense.server.service.balance.MonsterBalanceRegistry;
import com.denfense.server.service.balance.WaveBalanceRegistry;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.JsonNode;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.context.ActiveProfiles;

import java.time.LocalDateTime;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@AutoConfigureMockMvc
@ActiveProfiles("local")
class BattleSettlementEndToEndIntegrationTest {
    @Autowired MockMvc mockMvc;
    @Autowired ObjectMapper objectMapper;
    @Autowired UserRepository users;
    @Autowired BattleSettlementRepository settlements;
    @Autowired BattlePlayerSettlementRepository playerSettlements;
    @Autowired BalanceVersionRegistry versions;
    @Autowired WaveBalanceRegistry waves;
    @Autowired MonsterBalanceRegistry monsters;

    @Test
    void unityVictoryWave80PayloadPersistsSamePlayersAndReturnsAcceptedResponse() throws Exception {
        String suffix = UUID.randomUUID().toString().replace("-", "");
        User playerOne = users.save(new User("e2e-p1-" + suffix, "pw"));
        User playerTwo = users.save(new User("e2e-p2-" + suffix, "pw"));
        String sessionId = "e2e-session-" + suffix;
        String requestId = "e2e-request-" + suffix;
        LocalDateTime startedAt = LocalDateTime.of(2026, 8, 2, 12, 0);
        var rosterRequest = new BattleSessionRosterDtos.RegisterRequest(
                sessionId,
                "NEPTUNE",
                versions.getBalanceVersion(),
                versions.getContentHash(),
                List.of(
                        new BattleSessionRosterDtos.Player(1, playerOne.getUsername()),
                        new BattleSessionRosterDtos.Player(2, playerTwo.getUsername())));
        mockMvc.perform(post("/api/dev/battle/session-rosters")
                        .with(http -> { http.setRemoteAddr("127.0.0.1"); return http; })
                        .contentType("application/json")
                        .content(objectMapper.writeValueAsString(rosterRequest)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.status").value("REGISTERED"));

        Map<String, Integer> counts = expectedCounts(80);
        List<BattleSettlementDtos.Monster> monsterKills = counts.entrySet().stream()
                .sorted(Map.Entry.comparingByKey())
                .map(entry -> new BattleSettlementDtos.Monster(
                        entry.getKey(), entry.getValue(), entry.getKey().equals("WAVE_BOSS") ? entry.getValue() : 0,
                        monsters.getById(entry.getKey()).killGold() * entry.getValue()))
                .toList();
        int totalKills = counts.values().stream().mapToInt(Integer::intValue).sum();
        int bossKills = counts.getOrDefault("WAVE_BOSS", 0);
        int firstKills = totalKills / 2 + totalKills % 2;
        int secondKills = totalKills / 2;
        var unsigned = new BattleSettlementDtos.Request(
                requestId, sessionId, versions.getBalanceVersion(), versions.getContentHash(), "VICTORY", 80,
                "NEPTUNE", startedAt, startedAt.plusMinutes(20),
                List.of(
                        new BattleSettlementDtos.Player(playerOne.getUsername(), 1, false, null,
                                firstKills, 0, bossKills, 100, 0, 0, 100, false),
                        new BattleSettlementDtos.Player(playerTwo.getUsername(), 2, false, null,
                                secondKills, 0, 0, 100, 0, 0, 100, false)),
                monsterKills, List.of(), List.of(), "");
        var request = new BattleSettlementDtos.Request(
                unsigned.requestId(), unsigned.battleSessionId(), unsigned.balanceVersion(), unsigned.contentHash(),
                unsigned.result(), unsigned.finalWave(), unsigned.mapId(), unsigned.startedAt(), unsigned.finishedAt(),
                unsigned.players(), unsigned.monsterKills(), unsigned.waveSpawnFacts(), unsigned.partialWaveKills(),
                BattleSettlementSummaryHasher.compute(unsigned));

        String firstResponse = mockMvc.perform(post("/api/battle/settlements")
                        .contentType("application/json")
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.battleSessionId").value(sessionId))
                .andExpect(jsonPath("$.status").value("ACCEPTED"))
                .andExpect(jsonPath("$.alreadyProcessed").value(false))
                .andExpect(jsonPath("$.rewards").isArray())
                .andReturn().getResponse().getContentAsString();

        JsonNode firstRewards = objectMapper.readTree(firstResponse).path("rewards");
        assertThat(firstRewards).hasSize(20);
        assertThat(users.findById(playerOne.getId()).orElseThrow())
                .extracting(User::getGold, User::getUniversalPiece, User::getDiamond)
                .containsExactly(25_250, 225, 3_000);
        assertThat(users.findById(playerTwo.getId()).orElseThrow())
                .extracting(User::getGold, User::getUniversalPiece, User::getDiamond)
                .containsExactly(25_250, 225, 3_000);

        mockMvc.perform(post("/api/battle/settlements")
                        .contentType("application/json")
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.battleSessionId").value(sessionId))
                .andExpect(jsonPath("$.status").value("ACCEPTED"))
                .andExpect(jsonPath("$.alreadyProcessed").value(true))
                .andExpect(jsonPath("$.rewards").isArray());

        assertThat(users.findById(playerOne.getId()).orElseThrow())
                .extracting(User::getGold, User::getUniversalPiece, User::getDiamond)
                .containsExactly(25_250, 225, 3_000);
        assertThat(users.findById(playerTwo.getId()).orElseThrow())
                .extracting(User::getGold, User::getUniversalPiece, User::getDiamond)
                .containsExactly(25_250, 225, 3_000);

        BattleSettlement saved = settlements.findByBattleSessionId(sessionId).orElseThrow();
        assertThat(saved.getFinalWave()).isEqualTo(80);
        assertThat(saved.getMapId()).isEqualTo("NEPTUNE");
        assertThat(playerSettlements.findByBattleSettlementId(saved.getId()))
                .extracting(entry -> entry.getUser().getId())
                .containsExactlyInAnyOrder(playerOne.getId(), playerTwo.getId());
        assertThat(settlements.count()).isGreaterThanOrEqualTo(1);
    }

    private Map<String, Integer> expectedCounts(int finalWave) {
        Map<String, Integer> counts = new HashMap<>();
        for (int wave = 1; wave <= finalWave; wave++) {
            var waveSpec = waves.getWave("COOP_STANDARD", wave);
            for (var spawn : waves.getSpawns(waveSpec.spawnGroupId())) {
                int lanes = spawn.lanePolicy().equals("EACH_FIELD") ? 2 : 1;
                counts.merge(spawn.monsterId(), spawn.spawnCountPerField() * lanes, Integer::sum);
            }
        }
        return counts;
    }
}
