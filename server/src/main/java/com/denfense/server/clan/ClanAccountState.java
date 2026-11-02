package com.denfense.server.clan;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import java.time.Instant;
import static com.denfense.server.clan.ClanDtos.*;

/** User row is the mutex for this row, including its first insertion. Never delete cooldown history. */
@Entity @Table(name="clan_account_states",indexes={@Index(name="ix_clan_members",columnList="clan_id"),@Index(name="ix_clan_applicants",columnList="pending_clan_id")})
@Getter @NoArgsConstructor
public class ClanAccountState {
    @Id private Long userId;
    @Column(name="clan_id") private Long clanId;
    @Enumerated(EnumType.STRING) @Column(length=16) private Role role;
    private Instant joinedAt;
    private Instant rejoinAvailableAt;
    @Column(name="pending_clan_id") private Long pendingClanId;
    ClanAccountState(long userId) {this.userId=userId;}
    void join(long clanId,Role role,Instant now) {this.clanId=clanId;this.role=role;joinedAt=now;pendingClanId=null;}
    void leave(Instant availableAt) {clanId=null;role=null;joinedAt=null;rejoinAvailableAt=availableAt;}
    void role(Role role) {this.role=role;}
    void apply(Long clanId) {pendingClanId=clanId;}
}
