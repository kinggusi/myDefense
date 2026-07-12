package com.denfense.server.dto.response;

import lombok.Builder;
import lombok.Getter;

import java.time.LocalDateTime;

@Getter
@Builder
public class EconomyBalanceResponseDto {
    private String username;
    private int accountGold;
    private int gem;
    private int heart;
    private int universalPiece;
    private int growthCell;
    private int heartMax;
    private LocalDateTime nextHeartRecoveryAt;
    private LocalDateTime serverTime;
}
