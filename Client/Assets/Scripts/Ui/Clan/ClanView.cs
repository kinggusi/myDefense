using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using MyDefense.Lobby;

namespace MyDefense.Clans
{
    public sealed class ClanView : MonoBehaviour
    {
        public RectTransform safeRoot,content;
        public ScrollRect scroll;
        public Text titleText,subtitleText,statusText,pageText;
        public InputField searchInput;
        public Button refreshButton,searchButton,createButton,previousButton,nextButton,retryButton;
        [SerializeField] private RectTransform toolbar;
        [SerializeField] private Text listTitle;
        public Sprite mascotSprite;
        private static readonly Color Ink = new Color(.91f,.95f,1f);
        private static readonly Color Muted = new Color(.58f,.68f,.79f);
        private static readonly Color Accent = new Color(.29f,.79f,.87f);
        private GameObject modal;
        private readonly List<Button> mutationButtons=new List<Button>();
        private float lastBottom=-1;
        public bool HasModal=>modal!=null;
        public Action<string> SearchRequested;
        public Action CreateRequested,RefreshRequested,RetryRequested;
        public Action<int> PageRequested;
        public Action<ClanSummary> DetailRequested;
        public Action<string,long,long> CommandRequested;
        public Action SettingsRequested;
        public static ClanView Build(Transform parent)
        {
            var root=ShopCatalogView.Rect("ClanScreen",parent);ShopCatalogView.Stretch(root,0,0,1,1);
            var view=root.gameObject.AddComponent<ClanView>();
            view.safeRoot=ShopCatalogView.Rect("SafeContent",root);view.safeRoot.anchorMin=Vector2.zero;view.safeRoot.anchorMax=Vector2.one;
            view.safeRoot.offsetMin=new Vector2(48,225);view.safeRoot.offsetMax=new Vector2(-48,-165);
            // The shared profile, wallet HUD and navigation remain outside this subtree.
            var backdrop=Surface(view.safeRoot,"Backdrop",0,0,1,1,new Color(.025f,.055f,.10f,.87f));
            backdrop.border=new Color(0,0,0,0);backdrop.raycastTarget=false;
            view.titleText=Label(view.safeRoot,"Title","클랜",58,.025f,.935f,.7f,.995f,TextAnchor.MiddleLeft);
            view.titleText.fontStyle=FontStyle.Bold;
            view.refreshButton=Button(view.safeRoot,"Refresh","새로고침",.79f,.755f,.975f,.805f);
            view.refreshButton.onClick.AddListener(()=>view.RefreshRequested?.Invoke());
            view.subtitleText=Label(view.safeRoot,"Subtitle","함께할 동료를 찾아보세요",28,.025f,.895f,.975f,.935f,TextAnchor.MiddleLeft);view.subtitleText.color=Muted;
            view.statusText=Label(view.safeRoot,"Status","서버 정보를 확인하는 중...",21,.025f,.006f,.975f,.052f,TextAnchor.MiddleLeft);view.statusText.color=Muted;
            view.retryButton=Button(view.safeRoot,"Retry","같은 요청 재시도",.67f,.006f,.975f,.052f);
            view.retryButton.onClick.AddListener(()=>view.RetryRequested?.Invoke());view.retryButton.gameObject.SetActive(false);
            view.toolbar=ShopCatalogView.Rect("Toolbar",view.safeRoot);ShopCatalogView.Stretch(view.toolbar,.025f,.83f,.975f,.89f);
            view.searchInput=Input(view.toolbar,"SearchInput","클랜 이름 검색",0,.05f,.77f,.95f,40);
            view.searchButton=Button(view.toolbar,"Search","검색",.79f,.05f,1,.95f);
            view.searchButton.onClick.AddListener(()=>view.SearchRequested?.Invoke(view.searchInput.text));
            view.createButton=Button(view.safeRoot,"Create","클랜 생성",.73f,.945f,.975f,.989f);view.createButton.onClick.AddListener(()=>view.CreateRequested?.Invoke());
            view.listTitle=Label(view.safeRoot,"ListTitle","추천 클랜",36,.025f,.755f,.75f,.805f,TextAnchor.MiddleLeft);view.listTitle.fontStyle=FontStyle.Bold;
            var viewport=ShopCatalogView.Rect("Viewport",view.safeRoot);ShopCatalogView.Stretch(viewport,.015f,.115f,.985f,.75f);
            var image=viewport.gameObject.AddComponent<Image>();image.color=new Color(0,0,0,.001f);viewport.gameObject.AddComponent<RectMask2D>();
            view.content=ShopCatalogView.Rect("Rows",viewport);view.content.anchorMin=new Vector2(0,1);view.content.anchorMax=new Vector2(1,1);view.content.pivot=new Vector2(.5f,1);view.content.sizeDelta=Vector2.zero;
            var layout=view.content.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(10,10,8,42);layout.spacing=18;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
            view.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            view.scroll=viewport.gameObject.AddComponent<ScrollRect>();view.scroll.viewport=viewport;view.scroll.content=view.content;view.scroll.horizontal=false;view.scroll.vertical=true;view.scroll.movementType=ScrollRect.MovementType.Clamped;view.scroll.scrollSensitivity=38;
            view.previousButton=Button(view.safeRoot,"Previous","이전",.08f,.06f,.28f,.105f);view.previousButton.onClick.AddListener(()=>view.PageRequested?.Invoke(-1));
            view.nextButton=Button(view.safeRoot,"Next","다음",.72f,.06f,.92f,.105f);view.nextButton.onClick.AddListener(()=>view.PageRequested?.Invoke(1));
            view.pageText=Label(view.safeRoot,"Page","",26,.30f,.06f,.70f,.105f);
            return view;
        }
        private void Update()
        {
            if(safeRoot==null)return;var canvas=GetComponentInParent<Canvas>();float bottom=Screen.safeArea.yMin/Mathf.Max(.01f,canvas==null?1:canvas.scaleFactor);
            if(Mathf.Abs(lastBottom-bottom)<.1f)return;lastBottom=bottom;safeRoot.offsetMin=new Vector2(48,225+Mathf.Max(0,bottom));
        }
        // Unity does not serialize runtime lambda UnityEvent listeners into a prefab.
        // Bind after every controller attachment, including Resources prefab reload.
        public void BindCoreActions()
        {
            refreshButton.onClick.RemoveAllListeners();refreshButton.onClick.AddListener(()=>RefreshRequested?.Invoke());
            retryButton.onClick.RemoveAllListeners();retryButton.onClick.AddListener(()=>RetryRequested?.Invoke());
            searchButton.onClick.RemoveAllListeners();searchButton.onClick.AddListener(()=>SearchRequested?.Invoke(searchInput.text));
            createButton.onClick.RemoveAllListeners();createButton.onClick.AddListener(()=>CreateRequested?.Invoke());
            previousButton.onClick.RemoveAllListeners();previousButton.onClick.AddListener(()=>PageRequested?.Invoke(-1));
            nextButton.onClick.RemoveAllListeners();nextButton.onClick.AddListener(()=>PageRequested?.Invoke(1));
        }
        public void Render(ClanStatus state,ClanPolicyDto policy,ClanSearch results,string section,bool busy,bool pending,bool blocked,string message,double elapsed,bool fresh=true)
        {
            ClearRows();mutationButtons.Clear();
            bool joined=state?.clan!=null;
            titleText.text="클랜";
            subtitleText.text=state==null?"로그인 후 클랜 정보를 확인해 주세요.":joined?"동료들과 함께 지구를 지켜요":"함께할 동료를 찾아보세요";
            statusText.text=!string.IsNullOrEmpty(message)?message:busy?"서버에서 확인하는 중...":blocked?"저장된 요청 확인 실패 · 새 작업 중단":pending?"이전 요청의 결과를 확인해 주세요.":joined?"클랜 정보는 서버에 저장됩니다.":policy==null?"":$"가입 Lv.{policy.minimumLevel} 이상 · 생성 {policy.creationGold:N0} 골드";
            if(!joined&&!busy&&!pending&&!blocked&&string.IsNullOrEmpty(message)&&state!=null&&policy!=null&&!ClanClientContract.Eligible(state,policy,elapsed))statusText.text=ClanClientContract.Eligibility(state,policy,elapsed);
            toolbar.gameObject.SetActive(!joined);retryButton.gameObject.SetActive(pending&&!blocked);retryButton.interactable=!busy&&!blocked;
            ShopCatalogView.Stretch(statusText.rectTransform,.025f,.006f,pending&&!blocked?.64f:.975f,.052f);
            createButton.gameObject.SetActive(!joined);listTitle.gameObject.SetActive(!joined);
            ShopCatalogView.Stretch((RectTransform)refreshButton.transform,.79f,joined?.945f:.755f,.975f,joined?.989f:.805f);
            ShopCatalogView.Stretch(scroll.viewport,.015f,joined?.065f:.115f,.985f,joined?.89f:.75f);
            refreshButton.interactable=!busy;searchButton.interactable=!busy;searchInput.interactable=!busy;
            createButton.interactable=fresh&&!busy&&!pending&&!blocked&&state!=null&&policy!=null&&ClanClientContract.Eligible(state,policy,elapsed)&&state.accountGold>=policy.creationGold;
            if(policy!=null)searchInput.characterLimit=policy.nameMaximum*2;
            previousButton.gameObject.SetActive(!joined);nextButton.gameObject.SetActive(!joined);pageText.gameObject.SetActive(!joined);
            previousButton.interactable=!busy&&(results?.page??0)>0;nextButton.interactable=!busy&&(results?.hasNext??false);
            pageText.text=results==null?"":(results.page+1)+" 페이지";
            if(state==null||policy==null)return;
            if(!joined)
            {
                if(state.pendingClanId>0)
                {
                    var row=Row("PendingApplication",155);Label(row,"Text","가입 신청 대기 중\n클랜 #"+state.pendingClanId,27,.035f,.22f,.63f,.88f,TextAnchor.MiddleLeft);
                    AddAction(row,"CancelApplication","신청 취소",.65f,.2f,.96f,.8f,()=>CommandRequested?.Invoke("CANCEL_APPLICATION",state.pendingClanId,0));
                }
                if(results?.clans!=null)foreach(var c in results.clans)
                {
                    var row=Row("Clan_"+c.clanId,225);Emblem(row,"Emblem",.035f,.17f,.215f,.83f,true);
                    var name=Label(row,"Name",c.name,34,.25f,.62f,.73f,.91f,TextAnchor.MiddleLeft);name.fontStyle=FontStyle.Bold;
                    Label(row,"Mode",$"{ClanClientContract.ModeName(c.joinMode)} · {c.memberCount}/{c.maximumMembers}명",25,.25f,.39f,.73f,.62f,TextAnchor.MiddleLeft).color=Muted;
                    Label(row,"Description",Preview(c.description,37),24,.25f,.09f,.73f,.38f,TextAnchor.UpperLeft).color=Muted;
                    var button=Button(row,"Detail","살펴보기",.76f,.32f,.965f,.66f);button.onClick.AddListener(()=>DetailRequested?.Invoke(c));button.interactable=!busy;
                }
                if(results?.clans==null||results.clans.Length==0)TextRow("검색 결과가 없습니다. 다른 이름으로 찾아보세요.",130);
            }
            else
            {
                RenderIdentity(state);
                if(section=="MANAGE"&&ClanClientContract.Officer(state.role))RenderManagement(state);
                else RenderHome(state);
            }
            foreach(var button in mutationButtons)button.interactable=fresh&&!busy&&!pending&&!blocked;
        }
        private void RenderIdentity(ClanStatus state)
        {
            var identity=Row("ClanIdentity",245);Emblem(identity,"Emblem",.035f,.13f,.23f,.87f,true);
            var name=Label(identity,"ClanName",state.clan.clan.name,37,.27f,.60f,.82f,.91f,TextAnchor.MiddleLeft);name.fontStyle=FontStyle.Bold;
            Label(identity,"ClanMode",$"{ClanClientContract.ModeName(state.clan.clan.joinMode)} · {state.clan.clan.memberCount}/{state.clan.clan.maximumMembers}명",27,.27f,.38f,.96f,.60f,TextAnchor.MiddleLeft).color=Muted;
            Label(identity,"Description",Preview(state.clan.clan.description,38),24,.27f,.09f,.96f,.35f,TextAnchor.UpperLeft).color=Muted;
            if(state.role=="LEADER")AddAction(identity,"Settings","설정",.84f,.65f,.965f,.91f,()=>SettingsRequested?.Invoke());
        }
        private void RenderHome(ClanStatus state)
        {
            var notice=Row("Announcement",112);Label(notice,"Label","공지",30,.035f,.1f,.17f,.9f,TextAnchor.MiddleLeft).color=Accent;
            Label(notice,"Preview",Preview(string.IsNullOrWhiteSpace(state.clan.notice)?"아직 공지가 없어요.":state.clan.notice,33),25,.18f,.10f,.79f,.90f,TextAnchor.MiddleLeft);
            AddAction(notice,"Notice","전체보기",.80f,.19f,.965f,.81f,()=>ShowInformation(state.clan),false);
            var header=Row("MemberHeading",85,false);var text=Label(header,"Title","멤버  "+state.clan.clan.memberCount+"명",34,.015f,0,.55f,1,TextAnchor.MiddleLeft);text.fontStyle=FontStyle.Bold;
            if(ClanClientContract.Officer(state.role))AddAction(header,"Manage","가입 신청  "+(state.clan.applications?.Length??0),.63f,.08f,.985f,.92f,()=>CommandRequested?.Invoke("SECTION_MANAGE",0,0),false);
            RenderMembers(state.clan,state);
            var row=Row("LeaveActions",90,false);string op=state.role=="LEADER"?"DISBAND":"LEAVE";
            if(state.role!="LEADER"||state.clan.clan.memberCount==1)AddAction(row,op,ClanClientContract.ActionName(op),.64f,.1f,.985f,.9f,()=>CommandRequested?.Invoke(op,state.clan.clan.clanId,0));
            else Label(row,"TransferHint","탈퇴하려면 멤버에게 클랜장을 위임해 주세요.",23,.025f,0,.975f,1,TextAnchor.MiddleLeft).color=Muted;
        }
        private void ShowInformation(ClanDetail detail)
        {
            Modal("클랜 소개 · 공지",out var body);
            var viewport=ShopCatalogView.Rect("InformationViewport",body);ShopCatalogView.Stretch(viewport,0,0,1,1);
            var image=viewport.gameObject.AddComponent<Image>();image.color=new Color(0,0,0,.001f);viewport.gameObject.AddComponent<RectMask2D>();
            var text=Label(viewport,"FullInformation","클랜 소개\n"+(string.IsNullOrWhiteSpace(detail.clan.description)?"아직 소개가 없어요.":detail.clan.description)+"\n\n클랜 공지\n"+(string.IsNullOrWhiteSpace(detail.notice)?"아직 공지가 없어요.":detail.notice),29,0,1,1,1,TextAnchor.UpperLeft);
            text.resizeTextForBestFit=false;text.rectTransform.pivot=new Vector2(.5f,1);text.rectTransform.sizeDelta=new Vector2(0,100);
            text.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var reader=viewport.gameObject.AddComponent<ScrollRect>();reader.viewport=viewport;reader.content=text.rectTransform;reader.horizontal=false;reader.movementType=ScrollRect.MovementType.Clamped;reader.scrollSensitivity=38;
        }
        private void RenderManagement(ClanStatus state)
        {
            var heading=Row("ApplicationHeading",90,false);Label(heading,"Title","가입 신청",34,.015f,0,.58f,1,TextAnchor.MiddleLeft).fontStyle=FontStyle.Bold;
            AddAction(heading,"Members","멤버 목록",.65f,.08f,.985f,.92f,()=>CommandRequested?.Invoke("SECTION_MEMBERS",0,0),false);
            TextRow("승인 시 가입 조건과 정원을 다시 확인합니다.",80);
            var list=state.clan.applications??Array.Empty<ClanApplicant>();
            if(list.Length==0)TextRow("대기 중인 가입 신청이 없습니다.",130);
            foreach(var applicant in list)
            {
                var row=Row("Applicant_"+applicant.userId,145);Emblem(row,"Avatar",.025f,.18f,.13f,.82f,false);Label(row,"Name",applicant.displayName,29,.16f,.20f,.55f,.85f,TextAnchor.MiddleLeft);
                AddAction(row,"Approve","승인",.57f,.23f,.75f,.78f,()=>CommandRequested?.Invoke("APPROVE",state.clan.clan.clanId,applicant.userId));
                AddAction(row,"Reject","거절",.78f,.23f,.96f,.78f,()=>CommandRequested?.Invoke("REJECT",state.clan.clan.clanId,applicant.userId));
            }
            if(list.Length>=100)TextRow("먼저 접수된 신청부터 표시합니다. 처리 후 새로고침해 주세요.",125);
        }
        private void RenderMembers(ClanDetail detail,ClanStatus viewer)
        {
            foreach(var member in detail.members??Array.Empty<ClanMember>())
            {
                bool self=member.userId==viewer.userId;
                var row=Row("Member_"+member.userId,132);var surface=row.GetComponent<ClanSurfaceGraphic>();surface.color=self?new Color(.065f,.16f,.23f,.95f):new Color(.035f,.07f,.12f,.18f);surface.border=self?new Color(.18f,.38f,.5f,.65f):new Color(0,0,0,0);
                Emblem(row,"Avatar",.025f,.17f,.13f,.83f,false);
                Label(row,"Name",member.displayName+(self?" (나)":""),30,.17f,.13f,.62f,.87f,TextAnchor.MiddleLeft).fontStyle=FontStyle.Bold;
                var badge=Surface(row,"RoleBadge",.64f,.31f,.83f,.69f,new Color(.10f,.15f,.22f,.9f));badge.border=new Color(.26f,.38f,.50f,.6f);badge.corner=10;badge.raycastTarget=false;
                Label(badge.transform,"Role",ClanClientContract.RoleName(member.role),23,.04f,.06f,.96f,.94f).color=member.role=="MEMBER"?Muted:new Color(.88f,.73f,.39f);
                if(!self){var line=Surface(row,"Divider",.17f,0,.98f,.012f,new Color(.2f,.34f,.46f,.6f));line.thickness=0;line.raycastTarget=false;}
                string[] actions=ClanClientContract.MemberActions(viewer.role,viewer.userId,member);
                if(actions.Length>0)AddAction(row,"MemberActions","···",.86f,.19f,.975f,.81f,()=>ShowActions(member.displayName,actions,op=>CommandRequested?.Invoke(op,detail.clan.clanId,member.userId)));
            }
        }
        public void ShowDetail(ClanDetail detail,ClanStatus state,ClanPolicyDto policy,bool canAct,Action<string> join)
        {
            var panel=Modal("클랜 정보",out var body);
            Label(body,"Name",detail.clan.name,37,.04f,.78f,.96f,.97f);
            Label(body,"Info",$"{ClanClientContract.ModeName(detail.clan.joinMode)} · {detail.clan.memberCount}/{detail.clan.maximumMembers}명\n\n{detail.clan.description}",27,.05f,.37f,.95f,.77f,TextAnchor.UpperLeft);
            Label(body,"Leader","클랜장: "+Array.Find(detail.members,m=>m.role=="LEADER")?.displayName,25,.05f,.22f,.95f,.37f,TextAnchor.MiddleLeft);
            string op=detail.clan.joinMode=="OPEN"?"JOIN":"APPLY";
            var button=Button(body,"Join",ClanClientContract.ActionName(op),.08f,.02f,.92f,.18f);
            button.interactable=canAct&&ClanClientContract.Eligible(state,policy)&&detail.clan.memberCount<detail.clan.maximumMembers&&(op=="JOIN"||state.pendingClanId==0);
            button.onClick.AddListener(()=>{CloseModal();join(op);});
        }
        public void ShowForm(ClanPolicyDto policy,ClanDetail existing,Action<string,string,string,string> accepted)
        {
            bool editing=existing!=null;Modal(editing?"클랜 설정":"클랜 만들기",out var body);
            Label(body,"NameLabel",editing?existing.clan.name:$"클랜 이름 ({policy.nameMinimum}~{policy.nameMaximum}자)",27,.04f,.88f,.96f,.98f,TextAnchor.MiddleLeft);
            InputField name=editing?null:Input(body,"NameInput","클랜 이름",.04f,.77f,.96f,.88f,policy.nameMaximum*2);
            Label(body,"DescriptionLabel","소개 (한 줄)",24,.04f,.66f,.96f,.75f,TextAnchor.MiddleLeft);
            var description=Input(body,"DescriptionInput","함께할 왹져에게 한마디",.04f,.52f,.96f,.66f,policy.descriptionMaximum*2,true);description.text=existing?.clan.description??"";
            InputField notice=null;
            if(editing){Label(body,"NoticeLabel","공지 (한 줄 · 멤버에게만 표시)",24,.04f,.43f,.96f,.52f,TextAnchor.MiddleLeft);notice=Input(body,"NoticeInput","클랜 공지",.04f,.27f,.96f,.43f,policy.noticeMaximum*2,true);notice.text=existing.notice??"";}
            string mode=existing?.clan.joinMode??"OPEN";
            var modeButton=Button(body,"JoinMode",ClanClientContract.ModeName(mode),.04f,.14f,.49f,.25f);
            modeButton.onClick.AddListener(()=>{mode=mode=="OPEN"?"APPROVAL":"OPEN";modeButton.GetComponentInChildren<Text>().text=ClanClientContract.ModeName(mode);});
            Label(body,"Cost",editing?"클랜장만 변경할 수 있어요.":$"생성 비용 {policy.creationGold:N0} 골드",23,.51f,.14f,.96f,.25f);
            var save=Button(body,"Save",editing?"저장":"생성 내용 확인",.08f,.01f,.92f,.12f);
            save.onClick.AddListener(()=>{string n=name==null?"":name.text,d=description.text,no=notice==null?"":notice.text;CloseModal();accepted(n,d,no,mode);});
        }
        public void Confirm(string title,string message,Action accepted)
        {
            Modal(title,out var body);Label(body,"Message",message,30,.06f,.27f,.94f,.94f,TextAnchor.UpperLeft);
            var cancel=Button(body,"Cancel","취소",.05f,.04f,.47f,.20f);cancel.onClick.AddListener(CloseModal);
            var confirm=Button(body,"Confirm","확인",.53f,.04f,.95f,.20f);confirm.onClick.AddListener(()=>{CloseModal();accepted();});
        }
        private void ShowActions(string name,string[] operations,Action<string> selected)
        {
            Modal(name,out var body);for(int i=0;i<operations.Length;i++){string op=operations[i];float top=.92f-i*.24f;var button=Button(body,op,ClanClientContract.ActionName(op),.08f,top-.18f,.92f,top);button.onClick.AddListener(()=>{CloseModal();selected(op);});}
        }
        private RectTransform Modal(string title,out RectTransform body)
        {
            CloseModal();var overlay=ShopCatalogView.Rect("ClanModal",safeRoot);ShopCatalogView.Stretch(overlay,0,0,1,1);overlay.gameObject.AddComponent<Image>().color=new Color(0,.008f,.02f,.96f);modal=overlay.gameObject;
            var panel=ShopCatalogView.Rect("Panel",overlay);ShopCatalogView.Stretch(panel,.025f,.025f,.975f,.975f);var frame=panel.gameObject.AddComponent<ClanSurfaceGraphic>();frame.color=new Color(.035f,.065f,.115f,1);frame.border=new Color(.27f,.46f,.59f,.75f);
            Label(panel,"Title",title,37,.06f,.89f,.76f,.98f,TextAnchor.MiddleLeft);var close=Button(panel,"Close","닫기",.78f,.90f,.95f,.97f);close.onClick.AddListener(CloseModal);
            body=ShopCatalogView.Rect("Body",panel);ShopCatalogView.Stretch(body,.03f,.035f,.97f,.88f);return panel;
        }
        public void CloseModal(){if(modal!=null){modal.SetActive(false);if(Application.isPlaying)Destroy(modal);else DestroyImmediate(modal);modal=null;}}
        private void OnDisable(){CloseModal();}
        private void ClearRows(){for(int i=content.childCount-1;i>=0;i--){var child=content.GetChild(i).gameObject;child.SetActive(false);if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);}}
        private RectTransform Row(string name,float height,bool frame=true){var row=ShopCatalogView.Rect(name,content);row.gameObject.AddComponent<LayoutElement>().preferredHeight=height;if(frame){var surface=row.gameObject.AddComponent<ClanSurfaceGraphic>();surface.color=new Color(.055f,.105f,.17f,.97f);surface.raycastTarget=true;}return row;}
        private void TextRow(string text,float height){var row=Row("Text",height);Label(row,"Label",text,27,.035f,.05f,.965f,.95f,TextAnchor.UpperLeft);}
        private Button AddAction(Transform parent,string name,string label,float x0,float y0,float x1,float y1,Action action,bool mutation=true){var button=Button(parent,name,label,x0,y0,x1,y1);button.onClick.AddListener(()=>action());if(mutation)mutationButtons.Add(button);return button;}
        public static Text Label(Transform parent,string name,string value,int size,float x0,float y0,float x1,float y1,TextAnchor alignment=TextAnchor.MiddleCenter)
        {var text=ShopCatalogView.Label(parent,name,value,size,x0,y0,x1,y1,alignment);text.supportRichText=false;text.color=Ink;text.resizeTextMaxSize=size;text.resizeTextMinSize=Mathf.Max(18,size-7);text.raycastTarget=false;return text;}
        public static Button Button(Transform parent,string name,string value,float x0,float y0,float x1,float y1)
        {
            var surface=Surface(parent,name,x0,y0,x1,y1,new Color(.08f,.19f,.27f,.98f));surface.border=new Color(.28f,.68f,.78f,.85f);surface.corner=12;surface.thickness=1.8f;
            var button=surface.gameObject.AddComponent<Button>();button.targetGraphic=surface;var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.85f,.95f,1);colors.pressedColor=new Color(.66f,.82f,.9f);colors.disabledColor=new Color(.5f,.55f,.6f,.5f);button.colors=colors;
            Label(surface.transform,"Text",value,27,.055f,.06f,.945f,.94f).fontStyle=FontStyle.Bold;return button;
        }
        private static ClanSurfaceGraphic Surface(Transform parent,string name,float x0,float y0,float x1,float y1,Color fill)
        {var rect=ShopCatalogView.Rect(name,parent);ShopCatalogView.Stretch(rect,x0,y0,x1,y1);var graphic=rect.gameObject.AddComponent<ClanSurfaceGraphic>();graphic.color=fill;return graphic;}
        private void Emblem(Transform parent,string name,float x0,float y0,float x1,float y1,bool shield)
        {
            var backing=Surface(parent,name,x0,y0,x1,y1,new Color(.11f,.22f,.31f,1));backing.shape=shield?ClanSurfaceGraphic.Shape.Shield:ClanSurfaceGraphic.Shape.Circle;backing.border=shield?new Color(.75f,.69f,.43f):new Color(.38f,.53f,.67f);backing.thickness=shield?6:2;backing.raycastTarget=false;
            var picture=ShopCatalogView.Rect("Mascot",backing.transform);ShopCatalogView.Stretch(picture,.16f,.12f,.84f,.84f);
            if(mascotSprite!=null){var image=picture.gameObject.AddComponent<Image>();image.sprite=mascotSprite;image.preserveAspect=true;image.raycastTarget=false;}
            else {var fallback=Label(picture,"Fallback","왹",40,0,0,1,1);fallback.color=Accent;}
        }
        private static string Preview(string value,int length)
        {
            if(string.IsNullOrEmpty(value))return "소개가 아직 없어요.";
            int[] elements=StringInfo.ParseCombiningCharacters(value);
            return elements.Length<=length?value:value.Substring(0,elements[length])+"…";
        }
        public static char ValidateSingleLineText(string text,int index,char character)
        {
            // InputField uses this both for typed/pasted characters and programmatic .text assignment.
            return char.IsControl(character)||char.GetUnicodeCategory(character)==UnicodeCategory.Format||character=='<'||character=='>'?'\0':character;
        }
        private static InputField Input(Transform parent,string name,string placeholder,float x0,float y0,float x1,float y1,int maximum,bool filterSingleLine=false)
        {
            var rect=ShopCatalogView.Rect(name,parent);ShopCatalogView.Stretch(rect,x0,y0,x1,y1);var background=rect.gameObject.AddComponent<ClanSurfaceGraphic>();background.color=new Color(.07f,.12f,.19f,.98f);background.border=new Color(.3f,.43f,.58f,.85f);
            var input=rect.gameObject.AddComponent<InputField>();input.targetGraphic=background;input.characterLimit=maximum;input.contentType=InputField.ContentType.Standard;input.lineType=InputField.LineType.SingleLine;
            if(filterSingleLine)input.onValidateInput=ValidateSingleLineText;
            input.textComponent=Label(rect,"Text","",28,.035f,.06f,.965f,.94f,TextAnchor.MiddleLeft);input.textComponent.resizeTextForBestFit=false;
            var hint=Label(rect,"Placeholder",placeholder,25,.035f,.06f,.965f,.94f,TextAnchor.MiddleLeft);hint.color=new Color(.43f,.56f,.62f);input.placeholder=hint;
            input.caretColor=new Color(.4f,.9f,.95f);input.customCaretColor=true;return input;
        }
    }
}
