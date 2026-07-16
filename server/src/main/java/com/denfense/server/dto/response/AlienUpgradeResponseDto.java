package com.denfense.server.dto.response;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;
import java.math.BigDecimal;

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
    private int maxLevel;
    private boolean canUpgrade;
    private AlienUpgradeBlockReason cannotUpgradeReason;
    private int nextRequiredPieces;
    private int nextRequiredUniversalPiece;
    private int nextRequiredGold;
    private int nextRequiredGrowthCell;
    private BigDecimal currentAtk;
    private BigDecimal currentMp;
    private BigDecimal currentAtkSpeed;
    private BigDecimal currentRange;
}
