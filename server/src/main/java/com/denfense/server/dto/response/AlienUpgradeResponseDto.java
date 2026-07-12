package com.denfense.server.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class AlienUpgradeResponseDto {
    private Long alienId;
    private String alienName;
    private int beforeLevel;
    private int afterLevel;
    
    private int requiredPieces;
    private int usedPieces;
    private int remainingPieces;
    
    private int usedUniversalPiece;
    private int remainingUniversalPiece;
    
    private int usedGold;
    private int remainingGold;
    
    private int usedGrowthCell;
    private int remainingGrowthCell;
    
    private boolean maxLevelReached;
}
