package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import java.time.Instant;

@Entity @Getter @NoArgsConstructor
@Table(name="mythic_breeding_slots", uniqueConstraints=@UniqueConstraint(name="uk_breeding_user_slot", columnNames={"user_id","slot_no"}))
public class MythicBreedingSlot {
    @Id @GeneratedValue(strategy=GenerationType.IDENTITY) private Long id;
    @ManyToOne(fetch=FetchType.LAZY, optional=false) @JoinColumn(name="user_id", nullable=false) private User user;
    @Column(name="slot_no", nullable=false) private int slotNo;
    @Enumerated(EnumType.STRING) @Column(nullable=false) private MythicBreedingSlotStatus status;
    @Enumerated(EnumType.STRING) @Column(name="unlock_source") private MythicBreedingUnlockSource unlockSource;
    private Long resultAlienId;
    private Instant startedAt;
    private Instant readyAt;
    private Instant claimedAt;
    private String startRequestId;
    private String claimRequestId;
    private String unlockRequestId;
    private Long lastClaimedAlienId;
    @Version private long version;

    public MythicBreedingSlot(User user, int slotNo, MythicBreedingSlotStatus status, MythicBreedingUnlockSource source) {
        this.user=user; this.slotNo=slotNo; this.status=status; this.unlockSource=source;
    }
    public void unlock(MythicBreedingUnlockSource source, String requestId) { status=MythicBreedingSlotStatus.AVAILABLE; unlockSource=source; unlockRequestId=requestId; }
    public void start(long result, Instant now, Instant ready, String requestId) { status=MythicBreedingSlotStatus.BREEDING; resultAlienId=result; startedAt=now; readyAt=ready; startRequestId=requestId; claimRequestId=null; }
    public boolean isReady(Instant now) { return status==MythicBreedingSlotStatus.BREEDING && readyAt!=null && !now.isBefore(readyAt); }
    public void markReady() { if (status==MythicBreedingSlotStatus.BREEDING) status=MythicBreedingSlotStatus.REWARD_READY; }
    public void accelerate(Instant newReadyAt, Instant now) {
        if (status != MythicBreedingSlotStatus.BREEDING) return;
        readyAt = newReadyAt;
        if (!now.isBefore(newReadyAt)) markReady();
    }
    public void claim(Instant now, String requestId) { lastClaimedAlienId=resultAlienId; claimedAt=now; claimRequestId=requestId; status=MythicBreedingSlotStatus.AVAILABLE; resultAlienId=null; startedAt=null; readyAt=null; }
}
