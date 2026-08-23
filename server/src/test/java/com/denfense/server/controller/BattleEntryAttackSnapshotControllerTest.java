package com.denfense.server.controller;

import com.denfense.server.dto.battle.BattleAttackSnapshotDtos;
import com.denfense.server.service.BattleEntryAttackSnapshotService;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;

import java.math.BigDecimal;
import java.util.List;

import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@AutoConfigureMockMvc
@ActiveProfiles("test")
class BattleEntryAttackSnapshotControllerTest {

    @Autowired
    private MockMvc mockMvc;

    @MockBean
    private BattleEntryAttackSnapshotService service;

    @Test
    void returnsNumericServerCalculatedStatsAndCanonicalIdentity() throws Exception {
        when(service.getForPlayer("player-a")).thenReturn(new BattleAttackSnapshotDtos.Response(
                "player-a",
                "1-version",
                "content-hash",
                List.of(new BattleAttackSnapshotDtos.AlienAttack(
                        7L,
                        4,
                        new BigDecimal("25.00"),
                        new BigDecimal("1.2500"),
                        new BigDecimal("4.5000")))));

        mockMvc.perform(get("/api/battle/entry/attack-snapshots").param("playerId", "player-a"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.playerId").value("player-a"))
                .andExpect(jsonPath("$.balanceVersion").value("1-version"))
                .andExpect(jsonPath("$.contentHash").value("content-hash"))
                .andExpect(jsonPath("$.aliens[0].alienId").value(7))
                .andExpect(jsonPath("$.aliens[0].level").value(4))
                .andExpect(jsonPath("$.aliens[0].damage").value(25.0))
                .andExpect(jsonPath("$.aliens[0].attackRate").value(1.25))
                .andExpect(jsonPath("$.aliens[0].range").value(4.5));
    }
}
