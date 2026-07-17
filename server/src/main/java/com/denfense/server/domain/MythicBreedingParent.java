package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Entity @Getter @NoArgsConstructor
@Table(name="mythic_breeding_parents", uniqueConstraints={
        @UniqueConstraint(name="uk_breeding_parent_user_alien", columnNames="user_alien_id"),
        @UniqueConstraint(name="uk_breeding_parent_slot_order", columnNames={"breeding_slot_id","parent_order"})})
public class MythicBreedingParent {
    @Id @GeneratedValue(strategy=GenerationType.IDENTITY) private Long id;
    @ManyToOne(fetch=FetchType.LAZY, optional=false) @JoinColumn(name="breeding_slot_id", nullable=false) private MythicBreedingSlot breedingSlot;
    @ManyToOne(fetch=FetchType.LAZY, optional=false) @JoinColumn(name="user_alien_id", nullable=false) private UserAlien userAlien;
    @Enumerated(EnumType.STRING) @Column(name="parent_order", nullable=false) private MythicBreedingParentOrder parentOrder;
    public MythicBreedingParent(MythicBreedingSlot slot, UserAlien alien, MythicBreedingParentOrder order) { this.breedingSlot=slot; this.userAlien=alien; this.parentOrder=order; }
}
