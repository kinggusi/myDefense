package com.denfense.server.dto.response;

import lombok.Builder;
import lombok.Getter;

import java.math.BigDecimal;

@Getter
@Builder
public class AlienUpgradeStatusResponseDto {
    private Long alienId;
    private String alienName;
    private String grade;
    private boolean owned;
    private boolean specLocked;
    private int currentLevel;
    private int currentPieces;
    private int universalPiece;
    private int gold;
    private int growthCell;
    private int maxLevel;
    private boolean maxLevelReached;
    private boolean canUpgrade;
    private AlienUpgradeBlockReason cannotUpgradeReason;
    private int requiredPieces;
    private int requiredUniversalPiece;
    private int requiredGold;
    private int requiredGrowthCell;
    private int baseAtk;
    private int baseMp;
    private double atkSpeed;
    private double range;
    private BigDecimal currentAtk;
    private BigDecimal currentMp;
    private BigDecimal currentAtkSpeed;
    private BigDecimal currentRange;
}
