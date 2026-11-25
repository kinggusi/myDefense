using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusion;
using MyDefense.Battle;
using MyDefense.Battle.Combat;
using MyDefense.Battle.Editor;
using MyDefense.Battle.Presentation;
using MyDefense.Battle.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
using Assert=NUnit.Framework.Assert;

namespace MyDefenseGame.Editor.Tests
{
    public sealed class BattleMonsterPresentationTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private Scene _scene;
        private BattleMonsterArtCatalog _catalog;
        private readonly List<BattleMonsterArtView> _views=new();
        private readonly List<MonsterHealthView> _health=new();
        private readonly List<BattleMonsterPresentationCoordinator> _coordinators=new();
        [SetUp] public void SetUp()
        {
            _scene=EditorSceneManager.NewPreviewScene();
            _catalog=Resources.Load<BattleMonsterArtCatalog>(BattleMonsterArtCatalog.ResourcePath);
            Assert.That(_catalog!=null&&_catalog.Available,Is.True,"Generate Battle Presentation Refinement Assets before running these tests.");
        }
        [TearDown] public void TearDown()
        {
            foreach(var coordinator in _coordinators)if(coordinator!=null)coordinator.Suspend();_coordinators.Clear();
            foreach(var view in _views)if(view!=null)view.ReleaseArtwork();_views.Clear();
            foreach(var health in _health)if(health!=null)Invoke(health,"OnDisable");_health.Clear();
            if(_scene.IsValid())EditorSceneManager.ClosePreviewScene(_scene);
        }
        private GameObject Create(string name,params System.Type[] components)
        {var go=new GameObject(name,components);SceneManager.MoveGameObjectToScene(go,_scene);return go;}
        private static object Invoke(object target,string method,params object[] values)=>target.GetType().GetMethod(method,Hidden).Invoke(target,values);
        private static void Set(object target,string field,object value)=>target.GetType().GetField(field,Hidden).SetValue(target,value);
        private static T Get<T>(object target,string field)=>(T)target.GetType().GetField(field,Hidden).GetValue(target);
        private static void Context(BattleRunnerLifecycle lifecycle,string session)
            =>Set(lifecycle,"<SessionContext>k__BackingField",new BattleSessionContext(session,"balance","canonical-hash","content","battle-hash",1));
        private GameObject Monster(out MonsterStat stat)
        {
            var root=GameObject.CreatePrimitive(PrimitiveType.Cube);root.name="Existing logical Monster";SceneManager.MoveGameObjectToScene(root,_scene);
            root.transform.SetPositionAndRotation(new Vector3(2,1.14f,3),Quaternion.Euler(0,123,0));
            root.transform.localScale=Vector3.one*.83f;stat=root.AddComponent<MonsterStat>();stat.InitializeHp(100);return root;
        }
        private BattleMonsterArtView View(GameObject root,string id="NORMAL_MONSTER",float scale=1)
        {var view=root.AddComponent<BattleMonsterArtView>();_views.Add(view);Assert.That(view.Configure(_catalog,id,scale),Is.True);return view;}
        private MonsterHealthView Health(GameObject root,out RectTransform canvas,out TMP_Text text)
        {
            var canvasObject=Create("Existing HP canvas",typeof(RectTransform),typeof(Canvas));canvasObject.transform.SetParent(root.transform,false);canvas=(RectTransform)canvasObject.transform;
            var label=Create("HP",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));label.transform.SetParent(canvas,false);text=label.GetComponent<TMP_Text>();
            var health=root.AddComponent<MonsterHealthView>();Set(health,"_monsterStat",root.GetComponent<MonsterStat>());Set(health,"_monsterRenderer",root.GetComponent<Renderer>());
            Set(health,"_healthCanvasRoot",canvas);Set(health,"_hpText",text);Invoke(health,"Awake");Invoke(health,"OnEnable");_health.Add(health);return health;
        }

        [Test]
        public void CanonicalCatalogHasThreeDistinctTwelveFrameWalksAndSoftBlendEffects()
        {
            var textures=new HashSet<Texture2D>();
            foreach(string id in new[]{"NORMAL_MONSTER","ELITE_MONSTER","WAVE_BOSS"})
            {
                BattlePresentationAtlas atlas=_catalog.Resolve(id);Assert.That(atlas.Count,Is.EqualTo(12));Assert.That(textures.Add(atlas.Texture),Is.True);
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(atlas.Texture));
                Assert.That(importer.isReadable,Is.False);Assert.That(importer.mipMapsPreserveCoverage,Is.True);
                Assert.That(atlas.Texture.filterMode,Is.EqualTo(FilterMode.Trilinear));Assert.That(atlas.Texture.mipmapCount,Is.GreaterThan(1));
                for(int frame=0;frame<12;frame++)
                {
                    Sprite sprite=atlas.Create(frame,1);
                    try
                    {
                        Assert.That(sprite,Is.Not.Null);Assert.That(sprite.rect.height/sprite.pixelsPerUnit,Is.EqualTo(1).Within(.0001));
                        Assert.That(sprite.pivot.y/sprite.pixelsPerUnit,Is.InRange(0,.02f),"Foot pivot is near the lowest visible pixels rather than cell center.");
                        RectInt cell=BattlePresentationRefinementAssetGenerator.GridCell(atlas.Texture.width,atlas.Texture.height,3,4,frame%3,frame/3);
                        Assert.That(sprite.rect.x+sprite.pivot.x,Is.EqualTo(cell.center.x).Within(.01),"Gait crop must keep the original horizontal registration.");
                    }
                    finally{Object.DestroyImmediate(sprite);}
                }
            }
            Assert.That(_catalog.Resolve("UNMAPPED_MONSTER"),Is.Null);Assert.That(_catalog.Portal.Count,Is.EqualTo(8));Assert.That(_catalog.Merge.Count,Is.EqualTo(6));
            Assert.That(_catalog.EffectShader.name,Is.EqualTo("Sprites/Default"));Assert.That(_catalog.EffectShader,Is.Not.SameAs(_catalog.Shader));
        }

        [Test]
        public void RoundedPortalGridPartitionsNonDivisibleDimensionsWithoutLostOrOverlappingPixels()
        {
            int area=0;
            for(int row=0;row<2;row++)for(int col=0;col<4;col++)
            {
                RectInt cell=BattlePresentationRefinementAssetGenerator.GridCell(1774,887,4,2,col,row);area+=cell.width*cell.height;
                Assert.That(cell.xMin,Is.EqualTo(col==0?0:BattlePresentationRefinementAssetGenerator.GridCell(1774,887,4,2,col-1,row).xMax));
                if(col==3)Assert.That(cell.xMax,Is.EqualTo(1774));
                if(row==0)Assert.That(cell.yMax,Is.EqualTo(887));else Assert.That(cell.yMin,Is.Zero);
            }
            Assert.That(area,Is.EqualTo(1774*887));
            Assert.That(BattlePresentationRefinementAssetGenerator.GridCell(1774,887,4,2,0,0).yMin,
                Is.EqualTo(BattlePresentationRefinementAssetGenerator.GridCell(1774,887,4,2,0,1).yMax));
        }

        [Test]
        public void SharedFrameLeaseSurvivesOneOwnerAndReleasesOnlyOwnedGeometryAfterLastOwner()
        {
            int before=BattlePresentationFrameCache.Count;
            var a=BattlePresentationFrameCache.Acquire(_catalog.Resolve("NORMAL_MONSTER"),0,1.123f);
            var b=BattlePresentationFrameCache.Acquire(_catalog.Resolve("NORMAL_MONSTER"),0,1.123f);
            Sprite sprite=a.Sprite;Mesh mesh=a.Mesh;
            try
            {
                Assert.That(b.Sprite,Is.SameAs(sprite));Assert.That(b.Mesh,Is.SameAs(mesh));Assert.That(BattlePresentationFrameCache.Count,Is.EqualTo(before+1));
                a.Dispose();Assert.That(sprite!=null&&mesh!=null,Is.True);b.Dispose();b.Dispose();
                Assert.That(sprite==null&&mesh==null,Is.True);Assert.That(BattlePresentationFrameCache.Count,Is.EqualTo(before));
                Assert.That(_catalog.Resolve("NORMAL_MONSTER").Texture!=null,Is.True);
            }
            finally{a.Dispose();b.Dispose();}
        }

        [TestCase("NORMAL_MONSTER",1f)]
        [TestCase("ELITE_MONSTER",1f)]
        [TestCase("WAVE_BOSS",2f)]
        public void ActualPaintedViewLeavesLogicalRootColliderAndStatsUntouchedAndRestoresLegacyRenderer(string id,float scale)
        {
            GameObject root=Monster(out MonsterStat stat);var collider=root.GetComponent<BoxCollider>();Bounds colliderBounds=collider.bounds;
            Vector3 position=root.transform.position,originalScale=root.transform.localScale;Quaternion rotation=root.transform.rotation;
            Renderer legacy=root.GetComponent<Renderer>();int before=BattlePresentationFrameCache.Count;
            BattleMonsterArtView view=View(root,id,scale);float now=Time.unscaledTime;
            view.UpdatePresentation(position+new Vector3(-1,0,0),false,now+.1f);
            Assert.That(root.transform.position,Is.EqualTo(position));Assert.That(root.transform.rotation,Is.EqualTo(rotation));Assert.That(root.transform.localScale,Is.EqualTo(originalScale));
            Assert.That(collider.enabled,Is.True);Assert.That(collider.bounds,Is.EqualTo(colliderBounds));Assert.That(root.GetComponentsInChildren<Collider>().Length,Is.EqualTo(1));
            Assert.That(stat.CurrentHp,Is.EqualTo(100));Assert.That(stat.MaxHp,Is.EqualTo(100));Assert.That(stat.IsDead,Is.False);Assert.That(legacy.enabled,Is.False);
            var body=view.BodyRenderer;Material material=body.sharedMaterial;
            Assert.That(body.transform.position.y,Is.EqualTo(BattleMonsterArtView.FootHeight).Within(.000001f));
            Assert.That(Quaternion.Angle(body.transform.rotation,Quaternion.Euler(25,0,0)),Is.LessThan(.01));
            Assert.That(Get<int>(view,"_direction"),Is.EqualTo(1));Assert.That(Get<bool>(view,"_mirror"),Is.True);
            view.ReleaseArtwork();view.ReleaseArtwork();Assert.That(legacy.enabled,Is.True);Assert.That(material==null&&body==null,Is.True);
            Assert.That(BattlePresentationFrameCache.Count,Is.EqualTo(before));
        }

        [Test]
        public void RemappedMovementChoosesDirectionAndClampsGaitRateWithoutRockingRoot()
        {
            GameObject root=Monster(out _);BattleMonsterArtView view=View(root);Vector3 position=root.transform.position;Quaternion original=root.transform.rotation;
            float now=Time.unscaledTime;view.UpdatePresentation(position+Vector3.forward*10,false,now+.01f);
            Assert.That(Get<int>(view,"_direction"),Is.EqualTo(2));Assert.That(Get<float>(view,"_distance"),Is.InRange(0,.101f));
            view.UpdatePresentation(position+Vector3.back*10,false,now+.02f);
            Assert.That(Get<int>(view,"_direction"),Is.Zero);Assert.That(Get<float>(view,"_distance"),Is.InRange(0,.202f));
            Assert.That(root.transform.rotation,Is.EqualTo(original));Assert.That(root.transform.position,Is.EqualTo(position));
        }

        [TestCase(BattleUnitVisual.Diad)]
        [TestCase(BattleUnitVisual.Mecharon)]
        [TestCase(BattleUnitVisual.Puppeteer1)]
        [TestCase(BattleUnitVisual.Puppeteer2)]
        public void ActualPaintedFlightKeepsEmissionAndSimulationRootButEndsOnTheLockedMovingRemappedMonster(BattleUnitVisual visual)
        {
            GameObject target=Monster(out MonsterStat stat);BattleMonsterArtView monster=View(target);
            GameObject other=Monster(out _);other.transform.position=target.transform.position+Vector3.right*.2f;BattleMonsterArtView otherView=View(other);
            var source=Create("FusionKidnapUnit",typeof(MeshFilter),typeof(MeshRenderer),typeof(UnitData));
            source.transform.position=new Vector3(-3,.8f,-4);source.GetComponent<UnitData>().specId=29;
            var art=source.AddComponent<BattleUnitArtView>();
            var projectile=GameObject.CreatePrimitive(PrimitiveType.Sphere);SceneManager.MoveGameObjectToScene(projectile,_scene);
            var painted=projectile.AddComponent<BattlePaintedProjectileView>();
            var impacts=Object.FindObjectsByType<BattleArtImpact>(FindObjectsSortMode.None);
            try
            {
                Assert.That(art.Configure(Resources.Load<BattleUnitArtCatalog>(BattleUnitArtCatalog.ResourcePath),visual,true),Is.True);
                BattleUnitArtShot shot=art.CaptureShot(73);projectile.transform.position=shot.SimulationOrigin;
                Vector3 authorityPosition=projectile.transform.position;Bounds collider=projectile.GetComponent<Collider>().bounds;
                Vector3 targetRoot=target.transform.position;Invoke(painted,"PrepareArtwork",art,shot,73UL);
                Invoke(painted,"DrawArtwork",shot.SimulationOrigin,targetRoot,target.transform);
                SpriteRenderer paint=Get<SpriteRenderer>(painted,"_paint");
                Assert.That(Vector3.Distance(paint.transform.position,shot.ResolveEmissionPoint(targetRoot)),Is.LessThan(.0001),"Monster height correction must be zero at launch.");
                Vector3 current=Vector3.Lerp(shot.SimulationOrigin,targetRoot,.5f);
                Invoke(painted,"DrawArtwork",current,targetRoot,target.transform);
                Vector3 expected=current+(shot.ResolveEmissionPoint(targetRoot)-shot.SimulationOrigin)*.5f+(monster.ResolveHitPoint(targetRoot)-targetRoot)*.5f;
                if(visual>=BattleUnitVisual.Puppeteer1)expected+=Vector3.up*(visual==BattleUnitVisual.Puppeteer2?.38f:.16f);
                Assert.That(Vector3.Distance(paint.transform.position,expected),Is.LessThan(.0001),"Visual correction blends along the unchanged logical flight.");
                // A proxy can supply its remapped lane position before the painted Monster's later update.
                Vector3 remappedRoot=targetRoot+new Vector3(1.7f,0,-11);
                Invoke(painted,"DrawArtwork",remappedRoot,remappedRoot,target.transform);
                Vector3 socket=monster.ResolveHitPoint(remappedRoot);
                Assert.That(Vector3.Distance(paint.transform.position,socket),Is.LessThan(.0001));
                Assert.That(Vector3.Distance(socket,BattleMonsterArtView.ResolveHitPoint(target.transform,remappedRoot)),Is.LessThan(.0001),"Accepted hit and painted flight share the same visual socket.");
                Assert.That(Vector3.Distance(socket,otherView.ResolveHitPoint(other.transform.position)),Is.GreaterThan(1),"A nearby Monster must not replace the locked target.");
                Assert.That(projectile.transform.position,Is.EqualTo(authorityPosition));Assert.That(projectile.GetComponent<Collider>().bounds,Is.EqualTo(collider));
                Assert.That(target.transform.position,Is.EqualTo(targetRoot));Assert.That(stat.CurrentHp,Is.EqualTo(100));
                Invoke(painted,"DrawArtwork",remappedRoot,remappedRoot,null);
                Assert.That(Vector3.Distance(paint.transform.position,remappedRoot),Is.LessThan(.0001),"A missing visual target falls back to the supplied locked logical endpoint, never another Monster.");
            }
            finally
            {
                painted.ReleaseArtwork();art.ReleaseArtwork();
                foreach(var impact in Object.FindObjectsByType<BattleArtImpact>(FindObjectsSortMode.None).Where(value=>!impacts.Contains(value)))
                {impact.ReleaseArtwork();Object.DestroyImmediate(impact.gameObject);}
            }
        }

        [Test]
        public void MissingOrReleasedMonsterArtworkUsesTheExistingLogicalPresentationPoint()
        {
            GameObject root=Monster(out _);Vector3 local=new Vector3(-5,1.14f,6);
            Assert.That(BattleMonsterArtView.ResolveHitPoint(root.transform,local),Is.EqualTo(local));
            BattleMonsterArtView view=View(root);Vector3 socket=view.ResolveHitPoint(local);
            Assert.That(socket.y,Is.LessThan(local.y));Assert.That(socket.x,Is.EqualTo(local.x));
            Assert.That(socket.z,Is.GreaterThan(local.z),"Use the same fixed pitched geometry as the body, not stale renderer bounds.");
            view.ReleaseArtwork();Assert.That(BattleMonsterArtView.ResolveHitPoint(root.transform,local),Is.EqualTo(local));
        }

        [Test]
        public void AcceptedHitVisualStampRejectsRecycledNetworkIdsAndOldSessionsWithoutChangingLockedHitEligibility()
        {
            Assert.That(BattleProjectileSpawner.MatchesPresentationHitIdentity("session-A",7,"session-A",7),Is.True);
            Assert.That(BattleProjectileSpawner.MatchesPresentationHitIdentity("session-A",7,"session-A",8),Is.False);
            Assert.That(BattleProjectileSpawner.MatchesPresentationHitIdentity("session-A",1,"session-B",1),Is.False);
            Assert.That(BattleProjectileSpawner.MatchesPresentationHitIdentity("",7,"",7),Is.False);
            Assert.That(BattleProjectileSpawner.MatchesPresentationHitIdentity("session-A",0,"session-A",0),Is.False);
            var locked=new NetworkId{Raw=101};var other=new NetworkId{Raw=102};
            Assert.That(BattleProjectileNetworkState.IsIntendedPrimaryTarget(locked,locked),Is.True);
            Assert.That(BattleProjectileNetworkState.IsIntendedPrimaryTarget(locked,other),Is.False);
        }

        [Test]
        public void RepeatedProxyHpDoesNotRestartFlashOrTextAndProxyDeadFadesOnceWithoutOnDied()
        {
            GameObject root=Monster(out MonsterStat stat);MonsterHealthView health=Health(root,out RectTransform canvas,out TMP_Text text);BattleMonsterArtView view=View(root);
            int died=0;stat.OnDied+=()=>died++;
            for(int i=0;i<120;i++)stat.ApplyNetworkState(100,100,false);
            Assert.That(view.HitFlashCount,Is.Zero);Assert.That(health.TextRefreshCount,Is.EqualTo(1));Assert.That(text.text,Is.EqualTo("100 / 100"));
            stat.ApplyNetworkState(75,100,false);for(int i=0;i<120;i++)stat.ApplyNetworkState(75,100,false);
            Assert.That(view.HitFlashCount,Is.EqualTo(1));Assert.That(health.TextRefreshCount,Is.EqualTo(2));
            Invoke(health,"LateUpdate");Assert.That(canvas.position.y,Is.EqualTo(view.BodyRenderer.bounds.max.y+.25f).Within(.001));
            stat.ApplyNetworkState(0,100,true);Invoke(view,"LateUpdate");float deathAt=Get<float>(view,"_deathAt");
            Assert.That(died,Is.Zero,"Proxy ApplyNetworkState does not publish local death callbacks.");Assert.That(view.IsDying,Is.True);Assert.That(canvas.gameObject.activeSelf,Is.False);
            view.UpdatePresentation(root.transform.position,true,deathAt+.5f);Assert.That(view.Opacity,Is.EqualTo(.5f).Within(.01));
            for(int i=0;i<20;i++){stat.ApplyNetworkState(0,100,true);view.UpdatePresentation(root.transform.position,true,deathAt+.8f);}
            Assert.That(Get<float>(view,"_deathAt"),Is.EqualTo(deathAt));Assert.That(view.Opacity,Is.EqualTo(.2f).Within(.01));Assert.That(view.HitFlashCount,Is.EqualTo(1));
            Assert.That(health.TextRefreshCount,Is.EqualTo(3));Invoke(health,"LateUpdate");Assert.That(canvas.gameObject.activeSelf,Is.False);
        }

        [Test]
        public void AcceptedEventGateAllowsOneEffectPerApprovalAndAllowsLaterApprovalOfSamePair()
        {
            var gate=new BattlePresentationEventGate();Assert.That(gate.AcceptMerge(1,3,4),Is.True);Assert.That(gate.AcceptMerge(1,3,4),Is.False);
            Assert.That(gate.AcceptMerge(2,3,4),Is.True,"The same slot pair can be legitimately merged later.");
            Assert.That(gate.AcceptMerge(3,4,4),Is.False,"Selecting the result of Legendary materials is not another two-material Merge.");
            Assert.That(gate.AcceptMerge(3,-1,4),Is.False);Assert.That(gate.AcceptMerge(3,0,24),Is.False);Assert.That(gate.AcceptMerge(0,3,4),Is.False);
            Assert.That(gate.AcceptSpawn(3,1),Is.True);Assert.That(gate.AcceptSpawn(4,1),Is.False);Assert.That(gate.AcceptSpawn(3,2),Is.False);
            gate.Clear();Assert.That(gate.AcceptSpawn(1,1),Is.True,"Runtime IDs and notification sequences may restart in the next session.");
        }

        [Test]
        public void CoordinatorRejectsOldSessionNotificationsAndClearsPendingBeforeReusingSequenceAndRuntimeIds()
        {
            int before=BattlePresentationFrameCache.Count;
            var lifecycle=Create("Lifecycle").AddComponent<BattleRunnerLifecycle>();Context(lifecycle,"session-A");
            var sender=Create("Persistent authority",typeof(NetworkObject)).AddComponent<BattleWaveStateAuthority>();
            var coordinator=Create("Presentation coordinator").AddComponent<BattleMonsterPresentationCoordinator>();_coordinators.Add(coordinator);coordinator.Initialize(lifecycle,null);
            Invoke(coordinator,"MergeAccepted",sender,"session-A",1UL,1,3,4);
            Invoke(coordinator,"MergeAccepted",sender,"session-A",1UL,1,3,4);
            Invoke(coordinator,"SpawnAccepted",sender,"session-A",2UL,default(NetworkId),1UL,0);
            Assert.That(Get<IList>(coordinator,"_merges").Count,Is.EqualTo(1));Assert.That(Get<IList>(coordinator,"_spawns").Count,Is.EqualTo(1));
            Context(lifecycle,"session-B");Invoke(coordinator,"MergeAccepted",sender,"session-A",3UL,1,3,4);
            Assert.That(Get<IList>(coordinator,"_merges").Count,Is.Zero);Assert.That(Get<IList>(coordinator,"_spawns").Count,Is.Zero);
            Invoke(coordinator,"MergeAccepted",sender,"session-B",1UL,1,3,4);Invoke(coordinator,"SpawnAccepted",sender,"session-B",2UL,default(NetworkId),1UL,0);
            Assert.That(Get<IList>(coordinator,"_merges").Count,Is.EqualTo(1));Assert.That(Get<IList>(coordinator,"_spawns").Count,Is.EqualTo(1));
            Invoke(coordinator,"ResetFromAuthority",sender);Assert.That(Get<IList>(coordinator,"_merges").Count,Is.Zero);Assert.That(Get<IList>(coordinator,"_spawns").Count,Is.Zero);
            Assert.That(coordinator.MergeEffectCount,Is.Zero);Assert.That(coordinator.SpawnEffectCount,Is.Zero,"Receiving a notification before the actual object/board exists must not fabricate an effect.");
            coordinator.Suspend();coordinator.Suspend();Assert.That(BattlePresentationFrameCache.Count,Is.EqualTo(before));
        }

        [Test]
        public void SpawnReadinessWaitsForActualObjectIdentityViewAndLocalPlacementButNeverReplaysExpiredOrDeadBirths()
        {
            Assert.That(BattlePresentationReadiness.Spawn(false,false,false,false,false,false,false),Is.EqualTo(BattleVisualReadiness.Wait));
            Assert.That(BattlePresentationReadiness.Spawn(true,false,false,false,false,false,false),Is.EqualTo(BattleVisualReadiness.Wait));
            Assert.That(BattlePresentationReadiness.Spawn(true,true,true,false,false,false,false),Is.EqualTo(BattleVisualReadiness.Wait));
            Assert.That(BattlePresentationReadiness.Spawn(true,true,true,false,true,false,false),Is.EqualTo(BattleVisualReadiness.Wait));
            Assert.That(BattlePresentationReadiness.Spawn(true,true,true,false,true,true,false),Is.EqualTo(BattleVisualReadiness.Ready));
            Assert.That(BattlePresentationReadiness.Spawn(true,true,true,true,true,true,false),Is.EqualTo(BattleVisualReadiness.Skip));
            Assert.That(BattlePresentationReadiness.Spawn(true,true,false,false,true,true,false),Is.EqualTo(BattleVisualReadiness.Skip));
            Assert.That(BattlePresentationReadiness.Spawn(false,false,false,false,false,false,true),Is.EqualTo(BattleVisualReadiness.Skip));
        }

        [Test]
        public void PortalLaneRequiresBoundLocalIdentityAndP2PerspectiveAndKeepsDailyBossOnTheP1Lane()
        {
            Assert.That(BattlePresentationReadiness.TryLane(0,false,BattleMonsterLanePolicy.EACH_FIELD,1,out _),Is.False);
            Assert.That(BattlePresentationReadiness.TryLane(2,false,BattleMonsterLanePolicy.EACH_FIELD,1,out _),Is.False);
            Assert.That(BattlePresentationReadiness.TryLane(2,true,BattleMonsterLanePolicy.EACH_FIELD,1,out var remote),Is.True);Assert.That(remote,Is.EqualTo(LaneType.Player2Lane));
            Assert.That(BattlePresentationReadiness.TryLane(2,true,BattleMonsterLanePolicy.EACH_FIELD,2,out var local),Is.True);Assert.That(local,Is.EqualTo(LaneType.Player1Lane));
            Assert.That(BattlePresentationReadiness.TryLane(1,false,BattleMonsterLanePolicy.EACH_FIELD,1,out var soloBoss),Is.True);Assert.That(soloBoss,Is.EqualTo(LaneType.Player1Lane));
            Assert.That(BattlePresentationReadiness.TryLane(1,false,BattleMonsterLanePolicy.BOSS_SHARED,0,out var shared),Is.True);Assert.That(shared,Is.EqualTo(LaneType.BossSharedLane));
        }
    }
}
