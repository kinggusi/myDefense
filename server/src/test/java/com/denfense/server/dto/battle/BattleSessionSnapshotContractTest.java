package com.denfense.server.dto.battle;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;

import java.lang.reflect.RecordComponent;
import java.util.Arrays;

import static org.assertj.core.api.Assertions.assertThat;

class BattleSessionSnapshotContractTest {
    @Test
    void snapshotComponentsMatchUnityResumeContract() {
        assertNames(BattleSessionSnapshotDtos.Snapshot.class,
                "schemaVersion", "battleSessionId", "balanceVersion", "contentHash",
                "matchState", "currentWave", "currentWaveSpecId", "waveType", "wavePhase",
                "waveTimeRemainingSeconds", "bossTimeRemainingSeconds",
                "capturedAtTick", "players", "boardObjects");
        assertNames(BattleSessionSnapshotDtos.Player.class,
                "playerId", "playerSlot", "battleState", "connectionState", "inGameGold",
                "currentKidnapCost", "eliminatedWave");
        assertNames(BattleSessionSnapshotDtos.BoardObject.class,
                "objectId", "ownerPlayerSlot", "objectType", "gridX", "gridY", "alienSpecId",
                "grade", "pendingMutationType", "activeMutationType", "mutationRerollCount", "mutationType");
    }

    @Test
    void nullableEliminatedWaveAndAlienSpecIdDeserialize() throws Exception {
        String json = """
                {"schemaVersion":1,"battleSessionId":"session-1","balanceVersion":"balance-v1","contentHash":"hash",
                 "matchState":"RUNNING","currentWave":12,"currentWaveSpecId":"WAVE_12","waveType":"REGULAR","wavePhase":"SPAWNING",
                 "waveTimeRemainingSeconds":8,"bossTimeRemainingSeconds":0,"capturedAtTick":900,
                 "players":[{"playerId":"p1","playerSlot":1,"battleState":"ACTIVE","connectionState":"CONNECTED","inGameGold":60,"currentKidnapCost":30,"eliminatedWave":null}],
                 "boardObjects":[{"objectId":7,"ownerPlayerSlot":1,"objectType":"ALIEN","gridX":2,"gridY":3,"alienSpecId":22,"grade":"MYTHIC","pendingMutationType":"NONE","activeMutationType":"NONE","mutationRerollCount":0,"mutationType":null}]}
                """;
        BattleSessionSnapshotDtos.Snapshot snapshot = new ObjectMapper().readValue(json, BattleSessionSnapshotDtos.Snapshot.class);
        assertThat(snapshot.players().get(0).eliminatedWave()).isNull();
        assertThat(snapshot.boardObjects().get(0).alienSpecId()).isEqualTo(22L);
        assertThat(snapshot.currentWave()).isEqualTo(12);
    }

    private static void assertNames(Class<?> type, String... names) {
        assertThat(type.isRecord()).isTrue();
        assertThat(Arrays.stream(type.getRecordComponents()).map(RecordComponent::getName))
                .containsExactly(names);
    }
}
