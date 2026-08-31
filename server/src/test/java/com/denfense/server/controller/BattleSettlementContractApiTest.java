package com.denfense.server.controller;

import com.denfense.server.dto.battle.BattleSettlementDtos;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.service.BattleSettlementService;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;

import java.time.LocalDateTime;
import java.util.List;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;

@SpringBootTest
@AutoConfigureMockMvc
@ActiveProfiles("test")
class BattleSettlementContractApiTest {
    @Autowired
    private MockMvc mockMvc;

    @MockBean
    private BattleSettlementService service;

    @Test
    void successReturnsSettlementResponseShape() throws Exception {
        when(service.settle(any())).thenReturn(
                new BattleSettlementDtos.Response("session-1", "ACCEPTED", false, List.of()));

        mockMvc.perform(post("/api/battle/settlements")
                        .contentType("application/json")
                        .content(validJson()))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.battleSessionId").value("session-1"))
                .andExpect(jsonPath("$.status").value("ACCEPTED"))
                .andExpect(jsonPath("$.alreadyProcessed").value(false))
                .andExpect(jsonPath("$.rewards").isArray())
                .andExpect(jsonPath("$.rewards").isEmpty());
    }

    @Test
    void summaryValidationUsesCommonErrorContract() throws Exception {
        when(service.settle(any())).thenThrow(new BusinessException(ErrorCode.BATTLE_SUMMARY_INVALID));

        mockMvc.perform(post("/api/battle/settlements")
                        .contentType("application/json")
                        .content(validJson()))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.code").value("BATTLE_SUMMARY_INVALID"))
                .andExpect(jsonPath("$.message").value(ErrorCode.BATTLE_SUMMARY_INVALID.getMessage()));
    }

    @Test
    void conflictUsesHttp409AndDoesNotExposeDatabaseDetails() throws Exception {
        when(service.settle(any())).thenThrow(new BusinessException(ErrorCode.BATTLE_SETTLEMENT_CONFLICT));

        mockMvc.perform(post("/api/battle/settlements")
                        .contentType("application/json")
                        .content(validJson()))
                .andExpect(status().isConflict())
                .andExpect(jsonPath("$.code").value("BATTLE_SETTLEMENT_CONFLICT"))
                .andExpect(content().string(org.hamcrest.Matchers.not(org.hamcrest.Matchers.containsString("constraint"))));
    }

    private String validJson() {
        LocalDateTime started = LocalDateTime.of(2026, 7, 27, 12, 0);
        LocalDateTime finished = started.plusMinutes(5);
        return "{" +
                "\"requestId\":\"request-1\"," +
                "\"battleSessionId\":\"session-1\"," +
                "\"balanceVersion\":\"balance-v1\"," +
                "\"contentHash\":\"content-v1\"," +
                "\"result\":\"VICTORY\"," +
                "\"finalWave\":1," +
                "\"mapId\":\"EARTH\"," +
                "\"startedAt\":\"" + started + "\"," +
                "\"finishedAt\":\"" + finished + "\"," +
                "\"players\":[]," +
                "\"monsterKills\":[]," +
                "\"waveSpawnFacts\":[]," +
                "\"partialWaveKills\":[]," +
                "\"summaryHash\":\"hash-1\"" +
                "}";
    }
}
