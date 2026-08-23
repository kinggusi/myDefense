package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Entity
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
@Table(name = "battle_reward_claims", uniqueConstraints = @UniqueConstraint(
        name = "uk_battle_reward_user_key", columnNames = {"user_id", "reward_key"}))
public class BattleRewardClaim {
    @Id @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "user_id", nullable = false)
    private User user;
    @Column(nullable = false, length = 128)
    private String rewardKey;
    @Column(nullable = false, length = 64)
    private String battleSessionId;
    @Column(nullable = false, length = 32)
    private String rewardType;
    @Column(nullable = false)
    private int gold;
    @Column(nullable = false)
    private int universalPiece;
    @Column(nullable = false)
    private int diamond;
    @Column(nullable = false)
    private LocalDateTime claimedAt;

    public BattleRewardClaim(User user, String rewardKey, String battleSessionId, String rewardType,
                             int gold, int universalPiece, int diamond) {
        this.user = user;
        this.rewardKey = rewardKey;
        this.battleSessionId = battleSessionId;
        this.rewardType = rewardType;
        this.gold = gold;
        this.universalPiece = universalPiece;
        this.diamond = diamond;
        this.claimedAt = LocalDateTime.now();
    }
}
