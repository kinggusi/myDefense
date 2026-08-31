package com.denfense.server.dto.battle;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import com.denfense.server.domain.BattleResult;
import org.junit.jupiter.api.Test;

import java.lang.reflect.RecordComponent;
import java.time.LocalDateTime;
import java.util.Arrays;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class BattleSettlementContractTest {

    @Test
    void requestComponentsMatchUnityBattleSettlementSummary() {
        assertRecordComponents(
                BattleSettlementDtos.Request.class,
                "requestId",
                "battleSessionId",
                "balanceVersion",
                "contentHash",
                "result",
                "finalWave",
                "mapId",
                "startedAt",
                "finishedAt",
                "players",
                "monsterKills",
                "waveSpawnFacts",
                "partialWaveKills",
                "summaryHash"
        );
    }

    @Test
    void playerComponentsMatchUnityPlayerSummary() {
        assertRecordComponents(
                BattleSettlementDtos.Player.class,
                "playerId",
                "playerSlot",
                "eliminated",
                "eliminatedWave",
                "kills",
                "supportKills",
                "bossKills",
                "initialInGameGold",
                "inGameGoldEarned",
                "inGameGoldSpent",
                "finalInGameGold",
                "abandoned"
        );
    }

    @Test
    void monsterComponentsMatchUnityMonsterSummary() {
        assertRecordComponents(
                BattleSettlementDtos.Monster.class,
                "monsterSpecId",
                "totalKills",
                "bossKills",
                "totalKillGold"
        );
    }

    @Test
    void waveSpawnFactComponentsMatchUnityWaveSpawnFactSummary() {
        assertRecordComponents(
                BattleSettlementDtos.WaveSpawnFact.class,
                "runtimeMonsterId",
                "spawnWave",
                "spawnGroupId",
                "monsterSpecId",
                "lanePolicy",
                "fieldOwnerPlayerSlot",
                "spawnOrder",
                "spawnOrdinal"
        );
    }

    @Test
    void partialWaveKillComponentsMatchUnityPartialWaveKillSummary() {
        assertRecordComponents(
                BattleSettlementDtos.PartialWaveKill.class,
                "runtimeMonsterId",
                "spawnWave",
                "spawnGroupId",
                "monsterSpecId",
                "lanePolicy",
                "fieldOwnerPlayerSlot",
                "spawnOrder",
                "spawnOrdinal",
                "killerPlayerSlot",
                "supportPlayerSlot"
        );
    }

    @Test
    void resultValuesMatchUnityTransportConstants() {
        assertThat(Arrays.stream(BattleResult.values()).map(Enum::name))
                .containsExactly("VICTORY", "DEFEAT", "ABORTED");
    }

    @Test
    void componentTypesMatchUnityJsonContract() {
        assertRecordComponentTypes(
                BattleSettlementDtos.Request.class,
                String.class,
                String.class,
                String.class,
                String.class,
                String.class,
                int.class,
                String.class,
                LocalDateTime.class,
                LocalDateTime.class,
                List.class,
                List.class,
                List.class,
                List.class,
                String.class
        );
        assertRecordComponentTypes(
                BattleSettlementDtos.Player.class,
                String.class,
                int.class,
                boolean.class,
                Integer.class,
                int.class,
                int.class,
                int.class,
                int.class,
                int.class,
                int.class,
                int.class,
                boolean.class
        );
        assertRecordComponentTypes(
                BattleSettlementDtos.Monster.class,
                String.class,
                int.class,
                int.class,
                int.class
        );
        assertRecordComponentTypes(
                BattleSettlementDtos.WaveSpawnFact.class,
                String.class,
                int.class,
                String.class,
                String.class,
                String.class,
                Integer.class,
                int.class,
                int.class
        );
        assertRecordComponentTypes(
                BattleSettlementDtos.PartialWaveKill.class,
                String.class,
                int.class,
                String.class,
                String.class,
                String.class,
                Integer.class,
                int.class,
                int.class,
                int.class,
                Integer.class
        );
    }

    @Test
    void unityJsonShapeDeserializesNullableWaveAndIsoLocalDateTimes() throws Exception {
        String json = """
                {
                  "requestId":"request-1",
                  "battleSessionId":"session-1",
                  "balanceVersion":"balance-v1",
                  "contentHash":"content-hash",
                  "result":"VICTORY",
                  "finalWave":80,
                  "mapId":"EARTH",
                  "startedAt":"2026-07-18T12:00:00",
                  "finishedAt":"2026-07-18T12:20:00",
                  "players":[
                    {"playerId":"player-a","playerSlot":1,"eliminated":false,"eliminatedWave":null,"kills":10,"supportKills":2,"bossKills":1,"initialInGameGold":100,"inGameGoldEarned":50,"inGameGoldSpent":20,"finalInGameGold":130},
                    {"playerId":"player-b","playerSlot":2,"eliminated":true,"eliminatedWave":79,"kills":5,"supportKills":1,"bossKills":0,"initialInGameGold":100,"inGameGoldEarned":20,"inGameGoldSpent":10,"finalInGameGold":110}
                  ],
                  "monsterKills":[{"monsterSpecId":"NORMAL_MONSTER","totalKills":15,"bossKills":0,"totalKillGold":300}],
                  "waveSpawnFacts":[{"runtimeMonsterId":"18446744073709551615","spawnWave":80,"spawnGroupId":"WAVE_80_BOSS","monsterSpecId":"WAVE_BOSS","lanePolicy":"BOSS_SHARED","fieldOwnerPlayerSlot":null,"spawnOrder":1,"spawnOrdinal":1}],
                  "partialWaveKills":[{"runtimeMonsterId":"18446744073709551615","spawnWave":80,"spawnGroupId":"WAVE_80_BOSS","monsterSpecId":"WAVE_BOSS","lanePolicy":"BOSS_SHARED","fieldOwnerPlayerSlot":null,"spawnOrder":1,"spawnOrdinal":1,"killerPlayerSlot":1,"supportPlayerSlot":null}],
                  "summaryHash":"summary-hash"
                }
                """;
        ObjectMapper mapper = new ObjectMapper().registerModule(new JavaTimeModule());

        BattleSettlementDtos.Request request = mapper.readValue(json, BattleSettlementDtos.Request.class);

        assertThat(request.startedAt()).isEqualTo(LocalDateTime.of(2026, 7, 18, 12, 0));
        assertThat(request.mapId()).isEqualTo("EARTH");
        assertThat(request.players().get(0).eliminatedWave()).isNull();
        assertThat(request.players().get(1).eliminatedWave()).isEqualTo(79);
        assertThat(request.monsterKills().get(0).totalKillGold()).isEqualTo(300);
        assertThat(request.waveSpawnFacts()).singleElement().satisfies(fact -> {
            assertThat(fact.runtimeMonsterId()).isEqualTo("18446744073709551615");
            assertThat(fact.spawnGroupId()).isEqualTo("WAVE_80_BOSS");
            assertThat(fact.fieldOwnerPlayerSlot()).isNull();
        });
        assertThat(request.partialWaveKills()).singleElement().satisfies(kill -> {
            assertThat(kill.runtimeMonsterId()).isEqualTo("18446744073709551615");
            assertThat(kill.spawnGroupId()).isEqualTo("WAVE_80_BOSS");
            assertThat(kill.killerPlayerSlot()).isEqualTo(1);
            assertThat(kill.supportPlayerSlot()).isNull();
        });
    }

    private static void assertRecordComponents(Class<?> recordType, String... expectedNames) {
        assertThat(recordType.isRecord()).isTrue();
        assertThat(Arrays.stream(recordType.getRecordComponents()).map(RecordComponent::getName))
                .containsExactly(expectedNames);
    }

    private static void assertRecordComponentTypes(Class<?> recordType, Class<?>... expectedTypes) {
        assertThat(Arrays.stream(recordType.getRecordComponents()).map(RecordComponent::getType))
                .containsExactly(expectedTypes);
    }
}
