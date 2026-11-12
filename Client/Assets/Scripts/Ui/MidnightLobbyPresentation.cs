using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Non-destructive lobby skin. Only instantiated scene objects are styled; disabling the preference
/// and restarting Play restores the authored legacy scene/prefabs. No game requests originate here.
/// </summary>
public sealed class MidnightLobbyPresentation : MonoBehaviour
{
    private LobbyManager lobby;
    private Canvas canvas;
    private int selected=-1;
    private bool installed;
    private RectTransform mainBody;
    private float previousBottom=-1;
    private float previousBodyHeight=-1;
    private readonly MidnightNavGraphic[] midnightSymbols=new MidnightNavGraphic[5];
    public static void Install(LobbyManager manager)
    {
        if(manager==null||manager.GetComponent<MidnightLobbyPresentation>()!=null)return;
        var skin=manager.gameObject.AddComponent<MidnightLobbyPresentation>();skin.lobby=manager;
        if(manager.text_UserName!=null)MidnightLobbyTheme.RegisterCanvas(manager.text_UserName.GetComponentInParent<Canvas>());
    }
    private void LateUpdate()
    {
        if(lobby==null||!MidnightLobbyTheme.Enabled)return;
        if(!installed)
        {
            canvas=lobby.text_UserName!=null?lobby.text_UserName.GetComponentInParent<Canvas>():null;
            if(canvas==null||lobby.viewObjects==null||lobby.viewObjects.Length<5)return;
            BuildShell();installed=true;
        }
        int tab=System.Array.FindIndex(lobby.viewObjects,v=>v!=null&&v.activeSelf);
        if(tab!=selected) { selected=tab;StyleNavigation(); }
        float bottom=Screen.safeArea.yMin/Mathf.Max(.01f,canvas.scaleFactor);
        if(mainBody!=null&&Mathf.Abs(previousBottom-bottom)>.1f)
        {previousBottom=bottom;mainBody.offsetMin=new Vector2(0,MidnightLobbyTheme.ContentBottom+Mathf.Max(0,bottom));}
        if(mainBody!=null&&Mathf.Abs(previousBodyHeight-mainBody.rect.height)>.1f)
        {previousBodyHeight=mainBody.rect.height;LayoutMainControls(mainBody);}
    }
    private void BuildShell()
    {
        // Canvas/tab_bar ancestors remain unchanged. The profile itself is now explicitly
        // approved for compact restyling; bound text/button/Slider objects are preserved.
        MidnightLobbyTheme.RegisterCanvas(canvas);
        var oldBackground=canvas.transform.Find("LobbySpaceshipBackground");
        if(oldBackground!=null)oldBackground.gameObject.SetActive(false);
        var bg=MidnightLobbyTheme.Art(canvas.transform,"MidnightBackground",MidnightLobbyTheme.BackgroundResource,0,0,1,1);
        bg.transform.SetAsFirstSibling();
        var shade=LobbyFinalPresentation.Child(canvas.transform,"MidnightContentShade");
        ShopCatalogView.Stretch(shade,0,0,1,1);shade.offsetMax=new Vector2(0,-140);
        var shadeImage=LobbyFinalPresentation.Get<Image>(shade.gameObject);shadeImage.color=new Color(.015f,.025f,.055f,.37f);shadeImage.raycastTarget=false;
        shade.SetSiblingIndex(bg.transform.GetSiblingIndex()+1);
        foreach(var view in lobby.viewObjects)
        {
            if(view==null)continue;
            var image=view.GetComponent<Image>();if(image!=null) { image.color=Color.clear;image.raycastTarget=false; }
        }
        BuildCollection();BuildMain();BuildContent();MidnightHeaderPresentation.Apply(lobby);
        // One initial pass for authored legacy frames. Future popup instances opt in on OnEnable.
        foreach(var frame in canvas.GetComponentsInChildren<LobbyNeonGraphic>(true))
            if(!MidnightLobbyTheme.IsProtectedProfile(frame.transform))frame.UseMidnightStyle();
        var nav=lobby.GetComponent<LobbyFinalPresentation>();
        if(nav!=null&&nav.navigation!=null)
        {
            var image=nav.navigation.GetComponent<Image>();if(image!=null)image.color=new Color(.035f,.055f,.095f,.98f);
            var line=ShopCatalogView.Rect("MidnightNavDivider",nav.navigation);ShopCatalogView.Stretch(line,0,.99f,1,1);
            var divider=line.gameObject.AddComponent<Image>();divider.color=new Color(.32f,.43f,.60f,.5f);divider.raycastTarget=false;
            string[] names={"Button_Shop","Button_Unit","Button_Main","Button_Clan","Button_Etc"};
            for(int i=0;i<names.Length;i++)
            {
                var button=nav.navigation.Find(names[i]);if(button==null)continue;
                var oldIcon=button.Find("FinalIcon");if(oldIcon!=null)oldIcon.gameObject.SetActive(false);
                foreach(var oldSymbol in button.GetComponentsInChildren<LobbyNavIconGraphic>(true))oldSymbol.enabled=false;
                if(i==1&&nav.mascot!=null)nav.mascot.enabled=false;
                var icon=LobbyFinalPresentation.Child(button,"MidnightIcon");
                icon.anchorMin=icon.anchorMax=new Vector2(.5f,.57f);icon.pivot=new Vector2(.5f,.5f);icon.sizeDelta=new Vector2(106,106);icon.anchoredPosition=Vector2.zero;
                var symbol=LobbyFinalPresentation.Get<MidnightNavGraphic>(icon.gameObject);symbol.Configure(i);midnightSymbols[i]=symbol;
                var underline=button.Find("SelectedUnderline") as RectTransform;
                if(underline!=null)LobbyFinalPresentation.Place(underline,.20f,.22f,.80f,.25f);
            }
            nav.ApplySafeBottom(Screen.safeArea.yMin/Mathf.Max(.01f,canvas.scaleFactor));
        }
        StyleNavigation();
    }
    private void BuildCollection()
    {
        var root=lobby.viewObjects[1].transform;
        var old=root.Find("CollectionTitle");if(old!=null)old.gameObject.SetActive(false);
        Header(root,"내 유닛");
        var scroll=lobby.unitGridContent!=null?lobby.unitGridContent.GetComponentInParent<ScrollRect>(true):null;
        if(scroll!=null)
        {
            var r=(RectTransform)scroll.transform;r.anchorMin=new Vector2(.02f,0);r.anchorMax=new Vector2(.98f,1);r.offsetMax=new Vector2(0,-505);r.offsetMin=new Vector2(0,MidnightLobbyTheme.ContentBottom);
            var grid=lobby.unitGridContent.GetComponent<LobbySectionGridLayout>();
            if(grid!=null){grid.spacing=new Vector2(16,18);grid.padding=new RectOffset(18,18,8,50);}
            lobby.unitGridContent.GetComponent<LobbyCollectionGrid>()?.RefreshLayout();
        }
    }
    private static void Header(Transform root,string title)
    {
        var eyebrow=root.Find("MidnightEyebrow");if(eyebrow!=null)eyebrow.gameObject.SetActive(false);
        var label=LobbyFinalPresentation.TextChild(root,"MidnightTitle",title,120);
        Top(label.rectTransform,.055f,.94f,200,360);label.alignment=TextAnchor.MiddleLeft;MidnightLobbyTheme.DisplayTypography(label,120);
    }
    private void BuildMain()
    {
        var root=lobby.viewObjects[2].transform;Header(root,"메인");
        mainBody=ShopCatalogView.Rect("MidnightMainBody",root);mainBody.anchorMin=Vector2.zero;mainBody.anchorMax=Vector2.one;
        mainBody.offsetMin=new Vector2(0,MidnightLobbyTheme.ContentBottom);mainBody.offsetMax=new Vector2(0,-350);
        var art=MidnightLobbyTheme.Art(mainBody,"MidnightPlanet",MidnightLobbyTheme.MainResource,.04f,.31f,.96f,1);
        art.transform.SetAsFirstSibling();
        var planet=ShopCatalogView.Label(mainBody,"MidnightPlanetName","해왕성",64,.15f,.65f,.85f,.71f);
        planet.fontStyle=FontStyle.Bold;
        var progressPlate=ShopCatalogView.Rect("MidnightProgressPlate",mainBody);
        ShopCatalogView.Stretch(progressPlate,.12f,.61f,.88f,.65f);
        var progressBackground=progressPlate.gameObject.AddComponent<Image>();
        progressBackground.color=new Color(.025f,.045f,.085f,.88f);progressBackground.raycastTarget=false;
        ShopCatalogView.Label(progressPlate,"MidnightProgress","행성 진행 정보 · 연동 준비 중",32,.025f,0,.975f,1).color=MidnightLobbyTheme.Ink;
        ShopCatalogView.Label(mainBody,"MidnightMode","2인 협동 디펜스",44,.15f,.29f,.85f,.35f);
        var battle=ShopCatalogView.Button(mainBody,"MidnightBattleUnavailable","전투 시작",.08f,.14f,.92f,.275f);
        battle.GetComponent<LobbyNeonGraphic>().UseMidnightMainCta();
        battle.GetComponentInChildren<Text>().color=new Color(.025f,.065f,.10f);
        MidnightLobbyTheme.DisplayTypography(battle.GetComponentInChildren<Text>(),105);
        battle.interactable=false;
        var disabled=battle.colors;disabled.disabledColor=new Color(.92f,.96f,1f,1f);battle.colors=disabled;
        ShopCatalogView.Label(mainBody,"MidnightBattleNotice","연결 준비 중 · 운영 매칭 연결 후 이용 가능",28,.08f,.09f,.92f,.14f).color=MidnightLobbyTheme.Muted;
        var daily=ShopCatalogView.Button(mainBody,"MidnightDailyUnavailable","일일 콘텐츠",.08f,0,.49f,.085f);
        MidnightLobbyTheme.Typography(daily.GetComponentInChildren<Text>(),36);
        UtilityIcon(daily.transform,6);LobbyFinalPresentation.Place(daily.GetComponentInChildren<Text>().rectTransform,.25f,.06f,.94f,.94f);
        daily.interactable=false;
        var tutorial=root.Find("TutorialEntry") as RectTransform;if(tutorial!=null)PositionTutorial(tutorial);
        foreach(var shortcut in root.GetComponentsInChildren<QuestShortcutView>(true))PositionQuest(shortcut);
        var account=root.Find("AccountLinkShortcut") as RectTransform;if(account!=null)PositionAccountLink(account);
        LayoutMainControls(mainBody);
    }
    public static void LayoutMainControls(RectTransform body)
    {
        float height=Mathf.Max(500,body.rect.height),ctaHeight=Mathf.Min(200,height*.16f),subHeight=Mathf.Min(100,height*.085f);
        float bottom=Mathf.Clamp(height*.14f,120,260);
        BottomBox(body.Find("MidnightBattleUnavailable") as RectTransform,.08f,.92f,bottom,ctaHeight);
        BottomBox(body.Find("MidnightMode") as RectTransform,.15f,.85f,bottom+ctaHeight+22,60);
        float noticeBottom=Mathf.Max(subHeight+8,bottom-65);
        BottomBox(body.Find("MidnightBattleNotice") as RectTransform,.08f,.92f,noticeBottom,Mathf.Max(25,bottom-noticeBottom-8));
        BottomBox(body.Find("MidnightDailyUnavailable") as RectTransform,.08f,.49f,0,subHeight);
        BottomBox(body.Find("TutorialEntry") as RectTransform,.52f,.92f,0,subHeight);
    }
    private static void BottomBox(RectTransform rect,float left,float right,float bottom,float height)
    {
        if(rect==null)return;rect.anchorMin=new Vector2(left,0);rect.anchorMax=new Vector2(right,0);
        rect.offsetMin=new Vector2(0,bottom);rect.offsetMax=new Vector2(0,bottom+height);
    }
    private void BuildContent()
    {
        var root=lobby.viewObjects[4].transform;Header(root,"콘텐츠");
        var panel=ShopCatalogView.Rect("MidnightContentPanel",root);ShopCatalogView.Stretch(panel,.07f,.32f,.93f,.72f);
        ShopCatalogView.Frame(panel,LobbyNeonGraphic.FrameStyle.Panel);
        ShopCatalogView.Label(panel,"Title","새로운 탐사를 준비 중이에요",40,.07f,.69f,.93f,.91f);
        ShopCatalogView.Label(panel,"Description","배양 구역 · 변이 연구소\n전투 진입 연동 후 이곳에서 만나요",29,.08f,.25f,.92f,.68f).color=MidnightLobbyTheme.Muted;
    }
    private void StyleCurrencies()
    {
        var values=new[]{lobby.text_Heart,lobby.text_Gold,lobby.text_Diamond};
        foreach(var value in values)
        {
            if(value==null||MidnightLobbyTheme.IsProtectedProfile(value.transform))continue;
            value.alignment=TMPro.TextAlignmentOptions.MidlineRight;
            value.enableAutoSizing=true;value.fontSizeMin=16;value.fontSizeMax=30;
            LobbyFinalPresentation.Place(value.rectTransform,.22f,.14f,.77f,.86f);
            var plus=value.transform.parent.Find("Plus Button") as RectTransform;
            if(plus!=null){LobbyFinalPresentation.Place(plus,.84f,.5f,.97f,.5f);StyleCurrencyPlus(plus);}
            foreach(var frame in value.transform.parent.GetComponentsInChildren<LobbyNeonGraphic>(true)) frame.UseMidnightStyle();
        }
    }
    public static void PositionQuest(QuestShortcutView shortcut)
    {
        Top((RectTransform)shortcut.transform,.74f,.96f,235,325);
        if(shortcut.statusText!=null)shortcut.statusText.gameObject.SetActive(false);
        var title=shortcut.transform.Find("Title")?.GetComponent<Text>();
        if(title!=null){LobbyFinalPresentation.Place(title.rectTransform,.25f,.08f,.92f,.92f);MidnightLobbyTheme.Typography(title,38);title.alignment=TextAnchor.MiddleCenter;}
        UtilityIcon(shortcut.transform,5);
        if(shortcut.badgeObject!=null)LobbyFinalPresentation.Place((RectTransform)shortcut.badgeObject.transform,.86f,.70f,1f,1.05f);
    }
    public static void StyleCurrencyPlus(RectTransform plus)
    {
        var label=plus.Find("NeonPlusLabel")?.GetComponent<Text>()??plus.GetComponentInChildren<Text>(true);
        if(label==null)return;
        LobbyFinalPresentation.Place(label.rectTransform,0,0,1,1);
        label.gameObject.SetActive(true);label.enabled=true;label.text="+";label.color=MidnightLobbyTheme.Ink;
        label.fontSize=22;label.resizeTextForBestFit=true;label.resizeTextMinSize=10;label.resizeTextMaxSize=22;
        label.horizontalOverflow=HorizontalWrapMode.Overflow;label.verticalOverflow=VerticalWrapMode.Overflow;
        label.alignment=TextAnchor.MiddleCenter;label.alignByGeometry=true;label.raycastTarget=false;
        label.transform.SetAsLastSibling();
    }
    public static void PositionAccountLink(RectTransform rect)
    {
        Top(rect,.53f,.71f,235,325);
        rect.GetComponent<LobbyNeonGraphic>()?.UseMidnightStyle();
        var label=rect.GetComponentInChildren<Text>();if(label!=null){label.text="계정 연동";MidnightLobbyTheme.Typography(label,32);}
    }
    public static void PositionTutorial(RectTransform entry)
    {
        var root=entry.parent;
        var body=root.name=="MidnightMainBody"?root:root.Find("MidnightMainBody");
        if(body==null)return;
        entry.SetParent(body,false);LobbyFinalPresentation.Place(entry,.52f,0,.92f,.085f);
        var label=entry.GetComponentInChildren<Text>();if(label!=null){label.text="튜토리얼";MidnightLobbyTheme.Typography(label,36);LobbyFinalPresentation.Place(label.rectTransform,.25f,.06f,.94f,.94f);}
        UtilityIcon(entry,7);
        LayoutMainControls((RectTransform)body);
    }
    public static void StyleAuth(MyDefense.Auth.AuthStartupView auth)
    {
        var texture=Resources.Load<Texture2D>(MidnightLobbyTheme.BackgroundResource);
        if(auth.background!=null&&texture!=null)
        {
            auth.background.texture=texture;auth.background.raycastTarget=false;
            var fit=auth.background.GetComponent<AspectRatioFitter>();
            if(fit!=null)fit.aspectRatio=(float)texture.width/texture.height;
        }
        if(auth.title!=null)auth.title.color=MidnightLobbyTheme.Ink;
        if(auth.subtitle!=null)auth.subtitle.color=MidnightLobbyTheme.Muted;
        if(auth.notice!=null)auth.notice.color=MidnightLobbyTheme.Muted;
    }
    public static void StyleMaterials(Transform panel)
    {
        Top((RectTransform)panel,.04f,.665f,350,480);
        var frame=panel.Find("LobbyNeonFrame");if(frame!=null)frame.gameObject.SetActive(false);
        var divider=panel.Find("RowDivider");if(divider!=null)divider.gameObject.SetActive(false);
        string[] names={"WakjeoDnaCard","GrowthCellCard"};string[] labels={"왹져DNA","성장세포"};
        for(int i=0;i<2;i++)
        {
            var row=panel.Find(names[i]) as RectTransform;if(row==null)continue;
            LobbyFinalPresentation.Place(row,i*.51f,0,i*.51f+.49f,1);
            var border=LobbyFinalPresentation.Frame(row.gameObject);border.gameObject.SetActive(true);border.UseMidnightStyle();
            var icon=row.Find("MaterialIcon") as RectTransform;if(icon!=null)
            {
                LobbyFinalPresentation.Place(icon,.015f,.08f,.31f,.92f);
                var old=icon.GetComponent<Graphic>();if(old!=null)old.enabled=false;
                MidnightIconAtlas.Place(icon,i==0?MidnightIconAtlas.Icon.Dna:MidnightIconAtlas.Icon.Cells);
            }
            var name=LobbyFinalPresentation.TextChild(row,"MaterialName",labels[i],28);LobbyFinalPresentation.Place(name.rectTransform,.31f,.53f,.79f,.90f);name.alignment=TextAnchor.MiddleLeft;MidnightLobbyTheme.Typography(name,28);
            var value=row.Find("Value") as RectTransform;if(value!=null){LobbyFinalPresentation.Place(value,.31f,.10f,.78f,.52f);var text=value.GetComponent<Text>();text.alignment=TextAnchor.MiddleLeft;MidnightLobbyTheme.Typography(text,34);}
            var plus=row.Find("PurchasePlus") as RectTransform;if(plus!=null)LobbyFinalPresentation.Place(plus,.80f,.5f,.95f,.5f);
        }
    }
    public static void StyleBreeding(MythicBreedingShortcutView view)
    {
        Top((RectTransform)view.transform,.69f,.96f,350,480);
        LobbyFinalPresentation.Frame(view.gameObject).UseMidnightAccent(new Color(.23f,.14f,.30f,.98f));
        var dna=LobbyFinalPresentation.Child(view.transform,"MidnightDnaIcon");LobbyFinalPresentation.Place(dna,.055f,.40f,.26f,.93f);
        var oldDna=dna.GetComponent<MyDefense.Lobby.LobbyMaterialIconGraphic>();if(oldDna!=null)oldDna.enabled=false;
        MidnightIconAtlas.Place(dna,MidnightIconAtlas.Icon.Dna);
        LobbyFinalPresentation.Place(view.titleText.rectTransform,.30f,.49f,.72f,.90f);MidnightLobbyTheme.Typography(view.titleText,32);
        LobbyFinalPresentation.Place(view.statusText.rectTransform,.09f,.07f,.88f,.39f);MidnightLobbyTheme.Typography(view.statusText,24,FontStyle.Normal);
        var arrow=view.transform.Find("EntryArrow") as RectTransform;if(arrow!=null)LobbyFinalPresentation.Place(arrow,.85f,.26f,.98f,.80f);
    }
    private void StyleNavigation()
    {
        var nav=lobby.GetComponent<LobbyFinalPresentation>();if(nav==null)return;
        if(nav.navigationImages!=null)
            for(int i=0;i<nav.navigationImages.Length;i++)
            {
                var image=nav.navigationImages[i];if(image==null)continue;
                image.color=i==selected?MidnightLobbyTheme.Accent:MidnightLobbyTheme.Muted;image.preserveAspect=true;
            }
        if(nav.mascot!=null)nav.mascot.color=selected==1?MidnightLobbyTheme.Accent:MidnightLobbyTheme.Muted;
        for(int i=0;i<midnightSymbols.Length;i++)
            if(midnightSymbols[i]!=null)midnightSymbols[i].SetSelected(i==selected);
    }
    public void RefreshNavigation(int index) {selected=index;StyleNavigation();}
    public static MidnightNavGraphic UtilityIcon(Transform parent,int index,string name="MidnightUtilityIcon",float left=.06f,float right=.23f)
    {
        var rect=LobbyFinalPresentation.Child(parent,name);LobbyFinalPresentation.Place(rect,left,.19f,right,.81f);
        var graphic=LobbyFinalPresentation.Get<MidnightNavGraphic>(rect.gameObject);graphic.Configure(index);graphic.color=MidnightLobbyTheme.Ink;return graphic;
    }
    public static void Top(RectTransform rect,float left,float right,float top,float bottom)
    {
        rect.anchorMin=new Vector2(left,1);rect.anchorMax=new Vector2(right,1);rect.pivot=new Vector2(.5f,1);
        rect.offsetMin=new Vector2(0,-bottom);rect.offsetMax=new Vector2(0,-top);
    }
}
