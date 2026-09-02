package com.denfense.server.dto.battle;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;

import java.lang.reflect.RecordComponent;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Arrays;

import static org.assertj.core.api.Assertions.assertThat;

class BattleSessionSnapshotContractTest {
    @Test
    void snapshotComponentsMatchUnityResumeContract() {
        assertNames(BattleSessionSnapshotDtos.Snapshot.class,
                "schemaVersion", "battleSessionId", "mapId", "balanceVersion", "contentHash",
                "matchState", "currentWave", "currentWaveSpecId", "waveType", "wavePhase",
                "waveTimeRemainingSeconds", "bossTimeRemainingSeconds",
                "capturedAtTick", "players", "boardObjects", "mythicChoices", "monsters");
        assertNames(BattleSessionSnapshotDtos.Player.class,
                "playerId", "playerSlot", "battleState", "connectionState", "inGameGold",
                "currentKidnapCost", "normalResonanceLevel", "mythicResonanceLevel", "eliminatedWave");
        assertNames(BattleSessionSnapshotDtos.BoardObject.class,
                "objectId", "ownerPlayerSlot", "objectType", "gridX", "gridY", "alienSpecId",
                "grade", "pendingMutationType", "activeMutationType", "mutationRerollCount", "mutationType", "mutationState");
        assertNames(BattleSessionSnapshotDtos.MythicChoice.class,
                "playerSlot", "targetBoardSlot", "candidateAlienIds", "freeRerollsRemaining",
                "paidRerollsRemaining", "remainingSeconds");
        assertNames(BattleSessionSnapshotDtos.Monster.class,
                "runtimeMonsterId", "monsterId", "lanePolicy", "fieldOwnerPlayerId", "spawnWave",
                "currentHp", "maxHp", "dead", "x", "y", "z");
    }

    @Test
    void nullableEliminatedWaveAndAlienSpecIdDeserialize() throws Exception {
        String json = """
                {"schemaVersion":3,"battleSessionId":"session-1","mapId":"EARTH","balanceVersion":"balance-v1","contentHash":"hash",
                 "matchState":"RUNNING","currentWave":12,"currentWaveSpecId":"WAVE_12","waveType":"REGULAR","wavePhase":"SPAWNING",
                 "waveTimeRemainingSeconds":8,"bossTimeRemainingSeconds":0,"capturedAtTick":900,
                 "players":[{"playerId":"p1","playerSlot":1,"battleState":"ACTIVE","connectionState":"CONNECTED","inGameGold":60,"currentKidnapCost":30,"normalResonanceLevel":3,"mythicResonanceLevel":1,"eliminatedWave":null}],
                 "boardObjects":[{"objectId":7,"ownerPlayerSlot":1,"objectType":"ALIEN","gridX":2,"gridY":3,"alienSpecId":22,"grade":"MYTHIC","pendingMutationType":"NONE","activeMutationType":"NONE","mutationRerollCount":0,"mutationType":null,"mutationState":"SEALED"}],
                 "mythicChoices":[{"playerSlot":1,"targetBoardSlot":7,"candidateAlienIds":[29,30,31],"freeRerollsRemaining":1,"paidRerollsRemaining":1,"remainingSeconds":10}],
                 "monsters":[{"runtimeMonsterId":18446744073709551615,"monsterId":"MON","lanePolicy":"EACH_FIELD","fieldOwnerPlayerId":"p1","spawnWave":12,"currentHp":50,"maxHp":100,"dead":false,"x":1,"y":2,"z":3}]}
                """;
        BattleSessionSnapshotDtos.Snapshot snapshot = new ObjectMapper().readValue(json, BattleSessionSnapshotDtos.Snapshot.class);
        assertThat(snapshot.players().get(0).eliminatedWave()).isNull();
        assertThat(snapshot.boardObjects().get(0).alienSpecId()).isEqualTo(22L);
        assertThat(snapshot.currentWave()).isEqualTo(12);
        assertThat(snapshot.mapId()).isEqualTo("EARTH");
        assertThat(snapshot.monsters().get(0).runtimeMonsterId().toString())
                .isEqualTo("18446744073709551615");
    }

    @Test
    void v3CanonicalJsonMatchesUnitySharedFixtureByteForByte() throws Exception {
        String fixture = Files.readString(Path.of("..", "contracts", "battle-session-snapshot-v3.json")).trim();
        ObjectMapper mapper = new ObjectMapper();

        BattleSessionSnapshotDtos.Snapshot snapshot =
                mapper.readValue(fixture, BattleSessionSnapshotDtos.Snapshot.class);

        assertThat(snapshot.schemaVersion()).isEqualTo(3);
        assertThat(snapshot.mapId()).isEqualTo("EARTH");
        assertThat(mapper.writeValueAsString(snapshot)).isEqualTo(fixture);
    }

    private static void assertNames(Class<?> type, String... names) {
        assertThat(type.isRecord()).isTrue();
        assertThat(Arrays.stream(type.getRecordComponents()).map(RecordComponent::getName))
                .containsExactly(names);
    }
}
