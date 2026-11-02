#if UNITY_EDITOR
using System;
using MyDefense.Auth;
using UnityEngine;

namespace MyDefense.Clans
{
    /// <summary>Editor-only presentation fixture. No HTTP, real credentials or economy mutation.</summary>
    public sealed class ClanUiFixture : MonoBehaviour
    {
        public ClanView view;
        private ClanController controller;
        private FixtureTransport transport;
        private int index;
        private readonly string[] modes={"미가입","신청 중","멤버","부클랜장","클랜장","긴 목록","응답 유실"};
        private void Start()
        {
            controller=gameObject.AddComponent<ClanController>();transport=new FixtureTransport();
            ClanView.Label(transform,"FixtureNotice","클랜 UI 테스트 · 실제 서버/계정/골드 변경 없음",24,.03f,.94f,.97f,.985f);
            var next=ClanView.Button(transform,"NextFixture","다음 테스트 상태",.07f,.015f,.93f,.075f);
            next.onClick.AddListener(()=>{index=(index+1)%modes.Length;Show();});Show();
        }
        private void Show()
        {
            transport.status=Example(index);transport.drop=index==6;
            controller.Attach(view,transport,new MemoryClanPendingStore(),null);controller.SetLobbyTab(3);
            view.statusText.text="UI 전용 · "+modes[index];
        }
        public static ClanPolicyDto Policy()=>new ClanPolicyDto{minimumLevel=10,creationGold=50000,maximumMembers=30,rejoinCooldownSeconds=86400,nameMinimum=2,nameMaximum=20,descriptionMaximum=120,noticeMaximum=500};
        public static ClanStatus Example(int mode)
        {
            var value=new ClanStatus{userId=100,accountLevel=10,accountGold=100000,serverTime="2026-11-02T00:00:00Z"};
            if(mode==1)value.pendingClanId=7;
            if(mode>=2&&mode<=5)
            {
                value.role=mode==2?"MEMBER":mode==3?"DEPUTY":"LEADER";
                int count=mode==5?30:3;var members=new ClanMember[count];
                for(int i=0;i<count;i++)members[i]=new ClanMember{userId=100+i,displayName="Guest-"+(100+i),role=i==0?value.role:i==1&&value.role!="LEADER"?"LEADER":"MEMBER",joinedAt="2026-11-01T00:00:00Z"};
                value.clan=new ClanDetail{clan=new ClanSummary{clanId=7,name="왹져 방위대",description="함께 지구를 지켜요.",joinMode="APPROVAL",memberCount=count,maximumMembers=30},revision=12,
                    notice=mode==5?new string('가',500):"오늘도 함께 즐겨요. 가입 신청은 관리 화면에서 확인할 수 있어요.",members=members,
                    applications=value.role=="MEMBER"?Array.Empty<ClanApplicant>():new[]{new ClanApplicant{userId=200,displayName="새로운 왹져"}}};
            }
            return value;
        }
        private sealed class FixtureTransport:IClanTransport
        {
            public ClanStatus status;public bool drop;
            public long UserId=>100;public bool IsCurrent=>true;
            public void Send(string path,string method,string json,Action<AuthHttpResult> completed)
            {
                if(method=="POST"){completed(drop?new AuthHttpResult{Status=0}:new AuthHttpResult{Status=409,Body="{\"code\":\"CLAN_STATE_CHANGED\"}"});return;}
                object value=path.EndsWith("/policy")?(object)Policy():path.EndsWith("/me")?status:
                    path.Contains("?q=")?new ClanSearch{page=0,pageSize=20,clans=new[]{Summary()}}:
                    new ClanDetail{clan=Summary(),revision=1,members=new[]{new ClanMember{userId=999,role="LEADER",displayName="테스트 클랜장"}},applications=Array.Empty<ClanApplicant>()};
                completed(new AuthHttpResult{Status=200,Body=ClanJson.SerializeFixture(value)});
            }
            private ClanSummary Summary()=>new ClanSummary{clanId=7,name="별빛 왹져",description="함께할 왹져를 기다리고 있어요.",joinMode="OPEN",memberCount=1,maximumMembers=30};
        }
    }
}
#endif
