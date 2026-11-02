using System;
using MyDefense.Auth;
using UnityEngine;
using UnityEngine.Networking;

namespace MyDefense.Clans
{
    public sealed class ClanController : MonoBehaviour
    {
        private ClanView view;
        private IClanTransport transport;
        private IClanPendingStore store;
        private Action walletChanged;
        private ClanStatus status;
        private ClanPolicyDto policy;
        private ClanSearch search;
        private string pending,query="",section="NOTICE",message="";
        private int page,serial,binding;
        private bool busy,blocked,shown,fresh;
        private double statusAt;
        public ClanStatus Status=>status;
        public ClanPolicyDto Policy=>policy;
        public bool HasPending=>pending!=null;
        public bool IsBusy=>busy;
        public ClanView View=>view;
        private bool Valid=>this!=null&&transport!=null&&transport.IsCurrent;

        public void Install(LobbyManager owner)
        {
            if(owner.viewObjects==null||owner.viewObjects.Length<=3||owner.viewObjects[3]==null)return;
            if(view==null)
            {
                var root=owner.viewObjects[3].transform;
                var prefab=Resources.Load<GameObject>("Prefabs/Lobby/Clan/ClanScreen");
                view=prefab==null?ClanView.Build(root):Instantiate(prefab,root,false).GetComponent<ClanView>();
            }
            var auth=AuthSession.Instance;string endpoint=auth?.ApiBaseUrl;
            if(auth==null||auth.User==null||string.IsNullOrEmpty(endpoint)){Attach(view,null,null,null);return;}
            Attach(view,new AccountClanTransport(this,auth,endpoint),new ClanPendingRequestStore(endpoint,auth.User.userId),()=>owner.LoadLobbyData());
        }
        /// <summary>Same controller/view path for production, NO-HTTP fixtures and isolated authenticated HTTP smoke.</summary>
        public void Attach(ClanView screen,IClanTransport accountTransport,IClanPendingStore pendingStore,Action onWalletChanged)
        {
            binding++;serial++;busy=false;blocked=false;fresh=false;status=null;policy=null;search=null;pending=null;page=0;query="";section="NOTICE";message="";
            view=screen;transport=accountTransport;store=pendingStore;walletChanged=onWalletChanged;
            view.CloseModal();view.RefreshRequested=Refresh;view.RetryRequested=Retry;view.CreateRequested=Create;
            view.SearchRequested=Search;view.PageRequested=Page;view.DetailRequested=Detail;view.CommandRequested=Action;view.SettingsRequested=Settings;
            view.BindCoreActions();
            if(store!=null)try{pending=store.Load();if(pending!=null)ClanClientContract.ReadCommand(pending);}catch(Exception){blocked=true;}
            Render();
        }
        public void SetLobbyTab(int index)
        {
            shown=index==3;
            if(view==null)return;
            view.gameObject.SetActive(shown);
            if(!shown){view.CloseModal();return;}
            Render();if(Valid&&!busy)Refresh();
        }
        private void Update()
        {
            if(status!=null&&!Valid){status=null;policy=null;search=null;serial++;busy=false;view.CloseModal();message="계정이 변경됐어요. 다시 로그인해 주세요. 이전 요청은 보존됩니다.";Render();}
        }
        private bool Accept(int request,int generation)=>Valid&&request==serial&&generation==binding;
        public void Refresh()
        {
            if(!Valid||busy)return;
            busy=true;fresh=false;message="";Render();int request=++serial,generation=binding;
            transport.Send(ClanClientContract.Root+"/policy","GET",null,response=>
            {
                if(!Accept(request,generation))return;
                try{if(!response.Success)throw new InvalidOperationException();var p=ClanJson.Read<ClanPolicyDto>(response.Body);if(!ClanClientContract.ValidPolicy(p))throw new InvalidOperationException();policy=p;}
                catch(Exception){ReadFailure();return;}
                transport.Send(ClanClientContract.Root+"/me","GET",null,result=>
                {
                    if(!Accept(request,generation))return;
                    try{if(!result.Success)throw new InvalidOperationException();Apply(ClanJson.Read<ClanStatus>(result.Body));}
                    catch(Exception){ReadFailure();return;}
                    if(status.clan!=null){busy=false;Render();return;}
                    RequestSearch(request,generation);
                });
            });
        }
        private void RequestSearch(int request,int generation)
        {
            transport.Send(ClanClientContract.Root+"?q="+UnityWebRequest.EscapeURL(query)+"&page="+page+"&size=20","GET",null,result=>
            {
                if(!Accept(request,generation))return;
                try
                {
                    if(!result.Success)throw new InvalidOperationException();var value=ClanJson.Read<ClanSearch>(result.Body);
                    if(value==null||value.page!=page||value.pageSize<1||value.pageSize>20||value.clans==null||value.clans.Length>20)throw new InvalidOperationException();
                    foreach(var clan in value.clans)if(!ClanClientContract.ValidSummary(clan))throw new InvalidOperationException();
                    search=value;busy=false;Render();
                }
                catch(Exception){ReadFailure();}
            });
        }
        private void ReadFailure(){busy=false;fresh=false;message="서버 정보를 불러오지 못했어요. 새로고침해 주세요. 이전 요청은 보존됩니다.";Render();}
        private void Apply(ClanStatus value)
        {
            if(!ClanClientContract.ValidStatus(value,transport.UserId))throw new InvalidOperationException("Invalid clan status");
            if(status?.clan!=null&&value.clan?.clan.clanId==status.clan.clan.clanId&&value.clan.revision<status.clan.revision)throw new InvalidOperationException("Stale clan response");
            status=value;fresh=true;statusAt=Time.realtimeSinceStartupAsDouble;
            if(!ClanClientContract.Officer(status.role)&&section=="MANAGE")section="NOTICE";
        }
        public void Search(string value){if(!Valid||busy)return;query=value??"";page=0;Refresh();}
        public void Page(int direction){if(!Valid||busy||search==null)return;if(direction<0&&page>0)page--;else if(direction>0&&search.hasNext)page++;else return;Refresh();}
        private bool CanMutate=>Valid&&fresh&&!busy&&!blocked&&pending==null&&status!=null&&policy!=null;
        public void Create()
        {
            if(!CanMutate||!ClanClientContract.Eligible(status,policy,Time.realtimeSinceStartupAsDouble-statusAt))return;
            var quoted=policy;int accountBinding=binding;
            view.ShowForm(policy,null,(name,description,notice,mode)=>
            {
                if(!CanMutate||accountBinding!=binding)return;
                string json=ClanClientContract.Create(name,description,mode,quoted.creationGold);
                view.Confirm("클랜 생성 확인",$"{name}\n{ClanClientContract.ModeName(mode)}\n\n{quoted.creationGold:N0} 골드를 사용합니다.\n생성 비용은 환급되지 않습니다.",()=>{if(accountBinding==binding)Queue(json);});
            });
        }
        public void Detail(ClanSummary summary)
        {
            if(!Valid||busy||summary==null)return;busy=true;Render();int request=++serial,generation=binding;
            transport.Send(ClanClientContract.Root+"/"+summary.clanId,"GET",null,response=>
            {
                if(!Accept(request,generation))return;busy=false;
                try
                {
                    if(!response.Success)throw new InvalidOperationException();var detail=ClanJson.Read<ClanDetail>(response.Body);
                    if(!ClanClientContract.ValidDetail(detail)||detail.clan.clanId!=summary.clanId)throw new InvalidOperationException();
                    Render();if(shown)view.ShowDetail(detail,status,policy,CanMutate,op=>Action(op,detail.clan.clanId,0));
                }
                catch(Exception){ReadFailure();}
            });
        }
        public void Settings()
        {
            if(!CanMutate||status.role!="LEADER"||status.clan==null)return;
            var current=status.clan;int accountBinding=binding;
            view.ShowForm(policy,current,(name,description,notice,mode)=>
            {if(accountBinding==binding&&CanMutate)Queue(ClanClientContract.Update(current.clan.clanId,current.revision,description,notice,mode));});
        }
        public void Action(string operation,long clanId,long targetUserId)
        {
            if(operation.StartsWith("SECTION_",StringComparison.Ordinal)){section=operation.Substring(8);Render();return;}
            if(!CanMutate)return;
            long revision=status.clan?.revision??0;int accountBinding=binding;
            string payload;
            try{payload=ClanClientContract.Command(operation,clanId,revision,targetUserId);}catch(Exception){return;}
            string warning=operation=="LEAVE"||operation=="KICK"||operation=="DISBAND"?$"이 작업 후 {ClanClientContract.Duration(policy.rejoinCooldownSeconds)} 동안 재가입할 수 없습니다.":
                operation=="TRANSFER"?"클랜장을 위임하면 나는 일반 멤버가 됩니다.":operation=="APPLY"?"가입은 관리자의 승인 후 완료됩니다.":"클랜 정보는 서버에서 확인한 뒤 변경됩니다.";
            view.Confirm(ClanClientContract.ActionName(operation),warning,()=>{if(accountBinding==binding)Queue(payload);});
        }
        private void Queue(string json)
        {
            if(!CanMutate)return;
            try{ClanClientContract.ReadCommand(json);store.Save(json);pending=json;}
            catch(Exception){blocked=true;message="요청을 안전하게 저장하지 못해 전송하지 않았어요.";Render();return;}
            Retry();
        }
        public void Retry()
        {
            if(!Valid||busy||blocked||pending==null)return;
            busy=true;message="";Render();int request=++serial,generation=binding;string sent=pending;
            transport.Send(ClanClientContract.Root+"/commands","POST",sent,response=>
            {
                if(!Accept(request,generation)||pending!=sent)return;busy=false;
                if(response.Success)
                {
                    try{Apply(ClanJson.Read<ClanStatus>(response.Body));}
                    catch(Exception){message="응답을 확인하지 못했어요. 같은 요청으로 다시 확인해 주세요.";Render();return;}
                    if(!ClearPending())return;
                    message=response.Body==null?"처리 완료":"클랜 정보가 저장되었습니다.";Render();walletChanged?.Invoke();
                    if(shown)Refresh();
                    return;
                }
                string code=null;try{code=JsonUtility.FromJson<ClanError>(response.Body)?.code;}catch(Exception){}
                if(ClanClientContract.Definitive(response.Status,code))
                {
                    if(!ClearPending())return;message=ClanClientContract.ErrorMessage(code);Render();
                    // Refresh canonical roles/price before another operation; the error remains visible afterward.
                    string failure=message;Refresh();message=failure;Render();
                }
                else {message="처리 결과 확인이 필요해요. 새 요청을 만들지 말고 같은 요청 재시도를 눌러 주세요.";Render();}
            });
        }
        private bool ClearPending()
        {
            try{store.Clear();pending=null;return true;}
            catch(Exception){blocked=true;message="서버 응답은 받았지만 요청 기록을 정리하지 못했어요. 기록은 보존했습니다.";Render();return false;}
        }
        private void Render(){if(view!=null)view.Render(status,policy,search,section,busy,pending!=null,blocked,message,Time.realtimeSinceStartupAsDouble-statusAt,fresh);}
        private void OnDestroy(){serial++;binding++;if(view!=null)view.CloseModal();}
    }
}
