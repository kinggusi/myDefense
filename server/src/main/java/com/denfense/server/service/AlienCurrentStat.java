package com.denfense.server.service;

import java.math.BigDecimal;

public record AlienCurrentStat(
        BigDecimal currentAtk,
        BigDecimal currentMp,
        BigDecimal currentAtkSpeed,
        BigDecimal currentRange
) {
}
