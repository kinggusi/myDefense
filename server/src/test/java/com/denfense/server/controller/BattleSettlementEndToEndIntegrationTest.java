package com.denfense.server.controller;

import com.denfense.server.domain.BattleSettlement;
import com.denfense.server.domain.User;
import com.denfense.server.dto.battle.BattleSettlementDtos;
import com.denfense.server.repository.BattlePlayerSettlementRepository;
import com.denfense.server.repository.BattleSettlementRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.BattleSessionRosterRegistry;
import com.denfense.server.service.balance.BalanceVersionRegistry;
import com.denfense.server.service.balance.MonsterBalanceRegistry;
import com.denfense.server.service.balance.WaveBalanceRegistry;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.web.servlet.MockMvc;

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
class BattleSettlementEndToEndIntegrationTest {
    @Autowired MockMvc mockMvc;
    @Autowired ObjectMapper objectMapper;
    @Autowired UserRepository users;
    @Autowired BattleSettlementRepository settlements;
    @Autowired BattlePlayerSettlementRepository playerSettlements;
    @Autowired BalanceVersionRegistry versions;
    @Autowired BattleSessionRosterRegistry rosters;
    @Autowired WaveBalanceRegistry waves;
    @Autowired MonsterBalanceRegistry monsters;

    @Test
    void unityVictoryWave80PayloadPersistsSamePlayersAndReturnsAcceptedResponse() throws Exception {
        String suffix = UUID.randomUUID().toString().replace("-", "");
        User playerOne = users.save(new User("e2e-p1-" + suffix, "pw"));
        User playerTwo = users.save(new User("e2e-p2-" + suffix, "pw"));
        String sessionId = "e2e-session-" + suffix;
        String requestId = "e2e-request-" + suffix;
        String summaryHash = "e2e-hash-" + suffix;
        LocalDateTime startedAt = LocalDateTime.of(2026, 8, 2, 12, 0);
        rosters.register(sessionId, 1, playerOne.getUsername(), "NEPTUNE",
                versions.getBalanceVersion(), versions.getContentHash());
        rosters.register(sessionId, 2, playerTwo.getUsername(), "NEPTUNE",
                versions.getBalanceVersion(), versions.getContentHash());

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
        var request = new BattleSettlementDtos.Request(
                requestId, sessionId, versions.getBalanceVersion(), versions.getContentHash(), "VICTORY", 80,
                "NEPTUNE", startedAt, startedAt.plusMinutes(20),
                List.of(
                        new BattleSettlementDtos.Player(playerOne.getUsername(), 1, false, null,
                                firstKills, 0, bossKills, 100, 0, 0, 100, false),
                        new BattleSettlementDtos.Player(playerTwo.getUsername(), 2, false, null,
                                secondKills, 0, 0, 100, 0, 0, 100, false)),
                monsterKills, summaryHash);

        mockMvc.perform(post("/api/battle/settlements")
                        .contentType("application/json")
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.battleSessionId").value(sessionId))
                .andExpect(jsonPath("$.status").value("ACCEPTED"))
                .andExpect(jsonPath("$.alreadyProcessed").value(false))
                .andExpect(jsonPath("$.rewards").isArray());

        BattleSettlement saved = settlements.findByBattleSessionId(sessionId).orElseThrow();
        assertThat(saved.getFinalWave()).isEqualTo(80);
        assertThat(saved.getMapId()).isEqualTo("NEPTUNE");
        assertThat(playerSettlements.findByBattleSettlementId(saved.getId()))
                .extracting(entry -> entry.getUser().getId())
                .containsExactlyInAnyOrder(playerOne.getId(), playerTwo.getId());
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
