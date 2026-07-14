package com.denfense.server.service;

import java.time.LocalDateTime;

public record HeartSnapshot(
        int calculatedHeart,
        LocalDateTime nextHeartRecoveryAt,
        LocalDateTime effectiveLastHeartUpdateTime,
        LocalDateTime serverTime
) {
}
