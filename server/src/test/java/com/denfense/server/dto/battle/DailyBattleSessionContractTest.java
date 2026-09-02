package com.denfense.server.dto.battle;

import com.denfense.server.domain.DailyContentType;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;

import java.lang.reflect.RecordComponent;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Arrays;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class DailyBattleSessionContractTest {
    private final ObjectMapper mapper = new ObjectMapper();

    @Test
    void componentsMatchUnityContract() {
        assertThat(Arrays.stream(DailyBattleSessionDtos.Context.class.getRecordComponents())
                .map(RecordComponent::getName))
                .containsExactly("schemaVersion", "runId", "battleSessionId", "contentType", "stage",
                        "mapId", "balanceVersion", "contentHash");
    }

    @Test
    void canonicalJsonMatchesUnityFixtureByteForByte() throws Exception {
        String fixture = Files.readString(Path.of("..", "contracts", "daily-battle-session-v1.json")).trim();
        DailyBattleSessionDtos.Context context = mapper.readValue(fixture, DailyBattleSessionDtos.Context.class);

        assertThat(context.schemaVersion()).isEqualTo(DailyBattleSessionDtos.SCHEMA_VERSION);
        assertThat(context.contentType()).isEqualTo(DailyContentType.CULTIVATION_ZONE);
        assertThat(context.stage()).isEqualTo(3);
        assertThat(mapper.writeValueAsString(context)).isEqualTo(fixture);
    }

    @Test
    void rejectsContentTypeMapMismatch() {
        assertThatThrownBy(() -> new DailyBattleSessionDtos.Context(
                1, "run", "session", DailyContentType.CULTIVATION_ZONE, 1,
                "DAILY_MUTATION_LAB", "balance", "hash"))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("contentType/mapId");
    }

    @Test
    void rejectsStageOutsideOneThroughFive() {
        assertThatThrownBy(() -> new DailyBattleSessionDtos.Context(
                1, "run", "session", DailyContentType.MUTATION_LAB, 6,
                "DAILY_MUTATION_LAB", "balance", "hash"))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("stage");
    }
}
