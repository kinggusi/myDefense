package com.denfense.server.dto.battle;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;

import java.lang.reflect.RecordComponent;
import java.util.Arrays;

import static org.assertj.core.api.Assertions.assertThat;

class LegendaryChoiceStateContractTest {
    @Test
    void stateComponentsMatchUnityChoiceContract() {
        assertThat(Arrays.stream(LegendaryChoiceStateDtos.State.class.getRecordComponents())
                .map(RecordComponent::getName))
                .containsExactly("choiceId", "battleSessionId", "materialAlienIdA", "materialAlienIdB",
                        "candidateAlienIds", "rerollCount", "freeRerollsRemaining", "paidRerollsRemaining",
                        "selectionTimeoutSeconds", "remainingSeconds", "phase", "selectedAlienId",
                        "autoSelectPolicy", "battleContinuesDuringSelection");
    }

    @Test
    void openAndSelectedStatesDeserializeWithNullableSelection() throws Exception {
        String json = """
                {"choiceId":"choice-1","battleSessionId":"session-1","materialAlienIdA":101,"materialAlienIdB":102,
                 "candidateAlienIds":[201,202,203],"rerollCount":1,"freeRerollsRemaining":0,"paidRerollsRemaining":2,
                 "selectionTimeoutSeconds":8,"remainingSeconds":8,"phase":"OPEN","selectedAlienId":null,
                 "autoSelectPolicy":"FIRST","battleContinuesDuringSelection":true}
                """;
        var state = new ObjectMapper().readValue(json, LegendaryChoiceStateDtos.State.class);
        assertThat(state.candidateAlienIds()).containsExactly(201L, 202L, 203L);
        assertThat(state.selectedAlienId()).isNull();
        assertThat(state.battleContinuesDuringSelection()).isTrue();
    }
}
