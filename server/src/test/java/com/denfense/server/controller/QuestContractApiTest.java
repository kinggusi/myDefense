package com.denfense.server.controller;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest(properties = "spring.datasource.url=jdbc:h2:mem:quest-api;MODE=MySQL")
@AutoConfigureMockMvc
@ActiveProfiles("local")
class QuestContractApiTest {
    @Autowired MockMvc mockMvc;

    @Test
    void unknownPathReturns404WithoutLeakingStackTrace() throws Exception {
        mockMvc.perform(get("/api/not-a-real-route"))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath("$.code").value("RESOURCE_NOT_FOUND"))
                .andExpect(jsonPath("$.trace").doesNotExist());
    }

    @Test
    void boardContractExposesDailyWeeklyProgressRewardsAndReset() throws Exception {
        mockMvc.perform(get("/api/quests").param("username", "sh1"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.username").value("sh1"))
                .andExpect(jsonPath("$.daily.cycleType").value("DAILY"))
                .andExpect(jsonPath("$.daily.quests.length()").value(4))
                .andExpect(jsonPath("$.daily.milestones.length()").value(4))
                .andExpect(jsonPath("$.daily.milestones[3].requiredActivityPoints").value(100))
                .andExpect(jsonPath("$.weekly.cycleType").value("WEEKLY"))
                .andExpect(jsonPath("$.weekly.quests.length()").value(4))
                .andExpect(jsonPath("$.weekly.milestones[3].reward.diamond").value(300))
                .andExpect(jsonPath("$.achievements.length()").value(27))
                .andExpect(jsonPath("$.achievements[0].achievementId").value("ACH_BATTLE_001"))
                .andExpect(jsonPath("$.achievements[0].claimed").value(false))
                .andExpect(jsonPath("$.wallet.gold").isNumber())
                .andExpect(jsonPath("$.serverTime").exists())
                .andExpect(jsonPath("$.daily.nextResetAt").exists())
                .andExpect(jsonPath("$.weekly.nextResetAt").exists());
    }
}
