package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.Instant;

@Entity
@Getter
@NoArgsConstructor
@Table(name = "mythic_breeding_accelerations", uniqueConstraints =
        @UniqueConstraint(name = "uk_breeding_acceleration_user_request", columnNames = {"user_id", "request_id"}))
public class MythicBreedingAcceleration {
    @Id @GeneratedValue(strategy = GenerationType.IDENTITY) private Long id;
    @ManyToOne(fetch = FetchType.LAZY, optional = false) @JoinColumn(name = "user_id", nullable = false) private User user;
    @ManyToOne(fetch = FetchType.LAZY, optional = false) @JoinColumn(name = "breeding_slot_id", nullable = false) private MythicBreedingSlot slot;
    @Column(name = "request_id", nullable = false, length = 100) private String requestId;
    @Column(nullable = false) private int requestedUnits;
    @Column(nullable = false) private int appliedUnits;
    @Column(nullable = false) private int spentDiamond;
    @Column(nullable = false, length = 32) private String responseStatus;
    @Column(nullable = false) private int remainingDiamond;
    @Column(nullable = false) private Instant readyAtAfter;
    @Column(nullable = false, updatable = false) private Instant createdAt;

    public MythicBreedingAcceleration(User user, MythicBreedingSlot slot, String requestId, int requestedUnits,
                                      int appliedUnits, int spentDiamond, String responseStatus, int remainingDiamond,
                                      Instant readyAtAfter, Instant createdAt) {
        this.user = user;
        this.slot = slot;
        this.requestId = requestId;
        this.requestedUnits = requestedUnits;
        this.appliedUnits = appliedUnits;
        this.spentDiamond = spentDiamond;
        this.responseStatus = responseStatus;
        this.remainingDiamond = remainingDiamond;
        this.readyAtAfter = readyAtAfter;
        this.createdAt = createdAt;
    }
}
