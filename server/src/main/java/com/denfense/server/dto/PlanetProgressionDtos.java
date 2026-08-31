package com.denfense.server.dto;

import java.time.LocalDateTime;
import java.util.List;

public final class PlanetProgressionDtos {
    private PlanetProgressionDtos() {
    }

    public record Planet(String mapId, int order, boolean unlocked, LocalDateTime unlockedAt) {
    }

    public record Response(Long userId, List<Planet> planets) {
    }
}
