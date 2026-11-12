using System;
using System.Linq;
using System.Reflection;
using MyDefense.Clans;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyMidnightSocialUiTests
{
    private const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    private static GameObject CanvasRoot(int height=2160)
    {
        var root=new GameObject("MidnightSocialTest",typeof(RectTransform),typeof(Canvas));
        root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)root.transform).sizeDelta=new Vector2(1080,height);return root;
    }
    private static QuestUiView Quest(Transform root)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Lobby/Quest/QuestScreen.prefab");Assert.NotNull(prefab);
        var view=UnityEngine.Object.Instantiate(prefab,root).GetComponent<QuestUiView>();view.ApplyMidnightPresentation();return view;
    }
    [TestCase(1440)][TestCase(1920)][TestCase(2160)][TestCase(2340)]
    public void ContentCardsRemainScrollableUnavailableAndIdempotent(int height)
    {
        var root=CanvasRoot(height);
        try
        {
            var viewport=MidnightLobbyPresentation.BuildContentCards(root.transform);
            Assert.AreSame(viewport,MidnightLobbyPresentation.BuildContentCards(root.transform));
            var scroll=viewport.GetComponent<ScrollRect>();Assert.IsTrue(scroll.vertical);Assert.IsFalse(scroll.horizontal);
            Assert.AreEqual(2,scroll.content.childCount);
            foreach(var button in viewport.GetComponentsInChildren<Button>())
            {Assert.IsFalse(button.interactable);Assert.AreEqual(0,button.onClick.GetPersistentEventCount());Assert.AreEqual("연결 준비 중",button.GetComponentInChildren<Text>().text);}
            Assert.AreEqual(MidnightLobbyTheme.ContentBottom,viewport.offsetMin.y);
            Assert.IsFalse(viewport.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("3/3")));
            Canvas.ForceUpdateCanvases();Assert.Greater(viewport.rect.height,700);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(1440)][TestCase(1920)][TestCase(2340)]
    public void QuestHeaderMilestonesAndScrollDoNotOverlapAtMobileRatios(int height)
    {
        var root=CanvasRoot(height);
        try
        {
            var view=Quest(root.transform);var title=view.titleText;var close=view.closeButton;var slider=view.activitySlider;
            view.ApplyMidnightPresentation();Assert.AreSame(title,view.titleText);Assert.AreSame(close,view.closeButton);Assert.AreSame(slider,view.activitySlider);
            Canvas.ForceUpdateCanvases();Assert.Greater(view.questScrollRect.rect.height,200);
            Assert.That(view.questScrollRect.offsetMax.y,Is.LessThan(((RectTransform)view.milestoneRoot).offsetMin.y-10));
            Assert.AreEqual(-620,view.questScrollRect.offsetMax.y);
            view.SetMidnightCycleLayout(false);Assert.AreEqual(-310,view.questScrollRect.offsetMax.y);
            view.SetMidnightCycleLayout(true);Assert.AreEqual(-620,view.questScrollRect.offsetMax.y);
            Assert.IsTrue(close.interactable);Assert.GreaterOrEqual(((RectTransform)close.transform).rect.height,86);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(false,false)][TestCase(true,false)][TestCase(true,true)]
    public void QuestRowSeparatesServerTextAndStateWithoutChangingListener(bool completed,bool claimed)
    {
        var root=CanvasRoot();
        try
        {
            var view=Quest(root.transform);var button=UnityEngine.Object.Instantiate(view.questTemplate,view.questRoot);button.gameObject.SetActive(true);
            var original=button.GetComponentInChildren<Text>();original.text="legacy server summary";int calls=0;button.onClick.AddListener(()=>calls++);button.interactable=completed&&!claimed;
            view.StyleMidnightQuest(button,"서버 제목","서버 설명","7 / 10","Gold 123",claimed);
            int children=button.transform.childCount;
            view.StyleMidnightQuest(button,"서버 제목","서버 설명","7 / 10","Gold 123",claimed);
            Assert.AreEqual(children,button.transform.childCount);Assert.AreSame(original,button.GetComponentInChildren<Text>());
            Assert.AreEqual("legacy server summary",original.text);Assert.IsFalse(original.enabled);
            Assert.AreEqual("서버 제목",button.transform.Find("MidnightRowTitle").GetComponent<Text>().text);
            Assert.AreEqual("7 / 10",button.transform.Find("MidnightRowProgress").GetComponent<Text>().text);
            Assert.AreEqual("Gold 123",button.transform.Find("MidnightRowReward").GetComponent<Text>().text);
            Assert.AreEqual(claimed?"수령 완료":completed?"받기":"진행 중",button.transform.Find("MidnightRowState").GetComponent<Text>().text);
            Assert.AreEqual(completed&&!claimed,button.interactable);button.onClick.Invoke();Assert.AreEqual(1,calls);
            Assert.IsFalse(button.transform.Find("MidnightRowSurface").GetComponent<LobbyNeonGraphic>().HasMidnightButtonGloss);
            Assert.IsTrue(((LobbyNeonGraphic)button.targetGraphic).HasMidnightButtonGloss);
            foreach(var text in button.GetComponentsInChildren<Text>())Assert.IsFalse(text.raycastTarget);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void QuestModalHasOpaqueFullBoundsBackdropAndLeavesHudNavigationOutside()
    {
        var root=CanvasRoot();
        try
        {
            var view=Quest(root.transform);view.ApplyMidnightPresentation();
            var backdrop=view.GetComponent<Image>();Assert.IsTrue(backdrop.enabled);Assert.AreEqual(1f,backdrop.color.a);
            Assert.AreEqual(Image.Type.Simple,backdrop.type);Assert.IsNull(backdrop.sprite);Assert.IsTrue(backdrop.raycastTarget);
            Assert.AreSame(view.transform,backdrop.transform);Assert.AreEqual(1f,view.inputCanvasGroup.alpha);
            var rect=(RectTransform)view.transform;Assert.AreEqual(-160,rect.offsetMax.y);Assert.GreaterOrEqual(rect.offsetMin.y,MidnightLobbyTheme.ContentBottom);
            Assert.Less(backdrop.color.r,.1f);Assert.Less(backdrop.color.g,.15f);Assert.Less(backdrop.color.b,.2f);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(0)][TestCase(1)][TestCase(2)]
    public void QuestRepeatedRenderPreservesTemplatesAndSelectedTabState(int tab)
    {
        var root=CanvasRoot();
        try
        {
            var view=Quest(root.transform);var controller=root.AddComponent<QuestController>();
            typeof(QuestController).GetField("view",Private).SetValue(controller,view);
            var cycle=new QuestCycleDto{cycleType="DAILY",activityPoints=25,nextResetAt="2026-11-13T00:00:00",quests=new[]{new QuestItemDto{questId="ID",title="참가",description="전투 참가",progress=1,targetAmount=2,reward=new QuestRewardDto{gold=50}}},milestones=new[]{new QuestMilestoneDto{requiredActivityPoints=25,unlocked=true,reward=new QuestRewardDto{gold=100}}}};
            typeof(QuestController).GetField("board",Private).SetValue(controller,new QuestBoardResponse{serverTime="2026-11-12T00:00:00",daily=cycle,weekly=cycle,achievements=new[]{new QuestAchievementDto{achievementId="A",title="업적",description="누적 참가",tier=2,progress=3,targetAmount=4,reward=new QuestRewardDto{diamond=10}}}});
            var selected=typeof(QuestController).GetField("selectedTab",Private);selected.SetValue(controller,Enum.ToObject(selected.FieldType,tab));
            var templateText=view.questTemplate.GetComponentInChildren<Text>(true);string original=templateText.text;
            var render=typeof(QuestController).GetMethod("Render",Private);for(int i=0;i<3;i++)render.Invoke(controller,null);
            Assert.AreEqual(2,view.questRoot.childCount);Assert.AreEqual(original,templateText.text);Assert.IsTrue(templateText.enabled);
            var tabs=new[]{view.dailyTabButton,view.weeklyTabButton,view.achievementTabButton};
            for(int i=0;i<3;i++){Assert.AreEqual(i!=tab,tabs[i].interactable);Assert.AreEqual(i==tab,tabs[i].transform.Find("MidnightSelectedLine").gameObject.activeSelf);}
            Assert.AreEqual(tab!=2,view.milestoneRoot.gameObject.activeSelf);
            typeof(QuestController).GetMethod("SetBusy",Private).Invoke(controller,new object[]{true});Assert.IsTrue(view.closeButton.interactable);
            view.closeButton.onClick.AddListener(controller.Close);view.closeButton.onClick.Invoke();Assert.IsFalse(view.gameObject.activeSelf);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(false)][TestCase(true)]
    public void ClanRuntimeLayoutStylesPrefabAndPreservesBoundCallbacks(bool joined)
    {
        var root=CanvasRoot();
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Lobby/Clan/ClanScreen.prefab");Assert.NotNull(prefab);
            var view=UnityEngine.Object.Instantiate(prefab,root.transform).GetComponent<ClanView>();int calls=0;
            view.SearchRequested=_=>calls++;view.RefreshRequested=()=>calls++;view.BindCoreActions();
            var search=view.searchButton;var refresh=view.refreshButton;var input=view.searchInput;input.text="테스트";
            view.ApplyMidnightPresentation(joined);view.ApplyMidnightPresentation(joined);
            Assert.AreSame(search,view.searchButton);Assert.AreSame(refresh,view.refreshButton);Assert.AreSame(input,view.searchInput);Assert.AreEqual("테스트",input.text);
            search.onClick.Invoke();refresh.onClick.Invoke();Assert.AreEqual(2,calls);
            Assert.AreEqual(120,view.titleText.resizeTextMaxSize);Assert.AreEqual(joined?-275:-460,view.scroll.viewport.offsetMax.y);
            Assert.IsTrue(search.GetComponent<ClanSurfaceGraphic>().HasMidnightButtonFinish);
            Assert.IsFalse(view.safeRoot.Find("Backdrop").GetComponent<ClanSurfaceGraphic>().HasMidnightButtonFinish);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(0f)][TestCase(.5f)][TestCase(1f)]
    public void ClanButtonFinishUsesSharedSoftMeshWithOpacity(float alpha)
    {
        var root=new GameObject("ClanSurface",typeof(RectTransform),typeof(ClanSurfaceGraphic));
        try
        {
            var surface=root.GetComponent<ClanSurfaceGraphic>();surface.color=new Color(.08f,.19f,.27f,alpha);surface.UseMidnightButton(true);
            using(var actual=new VertexHelper())using(var expected=new VertexHelper())
            {
                var bounds=new Rect(0,0,450,100);
                typeof(ClanSurfaceGraphic).GetMethod("DrawMidnightButton",Private).Invoke(surface,new object[]{actual,bounds});
                MidnightSurfaceMesh.DrawReferenceButton(expected,bounds,new Color(1,1,1,alpha),10,1,true,true);
                var a=new Mesh();var b=new Mesh();try{actual.FillMesh(a);expected.FillMesh(b);CollectionAssert.AreEqual(b.vertices,a.vertices);CollectionAssert.AreEqual(b.colors32,a.colors32);CollectionAssert.AreEqual(b.triangles,a.triangles);}
                finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
