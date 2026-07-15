package com.denfense.server.dto.gacha;

import java.util.List;

public record GachaPurchaseResponseDto(
        String productId,
        String currencyType,
        int price,
        int remainingDiamond,
        int drawCount,
        List<GachaDrawDto> draws,
        List<GachaRewardDto> rewards
) {}
