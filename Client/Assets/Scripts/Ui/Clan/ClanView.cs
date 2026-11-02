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
            ShopCatalogView.Frame(view.safeRoot,LobbyNeonGraphic.FrameStyle.Panel);
            view.titleText=Label(view.safeRoot,"Title","클랜",43,.045f,.935f,.73f,.985f,TextAnchor.MiddleLeft);
            view.refreshButton=Button(view.safeRoot,"Refresh","새로고침",.75f,.94f,.96f,.982f);
            view.refreshButton.onClick.AddListener(()=>view.RefreshRequested?.Invoke());
            view.subtitleText=Label(view.safeRoot,"Subtitle","함께할 왹져를 찾아보세요",23,.045f,.89f,.96f,.932f,TextAnchor.MiddleLeft);
            view.statusText=Label(view.safeRoot,"Status","서버 정보를 확인하는 중...",23,.045f,.81f,.96f,.89f,TextAnchor.UpperLeft);
            view.retryButton=Button(view.safeRoot,"Retry","같은 요청 재시도",.50f,.755f,.96f,.802f);
            view.retryButton.onClick.AddListener(()=>view.RetryRequested?.Invoke());view.retryButton.gameObject.SetActive(false);
            view.toolbar=ShopCatalogView.Rect("Toolbar",view.safeRoot);ShopCatalogView.Stretch(view.toolbar,.04f,.69f,.96f,.752f);
            view.searchInput=Input(view.toolbar,"SearchInput","클랜 이름 검색",0,.10f,.61f,.90f,40);
            view.searchButton=Button(view.toolbar,"Search","검색",.63f,.10f,.78f,.90f);
            view.searchButton.onClick.AddListener(()=>view.SearchRequested?.Invoke(view.searchInput.text));
            view.createButton=Button(view.toolbar,"Create","생성",.80f,.10f,1,.90f);view.createButton.onClick.AddListener(()=>view.CreateRequested?.Invoke());
            var viewport=ShopCatalogView.Rect("Viewport",view.safeRoot);ShopCatalogView.Stretch(viewport,.035f,.075f,.965f,.68f);
            var image=viewport.gameObject.AddComponent<Image>();image.color=new Color(.006f,.018f,.035f,.92f);viewport.gameObject.AddComponent<RectMask2D>();
            view.content=ShopCatalogView.Rect("Rows",viewport);view.content.anchorMin=new Vector2(0,1);view.content.anchorMax=new Vector2(1,1);view.content.pivot=new Vector2(.5f,1);view.content.sizeDelta=Vector2.zero;
            var layout=view.content.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(14,14,12,40);layout.spacing=16;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
            view.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            view.scroll=viewport.gameObject.AddComponent<ScrollRect>();view.scroll.viewport=viewport;view.scroll.content=view.content;view.scroll.horizontal=false;view.scroll.vertical=true;view.scroll.movementType=ScrollRect.MovementType.Clamped;view.scroll.scrollSensitivity=38;
            view.previousButton=Button(view.safeRoot,"Previous","이전",.05f,.015f,.25f,.06f);view.previousButton.onClick.AddListener(()=>view.PageRequested?.Invoke(-1));
            view.nextButton=Button(view.safeRoot,"Next","다음",.75f,.015f,.95f,.06f);view.nextButton.onClick.AddListener(()=>view.PageRequested?.Invoke(1));
            view.pageText=Label(view.safeRoot,"Page","",24,.27f,.015f,.73f,.06f);
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
            titleText.text=joined?state.clan.clan.name:"클랜";
            subtitleText.text=joined?$"{ClanClientContract.RoleName(state.role)} · {state.clan.clan.memberCount}/{state.clan.clan.maximumMembers}명 · {ClanClientContract.ModeName(state.clan.clan.joinMode)}":state==null?"로그인 후 클랜 정보를 확인해 주세요.":$"Lv.{state.accountLevel} · 보유 골드 {state.accountGold:N0}";
            statusText.text=!string.IsNullOrEmpty(message)?message:busy?"서버에서 확인하는 중...":blocked?"저장된 요청을 확인하지 못했습니다. 요청은 보존했으며 새 작업을 중단했어요.":pending?"이전 요청의 결과 확인이 필요해요. 같은 요청 재시도는 중복 처리되지 않습니다.":joined?"클랜 정보는 서버에 저장됩니다.":ClanClientContract.Eligibility(state,policy,elapsed);
            toolbar.gameObject.SetActive(!joined);retryButton.gameObject.SetActive(pending&&!blocked);retryButton.interactable=!busy&&!blocked;
            refreshButton.interactable=!busy;searchButton.interactable=!busy;searchInput.interactable=!busy;
            createButton.interactable=fresh&&!busy&&!pending&&!blocked&&ClanClientContract.Eligible(state,policy,elapsed)&&state.accountGold>=policy.creationGold;
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
                var hint=Row("CreatePrice",95);Label(hint,"Text",$"클랜 생성 {policy.creationGold:N0} 골드 · 가입 Lv.{policy.minimumLevel} 이상",24,.035f,.1f,.965f,.9f,TextAnchor.MiddleLeft);
                if(results?.clans!=null)foreach(var c in results.clans)
                {
                    var row=Row("Clan_"+c.clanId,205);Label(row,"Name",c.name,31,.035f,.61f,.72f,.93f,TextAnchor.MiddleLeft);
                    Label(row,"Mode",$"{ClanClientContract.ModeName(c.joinMode)} · {c.memberCount}/{c.maximumMembers}명",23,.035f,.39f,.72f,.62f,TextAnchor.MiddleLeft);
                    Label(row,"Description",c.description,22,.035f,.06f,.72f,.39f,TextAnchor.UpperLeft);
                    var button=Button(row,"Detail","살펴보기",.75f,.27f,.965f,.72f);button.onClick.AddListener(()=>DetailRequested?.Invoke(c));button.interactable=!busy;
                }
                if(results?.clans==null||results.clans.Length==0)TextRow("검색 결과가 없습니다. 다른 이름으로 찾아보세요.",130);
            }
            else
            {
                var tabs=Row("Sections",85,false);
                AddAction(tabs,"Notice","공지",0,.05f,.31f,.95f,()=>CommandRequested?.Invoke("SECTION_NOTICE",0,0),false);
                AddAction(tabs,"Members","멤버",.34f,.05f,.65f,.95f,()=>CommandRequested?.Invoke("SECTION_MEMBERS",0,0),false);
                if(ClanClientContract.Officer(state.role))AddAction(tabs,"Manage","관리",.68f,.05f,1,.95f,()=>CommandRequested?.Invoke("SECTION_MANAGE",0,0),false);
                if(section=="MANAGE"&&ClanClientContract.Officer(state.role))RenderManagement(state);
                else if(section=="MEMBERS")RenderMembers(state.clan,state);
                else
                {
                    TextRow("클랜 소개\n"+(string.IsNullOrWhiteSpace(state.clan.clan.description)?"아직 소개가 없어요.":state.clan.clan.description),220);
                    TextRow("클랜 공지\n"+(string.IsNullOrWhiteSpace(state.clan.notice)?"아직 공지가 없어요.":state.clan.notice),Mathf.Clamp(160+(state.clan.notice?.Length??0)*2,230,1160));
                    var row=Row("LeaveActions",105,false);
                    string op=state.role=="LEADER"?"DISBAND":"LEAVE";
                    if(state.role!="LEADER"||state.clan.clan.memberCount==1)AddAction(row,op,ClanClientContract.ActionName(op),.05f,.08f,.95f,.92f,()=>CommandRequested?.Invoke(op,state.clan.clan.clanId,0));
                    else Label(row,"TransferHint","탈퇴하려면 멤버에게 클랜장을 위임해 주세요.",24,.03f,0,.97f,1);
                }
            }
            foreach(var button in mutationButtons)button.interactable=fresh&&!busy&&!pending&&!blocked;
        }
        private void RenderManagement(ClanStatus state)
        {
            if(state.role=="LEADER"){var row=Row("SettingsRow",100,false);AddAction(row,"Settings","소개 · 공지 · 가입 방식 설정",.02f,.08f,.98f,.92f,()=>SettingsRequested?.Invoke());}
            TextRow("가입 신청 · 승인 시 가입 조건과 정원을 다시 확인합니다.",100);
            var list=state.clan.applications??Array.Empty<ClanApplicant>();
            if(list.Length==0)TextRow("대기 중인 가입 신청이 없습니다.",130);
            foreach(var applicant in list)
            {
                var row=Row("Applicant_"+applicant.userId,160);Label(row,"Name",applicant.displayName,27,.04f,.32f,.54f,.85f,TextAnchor.MiddleLeft);
                AddAction(row,"Approve","승인",.57f,.23f,.75f,.78f,()=>CommandRequested?.Invoke("APPROVE",state.clan.clan.clanId,applicant.userId));
                AddAction(row,"Reject","거절",.78f,.23f,.96f,.78f,()=>CommandRequested?.Invoke("REJECT",state.clan.clan.clanId,applicant.userId));
            }
            if(list.Length>=100)TextRow("먼저 접수된 신청부터 표시합니다. 처리 후 새로고침해 주세요.",125);
        }
        private void RenderMembers(ClanDetail detail,ClanStatus viewer)
        {
            foreach(var member in detail.members??Array.Empty<ClanMember>())
            {
                var row=Row("Member_"+member.userId,145);Label(row,"Name",member.displayName+(member.userId==viewer.userId?" (나)":""),29,.04f,.44f,.72f,.91f,TextAnchor.MiddleLeft);
                Label(row,"Role",ClanClientContract.RoleName(member.role),23,.04f,.10f,.72f,.44f,TextAnchor.MiddleLeft);
                string[] actions=ClanClientContract.MemberActions(viewer.role,viewer.userId,member);
                if(actions.Length>0)AddAction(row,"MemberActions","관리",.76f,.23f,.96f,.78f,()=>ShowActions(member.displayName,actions,op=>CommandRequested?.Invoke(op,detail.clan.clanId,member.userId)));
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
            var panel=ShopCatalogView.Rect("Panel",overlay);ShopCatalogView.Stretch(panel,.025f,.025f,.975f,.975f);ShopCatalogView.Frame(panel,LobbyNeonGraphic.FrameStyle.Panel);
            Label(panel,"Title",title,37,.06f,.89f,.76f,.98f,TextAnchor.MiddleLeft);var close=Button(panel,"Close","닫기",.78f,.90f,.95f,.97f);close.onClick.AddListener(CloseModal);
            body=ShopCatalogView.Rect("Body",panel);ShopCatalogView.Stretch(body,.03f,.035f,.97f,.88f);return panel;
        }
        public void CloseModal(){if(modal!=null){modal.SetActive(false);if(Application.isPlaying)Destroy(modal);else DestroyImmediate(modal);modal=null;}}
        private void OnDisable(){CloseModal();}
        private void ClearRows(){for(int i=content.childCount-1;i>=0;i--){var child=content.GetChild(i).gameObject;child.SetActive(false);if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);}}
        private RectTransform Row(string name,float height,bool frame=true){var row=ShopCatalogView.Rect(name,content);row.gameObject.AddComponent<LayoutElement>().preferredHeight=height;if(frame){ShopCatalogView.Frame(row,LobbyNeonGraphic.FrameStyle.Panel);row.GetComponent<LobbyNeonGraphic>().raycastTarget=true;}return row;}
        private void TextRow(string text,float height){var row=Row("Text",height);Label(row,"Label",text,27,.035f,.05f,.965f,.95f,TextAnchor.UpperLeft);}
        private Button AddAction(Transform parent,string name,string label,float x0,float y0,float x1,float y1,Action action,bool mutation=true){var button=Button(parent,name,label,x0,y0,x1,y1);button.onClick.AddListener(()=>action());if(mutation)mutationButtons.Add(button);return button;}
        public static Text Label(Transform parent,string name,string value,int size,float x0,float y0,float x1,float y1,TextAnchor alignment=TextAnchor.MiddleCenter)
        {var text=ShopCatalogView.Label(parent,name,value,size,x0,y0,x1,y1,alignment);text.supportRichText=false;return text;}
        public static Button Button(Transform parent,string name,string value,float x0,float y0,float x1,float y1)
        {var button=ShopCatalogView.Button(parent,name,value,x0,y0,x1,y1);button.GetComponentInChildren<Text>().supportRichText=false;return button;}
        public static char ValidateSingleLineText(string text,int index,char character)
        {
            // InputField uses this both for typed/pasted characters and programmatic .text assignment.
            return char.IsControl(character)||char.GetUnicodeCategory(character)==UnicodeCategory.Format||character=='<'||character=='>'?'\0':character;
        }
        private static InputField Input(Transform parent,string name,string placeholder,float x0,float y0,float x1,float y1,int maximum,bool filterSingleLine=false)
        {
            var rect=ShopCatalogView.Rect(name,parent);ShopCatalogView.Stretch(rect,x0,y0,x1,y1);var background=rect.gameObject.AddComponent<Image>();background.color=new Color(.02f,.07f,.095f,.98f);
            var input=rect.gameObject.AddComponent<InputField>();input.targetGraphic=background;input.characterLimit=maximum;input.contentType=InputField.ContentType.Standard;input.lineType=InputField.LineType.SingleLine;
            if(filterSingleLine)input.onValidateInput=ValidateSingleLineText;
            input.textComponent=Label(rect,"Text","",28,.035f,.06f,.965f,.94f,TextAnchor.MiddleLeft);input.textComponent.resizeTextForBestFit=false;
            var hint=Label(rect,"Placeholder",placeholder,25,.035f,.06f,.965f,.94f,TextAnchor.MiddleLeft);hint.color=new Color(.43f,.56f,.62f);input.placeholder=hint;
            input.caretColor=new Color(.4f,.9f,.95f);input.customCaretColor=true;return input;
        }
    }
}
