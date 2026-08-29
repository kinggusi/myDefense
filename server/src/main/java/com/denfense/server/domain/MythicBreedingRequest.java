package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.Instant;

@Entity
@Getter
@NoArgsConstructor
@Table(name = "mythic_breeding_requests", uniqueConstraints =
        @UniqueConstraint(name = "uk_breeding_request_user_request", columnNames = {"user_id", "request_id"}))
public class MythicBreedingRequest {
    @Id @GeneratedValue(strategy = GenerationType.IDENTITY) private Long id;
    @ManyToOne(fetch = FetchType.LAZY, optional = false) @JoinColumn(name = "user_id", nullable = false) private User user;
    @Column(name = "request_id", nullable = false, length = 100) private String requestId;
    @Enumerated(EnumType.STRING) @Column(nullable = false, length = 16) private MythicBreedingRequestOperation operation;
    @Column(name = "slot_no", nullable = false) private int slotNo;
    @Column(name = "payload_key", nullable = false, length = 200) private String payloadKey;
    @Column(name = "response_status", nullable = false, length = 32) private String responseStatus;
    @Column(name = "response_unlock_source", length = 16) private String responseUnlockSource;
    @Column(name = "response_started_at") private Instant responseStartedAt;
    @Column(name = "response_ready_at") private Instant responseReadyAt;
    @Column(name = "response_result_alien_id") private Long responseResultAlienId;
    @Column(name = "response_claimed_at") private Instant responseClaimedAt;
    @Column(name = "created_at", nullable = false, updatable = false) private Instant createdAt;

    public MythicBreedingRequest(User user, String requestId, MythicBreedingRequestOperation operation,
                                 int slotNo, String payloadKey, String responseStatus,
                                 String responseUnlockSource, Instant responseStartedAt, Instant responseReadyAt,
                                 Long responseResultAlienId, Instant responseClaimedAt, Instant createdAt) {
        this.user = user;
        this.requestId = requestId;
        this.operation = operation;
        this.slotNo = slotNo;
        this.payloadKey = payloadKey;
        this.responseStatus = responseStatus;
        this.responseUnlockSource = responseUnlockSource;
        this.responseStartedAt = responseStartedAt;
        this.responseReadyAt = responseReadyAt;
        this.responseResultAlienId = responseResultAlienId;
        this.responseClaimedAt = responseClaimedAt;
        this.createdAt = createdAt;
    }
}
