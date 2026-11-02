package com.denfense.server.clan;

import java.time.Instant;
import java.util.List;
import java.util.UUID;
import com.fasterxml.jackson.annotation.JsonInclude;

/** Lobby-only HTTP contract; deliberately independent of Battle/Shared DTOs. */
public final class ClanDtos {
    private ClanDtos() {}
    public enum Role { LEADER, DEPUTY, MEMBER }
    public enum JoinMode { OPEN, APPROVAL }
    public enum Operation { CREATE, JOIN, APPLY, CANCEL_APPLICATION, APPROVE, REJECT, LEAVE, KICK, PROMOTE, DEMOTE, TRANSFER, UPDATE, DISBAND }
    public record Command(UUID requestId, Operation operation, Long clanId, Long targetUserId,
                          Long expectedRevision, String name, String description, String notice, JoinMode joinMode,
                          @JsonInclude(JsonInclude.Include.NON_NULL) Integer expectedCreationGold) {
        public Command(UUID requestId,Operation operation,Long clanId,Long targetUserId,Long expectedRevision,
                       String name,String description,String notice,JoinMode joinMode) {
            this(requestId,operation,clanId,targetUserId,expectedRevision,name,description,notice,joinMode,null);
        }
    }
    public record Member(long userId, String displayName, Role role, Instant joinedAt) {}
    public record Applicant(long userId, String displayName) {}
    public record Summary(long clanId, String name, String description, JoinMode joinMode, int memberCount, int maximumMembers) {}
    public record Detail(Summary clan, long revision, String notice, List<Member> members, List<Applicant> applications) {}
    public record Status(long userId, int accountGold, Role role, Instant rejoinAvailableAt,
                         Long pendingClanId, Detail clan, Instant serverTime, boolean replayed, int accountLevel) {}
    public record Search(List<Summary> clans, int page, int pageSize, boolean hasNext) {}
    public record Policy(int minimumLevel,int creationGold,int maximumMembers,long rejoinCooldownSeconds,
                         int nameMinimum,int nameMaximum,int descriptionMaximum,int noticeMaximum) {}
}
