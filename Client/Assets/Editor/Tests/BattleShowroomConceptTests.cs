using System.Linq;
using MyDefense.Battle.Showroom;
using MyDefenseGame.Editor.FeatureTesting;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyDefenseGame.Editor.Tests
{
    public sealed class BattleShowroomConceptTests
    {
        private const string ArtDirectory = "Assets/Art/Battle/ShowroomConcepts/";

        [TestCase(BattleShowroomConcept.Diad, BattleShowroomConceptArt.Diad, 0)]
        [TestCase(BattleShowroomConcept.Mecharon, BattleShowroomConceptArt.Mecharon, 0)]
        [TestCase(BattleShowroomConcept.PuppeteerPlan1, BattleShowroomConceptArt.Puppeteer, 3)]
        [TestCase(BattleShowroomConcept.PuppeteerPlan2, BattleShowroomConceptArt.Puppeteer, 1)]
        public void ConceptsChooseTheirOwnArtAndPuppeteersShareTheAtlas(
            BattleShowroomConcept concept, BattleShowroomConceptArt art, int dolls)
        {
            BattleShowroomConceptDefinition definition = BattleShowroomConceptDefinition.For(concept);
            Assert.That(definition.Art, Is.EqualTo(art));
            Assert.That(definition.DollCount, Is.EqualTo(dolls));
        }

        [Test]
        public void DiadBasicAlternatesAndLoopsThroughTheSameTimeline()
        {
            var playback = new BattleShowroomConceptPlayback();
            playback.Select(BattleShowroomConcept.Diad);
            playback.Play(BattleShowroomConceptAction.Basic);
            Assert.That(playback.IsAlternateBasic, Is.False);
            playback.Advance(1.26f, loop: true, auto: false);
            Assert.That(playback.Action, Is.EqualTo(BattleShowroomConceptAction.Basic));
            Assert.That(playback.IsAlternateBasic, Is.True);
            playback.Advance(1.26f, loop: false, auto: false);
            Assert.That(playback.Action, Is.EqualTo(BattleShowroomConceptAction.Idle));
        }

        [Test]
        public void PlanOneKeepsItsFirstGroupBoundsDeploymentAndBurstsTheActiveField()
        {
            var playback = new BattleShowroomConceptPlayback();
            playback.Select(BattleShowroomConcept.PuppeteerPlan1);
            Assert.That(playback.ActiveDollCount, Is.EqualTo(3));
            playback.Advance(9.9f, loop: false, auto: true);
            Assert.That(playback.Action, Is.EqualTo(BattleShowroomConceptAction.Idle));
            playback.Advance(0.11f, loop: false, auto: true);
            Assert.That(playback.Action, Is.EqualTo(BattleShowroomConceptAction.Deploy));
            Assert.That(playback.IsAutomatic, Is.True);
            Assert.That(playback.ActiveDollCount, Is.EqualTo(6));
            Assert.That(playback.FirstDeployingDoll, Is.EqualTo(3));
            playback.Advance(1.26f, loop: false, auto: true);
            Assert.That(playback.ActiveDollCount, Is.EqualTo(6));
            playback.Play(BattleShowroomConceptAction.Basic);
            Assert.That(playback.ActiveDollCount, Is.EqualTo(6));
            playback.Play(BattleShowroomConceptAction.Move);
            Assert.That(playback.Repositioned, Is.True);
            playback.Play(BattleShowroomConceptAction.Skill);
            Assert.That(playback.DollsAtSkillStart, Is.EqualTo(6));
            playback.Advance(0.9f, loop: false, auto: true);
            Assert.That(playback.ActiveDollCount, Is.Zero);
        }

        [Test]
        public void PlanTwoAutomaticallyStartsApexSkillWithOnlyOneDoll()
        {
            var playback = new BattleShowroomConceptPlayback();
            playback.Select(BattleShowroomConcept.PuppeteerPlan2);
            playback.Advance(9.9f, loop: false, auto: true);
            Assert.That(playback.Action, Is.EqualTo(BattleShowroomConceptAction.Idle));
            playback.Advance(0.11f, loop: false, auto: true);
            Assert.That(playback.Action, Is.EqualTo(BattleShowroomConceptAction.Skill));
            Assert.That(playback.IsAutomatic, Is.True);
            Assert.That(playback.ActiveDollCount, Is.EqualTo(1));
            playback.Advance(2.11f, loop: true, auto: true);
            Assert.That(playback.Action, Is.EqualTo(BattleShowroomConceptAction.Idle));
            Assert.That(playback.ActiveDollCount, Is.EqualTo(1));
        }

        [Test]
        public void AutomaticCueResumesTheSelectedManualLoop()
        {
            var playback = new BattleShowroomConceptPlayback();
            playback.Select(BattleShowroomConcept.PuppeteerPlan2);
            playback.Play(BattleShowroomConceptAction.Move);
            playback.Advance(10.01f, loop: true, auto: true);
            Assert.That(playback.Action, Is.EqualTo(BattleShowroomConceptAction.Skill));
            Assert.That(playback.IsAutomatic, Is.True);
            playback.Advance(2.11f, loop: true, auto: true);
            Assert.That(playback.Action, Is.EqualTo(BattleShowroomConceptAction.Move));
            Assert.That(playback.IsAutomatic, Is.False);
        }

        [Test]
        public void PlanOneSkillLoopRefillsPreviewDollsBetweenBursts()
        {
            var playback = new BattleShowroomConceptPlayback();
            playback.Select(BattleShowroomConcept.PuppeteerPlan1);
            playback.Play(BattleShowroomConceptAction.Skill);
            playback.Advance(0.9f, loop: true, auto: false);
            Assert.That(playback.ActiveDollCount, Is.Zero);
            playback.Advance(1.3f, loop: true, auto: false);
            Assert.That(playback.Action, Is.EqualTo(BattleShowroomConceptAction.Skill));
            Assert.That(playback.DollsAtSkillStart, Is.EqualTo(3));
            playback.Advance(2.2f, loop: false, auto: false);
            Assert.That(playback.Action, Is.EqualTo(BattleShowroomConceptAction.Idle));
            Assert.That(playback.ActiveDollCount, Is.Zero);
        }

        [Test]
        public void FormationDollsChooseTheClosestUnassignedTarget()
        {
            Vector2[] targets = { new(0f, 1f), new(0f, 4f), new(6f, 0f) };
            Assert.That(BattleShowroomConceptPlayback.ClosestAvailableTarget(Vector2.zero, targets, 0), Is.EqualTo(0));
            Assert.That(BattleShowroomConceptPlayback.ClosestAvailableTarget(Vector2.zero, targets, 1), Is.EqualTo(1));
            Assert.That(BattleShowroomConceptPlayback.ClosestAvailableTarget(Vector2.zero, targets, 7), Is.EqualTo(-1));
        }

        [Test]
        public void PlanTwoThrowDoesNotOvershootItsBurstApex()
        {
            Vector2 home = new(3f, 18f);
            Vector2 apex = new(15f, 5f);
            Vector2 previous = home;
            for (int index = 1; index <= 20; index++)
            {
                Vector2 position = BattleShowroomConceptPlayback.MoveBetweenHomeAndApex(home, apex, index / 20f, rising: true);
                Assert.That(position.y, Is.LessThanOrEqualTo(previous.y));
                Assert.That(position.y, Is.GreaterThanOrEqualTo(apex.y));
                previous = position;
            }
            Assert.That(previous, Is.EqualTo(apex));
        }

        [Test]
        public void CharacterSceneOwnsBoardPoseAndEffectTexturesOutsideProduction()
        {
            string[] dependencies = AssetDatabase.GetDependencies(BattleShowroomScenes.Character, true);
            string[] productionScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToArray();
            string[] productionDependencies = AssetDatabase.GetDependencies(productionScenes, true);
            foreach (string art in new[] { "DiadBoardViews", "MecharonBoardViews", "PuppeteerBoardViews",
                         "PuppeteerDollBoardViews", "DiadBoardAttack", "MecharonBoardAttack",
                         "PuppeteerBoardAttack", "PuppeteerDollBoardAttack", "MecharonBoardEffects",
                         "PuppeteerBoardEffects", "SFBoardTiles", "DiadBoardEffects",
                         "DiadBoardWindup", "MecharonBoardWindup", "PuppeteerBoardWindup",
                         "PuppeteerDollBoardWindup", "DiadBoardRecover", "MecharonBoardRecover",
                         "PuppeteerBoardRecover", "PuppeteerDollBoardRecover" })
            {
                string path = ArtDirectory + art + ".png";
                Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(path), Is.Not.Null);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
                Assert.That(importer.isReadable, Is.False);
                Assert.That(dependencies, Does.Contain(path));
                Assert.That(productionDependencies, Does.Not.Contain(path));
            }
        }

        [Test]
        public void AtlasCutsThreeColumnsAndFindsTheActualFootInsteadOfUsingCropCenter()
        {
            const int width = 60, height = 24;
            var pixels = new Color32[width * height];
            for (int frame = 0; frame < 3; frame++)
            {
                int left = frame * 20;
                pixels[3 * width + left + 5] = new Color32(255, 255, 255, 255);
                pixels[12 * width + left + 15] = new Color32(255, 255, 255, 255);
            }
            RectInt[] frames = BattleShowroomTileAtlas.FindAlphaFrames(pixels, width, height);
            float[] feet = BattleShowroomTileAtlas.FindFootPivots(pixels, width, frames);
            Assert.That(frames.Length, Is.EqualTo(3));
            for (int index = 0; index < 3; index++)
            {
                Assert.That(frames[index].xMin, Is.GreaterThanOrEqualTo(index * 20));
                Assert.That(frames[index].xMax, Is.LessThanOrEqualTo((index + 1) * 20));
                Assert.That(feet[index], Is.LessThan(0.35f));
            }
        }

        [Test]
        public void PlanOneBasicStaggersIndependentStrikesWithoutDeployingDolls()
        {
            var playback = new BattleShowroomConceptPlayback();
            playback.Select(BattleShowroomConcept.PuppeteerPlan1);
            playback.Play(BattleShowroomConceptAction.Basic);
            Assert.That(playback.ActiveDollCount, Is.EqualTo(3));

            Vector2 station = new(20f, 17f);
            Vector2 target = new(100f, 17f);
            float strike = BattleShowroomConceptPlayback.DollStrikeImpact;
            float first = BattleShowroomConceptPlayback.DollStrikeAge(strike, 0);
            float second = BattleShowroomConceptPlayback.DollStrikeAge(strike, 1);
            Assert.That(BattleShowroomConceptPlayback.DollStrikePosition(station, target, first), Is.EqualTo(target));
            Assert.That(BattleShowroomConceptPlayback.DollStrikePosition(station, target, second), Is.Not.EqualTo(target));
            Assert.That(BattleShowroomConceptPlayback.DollStrikePosition(station, target, 1.1f), Is.EqualTo(station));
            playback.Play(BattleShowroomConceptAction.Deploy);
            Assert.That(playback.ActiveDollCount, Is.EqualTo(6));
            playback.Play(BattleShowroomConceptAction.Deploy);
            Assert.That(playback.FirstDeployingDoll, Is.EqualTo(6));
        }

        [Test]
        public void EffectAtlasKeepsTheSixArtworkCellsInAuthoringOrder()
        {
            const int width = 30, height = 20;
            var pixels = new Color32[width * height];
            for (int index = 0; index < 6; index++)
            {
                int x = (index % 3) * 10 + 5;
                int y = 15 - (index / 3) * 10;
                pixels[y * width + x] = new Color32(255, 255, 255, 255);
            }

            RectInt[] frames = BattleShowroomEffectAtlas.FindFrames(pixels, width, height);
            Assert.That(frames.Length, Is.EqualTo(6));
            for (int index = 0; index < frames.Length; index++)
                Assert.That(frames[index].Contains(new Vector2Int((index % 3) * 10 + 5, 15 - (index / 3) * 10)), Is.True);
        }

        [Test]
        public void EachFacingSendsAttacksAlongItsBoardHeading()
        {
            Assert.That(BattleShowroomConceptPreview.AttackHeading(BattleShowroomFacing.Front), Is.EqualTo(Vector3.back));
            Assert.That(BattleShowroomConceptPreview.AttackHeading(BattleShowroomFacing.Side), Is.EqualTo(Vector3.right));
            Assert.That(BattleShowroomConceptPreview.AttackHeading(BattleShowroomFacing.Rear), Is.EqualTo(Vector3.forward));
        }

        [TestCase(BattleShowroomConceptAction.Idle)]
        [TestCase(BattleShowroomConceptAction.Basic)]
        [TestCase(BattleShowroomConceptAction.Skill)]
        [TestCase(BattleShowroomConceptAction.Deploy)]
        public void CastAndIdleKeepTheActorRootPlanted(BattleShowroomConceptAction action)
        {
            foreach (float time in new[] { 0f, 0.19f, 0.5f, 0.88f, 1.4f })
                Assert.That(BattleShowroomConceptPlayback.BodyMotionOffset(action, time, Vector3.right),
                    Is.EqualTo(Vector3.zero));
            Assert.That(BattleShowroomConceptPlayback.BodyMotionOffset(BattleShowroomConceptAction.Move,
                0.7f, Vector3.right).x, Is.GreaterThan(0f));
            Assert.That(BattleShowroomConceptPlayback.BodyMotionOffset(BattleShowroomConceptAction.Move,
                1.4f, Vector3.right), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void DiadCastUsesFourDistinctPoseStages()
        {
            Assert.That(BattleShowroomConceptPlayback.PoseAtElapsed(BattleShowroomConcept.Diad,
                BattleShowroomConceptAction.Idle, 0f), Is.EqualTo(BattleShowroomConceptPose.Idle));
            Assert.That(BattleShowroomConceptPlayback.PoseAtElapsed(BattleShowroomConcept.Diad,
                BattleShowroomConceptAction.Skill, 0.2f), Is.EqualTo(BattleShowroomConceptPose.Windup));
            Assert.That(BattleShowroomConceptPlayback.PoseAtElapsed(BattleShowroomConcept.Diad,
                BattleShowroomConceptAction.Skill, 0.6f), Is.EqualTo(BattleShowroomConceptPose.Attack));
            Assert.That(BattleShowroomConceptPlayback.PoseAtElapsed(BattleShowroomConcept.Diad,
                BattleShowroomConceptAction.Skill, 1.2f), Is.EqualTo(BattleShowroomConceptPose.Recover));
        }

        [TestCase(50f)]
        [TestCase(60f)]
        [TestCase(80f)]
        public void FrontOverheadCameraBoundsContainEveryBoardCorner(float pitch)
        {
            var bounds = new Bounds(new Vector3(0.55f, 0.015f, 0.55f), new Vector3(6.76f, 0.24f, 4.56f));
            Matrix4x4 view = Matrix4x4.Rotate(Quaternion.Inverse(Quaternion.Euler(pitch, 0f, 0f)));
            Bounds projected = BattleShowroomConceptPreview.TransformBounds(bounds, view);
            projected.Expand(0.0001f);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                Assert.That(projected.Contains(view.MultiplyPoint3x4(corner)), Is.True);
            }
        }

        [Test]
        public void CameraFramingEntryPointKeepsBoardColumnsVerticalAtEveryElevation()
        {
            var root = new GameObject("Front overhead camera test");
            try
            {
                Camera camera = root.AddComponent<Camera>();
                var preview = root.AddComponent<BattleShowroomConceptPreview>();
                var serialized = new SerializedObject(preview);
                serialized.FindProperty("_camera").objectReferenceValue = camera;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                foreach (float elevation in new[] { 50f, 60f, 75f, 80f })
                {
                    preview.FrameTileCamera(0.9f, 1f, elevation);
                    Assert.That(camera.transform.forward.x, Is.EqualTo(0f).Within(0.000001f));
                    Assert.That(camera.transform.up.x, Is.EqualTo(0f).Within(0.000001f));
                    Assert.That(camera.transform.right.z, Is.EqualTo(0f).Within(0.000001f));
                }
                preview.FrameTileCamera(0.9f, 1f);
                Assert.That(camera.transform.eulerAngles.x, Is.EqualTo(60f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void VisualVariantValidationRequiresTheSameCanvasAndEveryAlphaByte()
        {
            var original = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var variant = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var resized = new Texture2D(3, 2, TextureFormat.RGBA32, false);
            try
            {
                var source = new[]
                {
                    new Color32(200, 140, 90, 0), new Color32(20, 80, 140, 64),
                    new Color32(40, 70, 90, 128), new Color32(180, 220, 40, 255)
                };
                original.SetPixels32(source);
                Color32[] candidate = source.Select(pixel => new Color32(12, 10, 25, pixel.a)).ToArray();
                variant.SetPixels32(candidate);
                Assert.That(BattleShowroomSceneBuilder.ValidateVisualVariant(original, variant, out _), Is.True);
                candidate[2].a--;
                variant.SetPixels32(candidate);
                Assert.That(BattleShowroomSceneBuilder.ValidateVisualVariant(original, variant, out string alphaIssue), Is.False);
                Assert.That(alphaIssue, Does.Contain("Alpha"));
                Assert.That(BattleShowroomSceneBuilder.ValidateVisualVariant(original, resized, out string sizeIssue), Is.False);
                Assert.That(sizeIssue, Does.Contain("Canvas"));
            }
            finally
            {
                Object.DestroyImmediate(original);
                Object.DestroyImmediate(variant);
                Object.DestroyImmediate(resized);
            }
        }

        [Test]
        public void LookTextureOverrideReusesTheOriginalFramesAndFootPivot()
        {
            var original = new Texture2D(24, 8, TextureFormat.RGBA32, false);
            var variant = new Texture2D(24, 8, TextureFormat.RGBA32, false);
            var resized = new Texture2D(25, 8, TextureFormat.RGBA32, false);
            Sprite originalPose = null;
            Sprite variantPose = null;
            try
            {
                var atlas = new BattleShowroomTileAtlas(original,
                    new[] { new RectInt(0, 0, 8, 8), new RectInt(8, 0, 8, 8), new RectInt(16, 0, 8, 8) },
                    new[] { 0.25f, 0.5f, 0.75f });
                originalPose = atlas.CreateSprite(BattleShowroomFacing.Front, 1.4f);
                variantPose = atlas.CreateSprite(BattleShowroomFacing.Front, 1.4f, variant);
                Assert.That(variantPose.texture, Is.SameAs(variant));
                Assert.That(originalPose.texture, Is.SameAs(original));
                Assert.That(variantPose.rect, Is.EqualTo(originalPose.rect));
                Assert.That(variantPose.pivot, Is.EqualTo(originalPose.pivot));
                Assert.That(variantPose.bounds, Is.EqualTo(originalPose.bounds));
                Assert.That(variantPose.pixelsPerUnit, Is.EqualTo(originalPose.pixelsPerUnit));
                Assert.That(atlas.CreateSprite(BattleShowroomFacing.Front, 1.4f, resized), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(originalPose);
                Object.DestroyImmediate(variantPose);
                Object.DestroyImmediate(original);
                Object.DestroyImmediate(variant);
                Object.DestroyImmediate(resized);
            }
        }

        [Test]
        public void LookSwitchAndOriginalReturnPreservePlaybackAndCamera()
        {
            var root = new GameObject("Look comparison test");
            var board = new Texture2D(2, 2);
            var unit = new Texture2D(3, 1) { name = "ComparisonUnit" };
            try
            {
                Camera camera = root.AddComponent<Camera>();
                var preview = root.AddComponent<BattleShowroomConceptPreview>();
                var serialized = new SerializedObject(preview);
                serialized.FindProperty("_camera").objectReferenceValue = camera;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                preview.ConfigureLookVariantsForEditor(board, new[] { unit }, string.Empty);
                var playback = (BattleShowroomConceptPlayback)typeof(BattleShowroomConceptPreview)
                    .GetField("_playback", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .GetValue(preview);
                playback.Select(BattleShowroomConcept.PuppeteerPlan1);
                playback.Play(BattleShowroomConceptAction.Basic);
                playback.Advance(0.41f, false, false);
                preview.SetFacing(BattleShowroomFacing.Rear);
                preview.FrameTileCamera(0.9f, 2.4f, 75f);
                Vector3 cameraPosition = camera.transform.position;
                Quaternion cameraRotation = camera.transform.rotation;
                float cameraSize = camera.orthographicSize;

                Assert.That(preview.SetLook(BattleShowroomLook.DarkBold), Is.True);
                Assert.That(preview.Look, Is.EqualTo(BattleShowroomLook.DarkBold));
                Assert.That(preview.SetLook(BattleShowroomLook.Original), Is.True);
                Assert.That(preview.Look, Is.EqualTo(BattleShowroomLook.Original));
                Assert.That(playback.Action, Is.EqualTo(BattleShowroomConceptAction.Basic));
                Assert.That(playback.Clock, Is.EqualTo(0.41f));
                Assert.That(playback.Elapsed, Is.EqualTo(0.41f));
                Assert.That(playback.ActiveDollCount, Is.EqualTo(3));
                Assert.That(camera.transform.position, Is.EqualTo(cameraPosition));
                Assert.That(camera.transform.rotation, Is.EqualTo(cameraRotation));
                Assert.That(camera.orthographicSize, Is.EqualTo(cameraSize));

                preview.ConfigureLookVariantsForEditor(null, new[] { unit }, "Comparison artwork is missing.");
                Assert.That(preview.SetLook(BattleShowroomLook.DarkBold), Is.False);
                Assert.That(preview.Look, Is.EqualTo(BattleShowroomLook.Original));
                Assert.That(preview.LookUnavailableReason, Does.Contain("missing"));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(board);
                Object.DestroyImmediate(unit);
            }
        }

        [Test]
        public void PlacementSettingsRoundTripNormalizeInvalidAndDuplicateCells()
        {
            var settings = new BattleShowroomCharacter.Settings
            {
                boardPlacement = true, placementBrush = BattleShowroomConcept.PuppeteerPlan2,
                selectedPlacementSlot = 23,
                placements = new System.Collections.Generic.List<BattleShowroomPlacement>
                {
                    new() { slot = 0, concept = BattleShowroomConcept.Diad },
                    new() { slot = 23, concept = BattleShowroomConcept.Mecharon, facing = BattleShowroomFacing.Rear },
                    new() { slot = 0, concept = BattleShowroomConcept.PuppeteerPlan1 },
                    new() { slot = 24, concept = BattleShowroomConcept.Diad },
                    new() { slot = -1, concept = BattleShowroomConcept.Diad },
                    new() { slot = 7, concept = BattleShowroomConcept.Canonical },
                    new() { slot = 8, concept = BattleShowroomConcept.Diad, facing = (BattleShowroomFacing)999 }
                }
            };
            var restored = JsonUtility.FromJson<BattleShowroomCharacter.Settings>(JsonUtility.ToJson(settings));
            var layout = BattleShowroomPlacement.Normalize(restored.placements, out int rejected);
            Assert.That(restored.boardPlacement, Is.True);
            Assert.That(restored.selectedPlacementSlot, Is.EqualTo(23));
            Assert.That(layout.Select(item => item.slot), Is.EqualTo(new[] { 0, 23 }));
            Assert.That(layout[0].concept, Is.EqualTo(BattleShowroomConcept.PuppeteerPlan1));
            Assert.That(layout[1].facing, Is.EqualTo(BattleShowroomFacing.Rear));
            Assert.That(rejected, Is.EqualTo(5));
            Assert.That(BattleShowroomPlacement.Normalize(null, out _), Is.Empty);
        }

        [Test]
        public void RealPlacementComponentsShareOneBoardAndCameraAndReleaseOnlyOwnedResources()
        {
            var scene = EditorSceneManager.OpenPreviewScene(BattleShowroomScenes.Character);
            try
            {
                var owner = scene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<BattleShowroomConceptPreview>(true)).Single();
                Camera camera = PreviewField<Camera>(owner, "_camera");
                MeshRenderer[] tiles = PreviewField<MeshRenderer[]>(owner, "_boardTiles");
                Texture2D originalArt = PreviewField<BattleShowroomTileAtlas>(owner, "_diad").Texture;
                owner.Select(BattleShowroomConcept.PuppeteerPlan1);
                AdvancePreviewFrame(owner);
                var solo = PreviewField<GameObject>(owner, "_stage").transform.Find("Concept Body").GetComponent<MeshRenderer>();
                Assert.That(solo.enabled, Is.True);
                owner.SetBoardPlacementMode(true);
                Assert.That(solo.enabled, Is.False);
                Vector3 cameraPosition = camera.transform.position;
                Quaternion cameraRotation = camera.transform.rotation;
                Assert.That(owner.GetPlacementTile(0), Is.SameAs(tiles[18]));
                Assert.That(owner.GetPlacementTile(5), Is.SameAs(tiles[23]));
                Assert.That(owner.GetPlacementTile(18), Is.SameAs(tiles[0]));
                Assert.That(owner.GetPlacementTile(23), Is.SameAs(tiles[5]));

                for (int slot = 0; slot < 24; slot++)
                    Assert.That(owner.PlaceUnit(new BattleShowroomPlacement
                    {
                        slot = slot, concept = (BattleShowroomConcept)(slot % 4 + 1)
                    }), Is.True);
                Assert.That(owner.PlacedUnitCount, Is.EqualTo(24));
                Assert.That(owner.PlaceUnit(new BattleShowroomPlacement { slot = 24 }), Is.False);
                Assert.That(owner.PlaceUnit(new BattleShowroomPlacement { slot = 0, concept = BattleShowroomConcept.Canonical }), Is.False);
                Assert.That(camera.transform.position, Is.EqualTo(cameraPosition));
                Assert.That(camera.transform.rotation, Is.EqualTo(cameraRotation));
                Assert.That(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Count(),
                    Is.EqualTo(1));
                foreach (BattleShowroomPlacement item in owner.SnapshotPlacements())
                {
                    BattleShowroomConceptPreview actor = owner.GetPlacedUnit(item.slot);
                    Assert.That(PreviewField<Camera>(actor, "_camera"), Is.SameAs(camera));
                    Assert.That(PreviewField<MeshRenderer[]>(actor, "_boardTiles"), Is.Null);
                    Assert.That(PreviewField<MeshRenderer>(actor, "_boardFrame"), Is.Null);
                    Assert.That(PreviewField<Material>(actor, "_boardMaterial"), Is.Null);
                }

                var first = owner.GetPlacedUnit(0);
                AdvancePreviewFrame(first);
                Mesh ownedMesh = first.transform.Find("Concept Body").GetComponent<MeshFilter>().sharedMesh;
                Assert.That(ownedMesh, Is.Not.Null);
                Assert.That(owner.PlaceUnit(new BattleShowroomPlacement { slot = 0, concept = BattleShowroomConcept.Mecharon }), Is.True);
                Assert.That(first == null, Is.True);
                Assert.That(ownedMesh == null, Is.True);
                Assert.That(owner.PlacedUnitCount, Is.EqualTo(24));
                var other = owner.GetPlacedUnit(23);
                owner.SetBoardPlacementMode(false);
                owner.SetBoardPlacementMode(true);
                Assert.That(owner.GetPlacedUnit(23), Is.SameAs(other));
                Assert.That(owner.PlacedUnitCount, Is.EqualTo(24));
                Assert.That(owner.RemovePlacedUnit(0), Is.True);
                Assert.That(owner.GetPlacedUnit(23), Is.SameAs(other));
                Assert.That(owner.PlacedUnitCount, Is.EqualTo(23));
                owner.ClearPlacedUnits();
                Assert.That(owner.PlacedUnitCount, Is.Zero);
                Assert.That(other == null, Is.True);
                Assert.That(camera != null && tiles[0] != null && originalArt != null, Is.True);

                var ownedMeshes = new System.Collections.Generic.List<Mesh>();
                var ownedMaterials = new System.Collections.Generic.List<Material>();
                int lineCount = 0;
                for (int slot = 0; slot < 24; slot++)
                {
                    Assert.That(owner.PlaceUnit(new BattleShowroomPlacement
                    {
                        slot = slot, concept = BattleShowroomConcept.PuppeteerPlan1
                    }), Is.True);
                    BattleShowroomConceptPreview puppet = owner.GetPlacedUnit(slot);
                    puppet.Play(BattleShowroomConceptAction.Deploy);
                    PreviewField<BattleShowroomConceptPlayback>(puppet, "_playback").Advance(0.75f, false, false);
                    AdvancePreviewFrame(puppet);
                    Assert.That(puppet.ActiveDollCount, Is.EqualTo(6));
                    LineRenderer[] lines = puppet.GetComponentsInChildren<LineRenderer>(true);
                    Assert.That(lines.Length, Is.EqualTo(18));
                    lineCount += lines.Length;
                    ownedMaterials.Add(PreviewField<Material>(puppet, "_reliefMaterial"));
                    for (int frame = 0; frame < 3; frame++)
                        AdvancePreviewFrame(puppet);
                    ownedMeshes.AddRange(PreviewField<System.Collections.Generic.Dictionary<Sprite, Mesh>>(puppet, "_meshes").Values);
                    Assert.That(puppet.GetComponentsInChildren<LineRenderer>(true).Length, Is.EqualTo(18));
                }
                Assert.That(lineCount, Is.EqualTo(432));
                Assert.That(owner.GetComponentsInChildren<LineRenderer>(true).Count(line => line.enabled), Is.EqualTo(432));
                owner.ClearPlacedUnits();
                Assert.That(ownedMeshes.All(mesh => mesh == null), Is.True);
                Assert.That(ownedMaterials.All(material => material == null), Is.True);
                Assert.That(PreviewField<Material>(owner, "_boardMaterial") != null, Is.True);
                Assert.That(camera != null && tiles[0] != null && originalArt != null, Is.True);
            }
            finally
            {
                ClosePlacementPreviewScene(scene);
            }
        }

        [Test]
        public void PlacedActorsPreservePlaybackFitTopRowAndKeepReadableBoundedRibbonPools()
        {
            var scene = EditorSceneManager.OpenPreviewScene(BattleShowroomScenes.Character);
            try
            {
                var owner = scene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<BattleShowroomConceptPreview>(true)).Single();
                Camera camera = PreviewField<Camera>(owner, "_camera");
                owner.SetBoardPlacementMode(true);
                foreach (int slot in new[] { 0, 5, 18, 23, 14 })
                    Assert.That(owner.PlaceUnit(new BattleShowroomPlacement
                    {
                        slot = slot, concept = BattleShowroomConcept.PuppeteerPlan1
                    }), Is.True);
                BattleShowroomConceptPreview actor = owner.GetPlacedUnit(14);
                actor.Play(BattleShowroomConceptAction.Basic);
                var playback = PreviewField<BattleShowroomConceptPlayback>(actor, "_playback");
                playback.Advance(0.35f, false, false);
                owner.SetPlacedFacing(14, BattleShowroomFacing.Rear, true);
                owner.SetPlacedPlaybackOptions(true, false);
                if (owner.IsDarkBoldAvailable)
                {
                    Assert.That(owner.SetLook(BattleShowroomLook.DarkBold), Is.True);
                    Assert.That(actor.Look, Is.EqualTo(BattleShowroomLook.DarkBold));
                    Assert.That(owner.SetLook(BattleShowroomLook.Original), Is.True);
                }
                Assert.That(playback.Clock, Is.EqualTo(0.35f));
                Assert.That(playback.Action, Is.EqualTo(BattleShowroomConceptAction.Basic));
                Assert.That(playback.ActiveDollCount, Is.EqualTo(3));
                Assert.That(owner.GetPlacedUnit(0).Facing, Is.EqualTo(BattleShowroomFacing.Rear));
                owner.FrameTileCamera(0.9f, 1f, 60f);
                foreach (int slot in new[] { 0, 5 })
                {
                    BattleShowroomConceptPreview edge = owner.GetPlacedUnit(slot);
                    AdvancePreviewFrame(edge);
                    Bounds body = edge.transform.Find("Concept Body").GetComponent<MeshRenderer>().bounds;
                    foreach (float x in new[] { body.min.x, body.max.x })
                    foreach (float y in new[] { body.min.y, body.max.y })
                    foreach (float z in new[] { body.min.z, body.max.z })
                    {
                        Vector3 viewport = camera.WorldToViewportPoint(new Vector3(x, y, z));
                        Assert.That(viewport.x, Is.InRange(-0.002f, 1.002f));
                        Assert.That(viewport.y, Is.InRange(-0.002f, 1.002f));
                    }
                }
                actor.Play(BattleShowroomConceptAction.Deploy);
                playback.Advance(0.71f, true, false);
                AdvancePreviewFrame(actor);
                LineRenderer[] ribbons = actor.GetComponentsInChildren<LineRenderer>(true);
                Assert.That(ribbons.Length, Is.EqualTo(18));
                Assert.That(ribbons.All(line => Mathf.Approximately(line.startWidth, line.endWidth)), Is.True);
                foreach (LineRenderer core in ribbons.Where(line => line.sortingOrder == 4))
                {
                    float pixels = core.startWidth * Mathf.Max(1, camera.pixelHeight) / (2f * camera.orthographicSize);
                    Assert.That(pixels, Is.EqualTo(1.6f).Within(0.001f));
                }
                for (int frame = 0; frame < 12; frame++)
                    AdvancePreviewFrame(actor);
                Assert.That(actor.GetComponentsInChildren<LineRenderer>(true).Length, Is.EqualTo(18));
            }
            finally
            {
                ClosePlacementPreviewScene(scene);
            }
        }

        private static T PreviewField<T>(BattleShowroomConceptPreview preview, string name)
            => (T)typeof(BattleShowroomConceptPreview).GetField(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(preview);

        private static void AdvancePreviewFrame(BattleShowroomConceptPreview preview)
            => typeof(BattleShowroomConceptPreview).GetMethod("Update",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(preview, null);

        private static void ClosePlacementPreviewScene(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (BattleShowroomConceptPreview preview in scene.GetRootGameObjects().SelectMany(root =>
                         root.GetComponentsInChildren<BattleShowroomConceptPreview>(true)))
                typeof(BattleShowroomConceptPreview).GetMethod("ReleaseOwnedResources",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(preview, null);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
