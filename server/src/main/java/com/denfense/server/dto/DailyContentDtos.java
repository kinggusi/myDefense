package com.denfense.server.dto;

import com.denfense.server.domain.DailyContentRefundReason;
import com.denfense.server.domain.DailyContentRunStatus;
import com.denfense.server.domain.DailyContentType;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

import java.time.LocalDate;
import java.util.List;

public final class DailyContentDtos {
    private DailyContentDtos() {
    }

    public record ProgressResponse(String username, LocalDate entryDate, List<ContentProgress> contents) {
    }

    public record ContentProgress(DailyContentType contentType, int remainingEntries,
                                  int highestClearedStage, List<StageReward> stages) {
    }

    public record StageReward(int stage, int repeatReward, int firstClearReward, boolean unlocked, boolean sweepable) {
    }

    public record EnterRequest(@NotBlank @Size(max = 64) String requestId,
                               @NotBlank @Size(max = 64) String username,
                               @NotNull DailyContentType contentType,
                               @Min(1) @Max(5) int stage) {
    }

    public record SweepRequest(@NotBlank @Size(max = 64) String requestId,
                               @NotBlank @Size(max = 64) String username,
                               @NotNull DailyContentType contentType,
                               @Min(1) @Max(5) int stage) {
    }

    public record ResultRequest(@NotBlank @Size(max = 64) String requestId,
                                @NotBlank @Size(max = 36) String runId,
                                @NotBlank @Size(max = 64) String username, @NotNull ResultOutcome outcome,
                                DailyContentRefundReason refundReason) {
    }

    public enum ResultOutcome {
        CLEARED,
        FAILED,
        REFUNDED
    }

    public record RunResponse(String runId, DailyContentType contentType, int stage,
                              DailyContentRunStatus status, int remainingEntries,
                              int rewardAmount, boolean firstClear, boolean alreadyProcessed,
                              int growthCell, int mutationCatalyst) {
    }
}
