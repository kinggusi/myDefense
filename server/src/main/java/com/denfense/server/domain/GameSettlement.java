package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.AccessLevel;
import lombok.Builder;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Entity
@Table(name = "game_settlements")
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
public class GameSettlement {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, unique = true, length = 36)
    private String sessionId;

    @Column(nullable = false)
    private Long userId;

    @Column(nullable = false)
    private int clearedWave;

    @Column(nullable = false)
    private int rewardGold;

    @Column(nullable = false)
    private int accountGoldAfter;

    @Column(nullable = false)
    private LocalDateTime finishedAt;

    @Builder
    public GameSettlement(String sessionId, Long userId, int clearedWave, int rewardGold, int accountGoldAfter, LocalDateTime finishedAt) {
        this.sessionId = sessionId;
        this.userId = userId;
        this.clearedWave = clearedWave;
        this.rewardGold = rewardGold;
        this.accountGoldAfter = accountGoldAfter;
        this.finishedAt = finishedAt;
    }
}
