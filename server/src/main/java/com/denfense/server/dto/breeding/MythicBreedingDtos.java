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
    public record SlotsResponse(List<Slot> slots) {}
    public record CandidatesResponse(List<Candidate> candidates) {}
    public record StartResponse(int slotNo, String status, Instant readyAt) {}
    public record ClaimResponse(int slotNo, long resultAlienId, String status, Instant claimedAt) {}
}
