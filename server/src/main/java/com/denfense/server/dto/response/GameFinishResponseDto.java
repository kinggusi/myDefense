package com.denfense.server.dto.response;

import lombok.Builder;
import lombok.Data;

import java.time.LocalDateTime;

@Data
@Builder
public class GameFinishResponseDto {
    private Long userId;
    private String username;
    private String sessionId;
    private int clearedWave;
    private int rewardGold;
    private int accountGoldAfter;
    private LocalDateTime finishedAt;
    private boolean alreadyProcessed;
    private LocalDateTime serverTime;
}
