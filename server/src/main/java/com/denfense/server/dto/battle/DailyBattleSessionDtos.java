package com.denfense.server.dto.battle;

import com.denfense.server.domain.DailyContentType;
import com.fasterxml.jackson.annotation.JsonPropertyOrder;

public final class DailyBattleSessionDtos {
    public static final int SCHEMA_VERSION = 1;

    private DailyBattleSessionDtos() {
    }

    @JsonPropertyOrder({
            "schemaVersion", "runId", "battleSessionId", "contentType", "stage", "mapId",
            "balanceVersion", "contentHash"
    })
    public record Context(
            int schemaVersion,
            String runId,
            String battleSessionId,
            DailyContentType contentType,
            int stage,
            String mapId,
            String balanceVersion,
            String contentHash
    ) {
        public Context {
            if (schemaVersion != SCHEMA_VERSION) {
                throw new IllegalArgumentException("Unsupported Daily Battle Session schemaVersion.");
            }
            if (isBlank(runId) || isBlank(battleSessionId) || isBlank(balanceVersion) || isBlank(contentHash)) {
                throw new IllegalArgumentException("Daily Battle Session identity is required.");
            }
            if (stage < 1 || stage > 5) {
                throw new IllegalArgumentException("Daily Battle stage must be between 1 and 5.");
            }
            String expectedMapId = contentType == null ? null : switch (contentType) {
                case CULTIVATION_ZONE -> "DAILY_CULTIVATION_ZONE";
                case MUTATION_LAB -> "DAILY_MUTATION_LAB";
            };
            if (expectedMapId == null || !expectedMapId.equals(mapId)) {
                throw new IllegalArgumentException("Daily Battle contentType/mapId mismatch.");
            }
        }
    }

    private static boolean isBlank(String value) {
        return value == null || value.isBlank();
    }
}
