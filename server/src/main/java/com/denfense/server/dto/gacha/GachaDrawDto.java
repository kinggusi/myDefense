package com.denfense.server.dto.gacha;

public record GachaDrawDto(
        int order,
        Long alienId,
        String grade
) {}
