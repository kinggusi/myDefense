package com.denfense.server.dto;

import com.denfense.server.balance.QuestCycleType;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

import java.time.Instant;
import java.util.List;

public final class QuestDtos {
    private QuestDtos() {
    }

    public record BoardResponse(String username, Instant serverTime,
                                CycleResponse daily, CycleResponse weekly,
                                List<AchievementItem> achievements,
                                Wallet wallet) {
    }

    public record CycleResponse(QuestCycleType cycleType, String cycleKey, Instant nextResetAt,
                                int activityPoints, List<QuestItem> quests, List<MilestoneItem> milestones) {
    }

    public record QuestItem(String questId, String title, String description, long progress, long targetAmount,
                            int activityPoints, Reward reward, boolean completed, boolean claimed) {
    }

    public record MilestoneItem(int requiredActivityPoints, Reward reward, boolean unlocked, boolean claimed) {
    }

    public record AchievementItem(String achievementId, String category, int tier, String title, String description,
                                  long progress, long targetAmount, Reward reward,
                                  boolean completed, boolean claimed) {
    }

    public record Reward(int gold, int universalPiece, int diamond) {
    }

    public record Wallet(int gold, int universalPiece, int diamond) {
    }

    public record QuestClaimRequest(@NotBlank @Size(max = 64) String requestId,
                                    @NotBlank @Size(max = 64) String username,
                                    @NotBlank @Size(max = 64) String questId) {
    }

    public record MilestoneClaimRequest(@NotBlank @Size(max = 64) String requestId,
                                        @NotBlank @Size(max = 64) String username,
                                        @NotNull QuestCycleType cycleType,
                                        int requiredActivityPoints) {
    }

    public record AchievementClaimRequest(@NotBlank @Size(max = 64) String requestId,
                                          @NotBlank @Size(max = 64) String username,
                                          @NotBlank @Size(max = 64) String achievementId) {
    }

    public record ClaimResponse(String rewardKey, Reward reward, Wallet wallet, boolean alreadyProcessed) {
    }
}
