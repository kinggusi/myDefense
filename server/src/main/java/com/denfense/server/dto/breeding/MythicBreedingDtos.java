package com.denfense.server.dto.breeding;

import java.time.Instant;
import java.util.List;

public final class MythicBreedingDtos {
    private MythicBreedingDtos() {}
    public record Slot(int slotNo, String status, String unlockSource, Instant startedAt, Instant readyAt) {}
    public record Candidate(Long userAlienId, Long alienId, String name, int level, boolean selectable) {}
    public record UnlockRequest(String requestId) {}
    public record StartRequest(Long parentUserAlienIdA, Long parentUserAlienIdB, String requestId) {}
    public record ClaimRequest(String requestId) {}
    public record AccelerateRequest(String requestId, int units) {}
    public record SlotsResponse(List<Slot> slots, int accountLevel, int diamond, int slot2UnlockLevel,
                                int slot2GemPrice, int slot3GemPrice, int durationSeconds,
                                int accelerationUnitSeconds, int accelerationUnitDiamondCost) {}
    public record CandidatesResponse(List<Candidate> candidates) {}
    public record StartResponse(int slotNo, String status, Instant readyAt) {}
    public record ClaimResponse(int slotNo, long resultAlienId, String status, Instant claimedAt) {}
    public record AccelerateResponse(int slotNo, String status, int requestedUnits, int appliedUnits,
                                     int spentDiamond, int remainingDiamond, Instant readyAt) {}
}
