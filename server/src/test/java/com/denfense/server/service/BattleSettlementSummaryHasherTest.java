package com.denfense.server.service;

import com.denfense.server.dto.battle.BattleSettlementDtos;
import org.junit.jupiter.api.Test;

import java.time.LocalDateTime;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class BattleSettlementSummaryHasherTest {
    private static final String CROSS_RUNTIME_HASH =
            "21f0cfff90eec02ab6b1bdd6cdbff6cca51ba4e83ccef37ffd5e9f8cd0578895";

    @Test
    void canonicalJsonAndHashMatchUnityFixture() {
        var request = fixture();

        assertThat(BattleSettlementSummaryHasher.canonicalJson(request)).isEqualTo(
                "{\"requestId\":\"r\",\"battleSessionId\":\"s\",\"balanceVersion\":\"v\"," +
                        "\"contentHash\":\"h\",\"result\":\"DEFEAT\",\"finalWave\":0,\"mapId\":\"EARTH\"," +
                        "\"startedAt\":\"2026-08-29T01:02:03\",\"finishedAt\":\"2026-08-29T01:03:04\"," +
                        "\"players\":[{\"playerId\":\"a\",\"playerSlot\":1,\"eliminated\":true," +
                        "\"eliminatedWave\":1,\"kills\":1,\"supportKills\":0,\"bossKills\":0," +
                        "\"initialInGameGold\":100,\"inGameGoldEarned\":20,\"inGameGoldSpent\":0," +
                        "\"finalInGameGold\":120,\"abandoned\":false},{\"playerId\":\"b\",\"playerSlot\":2," +
                        "\"eliminated\":false,\"eliminatedWave\":null,\"kills\":0,\"supportKills\":1," +
                        "\"bossKills\":0,\"initialInGameGold\":100,\"inGameGoldEarned\":0," +
                        "\"inGameGoldSpent\":0,\"finalInGameGold\":100,\"abandoned\":false}]," +
                        "\"monsterKills\":[{\"monsterSpecId\":\"NORMAL_MONSTER\",\"totalKills\":1," +
                        "\"bossKills\":0,\"totalKillGold\":20}],\"partialWaveKills\":[{" +
                        "\"runtimeMonsterId\":\"18446744073709551615\",\"spawnWave\":1," +
                        "\"monsterSpecId\":\"NORMAL_MONSTER\",\"lanePolicy\":\"EACH_FIELD\"," +
                        "\"playerSlot\":1,\"spawnOrder\":1,\"spawnOrdinal\":1,\"killerPlayerId\":\"a\"," +
                        "\"supportPlayerId\":\"b\",\"killedAtTick\":42}],\"summaryHash\":\"\"}");
        assertThat(BattleSettlementSummaryHasher.compute(request)).isEqualTo(CROSS_RUNTIME_HASH);
    }

    private BattleSettlementDtos.Request fixture() {
        return new BattleSettlementDtos.Request(
                "r", "s", "v", "h", "DEFEAT", 0, "EARTH",
                LocalDateTime.of(2026, 8, 29, 1, 2, 3),
                LocalDateTime.of(2026, 8, 29, 1, 3, 4),
                List.of(
                        new BattleSettlementDtos.Player("a", 1, true, 1, 1, 0, 0,
                                100, 20, 0, 120, false),
                        new BattleSettlementDtos.Player("b", 2, false, null, 0, 1, 0,
                                100, 0, 0, 100, false)),
                List.of(new BattleSettlementDtos.Monster("NORMAL_MONSTER", 1, 0, 20)),
                List.of(new BattleSettlementDtos.PartialWaveKill(
                        "18446744073709551615", 1, "NORMAL_MONSTER", "EACH_FIELD", 1,
                        1, 1, "a", "b", 42L)),
                "ignored");
    }
}
