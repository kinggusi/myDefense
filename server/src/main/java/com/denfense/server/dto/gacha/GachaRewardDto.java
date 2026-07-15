package com.denfense.server.dto.gacha;

public record GachaRewardDto(
        Long alienId,
        String grade,
        int occurrenceCount,
        boolean newlyUnlocked,
        int piecesAdded,
        int currentLevel,
        int currentPieces
) {}
