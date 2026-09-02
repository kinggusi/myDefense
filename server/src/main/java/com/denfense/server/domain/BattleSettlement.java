package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Entity
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
@Table(name = "battle_settlements", uniqueConstraints = {
        @UniqueConstraint(name = "uk_battle_session", columnNames = "battle_session_id"),
        @UniqueConstraint(name = "uk_battle_request", columnNames = "request_id")
})
public class BattleSettlement {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    @Column(name = "battle_session_id", nullable = false, unique = true)
    private String battleSessionId;
    @Column(nullable = false, unique = true)
    private String requestId;
    @Column(nullable = false)
    private String summaryHash;
    @Column(nullable = false)
    private String balanceVersion;
    @Column(nullable = false)
    private String contentHash;
    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private BattleResult result;
    private int finalWave;
    @Column(length = 64)
    private String mapId;
    @Enumerated(EnumType.STRING)
    // Nullable only for pre-SessionSource legacy rows. New writes always supply
    // a server-owned value; null is treated as non-production and Quest-excluded.
    @Column(length = 32)
    private SessionSource sessionSource;
    private LocalDateTime startedAt;
    private LocalDateTime finishedAt;
    private LocalDateTime createdAt;
    private LocalDateTime updatedAt;
    @Enumerated(EnumType.STRING)
    private SettlementStatus status;

    public BattleSettlement(String battleSessionId, String requestId, String summaryHash,
                            String balanceVersion, String contentHash, BattleResult result,
                            int finalWave, String mapId, SessionSource sessionSource,
                            LocalDateTime startedAt, LocalDateTime finishedAt) {
        this.battleSessionId = battleSessionId;
        this.requestId = requestId;
        this.summaryHash = summaryHash;
        this.balanceVersion = balanceVersion;
        this.contentHash = contentHash;
        this.result = result;
        this.finalWave = finalWave;
        this.mapId = mapId;
        this.sessionSource = sessionSource;
        this.startedAt = startedAt;
        this.finishedAt = finishedAt;
        this.createdAt = LocalDateTime.now();
        this.updatedAt = createdAt;
        this.status = SettlementStatus.ACCEPTED;
    }
}
