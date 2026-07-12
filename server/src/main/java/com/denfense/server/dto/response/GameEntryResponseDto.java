package com.denfense.server.dto.response;

import lombok.Builder;
import lombok.Data;

import java.time.LocalDateTime;

@Data
@Builder
public class GameEntryResponseDto {
    private Long userId;
    private String username;
    private String sessionId;
    private int remainingHeart;
    private int inGameGold;
    private boolean reconnected;
    private LocalDateTime createdAt;
    private LocalDateTime serverTime;
}
