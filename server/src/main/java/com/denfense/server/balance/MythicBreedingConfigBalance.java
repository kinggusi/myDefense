package com.denfense.server.balance;

public record MythicBreedingConfigBalance(int durationSeconds, int slotCount, int slot2UnlockLevel,
                                          int slot2GemPrice, int slot3GemPrice, int duplicateRewardPieces,
                                          int accelerationUnitSeconds, int accelerationUnitDiamondCost,
                                          boolean enabled) {}
