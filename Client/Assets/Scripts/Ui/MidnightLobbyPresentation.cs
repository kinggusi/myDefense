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
        {previousBottom=bottom;mainBody.offsetMin=new Vector2(0,225+Mathf.Max(0,bottom));}
    }
    private void BuildShell()
    {
        // The profile and all its ancestors are intentionally untouched.
        MidnightLobbyTheme.RegisterCanvas(canvas);
        var oldBackground=canvas.transform.Find("LobbySpaceshipBackground");
        if(oldBackground!=null)oldBackground.gameObject.SetActive(false);
        var bg=MidnightLobbyTheme.Art(canvas.transform,"MidnightBackground",MidnightLobbyTheme.BackgroundResource,0,0,1,1);
        bg.transform.SetAsFirstSibling();
        foreach(var view in lobby.viewObjects)
        {
            if(view==null)continue;
            var image=view.GetComponent<Image>();if(image!=null) { image.color=Color.clear;image.raycastTarget=false; }
        }
        BuildCollection();BuildMain();BuildContent();StyleCurrencies();
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
                if(i==1)continue; // Preserve the original transparent mascot asset.
                var oldIcon=button.Find("FinalIcon");if(oldIcon!=null)oldIcon.gameObject.SetActive(false);
                var icon=LobbyFinalPresentation.Child(button,"MidnightIcon");LobbyFinalPresentation.Place(icon,.15f,.17f,.85f,.89f);
                var symbol=LobbyFinalPresentation.Get<MidnightNavGraphic>(icon.gameObject);symbol.Configure(i);midnightSymbols[i]=symbol;
                var underline=button.Find("SelectedUnderline") as RectTransform;
                if(underline!=null)LobbyFinalPresentation.Place(underline,.08f,.075f,.92f,.12f);
            }
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
            var r=(RectTransform)scroll.transform;r.offsetMax=new Vector2(r.offsetMax.x,-435);
            var grid=lobby.unitGridContent.GetComponent<LobbySectionGridLayout>();
            if(grid!=null){grid.spacing=new Vector2(16,18);grid.padding=new RectOffset(18,18,8,50);}
            lobby.unitGridContent.GetComponent<LobbyCollectionGrid>()?.RefreshLayout();
        }
    }
    private static void Header(Transform root,string title)
    {
        var eyebrow=root.Find("MidnightEyebrow");if(eyebrow!=null)eyebrow.gameObject.SetActive(false);
        var label=LobbyFinalPresentation.TextChild(root,"MidnightTitle",title,88);
        Top(label.rectTransform,.055f,.94f,165,275);label.alignment=TextAnchor.MiddleLeft;MidnightLobbyTheme.Typography(label,88,FontStyle.BoldAndItalic);
    }
    private void BuildMain()
    {
        var root=lobby.viewObjects[2].transform;Header(root,"메인");
        mainBody=ShopCatalogView.Rect("MidnightMainBody",root);mainBody.anchorMin=Vector2.zero;mainBody.anchorMax=Vector2.one;
        mainBody.offsetMin=new Vector2(0,225);mainBody.offsetMax=new Vector2(0,-290);
        var art=MidnightLobbyTheme.Art(mainBody,"MidnightPlanet",MidnightLobbyTheme.PlanetResource,.04f,.31f,.96f,1);
        art.transform.SetAsFirstSibling();
        var planet=ShopCatalogView.Label(mainBody,"MidnightPlanetName","해왕성",70,.15f,.55f,.85f,.62f);
        planet.fontStyle=FontStyle.Bold;
        var progressPlate=ShopCatalogView.Rect("MidnightProgressPlate",mainBody);
        ShopCatalogView.Stretch(progressPlate,.12f,.50f,.88f,.55f);
        var progressBackground=progressPlate.gameObject.AddComponent<Image>();
        progressBackground.color=new Color(.025f,.045f,.085f,.88f);progressBackground.raycastTarget=false;
        ShopCatalogView.Label(progressPlate,"MidnightProgress","행성 진행 정보 · 연동 준비 중",32,.025f,0,.975f,1).color=MidnightLobbyTheme.Ink;
        ShopCatalogView.Label(mainBody,"MidnightMode","2인 협동 디펜스",44,.15f,.29f,.85f,.35f);
        var battle=ShopCatalogView.Button(mainBody,"MidnightBattleUnavailable","전투 시작",.08f,.14f,.92f,.275f);
        battle.GetComponent<LobbyNeonGraphic>().UseMidnightPrimary();
        battle.GetComponentInChildren<Text>().color=new Color(.025f,.065f,.10f);
        MidnightLobbyTheme.Typography(battle.GetComponentInChildren<Text>(),66,FontStyle.BoldAndItalic);
        battle.interactable=false;
        var disabled=battle.colors;disabled.disabledColor=new Color(.55f,.70f,.80f,.85f);battle.colors=disabled;
        ShopCatalogView.Label(mainBody,"MidnightBattleNotice","연결 준비 중 · 운영 매칭 연결 후 이용 가능",28,.08f,.09f,.92f,.14f).color=MidnightLobbyTheme.Muted;
        var daily=ShopCatalogView.Button(mainBody,"MidnightDailyUnavailable","일일 콘텐츠",.08f,0,.49f,.085f);
        MidnightLobbyTheme.Typography(daily.GetComponentInChildren<Text>(),36);
        daily.interactable=false;
        var tutorial=root.Find("TutorialEntry") as RectTransform;if(tutorial!=null)PositionTutorial(tutorial);
        foreach(var shortcut in root.GetComponentsInChildren<QuestShortcutView>(true))PositionQuest(shortcut);
        var account=root.Find("AccountLinkShortcut") as RectTransform;if(account!=null)PositionAccountLink(account);
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
        Top((RectTransform)shortcut.transform,.74f,.96f,177,267);
        if(shortcut.statusText!=null)shortcut.statusText.gameObject.SetActive(false);
        var title=shortcut.transform.Find("Title")?.GetComponent<Text>();
        if(title!=null){LobbyFinalPresentation.Place(title.rectTransform,.10f,.08f,.87f,.92f);MidnightLobbyTheme.Typography(title,38);title.alignment=TextAnchor.MiddleCenter;}
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
        Top(rect,.53f,.71f,177,267);
        rect.GetComponent<LobbyNeonGraphic>()?.UseMidnightStyle();
        var label=rect.GetComponentInChildren<Text>();if(label!=null){label.text="계정 연동";MidnightLobbyTheme.Typography(label,32);}
    }
    public static void PositionTutorial(RectTransform entry)
    {
        var root=entry.parent;
        var body=root.name=="MidnightMainBody"?root:root.Find("MidnightMainBody");
        if(body==null)return;
        entry.SetParent(body,false);LobbyFinalPresentation.Place(entry,.52f,0,.92f,.085f);
        var label=entry.GetComponentInChildren<Text>();if(label!=null){label.text="튜토리얼";MidnightLobbyTheme.Typography(label,36);}
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
        Top((RectTransform)panel,.04f,.665f,285,415);
        var frame=panel.Find("LobbyNeonFrame");if(frame!=null)frame.gameObject.SetActive(false);
        var divider=panel.Find("RowDivider");if(divider!=null)divider.gameObject.SetActive(false);
        string[] names={"WakjeoDnaCard","GrowthCellCard"};string[] labels={"왹져DNA","성장세포"};
        for(int i=0;i<2;i++)
        {
            var row=panel.Find(names[i]) as RectTransform;if(row==null)continue;
            LobbyFinalPresentation.Place(row,i*.51f,0,i*.51f+.49f,1);
            var border=LobbyFinalPresentation.Frame(row.gameObject);border.gameObject.SetActive(true);border.UseMidnightStyle();
            var icon=row.Find("MaterialIcon") as RectTransform;if(icon!=null)LobbyFinalPresentation.Place(icon,.04f,.18f,.28f,.82f);
            var name=LobbyFinalPresentation.TextChild(row,"MaterialName",labels[i],28);LobbyFinalPresentation.Place(name.rectTransform,.31f,.53f,.79f,.90f);name.alignment=TextAnchor.MiddleLeft;MidnightLobbyTheme.Typography(name,28);
            var value=row.Find("Value") as RectTransform;if(value!=null){LobbyFinalPresentation.Place(value,.31f,.10f,.78f,.52f);var text=value.GetComponent<Text>();text.alignment=TextAnchor.MiddleLeft;MidnightLobbyTheme.Typography(text,34);}
            var plus=row.Find("PurchasePlus") as RectTransform;if(plus!=null)LobbyFinalPresentation.Place(plus,.80f,.5f,.95f,.5f);
        }
    }
    public static void StyleBreeding(MythicBreedingShortcutView view)
    {
        Top((RectTransform)view.transform,.69f,.96f,285,415);
        LobbyFinalPresentation.Frame(view.gameObject).UseMidnightAccent(new Color(.23f,.14f,.30f,.98f));
        var dna=LobbyFinalPresentation.Child(view.transform,"MidnightDnaIcon");LobbyFinalPresentation.Place(dna,.055f,.40f,.26f,.93f);
        LobbyFinalPresentation.Get<MyDefense.Lobby.LobbyMaterialIconGraphic>(dna.gameObject).Configure(MyDefense.Lobby.LobbyMaterialIconGraphic.MaterialKind.WakjeoDna);
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
            if(midnightSymbols[i]!=null)midnightSymbols[i].color=i==selected?MidnightLobbyTheme.Accent:MidnightLobbyTheme.Muted;
    }
    public void RefreshNavigation(int index) {selected=index;StyleNavigation();}
    public static void Top(RectTransform rect,float left,float right,float top,float bottom)
    {
        rect.anchorMin=new Vector2(left,1);rect.anchorMax=new Vector2(right,1);rect.pivot=new Vector2(.5f,1);
        rect.offsetMin=new Vector2(0,-bottom);rect.offsetMax=new Vector2(0,-top);
    }
}
