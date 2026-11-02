using System;
using System.Collections.Generic;
using System.Linq;
using MyDefense.Auth;
using MyDefense.Clans;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class ClanClientTests
{
    private GameObject root;
    private ClanView view;
    private ClanController controller;
    private FakeTransport transport;
    private MemoryClanPendingStore store;
    [SetUp] public void Setup()
    {
        root=new GameObject("ClanTest",typeof(RectTransform),typeof(Canvas));
        view=ClanView.Build(root.transform);controller=root.AddComponent<ClanController>();
        transport=new FakeTransport();store=new MemoryClanPendingStore();controller.Attach(view,transport,store,null);controller.SetLobbyTab(3);
    }
    [TearDown] public void TearDown(){UnityEngine.Object.DestroyImmediate(root);}
    [Test] public void CreatePayloadOmitsUnrelatedFieldsAndRetainsQuote()
    {
        string json=ClanClientContract.Create("우주 클랜","소개","APPROVAL",75000);
        StringAssert.DoesNotContain("clanId",json);StringAssert.DoesNotContain("targetUserId",json);
        var c=ClanClientContract.ReadCommand(json);Assert.AreEqual(75000,c.expectedCreationGold);Assert.AreEqual("우주 클랜",c.name);
    }
    [TestCase("JOIN")][TestCase("APPLY")][TestCase("CANCEL_APPLICATION")][TestCase("LEAVE")]
    public void BasicCommandsOmitRevisionAndQuote(string operation)
    {
        string json=ClanClientContract.Command(operation,7,9,8);
        StringAssert.DoesNotContain("expectedRevision",json);StringAssert.DoesNotContain("expectedCreationGold",json);
        Assert.AreEqual(operation,ClanClientContract.ReadCommand(json).operation);
    }
    [TestCase("APPROVE")][TestCase("REJECT")][TestCase("KICK")][TestCase("PROMOTE")][TestCase("DEMOTE")][TestCase("TRANSFER")]
    public void TargetCommandsRetainRevisionAndIdentity(string operation)
    {
        var value=ClanClientContract.ReadCommand(ClanClientContract.Command(operation,7,9,8));
        Assert.AreEqual(9,value.expectedRevision);Assert.AreEqual(8,value.targetUserId);Assert.AreEqual(7,value.clanId);
    }
    [Test] public void UpdateEscapesUserTextWithoutHtmlInterpretation()
    {
        var command=ClanClientContract.ReadCommand(ClanClientContract.Update(1,2,"quote \" test","공지","OPEN"));
        Assert.AreEqual("quote \" test",command.description);
        Assert.IsTrue(view.GetComponentsInChildren<Text>(true).All(x=>!x.supportRichText));
    }
    [Test] public void JavaNullablePendingIdParsesAsZero()
    {
        var status=ClanJson.Read<ClanStatus>("{\"userId\":100,\"accountGold\":0,\"accountLevel\":10,\"pendingClanId\":null,\"serverTime\":\"2026-11-02T00:00:00Z\",\"clan\":null,\"role\":null}");
        Assert.AreEqual(0,status.pendingClanId);Assert.IsTrue(ClanClientContract.ValidStatus(status,100));
    }
    [TestCase("{}")][TestCase("{\"clan\":null}")]
    public void EmptyMembershipObjectIsNotTreatedAsUnjoined(string membership)
    {
        string json="{\"userId\":100,\"accountGold\":0,\"accountLevel\":10,\"serverTime\":\"2026-11-02T00:00:00Z\",\"clan\":"+membership+"}";
        Assert.IsFalse(ClanClientContract.ValidStatus(ClanJson.Read<ClanStatus>(json),100));
    }
    [Test] public void EscapedNullTextDoesNotEraseRealMembership()
    {
        var status=ClanUiFixture.Example(4);status.clan.notice="문자열 \"clan\":null 유지";
        var parsed=ClanJson.Read<ClanStatus>(ClanJson.SerializeFixture(status));
        Assert.IsTrue(ClanClientContract.ValidStatus(parsed,100));Assert.AreEqual(status.clan.notice,parsed.clan.notice);
    }
    [Test] public void DuplicateJsonKeysAndTrailingObjectsAreRejected()
    {
        Assert.Throws<Newtonsoft.Json.JsonReaderException>(()=>ClanJson.Read<ClanStatus>("{\"clan\":null,\"clan\":{}}"));
        Assert.Throws<Newtonsoft.Json.JsonReaderException>(()=>ClanJson.Read<ClanStatus>("{\"clan\":null}{}"));
    }
    [Test] public void PolicyIsServerDrivenAndCooldownUsesServerClock()
    {
        var p=ClanUiFixture.Policy();p.minimumLevel=20;p.creationGold=12345;
        Assert.IsFalse(ClanClientContract.Eligible(transport.state,p));transport.state.accountLevel=20;
        Assert.IsTrue(ClanClientContract.Eligible(transport.state,p));
        transport.state.rejoinAvailableAt="2026-11-03T00:00:00Z";transport.state.serverTime="2026-11-02T00:00:00Z";
        Assert.AreEqual(86390,ClanClientContract.CooldownSeconds(transport.state,10));
    }
    [Test] public void DeputyCannotManageDeputyOrLeaderOrSelf()
    {
        Assert.IsEmpty(ClanClientContract.MemberActions("DEPUTY",100,new ClanMember{userId=101,role="DEPUTY"}));
        Assert.IsEmpty(ClanClientContract.MemberActions("LEADER",100,new ClanMember{userId=100,role="LEADER"}));
        CollectionAssert.AreEqual(new[]{"KICK"},ClanClientContract.MemberActions("DEPUTY",100,new ClanMember{userId=101,role="MEMBER"}));
    }
    [Test] public void PendingScopeSeparatesAccountAndEndpoint()
    {
        Assert.AreNotEqual(ClanPendingRequestStore.Scope("http://localhost/api",1),ClanPendingRequestStore.Scope("http://localhost/api",2));
        Assert.AreNotEqual(ClanPendingRequestStore.Scope("http://localhost/api",1),ClanPendingRequestStore.Scope("http://other/api",1));
    }
    [Test] public void CorruptPendingFailsClosedWithoutSending()
    {
        controller.Attach(view,transport,new BrokenStore(),null);controller.SetLobbyTab(3);controller.Action("JOIN",1,0);
        Assert.IsFalse(view.HasModal);Assert.IsEmpty(transport.posts);
    }
    [Test] public void RetryResendsExactBodyAndGetDoesNotClearUnknownOutcome()
    {
        QueueJoin();string original=transport.posts[0];transport.Complete(new AuthHttpResult{Status=0,Error="lost"});
        Assert.IsTrue(controller.HasPending);controller.Refresh();Assert.IsTrue(controller.HasPending);
        controller.Retry();Assert.AreEqual(original,transport.posts[1]);Assert.AreEqual(original,store.Load());
    }
    [Test] public void SuccessfulRetryClearsPendingAndAppliesLatestStatus()
    {
        QueueJoin();transport.state=ClanUiFixture.Example(2);transport.Complete(FakeTransport.Ok(transport.state));
        Assert.IsFalse(controller.HasPending);Assert.IsNull(store.Load());Assert.AreEqual("MEMBER",controller.Status.role);
    }
    [Test] public void DefinitiveBusinessRejectionClearsButAuthFailurePreservesPending()
    {
        QueueJoin();transport.Complete(new AuthHttpResult{Status=409,Body="{\"code\":\"CLAN_FULL\"}"});Assert.IsFalse(controller.HasPending);
        QueueJoin();transport.Complete(new AuthHttpResult{Status=401,Body="{\"code\":\"UNAUTHORIZED\"}"});Assert.IsTrue(controller.HasPending);
    }
    [Test] public void InvalidSuccessfulResponseRetainsPending()
    {
        QueueJoin();var wrong=ClanUiFixture.Example(0);wrong.userId=999;transport.Complete(FakeTransport.Ok(wrong));
        Assert.IsTrue(controller.HasPending);Assert.AreEqual(100,controller.Status.userId);
    }
    [Test] public void RebindingAccountIgnoresOldPostCallback()
    {
        QueueJoin();var old=transport;var next=new FakeTransport();controller.Attach(view,next,new MemoryClanPendingStore(),null);controller.SetLobbyTab(3);
        old.Complete(FakeTransport.Ok(ClanUiFixture.Example(2)));Assert.IsNull(controller.Status.clan);Assert.IsNotNull(store.Load());
    }
    [Test] public void TabCloseDuringBusyDoesNotOpenModalOnLateResponse()
    {
        QueueJoin();controller.SetLobbyTab(0);transport.Complete(FakeTransport.Ok(ClanUiFixture.Example(2)));
        Assert.IsFalse(view.gameObject.activeSelf);Assert.IsFalse(view.HasModal);Assert.IsFalse(controller.HasPending);
    }
    [Test] public void RefreshFailureDisablesMutationUntilFreshRead()
    {
        Assert.IsNotNull(controller.Status);Assert.IsTrue(ClanClientContract.ValidStatus(controller.Status,100));
        transport.failReads=true;controller.Refresh();controller.Action("JOIN",1,0);
        Assert.IsFalse(view.HasModal);Assert.IsFalse(view.createButton.interactable);Assert.IsEmpty(transport.posts);
    }
    [TestCase("null")][TestCase("zero")][TestCase("duplicate")][TestCase("blank")][TestCase("overflow")]
    public void InvalidApplicationsAreRejectedWithoutRenderingOrClearingPending(string kind)
    {
        QueueJoin();var bad=ClanUiFixture.Example(4);
        if(kind=="null")bad.clan.applications=new ClanApplicant[]{null};
        if(kind=="zero")bad.clan.applications=new[]{new ClanApplicant{userId=0,displayName="이름"}};
        if(kind=="duplicate")bad.clan.applications=new[]{new ClanApplicant{userId=200,displayName="이름"},new ClanApplicant{userId=200,displayName="다른 이름"}};
        if(kind=="blank")bad.clan.applications=new[]{new ClanApplicant{userId=200,displayName="  "}};
        if(kind=="overflow")bad.clan.applications=Enumerable.Range(200,101).Select(i=>new ClanApplicant{userId=i,displayName="이름"}).ToArray();
        Assert.IsFalse(ClanClientContract.ValidDetail(bad.clan));transport.Complete(FakeTransport.Ok(bad));
        Assert.IsTrue(controller.HasPending);Assert.IsNull(controller.Status.clan);
    }
    [Test] public void ModalCloseWorksWithoutCommand()
    {
        controller.Action("JOIN",1,0);Assert.IsTrue(view.HasModal);FindButton("Close").onClick.Invoke();
        Assert.IsFalse(view.HasModal);Assert.IsEmpty(transport.posts);
    }
    [Test] public void IntroductionAndNoticeAreSingleLineAndRejectPastedControls()
    {
        string savedDescription=null,savedNotice=null;
        view.ShowForm(ClanUiFixture.Policy(),ClanUiFixture.Example(4).clan,(name,description,notice,mode)=>{savedDescription=description;savedNotice=notice;});
        var description=view.GetComponentsInChildren<InputField>().Single(i=>i.name=="DescriptionInput");
        var notice=view.GetComponentsInChildren<InputField>().Single(i=>i.name=="NoticeInput");
        Assert.AreEqual(InputField.LineType.SingleLine,description.lineType);Assert.AreEqual(InputField.LineType.SingleLine,notice.lineType);
        // Exercise InputField's actual paste path. EditMode .text assignment with a shortening validator
        // has a Unity caret-clamping early-return bug; Play Mode clamps correctly.
        description.text="";notice.text="";
        var append=typeof(InputField).GetMethod("Append",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(string)},null);
        Assert.IsNotNull(append);append.Invoke(description,new object[]{"첫째\r\n둘째\n셋째"});append.Invoke(notice,new object[]{"함께\t즐겨요\r!\u202E<공지>"});
        Assert.AreEqual("첫째둘째셋째",description.text);Assert.AreEqual("함께즐겨요!공지",notice.text);
        FindButton("Save").onClick.Invoke();Assert.AreEqual("첫째둘째셋째",savedDescription);Assert.AreEqual("함께즐겨요!공지",savedNotice);
    }
    [TestCase('\r')][TestCase('\n')][TestCase('\t')][TestCase('\u0085')][TestCase('\u202E')][TestCase('<')][TestCase('>')]
    public void SingleLineCharacterValidatorBlocksServerRejectedCharacters(char character)
    {Assert.AreEqual('\0',ClanView.ValidateSingleLineText("공지",2,character));}
    [Test] public void SingleLineValidatorPreservesNormalKoreanAndSpaces()
    {foreach(char c in "함께 즐기는 왹져 방위대!")Assert.AreEqual(c,ClanView.ValidateSingleLineText("",0,c));}
    [Test] public void LongMemberListIsScrollableAndClipped()
    {
        transport.state=ClanUiFixture.Example(5);controller.Refresh();controller.Action("SECTION_MEMBERS",0,0);
        Assert.AreEqual(30,view.content.Cast<Transform>().Count(x=>x.name.StartsWith("Member_")));
        Assert.IsNotNull(view.scroll.viewport.GetComponent<RectMask2D>());Assert.IsTrue(view.scroll.vertical);Assert.IsFalse(view.scroll.horizontal);
        Assert.Greater(view.content.GetComponent<VerticalLayoutGroup>().padding.bottom,0);
    }
    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator MemberRowRaycastRoutesHeldDragToScrollRect()
    {
        var cameraObject=new GameObject("ClanRaycastCamera",typeof(Camera));
        var eventObject=new GameObject("ClanRaycastEvents",typeof(UnityEngine.EventSystems.EventSystem));
        var targetTexture=new RenderTexture(1080,1920,24);
        try
        {
            var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.targetTexture=targetTexture;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var raycaster=root.AddComponent<GraphicRaycaster>();
            transport.state=ClanUiFixture.Example(5);controller.Refresh();controller.Action("SECTION_MEMBERS",0,0);
            yield return null;Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(view.content);view.scroll.Rebuild(CanvasUpdate.PostLayout);camera.Render();
            Assert.Greater(view.content.rect.height,view.scroll.viewport.rect.height);
            var row=(RectTransform)view.content.Find("Member_100");
            var world=row.TransformPoint(new Vector3(row.rect.xMin+row.rect.width*.25f,row.rect.center.y,0));
            var pointer=new UnityEngine.EventSystems.PointerEventData(eventObject.GetComponent<UnityEngine.EventSystems.EventSystem>())
            {position=RectTransformUtility.WorldToScreenPoint(camera,world),button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
            var hits=new List<UnityEngine.EventSystems.RaycastResult>();raycaster.Raycast(pointer,hits);
            Assert.IsNotEmpty(hits);Assert.AreSame(row.gameObject,hits[0].gameObject);
            var handler=UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IDragHandler>(hits[0].gameObject);
            Assert.AreSame(view.scroll.gameObject,handler);
            pointer.pointerPressRaycast=hits[0];pointer.pressPosition=pointer.position;
            UnityEngine.EventSystems.ExecuteEvents.Execute(handler,pointer,UnityEngine.EventSystems.ExecuteEvents.initializePotentialDrag);
            UnityEngine.EventSystems.ExecuteEvents.Execute(handler,pointer,UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
            float before=view.content.anchoredPosition.y;pointer.position+=Vector2.up*180;pointer.delta=Vector2.up*180;
            UnityEngine.EventSystems.ExecuteEvents.Execute(handler,pointer,UnityEngine.EventSystems.ExecuteEvents.dragHandler);
            UnityEngine.EventSystems.ExecuteEvents.Execute(handler,pointer,UnityEngine.EventSystems.ExecuteEvents.endDragHandler);
            Assert.Greater(view.content.anchoredPosition.y,before+50);
        }
        finally{UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(eventObject);targetTexture.Release();UnityEngine.Object.DestroyImmediate(targetTexture);}
    }
    [Test] public void MemberHasNoSettingsOrApprovalControls()
    {
        transport.state=ClanUiFixture.Example(2);controller.Refresh();controller.Action("SECTION_MANAGE",0,0);
        Assert.IsFalse(view.GetComponentsInChildren<Button>().Any(b=>b.name=="Settings"||b.name=="Approve"||b.name=="Reject"));
    }
    [Test] public void MutationButtonsRemainDisabledWhilePending()
    {
        QueueJoin();transport.Complete(new AuthHttpResult{Status=0,Error="lost"});
        Assert.IsFalse(view.createButton.interactable);Assert.IsTrue(view.retryButton.interactable);
    }
    [Test] public void FixtureScenesAreNotProductionBuildScenes()
    {
        Assert.IsFalse(UnityEditor.EditorBuildSettings.scenes.Any(s=>s.enabled&&(s.path==ClanAssetBuilder.FixturePath||s.path==ClanAssetBuilder.HttpFixturePath)));
    }
    [Test] public void SavedPrefabReloadBindsToolbarAndButtonCallbacks()
    {
        var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ClanAssetBuilder.PrefabPath);
        Assert.IsNotNull(asset,"Run ClanAssetBuilder.Rebuild before the prefab regression test.");
        view=UnityEngine.Object.Instantiate(asset,root.transform).GetComponent<ClanView>();
        controller.Attach(view,transport,store,null);controller.SetLobbyTab(3);
        Assert.IsTrue(view.transform.Find("SafeContent/Toolbar").gameObject.activeSelf);
        view.createButton.onClick.Invoke();Assert.IsTrue(view.HasModal);view.CloseModal();
        transport.state=ClanUiFixture.Example(2);view.refreshButton.onClick.Invoke();
        Assert.IsFalse(view.transform.Find("SafeContent/Toolbar").gameObject.activeSelf);
        Assert.AreEqual("MEMBER",controller.Status.role);
    }
    private void QueueJoin(){controller.Action("JOIN",1,0);Assert.IsTrue(view.HasModal);FindButton("Confirm").onClick.Invoke();Assert.IsTrue(controller.IsBusy);}
    private Button FindButton(string name)=>view.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
    private sealed class BrokenStore:IClanPendingStore {public string Load()=>"bad-json";public void Save(string value)=>throw new Exception();public void Clear()=>throw new Exception();}
    private sealed class FakeTransport:IClanTransport
    {
        public ClanStatus state=ClanUiFixture.Example(0);public bool failReads;public long UserId=>100;public bool IsCurrent=>true;
        public readonly List<string> posts=new List<string>();private Action<AuthHttpResult> post;
        public void Send(string path,string method,string json,Action<AuthHttpResult> done)
        {
            if(method=="POST"){posts.Add(json);post=done;return;}
            if(failReads){done(new AuthHttpResult{Status=500,Error="failure"});return;}
            if(path.EndsWith("/policy"))done(Ok(ClanUiFixture.Policy()));
            else if(path.EndsWith("/me"))done(Ok(state));
            else done(Ok(new ClanSearch{clans=Array.Empty<ClanSummary>(),page=0,pageSize=20}));
        }
        public void Complete(AuthHttpResult response){var callback=post;post=null;callback(response);}
        public static AuthHttpResult Ok(object value)=>new AuthHttpResult{Status=200,Body=ClanJson.SerializeFixture(value)};
    }
}
