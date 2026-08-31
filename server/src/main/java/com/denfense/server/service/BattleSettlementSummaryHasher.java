package com.denfense.server.service;

import com.denfense.server.dto.battle.BattleSettlementDtos;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.time.format.DateTimeFormatter;
import java.util.HexFormat;

/**
 * Mirrors Unity BattleSettlementSummaryJson.SerializeForHash exactly. The
 * summaryHash property is omitted from the canonical SHA-256 input.
 */
public final class BattleSettlementSummaryHasher {
    private static final DateTimeFormatter DATE_TIME = DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ss");

    private BattleSettlementSummaryHasher() {
    }

    public static String compute(BattleSettlementDtos.Request request) {
        try {
            return HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256")
                    .digest(canonicalJson(request).getBytes(StandardCharsets.UTF_8)));
        } catch (NoSuchAlgorithmException exception) {
            throw new IllegalStateException("SHA-256 is unavailable.", exception);
        }
    }

    static String canonicalJson(BattleSettlementDtos.Request request) {
        StringBuilder builder = new StringBuilder(2048).append('{');
        string(builder, "requestId", request.requestId());
        string(builder, "battleSessionId", request.battleSessionId());
        string(builder, "balanceVersion", request.balanceVersion());
        string(builder, "contentHash", request.contentHash());
        string(builder, "result", request.result());
        integer(builder, "finalWave", request.finalWave());
        string(builder, "mapId", request.mapId());
        string(builder, "startedAt", request.startedAt() == null ? null : DATE_TIME.format(request.startedAt()));
        string(builder, "finishedAt", request.finishedAt() == null ? null : DATE_TIME.format(request.finishedAt()));
        property(builder, "players").append('[');
        for (int index = 0; index < request.players().size(); index++) {
            if (index > 0) builder.append(',');
            BattleSettlementDtos.Player player = request.players().get(index);
            builder.append('{');
            string(builder, "playerId", player.playerId());
            integer(builder, "playerSlot", player.playerSlot());
            bool(builder, "eliminated", player.eliminated());
            nullableInteger(builder, "eliminatedWave", player.eliminatedWave());
            integer(builder, "kills", player.kills());
            integer(builder, "supportKills", player.supportKills());
            integer(builder, "bossKills", player.bossKills());
            integer(builder, "initialInGameGold", player.initialInGameGold());
            integer(builder, "inGameGoldEarned", player.inGameGoldEarned());
            integer(builder, "inGameGoldSpent", player.inGameGoldSpent());
            integer(builder, "finalInGameGold", player.finalInGameGold());
            bool(builder, "abandoned", player.abandoned());
            builder.setLength(builder.length() - 1);
            builder.append('}');
        }
        builder.append("],");
        property(builder, "monsterKills").append('[');
        for (int index = 0; index < request.monsterKills().size(); index++) {
            if (index > 0) builder.append(',');
            BattleSettlementDtos.Monster monster = request.monsterKills().get(index);
            builder.append('{');
            string(builder, "monsterSpecId", monster.monsterSpecId());
            integer(builder, "totalKills", monster.totalKills());
            integer(builder, "bossKills", monster.bossKills());
            integer(builder, "totalKillGold", monster.totalKillGold(), false);
            builder.append('}');
        }
        builder.append("],");
        property(builder, "waveSpawnFacts").append('[');
        for (int index = 0; index < request.waveSpawnFacts().size(); index++) {
            if (index > 0) builder.append(',');
            BattleSettlementDtos.WaveSpawnFact fact = request.waveSpawnFacts().get(index);
            builder.append('{');
            string(builder, "runtimeMonsterId", fact.runtimeMonsterId());
            integer(builder, "spawnWave", fact.spawnWave());
            string(builder, "spawnGroupId", fact.spawnGroupId());
            string(builder, "monsterSpecId", fact.monsterSpecId());
            string(builder, "lanePolicy", fact.lanePolicy());
            nullableInteger(builder, "fieldOwnerPlayerSlot", fact.fieldOwnerPlayerSlot());
            integer(builder, "spawnOrder", fact.spawnOrder());
            integer(builder, "spawnOrdinal", fact.spawnOrdinal(), false);
            builder.append('}');
        }
        builder.append("],");
        property(builder, "partialWaveKills").append('[');
        for (int index = 0; index < request.partialWaveKills().size(); index++) {
            if (index > 0) builder.append(',');
            BattleSettlementDtos.PartialWaveKill kill = request.partialWaveKills().get(index);
            builder.append('{');
            string(builder, "runtimeMonsterId", kill.runtimeMonsterId());
            integer(builder, "spawnWave", kill.spawnWave());
            string(builder, "spawnGroupId", kill.spawnGroupId());
            string(builder, "monsterSpecId", kill.monsterSpecId());
            string(builder, "lanePolicy", kill.lanePolicy());
            nullableInteger(builder, "fieldOwnerPlayerSlot", kill.fieldOwnerPlayerSlot());
            integer(builder, "spawnOrder", kill.spawnOrder());
            integer(builder, "spawnOrdinal", kill.spawnOrdinal());
            integer(builder, "killerPlayerSlot", kill.killerPlayerSlot());
            nullableInteger(builder, "supportPlayerSlot", kill.supportPlayerSlot());
            builder.setLength(builder.length() - 1);
            builder.append('}');
        }
        builder.append(']');
        return builder.append('}').toString();
    }

    private static StringBuilder property(StringBuilder builder, String name) {
        return builder.append('"').append(name).append("\":");
    }

    private static void string(StringBuilder builder, String name, String value) {
        string(builder, name, value, true);
    }

    private static void string(StringBuilder builder, String name, String value, boolean comma) {
        property(builder, name);
        if (value == null) builder.append("null");
        else {
            builder.append('"');
            for (int index = 0; index < value.length(); index++) {
                char character = value.charAt(index);
                switch (character) {
                    case '"' -> builder.append("\\\"");
                    case '\\' -> builder.append("\\\\");
                    case '\b' -> builder.append("\\b");
                    case '\f' -> builder.append("\\f");
                    case '\n' -> builder.append("\\n");
                    case '\r' -> builder.append("\\r");
                    case '\t' -> builder.append("\\t");
                    default -> {
                        if (character < 0x20) builder.append(String.format("\\u%04x", (int) character));
                        else builder.append(character);
                    }
                }
            }
            builder.append('"');
        }
        if (comma) builder.append(',');
    }

    private static void integer(StringBuilder builder, String name, int value) {
        integer(builder, name, value, true);
    }

    private static void integer(StringBuilder builder, String name, int value, boolean comma) {
        number(builder, name, value, comma);
    }

    private static void number(StringBuilder builder, String name, long value, boolean comma) {
        property(builder, name).append(value);
        if (comma) builder.append(',');
    }

    private static void nullableInteger(StringBuilder builder, String name, Integer value) {
        property(builder, name).append(value == null ? "null" : value).append(',');
    }

    private static void bool(StringBuilder builder, String name, boolean value) {
        property(builder, name).append(value ? "true," : "false,");
    }
}
