package com.denfense.server;

import com.denfense.server.balance.DailyBattleStageBalance;
import com.denfense.server.domain.DailyContentType;
import com.denfense.server.service.balance.DailyBattleStageBalanceRegistry;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

@SpringBootTest
class DailyBattleStageBalanceIntegrationTest {
    @Autowired
    private DailyBattleStageBalanceRegistry registry;

    @Test
    void cultivationStagesAreContinuousAndBossFree() {
        for (int stage = 1; stage <= 5; stage++) {
            List<DailyBattleStageBalance> rows = registry.get(DailyContentType.CULTIVATION_ZONE, stage);
            assertThat(rows).hasSize(stage + 2);
            assertThat(rows).extracting(DailyBattleStageBalance::wave)
                    .containsExactlyElementsOf(java.util.stream.IntStream.rangeClosed(1, stage + 2).boxed().toList());
            assertThat(rows).noneMatch(DailyBattleStageBalance::boss);
            assertThat(rows).allMatch(row -> row.mapId().equals("DAILY_CULTIVATION_ZONE")
                    && row.lanePolicy().equals("PLAYER_ONE_ONLY")
                    && row.statusEffectType().equals("NONE"));
        }
    }

    @Test
    void mutationStagesEndWithOneBossAndUseDebuffs() {
        for (int stage = 1; stage <= 5; stage++) {
            int expectedFinalWave = stage + 2;
            List<DailyBattleStageBalance> rows = registry.get(DailyContentType.MUTATION_LAB, stage);
            assertThat(rows).hasSize(expectedFinalWave);
            assertThat(rows).filteredOn(DailyBattleStageBalance::boss).singleElement()
                    .satisfies(row -> {
                        assertThat(row.wave()).isEqualTo(expectedFinalWave);
                        assertThat(row.monsterSpecId()).isEqualTo("WAVE_BOSS");
                        assertThat(row.spawnCount()).isEqualTo(1);
                    });
            assertThat(rows).anyMatch(row -> !row.statusEffectType().equals("NONE"));
        }
    }
}
