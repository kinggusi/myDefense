package com.denfense.server.service.gacha;

public record GachaDrawResult(
        String productId,
        String gachaPoolId,
        String grade,
        Long alienId
) {
}
