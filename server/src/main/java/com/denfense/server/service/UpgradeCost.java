package com.denfense.server.service;

import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class UpgradeCost {
    private int requiredPieces;
    private int requiredGold;
    private int requiredGrowthCell;
}
