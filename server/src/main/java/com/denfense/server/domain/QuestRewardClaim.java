package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Entity
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
@Table(name = "quest_reward_claims", uniqueConstraints = {
        @UniqueConstraint(name = "uk_quest_reward_claim_user_reward", columnNames = {"user_id", "reward_key"}),
        @UniqueConstraint(name = "uk_quest_reward_claim_user_request", columnNames = {"user_id", "request_id"})
})
public class QuestRewardClaim {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "user_id", nullable = false)
    private User user;

    @Column(name = "reward_key", nullable = false, length = 128)
    private String rewardKey;

    @Column(name = "request_id", nullable = false, length = 64)
    private String requestId;

    @Column(name = "reward_gold", nullable = false)
    private int rewardGold;

    @Column(name = "reward_universal_piece", nullable = false)
    private int rewardUniversalPiece;

    @Column(name = "reward_diamond", nullable = false)
    private int rewardDiamond;

    @Column(name = "claimed_at", nullable = false)
    private LocalDateTime claimedAt;

    public QuestRewardClaim(User user, String rewardKey, String requestId,
                            int rewardGold, int rewardUniversalPiece, int rewardDiamond,
                            LocalDateTime claimedAt) {
        this.user = user;
        this.rewardKey = rewardKey;
        this.requestId = requestId;
        this.rewardGold = rewardGold;
        this.rewardUniversalPiece = rewardUniversalPiece;
        this.rewardDiamond = rewardDiamond;
        this.claimedAt = claimedAt;
    }
}
