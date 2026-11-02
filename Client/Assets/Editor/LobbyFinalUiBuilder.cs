#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Opt-in final collection styling. Never touches profile, tab_bar or Canvas transforms.</summary>
public static class LobbyFinalUiBuilder
{
    public const string BackgroundPath = "Assets/Art/Lobby/Final/SpaceshipBackground.png";
    public const string MascotPath = "Assets/Art/Lobby/Final/WakjeoMascot.png";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/MyDefense/Lobby/Apply Final Collection Design")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play Mode를 종료하세요.");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!=ScenePath || scene.isDirty) throw new InvalidOperationException("SampleScene을 열고 변경 사항을 저장한 뒤 적용하세요.");
        var lobby=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LobbyManager>(true)).FirstOrDefault();
        if(lobby==null || lobby.unitGridContent==null || lobby.text_UserName==null) throw new InvalidOperationException("로비 필수 참조 누락.");
        var canvas=lobby.text_UserName.GetComponentInParent<Canvas>();
        // Deliberately do not call LobbyNeonUiBuilder.StyleHeader: profile and all ancestors are immutable.
        StyleCurrencies(lobby);
        StyleBackground(canvas);
        StyleCollection(lobby);
        StyleNavigation(lobby,canvas);
        var material=lobby.viewObjects[1].transform.Find("LobbyMaterialCurrencies");
        if(material!=null) LobbyFinalPresentation.StyleMaterials(material);
        EditPrefab("Assets/Prefabs/Lobby/UnitCard.prefab",StyleCard);
        EditPrefab("Assets/Resources/Prefabs/Lobby/MythicBreeding/MythicBreedingShortcut.prefab",root=>
        {
            var view=root.GetComponent<MythicBreedingShortcutView>();
            if(view==null) throw new InvalidOperationException("교배 shortcut 참조 누락.");
            LobbyFinalPresentation.StyleBreeding(view);
        });
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("[LobbyFinal] 최종 내 유닛 디자인 적용. 프로필/공통 부모/게임 데이터 변경 없음.");
    }

    public static void StyleCurrencies(LobbyManager lobby)
    {
        TMP_Text[] values={lobby.text_Heart,lobby.text_Gold,lobby.text_Diamond};
        string[] icons={"Heart","Coin","Gem"};
        float[] left={.385f,.5875f,.79f};
        for(int i=0;i<values.Length;i++)
        {
            TMP_Text value=values[i];
            if(value==null) throw new InvalidOperationException("재화 수량 참조 누락.");
            var card=(RectTransform)value.transform.parent;
            if(card.parent.name!="tab_bar") throw new InvalidOperationException("재화 부모가 tab_bar가 아닙니다. 자동 재부모화하지 않습니다.");
            LobbyFinalPresentation.Place(card,left[i],.22f,left[i]+.19f,.78f);
            LobbyFinalPresentation.Frame(card.gameObject);
            LobbyFinalPresentation.Place(value.rectTransform,.25f,.14f,.70f,.86f);
            value.alignment=TextAlignmentOptions.MidlineRight; value.color=Color.white; value.raycastTarget=false;
            value.enableAutoSizing=true; value.fontSizeMin=12f; value.fontSizeMax=25f;
            value.textWrappingMode=TextWrappingModes.NoWrap; value.overflowMode=TextOverflowModes.Ellipsis;
            var icon=card.Find(icons[i]) as RectTransform;
            if(icon!=null)
            {
                LobbyFinalPresentation.Place(icon,.035f,.16f,.225f,.84f);
                var image=icon.GetComponent<Image>();
                if(image!=null) {image.preserveAspect=true;image.raycastTarget=false;}
            }
            var plus=card.Find("Plus Button") as RectTransform;
            if(plus==null) throw new InvalidOperationException("재화 + 버튼 누락.");
            LobbyFinalPresentation.Place(plus,.77f,.5f,.94f,.5f);
            var fit=LobbyFinalPresentation.Get<AspectRatioFitter>(plus.gameObject);
            fit.aspectMode=AspectRatioFitter.AspectMode.WidthControlsHeight;fit.aspectRatio=1f;
            LobbyFinalPresentation.Frame(plus.gameObject).Configure(LobbyNeonGraphic.FrameStyle.Button);
        }
    }

    private static void StyleBackground(Canvas canvas)
    {
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
        if(texture==null) throw new InvalidOperationException("배경 에셋 없음: "+BackgroundPath);
        var root=LobbyFinalPresentation.Child(canvas.transform,"LobbySpaceshipBackground");
        LobbyFinalPresentation.Place(root,0,0,1,1);root.SetAsFirstSibling();
        var image=LobbyFinalPresentation.Get<RawImage>(root.gameObject);image.texture=texture;image.color=Color.white;image.raycastTarget=false;
        var fit=LobbyFinalPresentation.Get<AspectRatioFitter>(root.gameObject);
        fit.aspectRatio=(float)texture.width/texture.height;fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
    }

    private static void StyleCollection(LobbyManager lobby)
    {
        var unit=lobby.viewObjects[1].transform;
        var title=LobbyFinalPresentation.TextChild(unit,"CollectionTitle","내 유닛",43);
        title.alignment=TextAnchor.MiddleLeft;title.color=new Color(.32f,.82f,.89f);
        var tr=title.rectTransform;tr.anchorMin=new Vector2(.06f,1);tr.anchorMax=new Vector2(.94f,1);tr.pivot=new Vector2(.5f,1);
        tr.offsetMin=new Vector2(0,-390);tr.offsetMax=new Vector2(0,-325);
        var scroll=lobby.unitGridContent.GetComponentInParent<ScrollRect>(true);
        if(scroll==null) throw new InvalidOperationException("컬렉션 ScrollRect 누락.");
        var r=(RectTransform)scroll.transform;
        LobbyFinalPresentation.Place(r,.025f,0,.975f,1);r.offsetMin=new Vector2(0,225);r.offsetMax=new Vector2(0,-400);
        var frame=scroll.transform.Find("LobbyNeonFrame");if(frame!=null) frame.gameObject.SetActive(false);
        var rootImage=scroll.GetComponent<Image>();if(rootImage!=null)rootImage.color=Color.clear;
        if(scroll.viewport==null || scroll.viewport==scroll.transform) throw new InvalidOperationException("분리된 컬렉션 viewport가 필요합니다.");
        LobbyFinalPresentation.Place(scroll.viewport,0,0,1,1);
        scroll.viewport.offsetMin=new Vector2(0,30);scroll.viewport.offsetMax=new Vector2(0,-8);
        LobbyFinalPresentation.Get<RectMask2D>(scroll.viewport.gameObject);
        var viewportImage=scroll.viewport.GetComponent<Image>();if(viewportImage!=null)viewportImage.color=Color.clear;
        scroll.horizontal=false;scroll.vertical=true;scroll.verticalNormalizedPosition=1;
        var grid=lobby.unitGridContent.GetComponent<LobbySectionGridLayout>();
        if(grid==null) throw new InvalidOperationException("미해금 구역 Grid 참조 누락.");
        grid.spacing=new Vector2(18,22);grid.padding=new RectOffset(36,36,8,50);
        lobby.unitGridContent.GetComponent<LobbyCollectionGrid>().RefreshLayout();
    }

    public static void StyleCard(GameObject root)
    {
        var card=root.GetComponent<UnitCardUI>();if(card==null) throw new InvalidOperationException("UnitCardUI 누락.");
        var frame=LobbyNeonUiBuilder.ApplyFrame(root,LobbyNeonGraphic.FrameStyle.Card);frame.UseFinalStyle();card.neonFrame=frame;
        ConfigureText(card.text_Grade,.07f,.88f,.64f,.97f,19,TextAnchor.MiddleLeft);
        ConfigureText(card.text_Level,.64f,.88f,.93f,.97f,18,TextAnchor.MiddleRight);
        ConfigureText(card.text_Name,.07f,.23f,.93f,.34f,23,TextAnchor.MiddleCenter);
        ConfigureText(card.text_Pieces,.08f,.045f,.92f,.16f,22,TextAnchor.MiddleCenter);
        if(card.ownershipStatusText!=null)card.ownershipStatusText.gameObject.SetActive(false);
        var well=root.transform.Find("PortraitWell") as RectTransform;
        if(well!=null)
        {
            LobbyFinalPresentation.Place(well,.13f,.37f,.87f,.86f);
            var portraitFrame=well.GetComponentInChildren<LobbyNeonGraphic>(true);if(portraitFrame!=null)portraitFrame.UseFinalStyle();
        }
        // Existing Sprite reference and fallback are deliberately retained.
        if(card.portraitImage!=null) {card.portraitImage.preserveAspect=true;card.portraitImage.raycastTarget=false;}
        if(card.image_Lock!=null)card.image_Lock.gameObject.SetActive(false);
    }

    private static void ConfigureText(Text t,float x0,float y0,float x1,float y1,int size,TextAnchor alignment)
    {
        if(t==null)return;LobbyFinalPresentation.Place(t.rectTransform,x0,y0,x1,y1);
        t.color=Color.white;t.raycastTarget=false;t.alignment=alignment;t.fontSize=size;
        t.resizeTextForBestFit=true;t.resizeTextMinSize=14;t.resizeTextMaxSize=size;
    }

    private static void StyleNavigation(LobbyManager lobby,Canvas canvas)
    {
        // Once supplied navigation assets have been installed, a full collection re-style
        // must not recreate procedural Graphics on the same Image GameObjects.
        var installed=lobby.GetComponent<LobbyFinalPresentation>();
        if(installed!=null && installed.navigationImages!=null && installed.navigationImages.Length>0)
        {
            int selected=Array.FindIndex(lobby.viewObjects,v=>v!=null&&v.activeSelf);
            LobbyNavigationAssetBuilder.Configure(installed,installed.activeNavigationSprites,
                installed.inactiveNavigationSprites,installed.iconShader,selected>=0?selected:2);
            return;
        }
        var nav=canvas.transform.Find("bottomNav") as RectTransform;
        if(nav==null)throw new InvalidOperationException("bottomNav 누락.");
        var group=nav.GetComponent<LayoutGroup>();if(group!=null)group.enabled=false;
        LobbyFinalPresentation.Place(nav,0,0,1,0);nav.offsetMin=Vector2.zero;nav.offsetMax=new Vector2(0,180);
        var bg=LobbyFinalPresentation.Get<Image>(nav.gameObject);bg.color=new Color(.007f,.026f,.043f,.97f);bg.raycastTarget=false;
        var presenter=LobbyFinalPresentation.Get<LobbyFinalPresentation>(lobby.gameObject);
        presenter.navigation=nav;presenter.collectionScroll=(RectTransform)lobby.unitGridContent.GetComponentInParent<ScrollRect>(true).transform;
        presenter.symbols=new LobbyNavIconGraphic[4];presenter.indicators=new GameObject[5];
        presenter.iconShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Scripts/Ui/LobbyIconSaturation.shader");
        string[] names={"Button_Shop","Button_Unit","Button_Main","Button_Clan","Button_Etc"};int symbolIndex=0;
        for(int i=0;i<names.Length;i++)
        {
            var button=nav.Find(names[i]) as RectTransform;if(button==null)throw new InvalidOperationException("메뉴 버튼 누락: "+names[i]);
            LobbyFinalPresentation.Place(button,.01f+i*.198f,.08f,.01f+(i+1)*.198f,.97f);
            var old=button.GetComponent<Image>();if(old!=null){old.color=Color.clear;old.raycastTarget=true;}
            // Persistent OpenTab listeners remain untouched; do not show old sample icon/label children.
            foreach(var text in button.GetComponentsInChildren<Text>(true))text.gameObject.SetActive(false);
            foreach(var text in button.GetComponentsInChildren<TMP_Text>(true))text.gameObject.SetActive(false);
            var icon=LobbyFinalPresentation.Child(button,"FinalIcon");LobbyFinalPresentation.Place(icon,.19f,.25f,.81f,.91f);
            if(i==1)
            {
                var image=LobbyFinalPresentation.Get<Image>(icon.gameObject);
                image.sprite=AssetDatabase.LoadAllAssetsAtPath(MascotPath).OfType<Sprite>().FirstOrDefault()
                    ??AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Lobby/Final/WakjeoMascot.jpg").OfType<Sprite>().FirstOrDefault();
                if(image.sprite==null)throw new InvalidOperationException("원본 마스코트 Sprite를 준비하세요.");
                image.preserveAspect=true;image.raycastTarget=false;presenter.mascot=image;
            }
            else {var symbol=LobbyFinalPresentation.Get<LobbyNavIconGraphic>(icon.gameObject);symbol.Configure(i);presenter.symbols[symbolIndex++]=symbol;}
            var underline=LobbyFinalPresentation.Child(button,"SelectedUnderline");LobbyFinalPresentation.Place(underline,.12f,.10f,.88f,.125f);
            var line=LobbyFinalPresentation.Get<Image>(underline.gameObject);line.color=new Color(.18f,.82f,.93f);line.raycastTarget=false;
            presenter.indicators[i]=underline.gameObject;
        }
        int active=Array.FindIndex(lobby.viewObjects,v=>v!=null&&v.activeSelf);presenter.SelectTab(active>=0?active:2);
    }

    private static void EditPrefab(string path,Action<GameObject> action)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try{action(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
#endif
