package com.denfense.server.controller;

import com.denfense.server.service.LocalFusionSessionRosterAdapter;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.context.ApplicationContext;
import org.springframework.test.context.ActiveProfiles;

import static org.assertj.core.api.Assertions.assertThat;

@SpringBootTest
@ActiveProfiles("prod")
class ProductionBattleRosterProfileIntegrationTest {
    @Autowired ApplicationContext context;

    @Test
    void productionContextDoesNotExposeLocalRosterControllerOrAdapter() {
        assertThat(context.getBeansOfType(LocalBattleSessionRosterController.class)).isEmpty();
        assertThat(context.getBeansOfType(LocalFusionSessionRosterAdapter.class)).isEmpty();
    }
}
