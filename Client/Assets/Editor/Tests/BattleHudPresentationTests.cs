using System.Collections;
using System.Reflection;
using MyDefense.Battle.Presentation;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MyDefenseGame.Editor.Tests
{
    public sealed class BattleHudPresentationTests
    {
        private Scene _scene;
        private GameObject _host;
        private BattleHudPresentation _hud;
        private EventSystem _previousEvents;
        private EventSystem _testEvents;
        private const BindingFlags Hidden = BindingFlags.Instance|BindingFlags.NonPublic;

        [SetUp] public void SetUp()
        {
            _scene=EditorSceneManager.NewPreviewScene();
            _previousEvents=EventSystem.current;
            GameObject events=Create("TestEvents",typeof(EventSystem));
            _testEvents=events.GetComponent<EventSystem>();
            // PreviewScene behaviours do not receive the runtime enable callbacks.
            // Register through the real EventSystem lifecycle before selecting it.
            if(!Registered(_testEvents))Invoke(_testEvents,"OnEnable");
            EventSystem.current=_testEvents;
            Assert.That(EventSystem.current,Is.SameAs(_testEvents));
            _host=Create("ActualBattleHudHost");
        }
        [TearDown] public void TearDown()
        {
            if(_hud!=null) Invoke(_hud,"OnDestroy");
            if(_testEvents!=null && Registered(_testEvents))Invoke(_testEvents,"OnDisable");
            if(_scene.IsValid())EditorSceneManager.ClosePreviewScene(_scene);
            // The setter rejects null and EventSystems that are no longer registered.
            if(_previousEvents!=null && Registered(_previousEvents))EventSystem.current=_previousEvents;
        }
        private GameObject Create(string name,params System.Type[] types)
        { var go=new GameObject(name,types); SceneManager.MoveGameObjectToScene(go,_scene); return go; }
        private BattleHudPresentation CreateHud()
        { _hud=_host.AddComponent<BattleHudPresentation>(); _hud.Initialize(null); return _hud; }
        private static object Invoke(object target,string method,params object[] args)
            => target.GetType().GetMethod(method,Hidden).Invoke(target,args);
        private static void Set(object target,string field,object value) => target.GetType().GetField(field,Hidden).SetValue(target,value);
        private static bool GetFlag(object target,string field) => (bool)target.GetType().GetField(field,Hidden).GetValue(target);
        private static bool Registered(EventSystem events)
            => ((IList)typeof(EventSystem).GetField("m_EventSystems",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).Contains(events);
        private Transform CanvasRoot()
        { foreach(GameObject root in _scene.GetRootGameObjects())if(root.name=="BattleHudCanvas")return root.transform; return null; }

        [TestCase(800,960,0,28,800,900)]
        [TestCase(960,540,28,10,904,510)]
        [TestCase(1080,1920,0,60,1080,1800)]
        public void SupportedSafeAreasKeepWorldPanelsAndThreeChoicesInsideVisibleScreen(int w,int h,int x,int y,int sw,int sh)
        {
            var safe=new Rect(x,y,sw,sh); BattleHudLayout layout=BattleHudLayout.Calculate(w,h,safe);
            Assert.That(safe.Contains(layout.Header.min),Is.True); Assert.That(safe.Contains(layout.Header.max-Vector2.one*.01f),Is.True);
            Assert.That(safe.Contains(layout.Footer.min),Is.True); Assert.That(layout.Header.Overlaps(layout.Footer),Is.False);
            Assert.That(layout.World.Overlaps(layout.Header),Is.False); Assert.That(layout.World.Overlaps(layout.Footer),Is.False);
            Assert.That(layout.World.height,Is.GreaterThan(h*.4f));
            Assert.That(safe.Contains(layout.Modal.min),Is.True); Assert.That(safe.Contains(layout.Modal.max-Vector2.one*.01f),Is.True);
            var hud=CreateHud(); hud.ApplyLayoutForScreen(w,h,safe,null);
            hud.Render(new BattleHudPresentation.DisplayState { ChoiceActive=true,CanChoose=true });
            RectTransform modal=CanvasRoot().Find("MythicChoiceModal") as RectTransform;
            Transform header=CanvasRoot().Find("MissionHeader");
            RectTransform status=header.Find("BattleStatus") as RectTransform;
            RectTransform card=header.Find("LocalPlayer") as RectTransform;
            RectTransform wave=header.Find("Wave") as RectTransform;
            if(!layout.Landscape)
            {
                Assert.That(status.anchoredPosition.y,Is.GreaterThan(card.anchoredPosition.y+card.sizeDelta.y));
                Assert.That(wave.anchoredPosition.y,Is.GreaterThan(status.anchoredPosition.y+status.sizeDelta.y));
            }
            RectTransform previous=null;
            for(int i=0;i<3;i++)
            {
                var choice=modal.Find("Choice"+i) as RectTransform;
                Rect rect=new(choice.anchoredPosition,choice.sizeDelta);
                Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(0)); Assert.That(rect.xMax,Is.LessThanOrEqualTo(modal.sizeDelta.x));
                Assert.That(rect.yMin,Is.GreaterThanOrEqualTo(0)); Assert.That(rect.yMax,Is.LessThanOrEqualTo(modal.sizeDelta.y));
                if(previous!=null)Assert.That(rect.xMin,Is.GreaterThan(previous.anchoredPosition.x+previous.sizeDelta.x));
                previous=choice;
            }
        }

        [Test]
        public void RealPresentationRepeatedInitializeHideShowAndDisposeRestoresOnlyItsLegacyControlsAndCamera()
        {
            GameObject legacy=Create("UI_Coop_HUD",typeof(RectTransform));
            GameObject buttonObject=Create("Btn_Summon",typeof(RectTransform),typeof(Image),typeof(Button)); buttonObject.transform.SetParent(legacy.transform,false);
            Image image=buttonObject.GetComponent<Image>(); Button button=buttonObject.GetComponent<Button>();
            var disabled=Create("DisabledOriginal",typeof(RectTransform),typeof(Image)); disabled.transform.SetParent(legacy.transform,false); disabled.GetComponent<Image>().enabled=false;
            var unrelated=Create("Unrelated",typeof(RectTransform),typeof(Image));
            var camera=Create("Main Camera",typeof(Camera)).GetComponent<Camera>(); camera.tag="MainCamera"; camera.orthographic=true; camera.orthographicSize=8;
            camera.rect=new Rect(.03f,.02f,.94f,.96f); Rect originalRect=camera.rect; Vector3 position=new(2,13,-5); camera.transform.position=position;
            Quaternion rotation=Quaternion.Euler(60,0,0); camera.transform.rotation=rotation;
            var hud=CreateHud(); hud.Initialize(null); hud.Initialize(null);
            Assert.That(image.enabled,Is.False); Assert.That(button.enabled,Is.False); Assert.That(unrelated.GetComponent<Image>().enabled,Is.True);
            int canvases=0; foreach(GameObject root in _scene.GetRootGameObjects())if(root.name=="BattleHudCanvas")canvases++;
            Assert.That(canvases,Is.EqualTo(1));
            hud.ApplyLayoutForScreen(800,960,new Rect(0,28,800,900),camera);
            Assert.That(camera.rect,Is.EqualTo(hud.Layout.NormalizedWorld(800,960)));
            Assert.That(camera.transform.position,Is.EqualTo(position)); Assert.That(camera.transform.rotation,Is.EqualTo(rotation));
            hud.Hide(); hud.Hide();
            Assert.That(image.enabled,Is.True); Assert.That(button.enabled,Is.True); Assert.That(disabled.GetComponent<Image>().enabled,Is.False);
            Assert.That(camera.rect,Is.EqualTo(originalRect)); Assert.That(camera.orthographicSize,Is.EqualTo(8));
            hud.Show(); Assert.That(button.enabled,Is.False);
            Transform ownedCanvas=CanvasRoot(); Invoke(hud,"OnDestroy");
            Assert.That(ownedCanvas==null,Is.True); Assert.That(button.enabled,Is.True); Assert.That(BattleHudPresentation.HasActiveHud,Is.False);
        }

        [Test]
        public void ModalAndHudPointerEventsCannotStartDragAndUiReleaseCancelsExistingDrag()
        {
            var hud=CreateHud(); hud.ApplyLayoutForScreen(800,960,new Rect(0,0,800,960),null);
            GameObject parent=Create("OriginalSlot"); GameObject floating=Create("MovingGrid");
            GameObject unit=Create("RealDragUnit"); unit.transform.SetParent(parent.transform,false);
            var drag=unit.AddComponent<FusionKidnapUnitDrag>();
            var pointer=new PointerEventData(EventSystem.current) { position=hud.Layout.Footer.center };
            drag.OnPointerDown(pointer); Assert.That(GetFlag(drag,"_pointerPressed"),Is.False);
            hud.Render(new BattleHudPresentation.DisplayState { ChoiceActive=true });
            pointer.position=hud.Layout.World.center; drag.OnPointerDown(pointer); Assert.That(GetFlag(drag,"_pointerPressed"),Is.False);
            hud.Render(new BattleHudPresentation.DisplayState());
            Set(drag,"_dragging",true); Set(drag,"_pointerPressed",true); Set(drag,"_originalParent",parent.transform);
            Set(drag,"_originalLocalPosition",new Vector3(.1f,.2f,.3f)); Set(drag,"_originalLocalScale",Vector3.one);
            unit.transform.SetParent(floating.transform,false); unit.transform.localPosition=Vector3.one*10;
            pointer.position=hud.Layout.Footer.center; drag.OnPointerUp(pointer);
            Assert.That(drag.IsDragging,Is.False); Assert.That(unit.transform.parent,Is.EqualTo(parent.transform));
            Assert.That(unit.transform.localPosition,Is.EqualTo(new Vector3(.1f,.2f,.3f)));
            Assert.That(GetFlag(drag,"_pointerPressed"),Is.False);
        }

        [Test]
        public void EndAndSpectatorPresentationDisablesActionsButBetweenWavesRunningRemainsAvailable()
        {
            Assert.That(BattleHudPresentation.AllowsCommands(true,MatchState.RUNNING,PlayerBattleState.ACTIVE),Is.True);
            foreach(MatchState end in new[]{MatchState.CLEARED,MatchState.FAILED})
                Assert.That(BattleHudPresentation.AllowsCommands(true,end,PlayerBattleState.ACTIVE),Is.False);
            foreach(PlayerBattleState observer in new[]{PlayerBattleState.ELIMINATED,PlayerBattleState.SPECTATING})
                Assert.That(BattleHudPresentation.AllowsCommands(true,MatchState.RUNNING,observer),Is.False);
            Assert.That(BattleHudPresentation.AllowsCommands(false,MatchState.RUNNING,PlayerBattleState.ACTIVE),Is.False);
            var hud=CreateHud(); hud.Render(new BattleHudPresentation.DisplayState());
            Transform canvas=CanvasRoot();
            Assert.That(canvas.Find("BattleControls/Kidnap").GetComponent<Button>().interactable,Is.False);
            Assert.That(canvas.Find("UnitUpgradeDrawer/MutationAction").GetComponent<Button>().interactable,Is.False);
            Assert.That(canvas.Find("UnitUpgradeDrawer/NormalResonance").GetComponent<Button>().interactable,Is.False);
        }

        [Test]
        public void ExistingKidnapAdapterEnableCyclesKeepExactlyOneListenerAndDisableRemovesIt()
        {
            GameObject legacy=Create("UI_Coop_HUD",typeof(RectTransform));
            var buttonObject=Create("Btn_Summon",typeof(RectTransform),typeof(Image),typeof(Button)); buttonObject.transform.SetParent(legacy.transform,false);
            Button button=buttonObject.GetComponent<Button>();
            var controller=_host.AddComponent<FusionBattleUiController>(); Set(controller,"_kidnapButton",button);
            Invoke(controller,"Awake"); _hud=_host.GetComponent<BattleHudPresentation>();
            for(int i=0;i<3;i++) { Invoke(controller,"OnEnable"); Invoke(controller,"OnEnable"); Assert.That(RuntimeCalls(button.onClick),Is.EqualTo(1)); Invoke(controller,"OnDisable"); Assert.That(RuntimeCalls(button.onClick),Is.Zero); }
        }

        [Test]
        public void RuntimeKoreanLabelsUseAFontWithRequiredGlyphsAndNoMockIdentities()
        {
            CreateHud(); Text label=CanvasRoot().Find("BattleControls/Kidnap/Label").GetComponent<Text>();
            Assert.That(label.font,Is.Not.Null);
            foreach(char ch in "왹져납치생체변이공명관전") Assert.That(label.font.HasCharacter(ch),Is.True,"Missing Korean glyph: "+ch);
            Assert.That(CanvasRoot().Find("MissionHeader/LocalPlayer/Identity").GetComponent<Text>().text,Does.Contain("참가자 확인 중"));
        }

        [Test]
        public void SpectatorAndExternalArtCameraPoseRemainOwnedByTheirExistingControllers()
        {
            var camera=Create("Main Camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=8;
            var spectator=Create("Spectator").AddComponent<BattleSpectatorCameraController>();
            var hud=CreateHud();
            Set(spectator,"_isSpectating",true);Set(hud,"_spectator",spectator);
            camera.orthographicSize=5;camera.transform.SetPositionAndRotation(new Vector3(0,12,-10),Quaternion.Euler(48,0,0));
            Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;
            hud.ApplyLayoutForScreen(960,540,new Rect(0,0,960,540),camera);
            Assert.That(camera.orthographicSize,Is.EqualTo(5));Assert.That(camera.transform.position,Is.EqualTo(position));Assert.That(camera.transform.rotation,Is.EqualTo(rotation));
            Set(spectator,"_isSpectating",false);
            hud.ApplyLayoutForScreen(800,960,new Rect(0,0,800,960),camera);
            Assert.That(camera.transform.position,Is.EqualTo(position));Assert.That(camera.transform.rotation,Is.EqualTo(rotation));
        }

        [Test]
        public void DestroyedPreviousCameraDoesNotPreventCapturingAndRestoringReplacementCamera()
        {
            var oldCamera=Create("Old Main Camera",typeof(Camera)).GetComponent<Camera>();
            oldCamera.tag="MainCamera";oldCamera.orthographic=true;oldCamera.orthographicSize=8;
            var hud=CreateHud();
            hud.ApplyLayoutForScreen(800,960,new Rect(0,0,800,960),oldCamera);
            Object.DestroyImmediate(oldCamera.gameObject);
            var next=Create("Replacement Main Camera",typeof(Camera)).GetComponent<Camera>();
            next.tag="MainCamera";next.orthographic=true;next.orthographicSize=6;
            Rect original=new Rect(.05f,.04f,.90f,.92f);next.rect=original;
            Vector3 position=new Vector3(1,12,-4);next.transform.position=position;
            Assert.DoesNotThrow(()=>hud.ApplyLayoutForScreen(800,960,new Rect(0,0,800,960),next));
            Assert.That(next.rect,Is.EqualTo(hud.Layout.NormalizedWorld(800,960)));
            Assert.That(next.transform.position,Is.EqualTo(position));
            hud.Hide();Assert.That(next.rect,Is.EqualTo(original));Assert.That(next.orthographicSize,Is.EqualTo(6));
        }

        [Test]
        public void DebugMarkerSuppressionIsSceneScopedAndRestoresOriginalRendererState()
        {
            var hud=CreateHud();
            Renderer marker=Create("MutationLabel",typeof(MeshRenderer)).GetComponent<Renderer>();
            BattleHudPresentation.SuppressWorldMarker(marker);Assert.That(marker.enabled,Is.False);
            Scene other=EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject foreign=new("ForeignMarker",typeof(MeshRenderer));SceneManager.MoveGameObjectToScene(foreign,other);
                Renderer untouched=foreign.GetComponent<Renderer>();BattleHudPresentation.SuppressWorldMarker(untouched);Assert.That(untouched.enabled,Is.True);
                hud.Hide();Assert.That(marker.enabled,Is.True);Assert.That(untouched.enabled,Is.True);
            }
            finally {EditorSceneManager.ClosePreviewScene(other);}
        }

        [TestCase(1)]
        [TestCase(2)]
        public void ReplicatedPlayerWarningsAndIdentitiesFollowLocalAndOtherSlotOnBothPeers(int localSlot)
        {
            var hud=CreateHud();
            var player1=BattleHudPresentation.PlayerDisplay.FromReplicatedCount("real-roster-one",81,PlayerBattleState.ACTIVE,80,90);
            var player2=BattleHudPresentation.PlayerDisplay.FromReplicatedCount("real-roster-two",93,PlayerBattleState.ACTIVE,80,90);
            var display=new BattleHudPresentation.DisplayState();
            BattleHudPresentation.AssignPlayerDisplays(display,localSlot,false,100,player1,player2);hud.Render(display);
            Text local=CanvasRoot().Find("MissionHeader/LocalPlayer/Count").GetComponent<Text>();
            Text other=CanvasRoot().Find("MissionHeader/OtherPlayer/Count").GetComponent<Text>();
            Assert.That(local.text,Is.EqualTo((localSlot==1?player1:player2).CountText(100)));
            Assert.That(other.text,Is.EqualTo((localSlot==1?player2:player1).CountText(100)));
            Assert.That(local.color,Is.EqualTo(localSlot==1?new Color(1f,.84f,.31f):new Color(1f,.54f,.24f)));
            Assert.That(other.color,Is.EqualTo(localSlot==1?new Color(1f,.54f,.24f):new Color(1f,.84f,.31f)));
            Assert.That(CanvasRoot().Find("MissionHeader/LocalPlayer/Identity").GetComponent<Text>().text,Does.Contain(localSlot==1?player1.Identity:player2.Identity));
        }

        [Test]
        public void EliminatedAndSpectatingTakePriorityOverCurrentDangerAndWarningUsingCanonicalThresholds()
        {
            var hud=CreateHud();var display=new BattleHudPresentation.DisplayState();
            Text count=CanvasRoot().Find("MissionHeader/LocalPlayer/Count").GetComponent<Text>();
            foreach(PlayerBattleState state in new[]{PlayerBattleState.ELIMINATED,PlayerBattleState.SPECTATING})
            {
                BattleHudPresentation.AssignPlayerDisplays(display,1,false,250,
                    BattleHudPresentation.PlayerDisplay.FromReplicatedCount("actual-user",17,state,10,15),default);
                hud.Render(display);Assert.That(count.color,Is.EqualTo(new Color(1f,.30f,.35f)));
                Assert.That(count.text,Does.StartWith(state==PlayerBattleState.SPECTATING?"관전":"탈락"));
                Assert.That(count.text,Does.EndWith("17 / 250"));
            }
            BattleHudPresentation.AssignPlayerDisplays(display,1,false,500,
                BattleHudPresentation.PlayerDisplay.FromReplicatedCount("actual-user",249,PlayerBattleState.ACTIVE,300,400),default);
            hud.Render(display);Assert.That(count.color,Is.EqualTo(BattleHudGraphic.Cyan));
            Assert.That(count.text,Does.StartWith("MONSTER"),"Use supplied canonical thresholds instead of a fixed count or limit percentage.");
        }

        [Test]
        public void DailyInactiveOtherFieldIsMutedEvenIfUnusedPlayerFlagsContainDanger()
        {
            var hud=CreateHud();var display=new BattleHudPresentation.DisplayState();
            BattleHudPresentation.AssignPlayerDisplays(display,1,true,100,
                new BattleHudPresentation.PlayerDisplay("solo-roster-user",9,PlayerBattleState.ACTIVE,false,false),
                new BattleHudPresentation.PlayerDisplay("",99,PlayerBattleState.ELIMINATED,true,true));
            hud.Render(display);
            Text other=CanvasRoot().Find("MissionHeader/OtherPlayer/Count").GetComponent<Text>();
            Assert.That(other.text,Is.EqualTo("—"));Assert.That(other.color,Is.EqualTo(new Color(.42f,.52f,.59f)));
            Text identity=CanvasRoot().Find("MissionHeader/OtherPlayer/Identity").GetComponent<Text>();
            Assert.That(identity.text,Does.Contain("솔로 콘텐츠 · 비활성"));Assert.That(identity.color,Is.EqualTo(other.color));
        }

        [Test]
        public void CurrentReplicatedCountRecoveryClearsDangerEvenAfterTheSessionHasPreviouslyReachedIt()
        {
            var hud=CreateHud();var display=new BattleHudPresentation.DisplayState();
            Text count=CanvasRoot().Find("MissionHeader/LocalPlayer/Count").GetComponent<Text>();
            foreach(int current in new[]{93,85,79})
            {
                BattleHudPresentation.AssignPlayerDisplays(display,2,false,100,default,
                    BattleHudPresentation.PlayerDisplay.FromReplicatedCount("actual-p2",current,PlayerBattleState.ACTIVE,80,90));
                hud.Render(display);
                Color expected=current==93?new Color(1f,.54f,.24f):current==85?new Color(1f,.84f,.31f):BattleHudGraphic.Cyan;
                Assert.That(count.color,Is.EqualTo(expected));
            }
            Assert.That(count.text,Is.EqualTo("MONSTER · 79 / 100"));
        }

        [Test]
        public void ActualProductTabAndDevelopmentTogglePathsAreMutuallyExclusiveAndClearStaleInput()
        {
            var hud=CreateHud();
            Transform canvas=CanvasRoot();
            Button mutation=canvas.Find("BattleControls/MutationTab").GetComponent<Button>();
            Button resonance=canvas.Find("BattleControls/ResonanceTab").GetComponent<Button>();
            GameObject drawer=canvas.Find("UnitUpgradeDrawer").gameObject;
            mutation.onClick.Invoke();Assert.That(drawer.activeSelf,Is.True);
            Assert.That(mutation.GetComponentInChildren<Text>().text,Does.StartWith("−"));
            Assert.That(BattleHudPresentation.ToggleDevelopmentDrawer(0),Is.True);
            Assert.That(drawer.activeSelf,Is.False);Assert.That(mutation.GetComponentInChildren<Text>().text,Does.StartWith("+"));
            Vector2 worldPoint=new(hud.Layout.World.x+hud.Layout.World.width*.18f,hud.Layout.World.center.y);
            Vector2 guiPoint=new(worldPoint.x,Screen.height-worldPoint.y);
            BattleHudPresentation.RegisterDevelopmentInput(new Rect(guiPoint-Vector2.one*10,Vector2.one*20));
            Assert.That(BattleHudPresentation.BlocksWorldInput(worldPoint),Is.True);
            resonance.onClick.Invoke();Assert.That(BattleHudPresentation.IsDevelopmentDrawerOpen(0),Is.False);
            Assert.That(drawer.activeSelf,Is.True);Assert.That(resonance.GetComponentInChildren<Text>().text,Does.StartWith("−"));
            Assert.That(BattleHudPresentation.BlocksWorldInput(worldPoint),Is.False,"Closed developer rect must not remain as an invisible blocker.");
            Assert.That(BattleHudPresentation.ToggleDevelopmentDrawer(1),Is.True);Assert.That(drawer.activeSelf,Is.False);
            mutation.onClick.Invoke();Assert.That(BattleHudPresentation.IsDevelopmentDrawerOpen(1),Is.False);Assert.That(drawer.activeSelf,Is.True);
            mutation.onClick.Invoke();Assert.That(drawer.activeSelf,Is.False);Assert.That(mutation.GetComponentInChildren<Text>().text,Does.StartWith("+"));
            Text unavailable=canvas.Find("UnitUpgradeDrawer/MutationAction/Label").GetComponent<Text>();
            Assert.That(unavailable.color,Is.EqualTo(new Color(.43f,.55f,.62f)),"Unavailable action must not use the active white caption.");
        }

        private static int RuntimeCalls(UnityEventBase calls)
        { return ((ICollection)typeof(UnityEventBase).GetMethod("PrepareInvoke",Hidden).Invoke(calls,null)).Count; }

        [TestCase(800,960,0,28,800,900)]
        [TestCase(960,540,28,10,904,510)]
        [TestCase(1080,1920,0,60,1080,1800)]
        public void AuthoredHudSkinBuildsOnItsOwnGraphicAndKeepsRealIdentityCountAndIconTextSeparated(int width,int height,int x,int y,int safeWidth,int safeHeight)
        {
            var style=Resources.Load<BattleHudStyleCatalog>(BattleHudStyleCatalog.ResourcePath);
            Assert.That(style!=null&&style.Available,Is.True,"Generate the authored HUD catalog before this asset-available regression.");
            var hud=CreateHud();hud.ApplyLayoutForScreen(width,height,new Rect(x,y,safeWidth,safeHeight),null);
            var display=new BattleHudPresentation.DisplayState{CanKidnap=true,KidnapCost=60,LocalIdentity="actual-roster-player-one",OtherIdentity="actual-roster-player-two",LocalCount="MONSTER · 11 / 100",OtherCount="MONSTER · 23 / 100",Status="BOSS · 18s"};
            hud.Render(display);Transform canvas=CanvasRoot(),header=canvas.Find("MissionHeader");
            RectTransform card=(RectTransform)header.Find("LocalPlayer"),status=(RectTransform)header.Find("BattleStatus"),wave=(RectTransform)header.Find("Wave");
            Assert.That(status.anchoredPosition.y,Is.GreaterThan(card.anchoredPosition.y+card.sizeDelta.y));
            Assert.That(wave.anchoredPosition.y,Is.GreaterThan(status.anchoredPosition.y+status.sizeDelta.y));
            Text statusText=status.GetComponent<Text>();
            foreach(string caption in new[]{"협동 전투 진행 중","BOSS · 남은 시간 18초"})
            {
                display.Status=caption;hud.Render(display);Assert.That(statusText.text,Is.EqualTo(caption));
                statusText.font.RequestCharactersInTexture(caption,statusText.fontSize,statusText.fontStyle);
                using var textMesh=new VertexHelper();
                typeof(Text).GetMethod("OnPopulateMesh",Hidden,null,new[]{typeof(VertexHelper)},null).Invoke(statusText,new object[]{textMesh});
                Assert.That(statusText.cachedTextGenerator.verts.Count,Is.GreaterThan(4),caption+" must generate actual glyph vertices at this resolution.");
                Assert.That(textMesh.currentVertCount,Is.GreaterThan(0),caption+" must produce a visible status/Boss timer mesh.");
            }
            foreach(string player in new[]{"LocalPlayer","OtherPlayer"})
            {
                Text identity=header.Find(player+"/Identity").GetComponent<Text>(),count=header.Find(player+"/Count").GetComponent<Text>();
                Assert.That(identity.text,Is.EqualTo(player=="LocalPlayer"?display.LocalIdentity:display.OtherIdentity));
                Assert.That(identity.alignment,Is.EqualTo(TextAnchor.MiddleCenter));Assert.That(count.alignment,Is.EqualTo(TextAnchor.MiddleCenter));
                Assert.That(identity.font,Is.SameAs(style.Korean));Assert.That(identity.fontStyle,Is.EqualTo(FontStyle.Normal));
                Assert.That(identity.rectTransform.anchoredPosition.y,Is.GreaterThan(count.rectTransform.anchoredPosition.y+count.rectTransform.sizeDelta.y));
                Assert.That(identity.text,Does.Not.Contain("내 필드"));Assert.That(identity.text,Does.Not.Contain("동료 필드"));
            }
            Button button=canvas.Find("BattleControls/Kidnap").GetComponent<Button>();RectTransform rect=(RectTransform)button.transform;
            Image skin=button.transform.Find("Skin").GetComponent<Image>();
            Assert.That(button.GetComponents<Graphic>().Length,Is.EqualTo(1),"The code-native frame and painted Image cannot share one GameObject.");
            Assert.That(skin.GetComponentInParent<Button>(),Is.SameAs(button));Assert.That(button.targetGraphic,Is.SameAs(skin));Assert.That(skin.raycastTarget,Is.True);
            Assert.That(button.GetComponent<BattleHudGraphic>().enabled,Is.False);Assert.That(skin.sprite,Is.SameAs(style.Kidnap));Assert.That(skin.type,Is.EqualTo(Image.Type.Simple));Assert.That(skin.preserveAspect,Is.True);
            Assert.That(rect.sizeDelta.x/rect.sizeDelta.y,Is.EqualTo(3).Within(.001));
            Assert.That(skin.rectTransform.anchorMin,Is.EqualTo(Vector2.zero));Assert.That(skin.rectTransform.anchorMax,Is.EqualTo(Vector2.one));
            Text label=button.transform.Find("Label").GetComponent<Text>(),price=button.transform.Find("Price").GetComponent<Text>();
            Assert.That(label.text,Is.EqualTo("왹져 납치"));Assert.That(price.text,Is.EqualTo("60 G"));Assert.That(price.font,Is.SameAs(style.Numbers));
            Assert.That(label.rectTransform.anchorMin.x,Is.GreaterThanOrEqualTo(.28f));Assert.That(price.rectTransform.anchorMin.x,Is.GreaterThanOrEqualTo(.28f));
            Assert.That(price.rectTransform.anchorMax.y,Is.LessThanOrEqualTo(label.rectTransform.anchorMin.y));
            Assert.That(header.GetComponent<BattleHudGraphic>().Flat,Is.True);
        }

        [Test]
        public void DisplayReadCacheAvoidsRepeatedReadsAndInvalidatesForChangedImmutableBalanceContext()
        {
            var first=new MyDefense.Battle.Runtime.BattleSessionContext("same-session","balance-v1","hash-v1","content-v1","battle-hash-v1",0);
            var next=new MyDefense.Battle.Runtime.BattleSessionContext("same-session","balance-v2","hash-v2","content-v2","battle-hash-v2",1);
            var cache=new BattlePresentationValueCache<int,int>();int reads=0;
            for(int frame=0;frame<120;frame++)Assert.That(cache.Get(first,1,()=>++reads),Is.EqualTo(1));
            Assert.That(cache.ReadCount,Is.EqualTo(1));Assert.That(cache.Get(first,2,()=>++reads),Is.EqualTo(2));
            Assert.That(cache.Get(next,2,()=>++reads),Is.EqualTo(3));cache.Clear();Assert.That(cache.Get(next,2,()=>++reads),Is.EqualTo(4));
            foreach(string property in new[]{"CanonicalBalanceVersion","CanonicalContentHash","BattleContentVersion","BattleContentHash"})
                Assert.That(typeof(MyDefense.Battle.Runtime.BattleSessionContext).GetProperty(property).GetSetMethod(true),Is.Null,property+" is immutable within a cached session.");
        }
    }
}
