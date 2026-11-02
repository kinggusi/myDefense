using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace MyDefense.Clans
{
    public static class ClanClientContract
    {
        public const string Root = "/clans";
        public static bool KnownRole(string role) => role=="LEADER"||role=="DEPUTY"||role=="MEMBER";
        public static string RoleName(string role) => role=="LEADER"?"클랜장":role=="DEPUTY"?"부클랜장":"멤버";
        public static string ModeName(string mode) => mode=="OPEN"?"자유 가입":"승인제";
        public static bool Officer(string role) => role=="LEADER"||role=="DEPUTY";
        public static bool ValidPolicy(ClanPolicyDto p) => p!=null&&p.minimumLevel>0&&p.creationGold>=0&&p.maximumMembers>1
            &&p.rejoinCooldownSeconds>=0&&p.nameMinimum>0&&p.nameMaximum>=p.nameMinimum&&p.nameMaximum<=40
            &&p.descriptionMaximum>=0&&p.descriptionMaximum<=250&&p.noticeMaximum>=0&&p.noticeMaximum<=1000;
        public static bool ValidStatus(ClanStatus s,long userId) => s!=null&&s.userId==userId&&s.accountGold>=0&&s.accountLevel>=1
            &&s.pendingClanId>=0&&Date(s.serverTime,out _)&&(string.IsNullOrEmpty(s.rejoinAvailableAt)||Date(s.rejoinAvailableAt,out _))
            &&(s.clan==null?string.IsNullOrEmpty(s.role):KnownRole(s.role)&&ValidDetail(s.clan)
                &&s.clan.members.Any(m=>m.userId==s.userId&&m.role==s.role));
        public static bool ValidSummary(ClanSummary c) => c!=null&&c.clanId>0&&!string.IsNullOrWhiteSpace(c.name)
            &&(c.joinMode=="OPEN"||c.joinMode=="APPROVAL")&&c.memberCount>=1&&c.maximumMembers>1;
        public static bool ValidDetail(ClanDetail d) => d!=null&&ValidSummary(d.clan)&&d.revision>=0&&d.members!=null
            &&d.members.Length==d.clan.memberCount&&d.members.All(m=>m!=null&&m.userId>0&&KnownRole(m.role)&&!string.IsNullOrWhiteSpace(m.displayName))
            &&d.members.Select(m=>m.userId).Distinct().Count()==d.members.Length
            &&d.members.Count(m=>m.role=="LEADER")==1
            &&(d.applications==null||(d.applications.Length<=100
                &&d.applications.All(a=>a!=null&&a.userId>0&&!string.IsNullOrWhiteSpace(a.displayName))
                &&d.applications.Select(a=>a.userId).Distinct().Count()==d.applications.Length));
        public static bool Date(string value,out DateTimeOffset parsed) => DateTimeOffset.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out parsed);
        public static long CooldownSeconds(ClanStatus status,double elapsedSeconds=0)
        {
            if(status==null||!Date(status.rejoinAvailableAt,out var until)||!Date(status.serverTime,out var now))return 0;
            return Math.Max(0,(long)Math.Ceiling((until-now).TotalSeconds-Math.Max(0,elapsedSeconds)));
        }
        public static string Duration(long seconds) => TimeSpan.FromSeconds(Math.Max(0,seconds)).TotalHours>=1
            ? Math.Ceiling(seconds/3600d).ToString(CultureInfo.InvariantCulture)+"시간" : Math.Ceiling(seconds/60d).ToString(CultureInfo.InvariantCulture)+"분";
        public static string Eligibility(ClanStatus s,ClanPolicyDto p,double elapsed=0)
        {
            if(!ValidPolicy(p)||s==null)return "서버 정보를 불러오는 중입니다.";
            if(s.clan!=null)return "이미 클랜에 소속되어 있습니다.";
            if(s.accountLevel<p.minimumLevel)return $"계정 Lv.{p.minimumLevel}부터 가입할 수 있어요. (현재 Lv.{s.accountLevel})";
            long seconds=CooldownSeconds(s,elapsed);return seconds>0?"재가입까지 "+Duration(seconds)+" 남음":"클랜에 가입하거나 직접 만들어 보세요.";
        }
        public static bool Eligible(ClanStatus s,ClanPolicyDto p,double elapsed=0) => ValidPolicy(p)&&s!=null&&s.clan==null&&s.accountLevel>=p.minimumLevel&&CooldownSeconds(s,elapsed)==0;
        public static string[] MemberActions(string actorRole,long actorId,ClanMember target)
        {
            if(target==null||actorId==target.userId||target.role=="LEADER")return Array.Empty<string>();
            if(actorRole=="DEPUTY")return target.role=="MEMBER"?new[]{"KICK"}:Array.Empty<string>();
            if(actorRole!="LEADER")return Array.Empty<string>();
            return new[]{target.role=="DEPUTY"?"DEMOTE":"PROMOTE","TRANSFER","KICK"};
        }
        public static string ActionName(string op) => op=="PROMOTE"?"부클랜장 임명":op=="DEMOTE"?"일반 멤버로 변경":op=="TRANSFER"?"클랜장 위임":
            op=="KICK"?"추방":op=="DISBAND"?"클랜 해산":op=="LEAVE"?"클랜 탈퇴":op=="APPROVE"?"가입 승인":op=="REJECT"?"신청 거절":
            op=="CANCEL_APPLICATION"?"가입 신청 취소":op=="JOIN"?"가입하기":op=="APPLY"?"가입 신청":op=="CREATE"?"클랜 생성":"저장";
        public static string Create(string name,string description,string mode,int price,string requestId=null) => JsonUtility.ToJson(new ClanCreateRequest
            {requestId=Id(requestId),operation="CREATE",name=name,description=description??"",joinMode=mode,expectedCreationGold=price});
        public static string Command(string operation,long clanId,long revision=0,long targetUserId=0,string requestId=null)
        {
            if(new[]{"APPROVE","REJECT","KICK","PROMOTE","DEMOTE","TRANSFER"}.Contains(operation))
                return JsonUtility.ToJson(new ClanTargetRequest{requestId=Id(requestId),operation=operation,clanId=clanId,expectedRevision=revision,targetUserId=targetUserId});
            if(operation=="DISBAND")return JsonUtility.ToJson(new ClanRevisionRequest{requestId=Id(requestId),operation=operation,clanId=clanId,expectedRevision=revision});
            if(!new[]{"JOIN","APPLY","CANCEL_APPLICATION","LEAVE"}.Contains(operation))throw new ArgumentException("Unsupported clan operation");
            return JsonUtility.ToJson(new ClanIdRequest{requestId=Id(requestId),operation=operation,clanId=clanId});
        }
        public static string Update(long clanId,long revision,string description,string notice,string mode,string requestId=null) => JsonUtility.ToJson(new ClanUpdateRequest
            {requestId=Id(requestId),operation="UPDATE",clanId=clanId,expectedRevision=revision,description=description??"",notice=notice??"",joinMode=mode});
        private static string Id(string value)=>value??Guid.NewGuid().ToString();
        public static ClanCommandEnvelope ReadCommand(string json)
        {
            if(string.IsNullOrEmpty(json)||json.Length>16384)throw new FormatException("Invalid clan pending request");
            var c=JsonUtility.FromJson<ClanCommandEnvelope>(json);
            if(c==null||!Guid.TryParse(c.requestId,out _)||!new[]{"CREATE","JOIN","APPLY","CANCEL_APPLICATION","APPROVE","REJECT","LEAVE","KICK","PROMOTE","DEMOTE","TRANSFER","UPDATE","DISBAND"}.Contains(c.operation))throw new FormatException("Invalid clan operation");
            if(c.operation=="CREATE") {if(string.IsNullOrWhiteSpace(c.name)||c.expectedCreationGold<0||(c.joinMode!="OPEN"&&c.joinMode!="APPROVAL"))throw new FormatException();}
            else if(c.clanId<=0||c.expectedRevision<0)throw new FormatException();
            if(new[]{"APPROVE","REJECT","KICK","PROMOTE","DEMOTE","TRANSFER"}.Contains(c.operation)&&c.targetUserId<=0)throw new FormatException();
            return c;
        }
        public static bool Definitive(long http,string code)
        {
            if(http!=400&&http!=403&&http!=404&&http!=409&&http!=422)return false;
            return new[]{"CLAN_INVALID_REQUEST","CLAN_PERMISSION_DENIED","CLAN_NOT_FOUND","CLAN_USER_NOT_FOUND","CLAN_CONFLICT","CLAN_NAME_TAKEN",
                "CLAN_STATE_CHANGED","CLAN_QUOTE_CHANGED","CLAN_ALREADY_JOINED","CLAN_LEVEL_REQUIRED","CLAN_REJOIN_COOLDOWN","CLAN_FULL",
                "CLAN_APPROVAL_REQUIRED","CLAN_OPEN_JOIN","CLAN_APPLICATION_EXISTS","CLAN_APPLICATION_MISSING","CLAN_LEADER_MUST_TRANSFER",
                "CLAN_ROLE_CHANGED","CLAN_NOT_EMPTY","CLAN_DISBANDED","INSUFFICIENT_ACCOUNT_GOLD"}.Contains(code);
        }
        public static string ErrorMessage(string code) => code=="CLAN_QUOTE_CHANGED"?"생성 비용이 변경됐어요. 새 비용을 확인해 주세요.":
            code=="CLAN_STATE_CHANGED"||code=="CLAN_ROLE_CHANGED"?"클랜 정보가 변경됐어요. 다시 확인해 주세요.":
            code=="CLAN_NAME_TAKEN"?"이미 사용 중인 클랜 이름입니다.":code=="CLAN_PERMISSION_DENIED"?"현재 역할로는 이 작업을 할 수 없어요.":
            code=="CLAN_FULL"?"클랜 정원이 가득 찼어요.":code=="INSUFFICIENT_ACCOUNT_GOLD"?"클랜 생성에 필요한 골드가 부족해요.":
            code=="CLAN_REJOIN_COOLDOWN"?"재가입 대기 시간이 남아 있어요.":code=="CLAN_NOT_EMPTY"||code=="CLAN_LEADER_MUST_TRANSFER"?"다른 멤버에게 위임한 뒤 탈퇴해 주세요. 혼자일 때만 해산할 수 있어요.":
            code=="CLAN_INVALID_REQUEST"?"이름이나 입력 내용을 확인해 주세요.":"요청이 처리되지 않았어요. 최신 클랜 정보를 확인해 주세요.";
    }
}
