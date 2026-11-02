package com.denfense.server.clan;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import java.time.Instant;
import static com.denfense.server.clan.ClanDtos.*;

@Entity @Table(name="clans", uniqueConstraints=@UniqueConstraint(name="uk_clan_name",columnNames="normalized_name"))
@Getter @NoArgsConstructor
public class Clan {
    @Id @GeneratedValue(strategy=GenerationType.IDENTITY) private Long id;
    @Column(nullable=false,length=80) private String name;
    @Column(name="normalized_name",nullable=false,length=100) private String normalizedName;
    @Column(nullable=false,length=500) private String description;
    @Column(nullable=false,length=2000) private String notice;
    @Enumerated(EnumType.STRING) @Column(nullable=false,length=16) private JoinMode joinMode;
    @Column(nullable=false) private long leaderId;
    @Column(nullable=false) private int memberCount;
    @Column(nullable=false) private long revision;
    @Column(nullable=false) private boolean active;
    private Instant disbandedAt;

    Clan(String name,String normalizedName,String description,JoinMode mode,long leaderId) {
        this.name=name;this.normalizedName=normalizedName;this.description=description;this.notice="";
        this.joinMode=mode;this.leaderId=leaderId;memberCount=1;active=true;
    }
    void memberAdded() {memberCount++;revision++;}
    void memberRemoved() {memberCount--;revision++;}
    void changed() {revision++;}
    void transfer(long id) {leaderId=id;changed();}
    void update(String description,String notice,JoinMode mode) {this.description=description;this.notice=notice;this.joinMode=mode;changed();}
    void disband(Instant now) {active=false;memberCount=0;disbandedAt=now;changed();}
}
