package com.denfense.server.controller;

import com.denfense.server.service.LocalFusionSessionRosterAdapter;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.context.ApplicationContext;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@SpringBootTest
@AutoConfigureMockMvc
class DefaultBattleRosterProfileIntegrationTest {
    @Autowired ApplicationContext context;
    @Autowired MockMvc mockMvc;

    @Test
    void omittedProfileDoesNotExposeLocalRosterControllerOrAdapter() {
        assertThat(context.getBeansOfType(LocalBattleSessionRosterController.class)).isEmpty();
        assertThat(context.getBeansOfType(LocalFusionSessionRosterAdapter.class)).isEmpty();
        assertThat(context.getBeansOfType(QuestController.class)).isEmpty();
    }

    @Test
    void omittedLocalProfileReturns404ForQuestInsteadOf500() throws Exception {
        mockMvc.perform(get("/api/quests").param("username", "sh1"))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath("$.code").value("RESOURCE_NOT_FOUND"));
    }
}
