using System;
using System.Reflection;
using MyDefense.Battle;
using MyDefense.Battle.Presentation;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MyDefenseGame.Editor.Tests
{
    public sealed class BattleBoardInteractionPresentationTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private Scene _scene;
        private GameObject _host;
        private FusionKidnapBoardView _board;
        private BattleBoardInteractionPresentation _interaction;
        private Camera _camera;
        private Transform _grid;
        [SetUp] public void SetUp()
        {
            _scene=EditorSceneManager.NewPreviewScene();_host=Create("Presentation host");
            _board=_host.AddComponent<FusionKidnapBoardView>();_interaction=_host.AddComponent<BattleBoardInteractionPresentation>();_interaction.Initialize(_board,null);
            Set(_board,"<Interaction>k__BackingField",_interaction);
            _grid=Create("GridManager").transform;Set(_board,"_player1Grid",_grid);
            for(int row=0;row<4;row++)for(int col=0;col<6;col++)
            {var tile=Create($"Tile_{col}_{row}").transform;tile.SetParent(_grid,false);tile.localPosition=new Vector3(col*1.1f,0,row*1.1f);}
            _camera=Create("Main Camera",typeof(Camera)).GetComponent<Camera>();_camera.tag="MainCamera";_camera.orthographic=true;_camera.transform.SetPositionAndRotation(new Vector3(3,12,0),Quaternion.Euler(60,0,0));
        }
        [TearDown] public void TearDown()
        {
            if(_interaction!=null){_interaction.CancelAll();Invoke(_interaction,"OnDestroy");}
            foreach(GameObject root in _scene.GetRootGameObjects())foreach(BattleUnitArtView view in root.GetComponentsInChildren<BattleUnitArtView>(true))view.ReleaseArtwork();
            EditorSceneManager.ClosePreviewScene(_scene);
        }
        private GameObject Create(string name,params Type[] types){var go=new GameObject(name,types);SceneManager.MoveGameObjectToScene(go,_scene);return go;}
        private GameObject Unit(int slot,bool artwork=false,BattleUnitVisual visual=BattleUnitVisual.Diad)
        {
            var unit=Create("FusionKidnapUnit",typeof(MeshFilter),typeof(MeshRenderer),typeof(CapsuleCollider),typeof(UnitData));
            unit.transform.SetParent(_board.TileAt(1,slot),false);unit.transform.localPosition=Vector3.up*.8f;unit.transform.localScale=Vector3.one*.55f;
            UnitData data=unit.GetComponent<UnitData>();data.serverId=(1L<<32)|(uint)(slot+1);data.specId=1;data.grade="NORMAL";
            var drag=unit.AddComponent<FusionKidnapUnitDrag>();drag.Initialize(_board,null,1,slot);
            if(artwork){var catalog=Resources.Load<BattleUnitArtCatalog>(BattleUnitArtCatalog.ResourcePath);Assert.That(catalog,Is.Not.Null);Assert.That(unit.AddComponent<BattleUnitArtView>().Configure(catalog,visual,true),Is.True);}
            return unit;
        }
        private object Preview(GameObject unit,uint generation=0)
        {
            Type type=typeof(BattleBoardInteractionPresentation).GetNestedType("RenderPreview",BindingFlags.NonPublic);
            object preview=Activator.CreateInstance(type,new object[]{unit.GetComponent<FusionKidnapUnitDrag>(),generation});
            Set(_interaction,"_preview",preview);Set(_interaction,"_generation",generation);return preview;
        }
        private static void Set(object target,string field,object value)=>target.GetType().GetField(field,Hidden).SetValue(target,value);
        private static object Invoke(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Hidden).Invoke(target,args);

        [TestCase(1L,1L,0,0,true)]
        [TestCase(1L,5L,0,0,false)]
        [TestCase(1L,1L,0,1,false)]
        [TestCase(29L,29L,4,4,false)]
        public void MergeHintsUseActualSpeciesAndGradeRegardlessOfDevelopmentArtwork(long source,long target,int sg,int tg,bool expected)
        {
            Assert.That(BattleBoardInteractionPresentation.IsMergePartner(0,1,true,true,source,target,(byte)sg,(byte)tg,true,false,false,false),Is.EqualTo(expected));
            Assert.That(BattleBoardInteractionPresentation.IsMergePartner(0,1,true,true,source,target,(byte)sg,(byte)tg,false,false,false,false),Is.False);
            Assert.That(BattleBoardInteractionPresentation.IsMergePartner(0,1,true,true,source,target,(byte)sg,(byte)tg,true,true,false,false),Is.False);
            Assert.That(BattleBoardInteractionPresentation.IsMergePartner(0,1,true,true,source,target,(byte)sg,(byte)tg,true,false,true,false),Is.False);
            Assert.That(BattleBoardInteractionPresentation.IsMergePartner(0,1,true,true,source,target,(byte)sg,(byte)tg,true,false,false,true),Is.False);
        }

        [Test]
        public void RenderOnlyDropAwaitKeepsRootDataColliderAndProjectileEmissionAtAuthoritativeOrigin()
        {
            GameObject unit=Unit(0,true);var drag=unit.GetComponent<FusionKidnapUnitDrag>();var art=unit.GetComponent<BattleUnitArtView>();
            Vector3 original=unit.transform.position,emission=art.EmissionPoint,foot=art.Foot;Transform parent=unit.transform.parent;
            string identity=JsonUtility.ToJson(unit.GetComponent<UnitData>());Bounds collider=unit.GetComponent<Collider>().bounds;
            Preview(unit);_interaction.MovePreview(drag,original+Vector3.right*2);_interaction.Await(drag,1);
            Assert.That(_interaction.HasPending,Is.True);Assert.That(_interaction.PreviewPosition.x,Is.Not.EqualTo(original.x));
            Assert.That(unit.transform.position,Is.EqualTo(original));Assert.That(unit.transform.parent,Is.SameAs(parent));
            Assert.That(art.Foot,Is.EqualTo(foot));Assert.That(art.EmissionPoint,Is.EqualTo(emission));Assert.That(unit.GetComponent<Collider>().bounds,Is.EqualTo(collider));
            Assert.That(JsonUtility.ToJson(unit.GetComponent<UnitData>()),Is.EqualTo(identity));
            Invoke(_interaction,"TickPreview",Time.unscaledTime+2);
            Assert.That(_interaction.HasPending,Is.False);Assert.That(unit.transform.position,Is.EqualTo(original));
        }

        [Test]
        public void HostImmediateAcknowledgementDoesNotSnapTheAcceptedVisualBackToSource()
        {
            GameObject unit=Unit(0);Preview(unit);var drag=unit.GetComponent<FusionKidnapUnitDrag>();_interaction.Await(drag,1);
            Assert.That(_interaction.HasPending,Is.True);
            _board.SnapUnitToSlot(unit,1,1);drag.UpdateSlotIndex(1);Vector3 accepted=unit.transform.position;
            _interaction.Acknowledge(unit,1,0,1);
            Assert.That(_interaction.HasPending,Is.False);Assert.That(unit.transform.position,Is.EqualTo(accepted));Assert.That(_interaction.SelectedSlot,Is.EqualTo(1));
        }

        [Test]
        public void PendingDropIgnoresSameOrOtherUnitPressAndDoesNotQueueHeldInputAfterAcknowledgement()
        {
            GameObject unit=Unit(0),other=Unit(2);var drag=unit.GetComponent<FusionKidnapUnitDrag>();
            Preview(unit);_interaction.Await(drag,1);Vector3 pendingPosition=_interaction.PreviewPosition;
            Assert.That(_interaction.BeginPreview(drag),Is.False);Assert.That(_interaction.BeginPreview(other.GetComponent<FusionKidnapUnitDrag>()),Is.False);
            Set(drag,"_pointerPressed",true);Set(other.GetComponent<FusionKidnapUnitDrag>(),"_pointerPressed",true);
            Invoke(drag,"BeginPointerPress",_camera.pixelRect.center);
            Invoke(other.GetComponent<FusionKidnapUnitDrag>(),"BeginPointerPress",_camera.pixelRect.center);
            Assert.That(_interaction.HasPending,Is.True);Assert.That(_interaction.PreviewPosition,Is.EqualTo(pendingPosition));
            _interaction.Acknowledge(unit,1,0,1);
            Assert.That((bool)drag.GetType().GetField("_pointerPressed",Hidden).GetValue(drag),Is.False);
            Assert.That((bool)drag.GetType().GetField("_dragging",Hidden).GetValue(drag),Is.False);
            Assert.That((bool)other.GetComponent<FusionKidnapUnitDrag>().GetType().GetField("_pointerPressed",Hidden).GetValue(other.GetComponent<FusionKidnapUnitDrag>()),Is.False);
        }

        [Test]
        public void SessionCancelClearsHeldInputAndOldAcknowledgementCannotClearNewSlotInstance()
        {
            GameObject old=Unit(0);var oldDrag=old.GetComponent<FusionKidnapUnitDrag>();Preview(old);Set(oldDrag,"_dragging",true);Set(oldDrag,"_pointerPressed",true);
            _interaction.CancelAll();
            Assert.That(oldDrag.IsDragging,Is.False);Assert.That((bool)oldDrag.GetType().GetField("_pointerPressed",Hidden).GetValue(oldDrag),Is.False);
            oldDrag.OnPointerUp(new PointerEventData(EventSystem.current){position=Vector2.zero});
            GameObject next=Unit(0);Preview(next,1);_interaction.Await(next.GetComponent<FusionKidnapUnitDrag>(),1);
            _interaction.Acknowledge(old,1,0,1);Assert.That(_interaction.HasPending,Is.True);
            _interaction.CancelAll();Assert.That(_interaction.HasPending,Is.False);
        }

        [Test]
        public void ReplacedSpeciesDuringAwaitCancelsOnlyRenderPreviewAndPreservesLatestUnitData()
        {
            GameObject unit=Unit(0);Preview(unit);_interaction.Await(unit.GetComponent<FusionKidnapUnitDrag>(),1);
            unit.GetComponent<UnitData>().specId=8;unit.GetComponent<UnitData>().grade="EPIC";
            var latest=new MaterialPropertyBlock();latest.SetColor("_Color",Color.green);unit.GetComponent<Renderer>().SetPropertyBlock(latest);
            Invoke(_interaction,"TickPreview",Time.unscaledTime);
            Assert.That(_interaction.HasPending,Is.False);Assert.That(unit.GetComponent<UnitData>().specId,Is.EqualTo(8));Assert.That(unit.GetComponent<UnitData>().grade,Is.EqualTo("EPIC"));
            var actual=new MaterialPropertyBlock();unit.GetComponent<Renderer>().GetPropertyBlock(actual);Assert.That(actual.GetColor("_Color"),Is.EqualTo(Color.green));
        }

        [TestCase(BattleUnitVisual.Puppeteer1)]
        [TestCase(BattleUnitVisual.Puppeteer2)]
        public void PuppetGhostControlStringsAndDollsMoveTogetherWithoutMovingOriginalWorldLines(BattleUnitVisual visual)
        {
            GameObject unit=Unit(0,true,visual);var art=unit.GetComponent<BattleUnitArtView>();
            LineRenderer original=unit.GetComponentInChildren<LineRenderer>();Vector3 old=original.GetPosition(0);
            GameObject ghost=art.CreateRenderGhost();
            try
            {
                LineRenderer line=ghost.GetComponentInChildren<LineRenderer>();Assert.That(line.useWorldSpace,Is.False);
                Vector3 before=line.transform.TransformPoint(line.GetPosition(0));Vector3 delta=new(1,.2f,2);ghost.transform.position+=delta;
                Assert.That(Vector3.Distance(line.transform.TransformPoint(line.GetPosition(0)),before+delta),Is.LessThan(.0001));
                Assert.That(original.useWorldSpace,Is.True);Assert.That(original.GetPosition(0),Is.EqualTo(old));
                Assert.That(ghost.GetComponentInChildren<UnitData>(),Is.Null);Assert.That(ghost.GetComponentInChildren<UnitAttack>(),Is.Null);Assert.That(ghost.GetComponentInChildren<Collider>(),Is.Null);
            }
            finally{ghost.GetComponent<BattleRenderGhostLease>().ReleaseResources();Object.DestroyImmediate(ghost);}
        }

        [Test]
        public void SamePointerFrameAcrossManyUnitsUsesOnlyOnePhysicalPick()
        {
            Vector2 point=_camera.pixelRect.center;int before=_interaction.PhysicsPickCount;
            for(int i=0;i<48;i++)Invoke(_interaction,"ResolvePick",_camera,point,1);
            Assert.That(_interaction.PhysicsPickCount-before,Is.EqualTo(1));
        }

        [Test]
        public void ProductHudClickDoesNotRunWorldPickingOrSelectBehindTheControls()
        {
            var hud=_host.AddComponent<BattleHudPresentation>();hud.Initialize(null);
            try
            {
                int before=_interaction.PhysicsPickCount;
                Assert.That(Invoke(_interaction,"ResolvePick",_camera,hud.Layout.Footer.center,1),Is.Null);
                Assert.That(_interaction.PhysicsPickCount,Is.EqualTo(before));
            }
            finally{Invoke(hud,"OnDestroy");}
        }

        [Test]
        public void DestroyedPendingSourceDisposesGhostWithoutRecreatingOrRestoringAnOldUnit()
        {
            GameObject unit=Unit(0);Preview(unit);_interaction.Await(unit.GetComponent<FusionKidnapUnitDrag>(),1);
            Object.DestroyImmediate(unit);Invoke(_interaction,"TickPreview",Time.unscaledTime);
            Assert.That(_interaction.HasPending,Is.False);
            foreach(GameObject root in _scene.GetRootGameObjects())Assert.That(root.name,Is.Not.EqualTo("Unit render-only drag preview"));
        }

        [Test]
        public void SharedPoseCacheRetainsOtherUnitsAndGhostUntilTheirLastLeaseIsReleased()
        {
            GameObject first=Unit(0,true);int created=BattleUnitPoseCache.CreatedCount;
            GameObject second=Unit(1,true);Assert.That(BattleUnitPoseCache.CreatedCount,Is.EqualTo(created));
            var art=first.GetComponent<BattleUnitArtView>();GameObject ghost=art.CreateRenderGhost();Mesh mesh=ghost.GetComponentInChildren<MeshFilter>().sharedMesh;
            art.ReleaseArtwork();second.GetComponent<BattleUnitArtView>().ReleaseArtwork();Assert.That(mesh!=null,Is.True);
            ghost.GetComponent<BattleRenderGhostLease>().ReleaseResources();Object.DestroyImmediate(ghost);Assert.That(mesh==null,Is.True);
        }

        [Test]
        public void DisabledOrUnavailableAuthorityCannotLeaveAnOwnedGradeCanvasVisible()
        {
            Canvas canvas=_host.GetComponentInChildren<Canvas>(true);Assert.That(canvas,Is.Not.Null);
            canvas.gameObject.SetActive(true);Invoke(_interaction,"OnDisable");Assert.That(canvas.gameObject.activeSelf,Is.False);
            canvas.gameObject.SetActive(true);Invoke(_interaction,"LateUpdate");Assert.That(canvas.gameObject.activeSelf,Is.False);
        }

        [Test]
        public void RealMergePartnerBadgeRendersDirectBrightDoubleEdgeAndSeparateDarkLabelWithoutChangingTheUnit()
        {
            Canvas canvas=_host.GetComponentInChildren<Canvas>(true);canvas.gameObject.SetActive(true);
            var style=Resources.Load<BattleHudStyleCatalog>(BattleHudStyleCatalog.ResourcePath);Assert.That(style,Is.Not.Null);
            Type type=typeof(BattleBoardInteractionPresentation).GetNestedType("Badge",BindingFlags.NonPublic);
            object badge=Activator.CreateInstance(type,new object[]{canvas.transform,style.Numbers,style.Korean});
            GameObject unit=Unit(0);Vector3 original=unit.transform.position;
            RectTransform Field(string name)=>(RectTransform)type.GetField(name,Hidden).GetValue(badge);
            Color32 MeshColor(BattleHudGraphic graphic)
            {
                using var mesh=new VertexHelper();typeof(BattleHudGraphic).GetMethod("OnPopulateMesh",Hidden,null,new[]{typeof(VertexHelper)},null).Invoke(graphic,new object[]{mesh});Assert.That(mesh.currentVertCount,Is.GreaterThan(0));
                var vertex=UIVertex.simpleVert;mesh.PopulateUIVertex(ref vertex,0);return vertex.color;
            }
            RectTransform outline=Field("_outline"),inner=Field("_inner"),under=Field("_underlay"),pill=Field("_pill"),grade=Field("_root");
            try
            {
                Vector3 visibleRoot=_camera.ViewportToWorldPoint(new Vector3(.5f,.5f,12))+Vector3.up*.735f;
                type.GetMethod("Draw").Invoke(badge,new object[]{_camera,visibleRoot,(byte)0,false,true});
                BattleHudGraphic edge=outline.GetComponent<BattleHudGraphic>(),second=inner.GetComponent<BattleHudGraphic>(),contrast=under.GetComponent<BattleHudGraphic>();
                Assert.That(edge.UseDirectOutlineColor&&second.UseDirectOutlineColor&&contrast.UseDirectOutlineColor,Is.True);
                Assert.That(MeshColor(edge),Is.EqualTo((Color32)edge.color),"The designated bright green must reach the mesh without multiplication by the dark HUD rim.");
                Assert.That(MeshColor(second),Is.EqualTo((Color32)second.color));Assert.That(second.color.g,Is.EqualTo(1));
                Assert.That(contrast.OutlineWidth,Is.GreaterThan(4+second.OutlineWidth));Assert.That(contrast.color.maxColorComponent,Is.LessThan(.03f));
                Assert.That(inner.gameObject.activeSelf&&pill.gameObject.activeSelf,Is.True);
                Text hint=pill.GetComponentInChildren<Text>();Assert.That(hint.text,Is.EqualTo("합성"));Assert.That(hint.font,Is.SameAs(style.Korean));Assert.That(hint.raycastTarget,Is.False);
                hint.font.RequestCharactersInTexture(hint.text,hint.fontSize,hint.fontStyle);
                using(var textMesh=new VertexHelper())
                {
                    typeof(Text).GetMethod("OnPopulateMesh",Hidden,null,new[]{typeof(VertexHelper)},null).Invoke(hint,new object[]{textMesh});
                    Assert.That(hint.cachedTextGenerator.verts.Count,Is.GreaterThanOrEqualTo(8),"The Korean label must actually generate glyph vertices at its real allocated size.");
                    Assert.That(textMesh.currentVertCount,Is.GreaterThanOrEqualTo(8),"Text overflow settings alone do not prove a visible label mesh.");
                }
                Assert.That(hint.color.g,Is.EqualTo(1));Assert.That(pill.GetComponent<BattleHudGraphic>().Flat,Is.True);
                var pillCorners=new Vector3[4];var gradeCorners=new Vector3[4];pill.GetWorldCorners(pillCorners);grade.GetWorldCorners(gradeCorners);
                Assert.That(pillCorners[1].y,Is.LessThan(gradeCorners[0].y),"The partner label belongs below the tile, separate from the grade badge and unit footprint.");
                type.GetMethod("Draw").Invoke(badge,new object[]{_camera,visibleRoot,(byte)0,true,false});
                Assert.That(inner.gameObject.activeSelf||pill.gameObject.activeSelf,Is.False,"Only valid partners receive the double edge and Merge pill.");
                Assert.That(MeshColor(edge),Is.EqualTo((Color32)new Color(.38f,.94f,1f)));
                type.GetMethod("Draw").Invoke(badge,new object[]{_camera,visibleRoot,(byte)0,false,false});
                Assert.That(outline.gameObject.activeSelf||under.gameObject.activeSelf,Is.False);Assert.That(unit.transform.position,Is.EqualTo(original));
            }
            finally{type.GetMethod("Dispose").Invoke(badge,null);}
            Assert.That(outline==null&&inner==null&&under==null&&pill==null&&grade==null,Is.True);
        }
    }
}
