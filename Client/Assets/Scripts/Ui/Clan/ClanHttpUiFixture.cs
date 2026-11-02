#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using MyDefense.Auth;
using UnityEngine;

namespace MyDefense.Clans
{
    /// <summary>Editor-only isolated HTTP harness. No credential files, login dialog or real account server.</summary>
    public sealed class ClanHttpUiFixture : MonoBehaviour
    {
        public ClanView view;
        public const string Endpoint="http://127.0.0.1:18083/api";
        private AuthSession ownedSession;
        private NetworkManager ownedNetwork;
        private ClanController controller;
        private bool changing;
        private readonly MemoryClanPendingStore[] stores={new MemoryClanPendingStore(),new MemoryClanPendingStore(),new MemoryClanPendingStore()};
        private void Start()
        {
            ClanView.Label(transform,"FixtureNotice","격리 HTTP 테스트 · 18083 / 임시 DB / 메모리 인증만",23,.03f,.94f,.97f,.985f);
            if(AuthSession.Instance!=null||NetworkManager.Instance!=null){view.statusText.text="기존 세션을 보호하기 위해 시작하지 않았습니다. Play를 종료하고 격리 씬만 열어 주세요.";return;}
            controller=gameObject.AddComponent<ClanController>();ownedNetwork=new GameObject("ClanFixtureNetwork").AddComponent<NetworkManager>();ownedNetwork.BaseUrl=Endpoint;
            for(int i=0;i<3;i++){int account=i;var button=ClanView.Button(transform,"FixtureAccount"+i,"테스트 계정 "+(i+1),.04f+i*.32f,.018f,.32f+i*.32f,.075f);button.onClick.AddListener(()=>Switch(account));}
            Switch(0);
        }
        public void Switch(int account){if(!changing&&account>=0&&account<3)StartCoroutine(Authenticate(account));}
        private IEnumerator Authenticate(int account)
        {
            changing=true;controller.Attach(view,null,null,null);view.statusText.text="격리 서버에 테스트 계정을 연결하는 중...";
            if(ownedSession!=null){Destroy(ownedSession.gameObject);yield return null;}
            if(AuthSession.Instance!=null){view.statusText.text="다른 계정 세션이 있어 중단했습니다.";changing=false;yield break;}
            ownedSession=new GameObject("ClanFixtureAuth").AddComponent<AuthSession>();
            var type=typeof(AuthSession);
            type.GetProperty("Instance",BindingFlags.Static|BindingFlags.Public).SetValue(null,ownedSession);
            type.GetProperty("ApiBaseUrl",BindingFlags.Instance|BindingFlags.Public).SetValue(ownedSession,Endpoint);
            type.GetField("store",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ownedSession,new RamCredentials());
            type.GetField("credential",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ownedSession,new StoredCredential{apiBaseUrl=Endpoint,guestSecret=new string((char)('A'+account),42)+"A"});
            bool success=false;yield return ownedSession.SignInGuest(ok=>success=ok);
            if(!success){view.statusText.text="격리 서버를 실행해 주세요. 실제 계정은 변경하지 않았습니다.";changing=false;yield break;}
            controller.Attach(view,new AccountClanTransport(controller,ownedSession,Endpoint),stores[account],null);controller.SetLobbyTab(3);changing=false;
        }
        private void OnDestroy(){if(ownedSession!=null)Destroy(ownedSession.gameObject);if(ownedNetwork!=null)Destroy(ownedNetwork.gameObject);}
        private sealed class RamCredentials:ISecureCredentialStore
        {
            private StoredCredential value;
            public StoredCredential Load()=>value;
            public void Save(StoredCredential credential){value=credential;}
            public void Dispose(){value=null;}
        }
    }
}
#endif
