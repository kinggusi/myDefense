using System;
using MyDefense.Auth;
using UnityEngine;
using UnityEngine.UI;

namespace MyDefense.Tutorial
{
    public sealed class TutorialController : MonoBehaviour
    {
        private LobbyManager lobby;
        private TutorialView view;
        private Button entry;
        private TutorialStatus status;
        private TutorialCommand pending;
        private long userId;
        private string endpoint;
        private int generation,requestGeneration;
        private bool busy,shown;
        private AuthSession session;
        private ITutorialPracticeLauncher launcher=new UnavailableTutorialPracticeLauncher();
        public TutorialStatus Status => status;
        public bool IsShown => shown;
        public bool HasPending => pending!=null;
        public static bool SameAccount(AuthSession captured,long id,int version,string url) => captured!=null&&TutorialPresentation.AccountMatches(ReferenceEquals(captured,AuthSession.Instance),id,captured.User?.userId??0,version,captured.Generation,url,NetworkManager.Instance?.BaseUrl);
        private bool Valid => this!=null&&SameAccount(session,userId,generation,endpoint);
        public void Install(LobbyManager owner)
        {
            Close();requestGeneration++;busy=false;pending=null;status=null;lobby=owner;
            session=AuthSession.Instance;userId=session?.User?.userId??0;generation=session?.Generation??0;endpoint=NetworkManager.Instance?.BaseUrl;
            if(entry!=null)entry.gameObject.SetActive(userId>0);
            if(userId<=0||owner.viewObjects==null||owner.viewObjects.Length<3||owner.viewObjects[2]==null)return;
            if(entry==null)
            {
                entry=ShopCatalogView.Button(owner.viewObjects[2].transform,"TutorialEntry","무료 연습 · 튜토리얼",.20f,.24f,.80f,.30f);
                entry.onClick.AddListener(Open);
            }
        }
        public void SetPracticeLauncher(ITutorialPracticeLauncher adapter) { launcher=adapter??new UnavailableTutorialPracticeLauncher();Render(); }
        public void Open()
        {
            if(!Valid)return;
            if(view==null)
            {
                var canvas=lobby.GetComponentInParent<Canvas>()??FindFirstObjectByType<Canvas>();
                if(canvas==null)return;
                view=TutorialView.Build(canvas.transform);
                view.closeButton.onClick.AddListener(Defer);
                view.refreshButton.onClick.AddListener(Refresh);
                view.primaryButton.onClick.AddListener(Primary);
                view.verifyButton.onClick.AddListener(()=>Send("VERIFY_PRACTICE"));
            }
            shown=true;view.gameObject.SetActive(true);view.transform.SetAsLastSibling();Render();
            if(!busy)Refresh();
        }
        public void Close() { shown=false;if(view!=null)view.gameObject.SetActive(false); }
        public void Defer() { Close();if(Valid&&!busy&&pending==null&&status!=null)Send("DEFER"); }
        public void Refresh()
        {
            if(!Valid||busy)return;busy=true;Render();int token=++requestGeneration;
            NetworkManager.Instance.Get("/tutorial",json=>
            {
                if(!Accept(token))return;busy=false;
                try { Reconcile(JsonUtility.FromJson<TutorialStatus>(json)); }
                catch(Exception) { Error("진행 응답을 읽지 못했어요. 다시 조회해 주세요."); }
            },error=>{if(Accept(token)){busy=false;Error("진행 조회에 실패했어요. 다시 조회해 주세요.");}});
        }
        private bool Accept(int token) => Valid&&token==requestGeneration;
        private void Reconcile(TutorialStatus value)
        {
            Apply(value);
            // GET is authoritative recovery after a lost POST response; no automatic new mutation request.
            pending=null;Render();
        }
        private void Apply(TutorialStatus value)
        {
            if(value==null||value.userId!=userId||!TutorialPresentation.Known(value.step)||value.revision<0)throw new InvalidOperationException();
            if(status!=null&&value.revision<status.revision)throw new InvalidOperationException();
            bool rewardChanged=status==null||!status.rewardGranted;
            status=value;Render();
            if(rewardChanged&&value.rewardGranted)lobby.LoadLobbyData();
        }
        private void Primary()
        {
            if(!Valid||busy||status==null)return;
            if(pending!=null){Retry();return;}
            if(status.step=="COMPLETED"){Close();return;}
            if(status.step=="PRACTICE_PENDING")
            {
                if(!status.practiceAvailable||!launcher.Available){Error("연습 전투 연결을 준비 중입니다. 나중에 이어서 할 수 있어요.");return;}
                Send("START_PRACTICE");return;
            }
            Send(TutorialPresentation.Operation(status.step));
        }
        private void Send(string operation)
        {
            if(!Valid||busy||status==null||pending!=null||string.IsNullOrEmpty(operation))return;
            pending=new TutorialCommand{requestId=Guid.NewGuid().ToString(),expectedRevision=status.revision,operation=operation,
                practiceRunId=operation=="VERIFY_PRACTICE"?status.practiceRunId:null};Retry();
        }
        private void Retry()
        {
            if(!Valid||busy||pending==null)return;busy=true;Render();int token=++requestGeneration;var sent=pending;
            NetworkManager.Instance.PostJson("/tutorial/commands",JsonUtility.ToJson(sent),json=>
            {
                if(!Accept(token))return;busy=false;
                try { Reconcile(JsonUtility.FromJson<TutorialStatus>(json)); }
                catch(Exception){Error("진행 응답을 확인하지 못했어요. 다시 조회해 주세요.");return;}
                if(!shown)return; // A late callback must not reopen a dismissed guide or launch practice.
                if(status.step=="COLLECTION_GUIDE"||status.step=="UPGRADE_GUIDE"||status.step=="BREEDING_GUIDE")lobby.OpenTab(1);
                if(sent.operation=="START_PRACTICE"&&status.step=="PRACTICE_PENDING"&&!string.IsNullOrEmpty(status.practiceRunId))
                {
                    if(!launcher.Available){Error("연습 전투 연결 준비 중입니다.");return;}
                    launcher.Launch(userId,status.practiceRunId,()=>{if(Accept(token)&&shown)Refresh();},message=>{if(Accept(token)&&shown)Error(message);});
                }
            },error=>{if(Accept(token)){busy=false;Error("요청 결과 확인이 필요해요. 같은 요청 재시도 또는 다시 조회를 눌러 주세요.");}});
        }
        private void Render() { if(view!=null&&shown)view.Render(status,busy,launcher.Available,pending!=null); }
        private void Error(string message) { Render();if(view!=null&&shown)view.statusText.text=message; }
        private void OnDestroy() { requestGeneration++;if(view!=null)Destroy(view.gameObject); }
    }
}
