using System;
using System.Linq;
using System.Reflection;
using MyDefense.Battle.Combat;
using MyDefense.Battle.Presentation;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MyDefenseGame.Editor.Tests
{
    public sealed class BattleArtPresentationTests
    {
        [Test]
        public void AllOriginalAndDarkBoldUnitPosesUseCoveragePreservingMinificationWithoutChangingEffects()
        {
            const string art="Assets/Art/Battle/ShowroomConcepts/";
            foreach(string look in new[]{"","VisualVariants/DarkBold/"})
            foreach(string unit in new[]{"Diad","Mecharon","Puppeteer","PuppeteerDoll"})
            foreach(string pose in new[]{"Views","Windup","Attack","Recover"})
            {
                string path=art+look+unit+"Board"+pose+".png";
                var texture=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);Assert.That(texture,Is.Not.Null,path);
                var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
                Assert.That(texture.mipmapCount,Is.GreaterThan(1),path);Assert.That(texture.filterMode,Is.EqualTo(FilterMode.Trilinear));
                Assert.That(importer.mipMapsPreserveCoverage,Is.True);Assert.That(importer.alphaTestReferenceValue,Is.EqualTo(.27f).Within(.0001));
                Assert.That(importer.isReadable,Is.False);
            }
            var effects=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(art+"MecharonBoardEffects.png");
            Assert.That(effects.mipmapCount,Is.EqualTo(1),"FX importer is outside the unit-pose sampling change.");
        }
        private static BattleUnitArtCatalog Catalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BattleUnitArtCatalog>(
                "Assets/Resources/Battle/Presentation/BattleUnitArtCatalog.asset");
            Assert.That(catalog, Is.Not.Null, "Run BattleArtAssetGenerator.GeneratePresentationAssets first.");
            return catalog;
        }

        [Test]
        public void CatalogContainsAllPoseDirectionsAndBothPuppetOptionsShareTheirActualArtwork()
        {
            BattleUnitArtCatalog catalog = Catalog();
            Assert.That(catalog.Unit(BattleUnitVisual.Puppeteer1), Is.SameAs(catalog.Unit(BattleUnitVisual.Puppeteer2)));
            foreach (BattleUnitVisual visual in Enum.GetValues(typeof(BattleUnitVisual)))
            {
                Assert.That(catalog.Available(visual, false), Is.True, visual + " original");
                Assert.That(catalog.Available(visual, true), Is.True, visual + " candidate");
                foreach (BattleArtPose pose in Enum.GetValues(typeof(BattleArtPose)))
                foreach (BattleArtFacing facing in Enum.GetValues(typeof(BattleArtFacing)))
                {
                    Sprite sprite = catalog.Unit(visual).Pose(pose).Create(facing, 1.44f, true);
                    try { Assert.That(sprite, Is.Not.Null, visual + "/" + pose + "/" + facing); }
                    finally { if (sprite != null) UnityEngine.Object.DestroyImmediate(sprite); }
                }
            }
        }

        [TestCase(BattleUnitVisual.Diad)]
        [TestCase(BattleUnitVisual.Mecharon)]
        [TestCase(BattleUnitVisual.Puppeteer1)]
        [TestCase(BattleUnitVisual.Puppeteer2)]
        public void RealUnitArtworkLeavesIdentityCalculatedSnapshotColliderAndRootTransformUnchanged(BattleUnitVisual visual)
        {
            var root = new GameObject("FusionKidnapUnit", typeof(MeshFilter), typeof(MeshRenderer), typeof(CapsuleCollider), typeof(UnitData));
            BattleUnitArtView view = null;
            try
            {
                root.transform.SetPositionAndRotation(new Vector3(3f, .8f, 4f), Quaternion.Euler(0f, 180f, 0f));
                root.transform.localScale = new Vector3(.61f, .55f, .83f);
                UnitData data = root.GetComponent<UnitData>();
                data.serverId = (1L << 32) | 7U; data.specId = 29; data.grade = "MYTHIC";
                data.activeMutationType = "TOXIC"; data.pendingMutationType = "NONE";
                UnitAttack attack = root.AddComponent<UnitAttack>();
                Invoke(attack, "Awake");
                AlienAttackSnapshot snapshot = AlienAttackSnapshot.FromCalculatedStats(data.serverId, 123f, 1.7f, 8f, "TOXIC");
                attack.ApplyAttackSnapshot(snapshot);
                string identity = JsonUtility.ToJson(data);
                Vector3 position = root.transform.position, scale = root.transform.localScale;
                Quaternion rotation = root.transform.rotation;
                view = root.AddComponent<BattleUnitArtView>();
                Assert.That(view.Configure(Catalog(), visual, true), Is.True);
                view.ObserveShot(41, new Vector3(-2f, 0f, 6f));
                Invoke(view, "LateUpdate");
                Assert.That(JsonUtility.ToJson(data), Is.EqualTo(identity));
                var applied = (AlienAttackSnapshot)typeof(UnitAttack).GetField("attackSnapshot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(attack);
                Assert.That(JsonUtility.ToJson(applied), Is.EqualTo(JsonUtility.ToJson(snapshot)));
                Assert.That(root.transform.position, Is.EqualTo(position));
                Assert.That(root.transform.rotation, Is.EqualTo(rotation));
                Assert.That(root.transform.localScale, Is.EqualTo(scale));
                Assert.That(root.GetComponent<CapsuleCollider>().enabled, Is.True);
                Assert.That(root.GetComponentsInChildren<UnitAttack>().Length, Is.EqualTo(1));
                Mesh ownedMesh = root.GetComponentsInChildren<MeshFilter>().First(filter => filter.sharedMesh != null).sharedMesh;
                Material ownedMaterial = root.GetComponentsInChildren<MeshRenderer>().First(renderer => renderer != root.GetComponent<MeshRenderer>()).sharedMaterial;
                view.ReleaseArtwork();
                Assert.That(ownedMesh == null, Is.True, "Owned relief mesh must be released.");
                Assert.That(ownedMaterial == null, Is.True, "Owned material must be released.");
                Assert.That(root.GetComponent<MeshRenderer>().enabled, Is.True);
                Assert.That(root.GetComponent<CapsuleCollider>().enabled, Is.True);
                Assert.That(Catalog().Unit(visual).Available(true), Is.True, "Shared source assets survive cleanup.");
            }
            finally
            {
                if (view != null) view.ReleaseArtwork();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PaintedBoardReplacesExactly48TopsAndRestoresOriginalTilesWithoutMovingThem()
        {
            var owner = new GameObject("Board surface test");
            var grids = new[] { new GameObject("P1").transform, new GameObject("P2").transform };
            BattleBoardSurfaceView surface = owner.AddComponent<BattleBoardSurfaceView>();
            try
            {
                grids[1].SetPositionAndRotation(new Vector3(0f, 0f, 7f), Quaternion.Euler(0f, 180f, 0f));
                foreach (Transform grid in grids)
                for (int row = 0; row < 4; row++)
                for (int col = 0; col < 6; col++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Tile_{col}_{row}"; tile.transform.SetParent(grid, false);
                    tile.transform.localPosition = new Vector3(col * 1.1f, 0f, row * 1.1f);
                    tile.transform.localScale = new Vector3(.9f, .1f, .9f);
                }
                Transform[] tiles = grids.SelectMany(grid => grid.Cast<Transform>()).ToArray();
                Vector3[] positions = tiles.Select(tile => tile.position).ToArray();
                Assert.That(surface.Configure(grids[0], grids[1], Catalog()), Is.True);
                Assert.That(surface.SurfaceCount, Is.EqualTo(48));
                for (int i = 0; i < tiles.Length; i++)
                {
                    Assert.That(tiles[i].position, Is.EqualTo(positions[i]));
                    Assert.That(tiles[i].GetComponent<BoxCollider>().enabled, Is.True);
                    Assert.That(tiles[i].GetComponent<MeshRenderer>().enabled, Is.False);
                    Assert.That(tiles[i].GetComponentsInChildren<Collider>().Length, Is.EqualTo(1));
                }
                Mesh mesh = tiles[0].Find("Painted tile top").GetComponent<MeshFilter>().sharedMesh;
                Material material = tiles[0].Find("Painted tile top").GetComponent<MeshRenderer>().sharedMaterial;
                surface.ReleaseSurfaces();
                Assert.That(mesh == null, Is.True); Assert.That(material == null, Is.True);
                Assert.That(tiles.All(tile => tile.GetComponent<MeshRenderer>().enabled), Is.True);
                Assert.That(Catalog().BoardTexture, Is.Not.Null);
            }
            finally
            {
                surface.ReleaseSurfaces();
                foreach (Transform grid in grids) UnityEngine.Object.DestroyImmediate(grid.gameObject);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [TestCase(16f / 9f)]
        [TestCase(4f / 3f)]
        [TestCase(9f / 16f)]
        [TestCase(21f / 9f)]
        public void PaintedBackdropCoversViewportAndPreservesUpperRightFocalAfterCameraAndAspectChanges(float aspect)
        {
            var cameraObject = new GameObject("Backdrop camera", typeof(Camera));
            var backdropObject = new GameObject("Backdrop", typeof(SpriteRenderer));
            var texture = new Texture2D(160, 90);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 160, 90), Vector2.one * .5f, 100f);
            try
            {
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true; camera.aspect = aspect; camera.orthographicSize = 6f;
                camera.transform.SetPositionAndRotation(new Vector3(2f, 12f, -6f), Quaternion.Euler(60f, 0f, 0f));
                SpriteRenderer renderer = backdropObject.GetComponent<SpriteRenderer>(); renderer.sprite = sprite;
                for (int i = 0; i < 2; i++)
                {
                    Vector3 cameraPosition = camera.transform.position;
                    Quaternion cameraRotation = camera.transform.rotation;
                    PlanetContentApplicator.FitBackdrop(camera, renderer);
                    Vector3[] corners = { new(-.8f, -.45f, 0f), new(-.8f, .45f, 0f), new(.8f, .45f, 0f), new(.8f, -.45f, 0f) };
                    Vector3[] viewport = corners.Select(point => camera.WorldToViewportPoint(renderer.transform.TransformPoint(point))).ToArray();
                    Assert.That(viewport.Min(point => point.x), Is.LessThanOrEqualTo(.0001f));
                    Assert.That(viewport.Max(point => point.x), Is.EqualTo(1f).Within(.0001f), "Keep the source image's right edge at the viewport right.");
                    Assert.That(viewport.Min(point => point.y), Is.LessThanOrEqualTo(.0001f));
                    Assert.That(viewport.Max(point => point.y), Is.EqualTo(1f).Within(.0001f), "Keep the source image's top edge at the viewport top.");
                    Vector3 focal = camera.WorldToViewportPoint(renderer.transform.TransformPoint(new Vector3(.72f, .405f, 0f)));
                    Assert.That(focal.x, Is.InRange(0f, 1f)); Assert.That(focal.y, Is.InRange(0f, 1f));
                    Assert.That(viewport.All(point => point.z > camera.nearClipPlane && point.z < camera.farClipPlane), Is.True);
                    Assert.That(renderer.transform.localScale.x, Is.EqualTo(renderer.transform.localScale.y).Within(.0001f), "Cover remains uniformly scaled.");
                    Assert.That(renderer.sprite, Is.SameAs(sprite));
                    Assert.That(camera.transform.position, Is.EqualTo(cameraPosition));
                    Assert.That(camera.transform.rotation, Is.EqualTo(cameraRotation));
                    camera.orthographicSize = 8f; camera.aspect *= .8f; camera.transform.position += Vector3.right * 4f;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(backdropObject);
                UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [TestCase("MIXED", BattleDevelopmentArtOverride.Selection.Mixed)]
        [TestCase("puppeteer1", BattleDevelopmentArtOverride.Selection.Puppeteer1)]
        [TestCase("MECHARON", BattleDevelopmentArtOverride.Selection.Mecharon)]
        public void DevelopmentSelectionUsesVisualOptionsInsteadOfInventedSpeciesMappings(string value,
            BattleDevelopmentArtOverride.Selection expected)
        {
            Assert.That(BattleDevelopmentArtOverride.TryParseSelection(value, out var actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(BattleDevelopmentArtOverride.TryParseSelection("MYTHIC29", out _), Is.False);
            Assert.That(BattleDevelopmentArtOverride.TryParseSelection("900", out _), Is.False);
        }

        [Test]
        public void MixedDevelopmentAliasSurvivesSlotAndOwnerChangesAndFreshReconstruction()
        {
            var unit = new GameObject("Alias identity", typeof(UnitData));
            try
            {
                UnitData data = unit.GetComponent<UnitData>(); data.specId = 37;
                var before = BattleDevelopmentArtOverride.VisualFor(BattleDevelopmentArtOverride.Selection.Mixed, data.specId);
                foreach (long runtimeId in new[] { (1L << 32) | 1L, (1L << 32) | 24L, (2L << 32) | 8L })
                {
                    data.serverId = runtimeId;
                    Assert.That(BattleDevelopmentArtOverride.VisualFor(BattleDevelopmentArtOverride.Selection.Mixed, data.specId),
                        Is.EqualTo(before), "Move/swap and local field perspective cannot change the chosen visual.");
                }
                var reconstructed = JsonUtility.FromJson<AliasSpec>("{\"specId\":37}");
                Assert.That(BattleDevelopmentArtOverride.VisualFor(BattleDevelopmentArtOverride.Selection.Mixed, reconstructed.specId),
                    Is.EqualTo(before), "A new peer derives the same alias from the replicated species identity.");
            }
            finally { UnityEngine.Object.DestroyImmediate(unit); }
        }

        [Test]
        public void FirstShotEnsuresViewWithoutPeriodicScanAndOldLaunchCannotAnimateAMergeResult()
        {
            var owner = new GameObject("Immediate Battle art");
            var source = NewVisualUnit(29);
            var senderObject = new GameObject("Persistent hit sender");
            BattleDevelopmentArtOverride controller = owner.AddComponent<BattleDevelopmentArtOverride>();
            try
            {
                typeof(BattleDevelopmentArtOverride).GetField("_catalog", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(controller, Catalog());
                controller.SetSelection(BattleDevelopmentArtOverride.Selection.Mixed);
                UnitData data = source.GetComponent<UnitData>();
                var view = (BattleUnitArtView)InvokeWith(controller, "EnsureView", data);
                Assert.That(view, Is.Not.Null, "First launch binds artwork immediately, without Update/scan.");
                Assert.That(InvokeWith(controller, "EnsureView", data), Is.SameAs(view));
                Assert.That(source.GetComponents<BattleUnitArtView>().Length, Is.EqualTo(1));
                BattleUnitArtShot shot = view.CaptureShot(47UL);
                Vector3 foot = shot.LaunchFoot;
                source.transform.position += Vector3.right * 4f;
                data.serverId = (2L << 32) | 12L;
                Assert.That(InvokeWith(controller, "EnsureView", data), Is.SameAs(view));
                Assert.That(shot.Matches(view), Is.True, "Moving/swapping the same species keeps the source eligible.");
                Assert.That(shot.LaunchFoot, Is.EqualTo(foot), "In-flight launch metadata never follows a moved unit.");
                data.specId = 30;
                Assert.That(shot.Matches(view), Is.False, "Even before the next scan, live merged species invalidates old pose updates.");
                InvokeWith(controller, "EnsureView", data);
                Assert.That(view.BoundSpecId, Is.EqualTo(30));
                Assert.That(shot.Matches(view), Is.False);
                Assert.That(shot.Visual, Is.EqualTo(BattleUnitVisual.Diad), "The projectile retains its old cosmetic identity.");
                BattleProjectileSpawner sender = senderObject.AddComponent<BattleProjectileSpawner>();
                int count = UnityEngine.Object.FindObjectsByType<BattleArtImpact>(FindObjectsSortMode.None).Length;
                InvokeWith(controller, "ObserveHit", sender, 999UL, data.serverId, Vector3.one);
                Assert.That(controller.SkippedUnstampedImpacts, Is.EqualTo(1));
                Assert.That(controller.ObservedImpacts, Is.Zero);
                Assert.That(UnityEngine.Object.FindObjectsByType<BattleArtImpact>(FindObjectsSortMode.None).Length, Is.EqualTo(count),
                    "An unstamped hit must never infer the current slot's new species.");
            }
            finally
            {
                Invoke(controller, "OnDisable");
                UnityEngine.Object.DestroyImmediate(owner);
                foreach (BattleUnitArtView view in source.GetComponents<BattleUnitArtView>()) view.ReleaseArtwork();
                UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(senderObject);
            }
        }

        [Test]
        public void PaintedProjectileInitializesAfterSourceDeletionAndDoesNotHideLegacyBeforeSuccess()
        {
            var source = NewVisualUnit(29);
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var existingImpacts = UnityEngine.Object.FindObjectsByType<BattleArtImpact>(FindObjectsSortMode.None);
            BattleUnitArtView sourceView = source.AddComponent<BattleUnitArtView>();
            BattlePaintedProjectileView painted = projectile.AddComponent<BattlePaintedProjectileView>();
            try
            {
                Assert.That(sourceView.Configure(Catalog(), BattleUnitVisual.Mecharon, true), Is.True);
                BattleUnitArtShot shot = sourceView.CaptureShot(53UL);
                InvokeWith(painted, "PrepareArtwork", sourceView, shot, 53UL);
                MeshRenderer original = projectile.GetComponent<MeshRenderer>();
                Assert.That(original.enabled, Is.True, "Keep the original visible until painted art is ready.");
                sourceView.ReleaseArtwork(); UnityEngine.Object.DestroyImmediate(source); source = null;
                Assert.That(shot.Matches(sourceView), Is.False);
                Assert.That(InvokeWith(painted, "InitializeArtwork", new Vector3(-3f, 0f, 7f)), Is.EqualTo(true));
                var paint = (SpriteRenderer)typeof(BattlePaintedProjectileView)
                    .GetField("_paint", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(painted);
                Assert.That(paint.enabled, Is.True); Assert.That(paint.sprite, Is.Not.Null);
                Assert.That(original.enabled, Is.False);
                Sprite expected = shot.CreateProjectileSprite();
                try { Assert.That(paint.sprite.texture, Is.SameAs(expected.texture), "The captured Mecharon shell survives source deletion."); }
                finally { UnityEngine.Object.DestroyImmediate(expected); }
                Sprite owned = paint.sprite;
                painted.ReleaseArtwork();
                Assert.That(owned == null, Is.True); Assert.That(original.enabled, Is.True);
                Assert.That(Catalog().Available(BattleUnitVisual.Mecharon, true), Is.True);
            }
            finally
            {
                painted.ReleaseArtwork();
                if (source != null) { sourceView.ReleaseArtwork(); UnityEngine.Object.DestroyImmediate(source); }
                UnityEngine.Object.DestroyImmediate(projectile);
                foreach (BattleArtImpact impact in UnityEngine.Object.FindObjectsByType<BattleArtImpact>(FindObjectsSortMode.None)
                    .Where(impact => !existingImpacts.Contains(impact)))
                { impact.ReleaseArtwork(); UnityEngine.Object.DestroyImmediate(impact.gameObject); }
            }
        }

        [Test]
        public void PuppetControlPulsesAndContactsReuseOneOwnedHandMagicSpriteAcrossFrames()
        {
            var source = NewVisualUnit(29);
            BattleUnitArtView view = source.AddComponent<BattleUnitArtView>();
            try
            {
                Assert.That(view.Configure(Catalog(), BattleUnitVisual.Puppeteer1, true), Is.True);
                var glyphs = source.GetComponentsInChildren<SpriteRenderer>().Where(renderer => renderer.name != "Contact shadow").ToArray();
                Assert.That(glyphs.Length, Is.EqualTo(7), "One hand, three flowing pulses and three doll endpoints.");
                Sprite handMagic = glyphs[0].sprite;
                Assert.That(handMagic, Is.Not.Null);
                Assert.That(glyphs.All(renderer => renderer.sprite == handMagic), Is.True);
                for (int frame = 0; frame < 8; frame++) Invoke(view, "LateUpdate");
                Assert.That(source.GetComponentsInChildren<SpriteRenderer>().Where(renderer => renderer.name != "Contact shadow"),
                    Is.EquivalentTo(glyphs), "No per-frame SpriteRenderer/GameObject allocation.");
                Assert.That(glyphs.All(renderer => renderer.sprite == handMagic && renderer.color.a >= .75f), Is.True);
                view.ReleaseArtwork();
                Assert.That(handMagic == null, Is.True, "The shared glyph within this actor is owned and released exactly once.");
                Assert.That(Catalog().Effects(BattleUnitVisual.Puppeteer1).Available, Is.True);
            }
            finally { view.ReleaseArtwork(); UnityEngine.Object.DestroyImmediate(source); }
        }

        [Test]
        public void SpawnerSessionBoundaryClearsLaunchQueuesAndReusedIdOneReanimatesSurvivingUnits()
        {
            var owner = new GameObject("Same-scene session artwork");
            var senderObject = new GameObject("Persistent spawner");
            var sources = new[] { NewVisualUnit(29), NewVisualUnit(30) };
            BattleDevelopmentArtOverride controller = owner.AddComponent<BattleDevelopmentArtOverride>();
            try
            {
                // Register exactly once even when EditMode lifecycle callbacks
                // are suppressed for a non-ExecuteAlways component.
                Invoke(controller, "OnDisable"); Invoke(controller, "OnEnable");
                typeof(BattleDevelopmentArtOverride).GetField("_catalog", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(controller, Catalog());
                controller.SetSelection(BattleDevelopmentArtOverride.Selection.Mixed);
                var views = sources.Select(source => (BattleUnitArtView)InvokeWith(controller, "EnsureView", source.GetComponent<UnitData>())).ToArray();
                BattleUnitArtShot[] before = views.Select(view => view.CaptureShot(1UL)).ToArray();
                foreach (BattleUnitArtView view in views) view.ObserveShot(1UL, Vector3.left);
                var launches = (System.Collections.IDictionary)typeof(BattleDevelopmentArtOverride)
                    .GetField("_shots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
                Type entryType = launches.GetType().GetGenericArguments()[1];
                launches.Add(1UL, Activator.CreateInstance(entryType, true));
                var pending = (System.Collections.IList)typeof(BattleDevelopmentArtOverride)
                    .GetField("_pendingProjectiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
                pending.Add(null); // A destroyed unresolved projectile still occupies a pending entry.
                BattleProjectileSpawner sender = senderObject.AddComponent<BattleProjectileSpawner>();
                object delivered = typeof(BattleProjectileSpawner).GetField("_presentedHits", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sender);
                delivered.GetType().GetMethod("Add").Invoke(delivered, new object[] { ("session-A",1UL,default(Fusion.NetworkId),7UL) });
                object queue = typeof(BattleProjectileSpawner).GetField("_presentationHits", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sender);
                queue.GetType().GetMethod("Enqueue").Invoke(queue, new object[] { (1UL,9L,default(Fusion.NetworkId),"session-A",7UL) });
                Invoke(sender, "ResetPresentationSession"); // The actual Spawned/Despawned common path.
                Assert.That(launches.Count, Is.Zero); Assert.That(pending.Count, Is.Zero);
                Assert.That(delivered.GetType().GetProperty("Count").GetValue(delivered), Is.EqualTo(0));
                Assert.That(queue.GetType().GetProperty("Count").GetValue(queue), Is.EqualTo(0));
                for (int i = 0; i < views.Length; i++)
                {
                    Assert.That(views[i].IsArtworkActive, Is.True, "Reset keeps artwork resources and the unit itself.");
                    Assert.That(before[i].Matches(views[i]), Is.False, "Late old-session cleanup cannot touch the new flight with the same ID.");
                    Assert.That(views[i].CaptureShot(1UL).Ordinal, Is.EqualTo(1));
                    Vector3 target = new(9f, 0f, 6f);
                    views[i].ObserveShot(1UL, target);
                    Assert.That(typeof(BattleUnitArtView).GetField("_aim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(views[i]),
                        Is.EqualTo(target), "ID 1 is a fresh animation in the new session.");
                }
            }
            finally
            {
                Invoke(controller, "OnDisable"); UnityEngine.Object.DestroyImmediate(owner);
                foreach (GameObject source in sources)
                {
                    foreach (BattleUnitArtView view in source.GetComponents<BattleUnitArtView>()) view.ReleaseArtwork();
                    UnityEngine.Object.DestroyImmediate(source);
                }
                UnityEngine.Object.DestroyImmediate(senderObject);
            }
        }

        [Test]
        public void ExplicitDevelopmentPhotonArgumentParsesWithoutAnyEditorMutationEntryPoint()
        {
            const string example = "11111111-2222-4333-8444-555555555555";
            Assert.That(BattleDevelopmentPhotonApplication.TryReadOverride(new[] { "Client.exe", "-battleArtPhotonAppId=" + example },
                out string parsed, out bool supplied), Is.True);
            Assert.That(parsed, Is.EqualTo(example)); Assert.That(supplied, Is.True);
            Assert.That(BattleDevelopmentPhotonApplication.TryReadOverride(new[] { "-fusionSession=fixture" }, out _, out supplied), Is.False);
            Assert.That(supplied, Is.False);
            Assert.That(BattleDevelopmentPhotonApplication.TryReadOverride(new[] { "-battleArtPhotonAppId=" + example,
                "-battleArtPhotonAppId=" + example }, out parsed, out supplied), Is.False);
            Assert.That(parsed, Is.Null); Assert.That(supplied, Is.True);
            Assert.That(typeof(BattleDevelopmentPhotonApplication).GetMethod("ApplyBeforeFirstScene", BindingFlags.Static | BindingFlags.NonPublic),
                Is.Null, "The mutating bootstrap is not compiled in Unity Editor, including Play Mode.");
        }

        [TestCase("")]
        [TestCase("not-an-application-guid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public void InvalidExplicitDevelopmentPhotonApplicationIsRejected(string value)
        {
            Assert.That(BattleDevelopmentPhotonApplication.TryReadOverride(new[] { "-battleArtPhotonAppId=" + value },
                out string parsed, out bool supplied), Is.False);
            Assert.That(parsed, Is.Null); Assert.That(supplied, Is.True);
        }

        private static GameObject NewVisualUnit(long species)
        {
            var source = new GameObject("FusionKidnapUnit", typeof(MeshFilter), typeof(MeshRenderer), typeof(UnitData));
            source.transform.position = new Vector3(2f, .8f, 3f);
            UnitData data = source.GetComponent<UnitData>(); data.specId = species; data.serverId = (1L << 32) | 1L;
            return source;
        }

        [Serializable] private sealed class AliasSpec { public long specId; }

        private static void Invoke(object target, string name)
            => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        private static object InvokeWith(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }
}
