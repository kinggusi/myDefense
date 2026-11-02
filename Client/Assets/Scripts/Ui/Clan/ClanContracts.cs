using System;

namespace MyDefense.Clans
{
    [Serializable] public sealed class ClanPolicyDto
    {
        public int minimumLevel, creationGold, maximumMembers, nameMinimum, nameMaximum, descriptionMaximum, noticeMaximum;
        public long rejoinCooldownSeconds;
    }
    [Serializable] public sealed class ClanSummary
    {
        public long clanId;
        public string name, description, joinMode;
        public int memberCount, maximumMembers;
    }
    [Serializable] public sealed class ClanMember { public long userId; public string displayName, role, joinedAt; }
    [Serializable] public sealed class ClanApplicant { public long userId; public string displayName; }
    [Serializable] public sealed class ClanDetail
    {
        public ClanSummary clan;
        public long revision;
        public string notice;
        public ClanMember[] members;
        public ClanApplicant[] applications;
    }
    [Serializable] public sealed class ClanStatus
    {
        public long userId;
        public int accountGold, accountLevel;
        public string role, rejoinAvailableAt, serverTime;
        // JSON null is represented as zero; canonical server IDs are strictly positive.
        public long pendingClanId;
        public ClanDetail clan;
        public bool replayed;
    }
    [Serializable] public sealed class ClanSearch { public ClanSummary[] clans; public int page, pageSize; public bool hasNext; }
    [Serializable] public sealed class ClanError { public string code, message; }
    [Serializable] public sealed class ClanCommandEnvelope
    {
        public string requestId, operation, name, description, notice, joinMode;
        public long clanId, targetUserId, expectedRevision;
        public int expectedCreationGold;
    }
    [Serializable] internal class ClanBaseRequest { public string requestId, operation; }
    [Serializable] internal sealed class ClanCreateRequest : ClanBaseRequest { public string name, description, joinMode; public int expectedCreationGold; }
    [Serializable] internal class ClanIdRequest : ClanBaseRequest { public long clanId; }
    [Serializable] internal class ClanRevisionRequest : ClanIdRequest { public long expectedRevision; }
    [Serializable] internal sealed class ClanTargetRequest : ClanRevisionRequest { public long targetUserId; }
    [Serializable] internal sealed class ClanUpdateRequest : ClanRevisionRequest { public string description, notice, joinMode; }
}
