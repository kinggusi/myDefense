package com.denfense.server.clan;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;
import java.time.Duration;

/** Server-owned lobby policy. Not part of the battle balance/hash contract. */
@Component
public record ClanPolicy(int minimumLevel,int creationGold,int maximumMembers,Duration rejoinCooldown,
                         int nameMinimum,int nameMaximum,int descriptionMaximum,int noticeMaximum) {
    public ClanPolicy(@Value("${mydefense.clan.minimum-level:10}") int minimumLevel,
            @Value("${mydefense.clan.creation-gold:50000}") int creationGold,
            @Value("${mydefense.clan.maximum-members:30}") int maximumMembers,
            @Value("${mydefense.clan.rejoin-cooldown:PT24H}") Duration rejoinCooldown,
            @Value("${mydefense.clan.name-minimum:2}") int nameMinimum,
            @Value("${mydefense.clan.name-maximum:20}") int nameMaximum,
            @Value("${mydefense.clan.description-maximum:120}") int descriptionMaximum,
            @Value("${mydefense.clan.notice-maximum:500}") int noticeMaximum) {
        if(minimumLevel<1||creationGold<0||maximumMembers<2||maximumMembers>100||rejoinCooldown.isNegative()
                ||nameMinimum<1||nameMaximum<nameMinimum||nameMaximum>40||descriptionMaximum<0||descriptionMaximum>250||noticeMaximum<0||noticeMaximum>1000)
            throw new IllegalArgumentException("Invalid clan policy");
        this.minimumLevel=minimumLevel;this.creationGold=creationGold;this.maximumMembers=maximumMembers;this.rejoinCooldown=rejoinCooldown;
        this.nameMinimum=nameMinimum;this.nameMaximum=nameMaximum;this.descriptionMaximum=descriptionMaximum;this.noticeMaximum=noticeMaximum;
    }
}
