using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyDefense.Battle;
using MyDefense.Battle.Presentation;
using MyDefense.Battle.Runtime;
using MyDefense.Battle.Showroom;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MyDefenseGame.Editor.Tests
{
    public sealed class PlanetBattlefieldTests
    {
        private static PlanetContentProfile Profile(string map)
        {
            var profile = AssetDatabase.LoadAssetAtPath<PlanetContentProfile>(
                "Assets/Resources/Battle/PlanetContent/Profiles/" + map + ".asset");
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.RequiresBattlefieldTheme, Is.True, "Run GenerateBattlefieldContent first: " + map);
            return profile;
        }

        [Test]
        public void AllNineProfilesRequireTheirOwnTerrainAndExplicitOriginalOrMatteSurfaceRecipe()
        {
            var terrains = new HashSet<Texture2D>(); var atlases = new HashSet<Texture2D>();
            foreach (string map in PlanetContentCatalog.CanonicalMapIds)
            {
                PlanetContentProfile profile = Profile(map);
                Assert.That(profile.ValidateBattlefieldTheme(out string error), Is.True, error);
                Assert.That(profile.BackgroundSprite, Is.Null);
                Assert.That(profile.EnvironmentMaterial.mainTexture, Is.SameAs(profile.BattlefieldTerrain));
                Assert.That(terrains.Add(profile.BattlefieldTerrain), Is.True, "Terrain cannot silently reuse a different planet.");
                string terrainPath=AssetDatabase.GetAssetPath(profile.BattlefieldTerrain);
                bool detailed=terrainPath.StartsWith("Assets/Art/Battle/Planets/SFDetailBattlefields/",StringComparison.Ordinal);
                bool matte=detailed||terrainPath.StartsWith("Assets/Art/Battle/Planets/MatteBattlefields/",StringComparison.Ordinal);
                string folder=detailed?"Assets/Art/Battle/Planets/SFDetailBattlefields/":matte?"Assets/Art/Battle/Planets/MatteBattlefields/":"Assets/Art/Battle/Planets/Battlefields/";
                Assert.That(terrainPath,Is.EqualTo(folder+map+"_Terrain.png"));
                Assert.That(AssetDatabase.GetAssetPath(profile.BattlefieldSurfaces),Is.EqualTo(matte?"Assets/Art/Battle/Planets/MatteBattlefields/SF_MatteSurfaces.png":folder+map+"_Surfaces.png"));
                if(!matte)Assert.That(atlases.Add(profile.BattlefieldSurfaces), Is.True, "Original Surfaces cannot silently reuse a different planet.");
                foreach (Texture2D texture in new[] { profile.BattlefieldTerrain, profile.BattlefieldSurfaces })
                {
                    Assert.That(texture.mipmapCount, Is.GreaterThan(1), map + " needs mipmaps for steep minification.");
                    Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Trilinear));
                    Assert.That(texture.anisoLevel, Is.EqualTo(4));
                }
            }
            PlanetContentProfile venus = Profile("VENUS");
            bool matteVenus=AssetDatabase.GetAssetPath(venus.BattlefieldTerrain).Contains("/MatteBattlefields/")||AssetDatabase.GetAssetPath(venus.BattlefieldTerrain).Contains("/SFDetailBattlefields/");
            Assert.That(venus.BattlefieldAtlasSplit.y, Is.EqualTo(matteVenus ? .5f : 665f / 1254f).Within(.00001f));
            Rect tile = PlanetBattlefieldView.Cell(venus, 0, 1), lane = PlanetBattlefieldView.Cell(venus, 0, 0);
            Assert.That(tile.yMin, Is.GreaterThan(venus.BattlefieldAtlasSplit.y));
            Assert.That(lane.yMax, Is.LessThan(venus.BattlefieldAtlasSplit.y));
            Assert.That(tile.xMax, Is.LessThan(venus.BattlefieldAtlasSplit.x));
            Assert.That(PlanetBattlefieldView.Cell(venus, 1, 0).xMin, Is.GreaterThan(venus.BattlefieldAtlasSplit.x));
        }

        [Test]
        public void AuthoredLaneRibbonMapsUAcrossWidthVAlongTravelAndFacesUp()
        {
            Scene scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Battle.unity");
            var root = new GameObject("Lane UV regression"); SceneManager.MoveGameObjectToScene(root, scene);
            PlanetBattlefieldView view = null;
            try
            {
                PlanetContentProfile profile = Profile("NEPTUNE");
                view = new PlanetBattlefieldView(root.transform, profile, PlanetBattlefieldLayout.FromScene(scene));
                Mesh mesh = view.Root.transform.Find("Planet tiles lanes and borders").GetComponent<MeshFilter>().sharedMesh;
                Vector3[] vertices = mesh.vertices; Vector2[] uv = mesh.uv;
                int first = -1;
                for (int i = 0; i < vertices.Length; i += 4)
                    if (Enumerable.Range(i, 4).All(index => Mathf.Abs(view.Root.transform.TransformPoint(vertices[index]).y - .012f) < .00001f))
                    { first = i; break; }
                Assert.That(first, Is.GreaterThanOrEqualTo(0), "Find an actual Player1 Lane quad, excluding tiles and border.");
                Rect cell = PlanetBattlefieldView.Cell(profile, 0, 0);
                Assert.That(uv[first], Is.EqualTo(cell.min));
                Assert.That(uv[first + 1].x, Is.EqualTo(uv[first].x));
                Assert.That(uv[first + 1].y, Is.EqualTo(cell.yMax));
                Assert.That(uv[first + 2], Is.EqualTo(cell.max));
                Assert.That(uv[first + 3].x, Is.EqualTo(cell.xMax));
                Assert.That(uv[first + 3].y, Is.EqualTo(uv[first].y));
                Assert.That(Vector3.Dot((vertices[first + 1] - vertices[first]).normalized, Vector3.forward),
                    Is.GreaterThan(.99f), "V follows the first authored northbound segment; rails must not turn into crossbars.");
                Assert.That(mesh.normals[first].y, Is.GreaterThan(.99f), "The road surface winding must face upward.");
            }
            finally { view?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MissingRequiredTextureFailsBeforeCreatingAnyPresentationInsteadOfFallingBack(bool missingTerrain)
        {
            PlanetContentProfile original = Profile("NEPTUNE");
            PlanetContentProfile broken = Object.Instantiate(original);
            var root = new GameObject("Missing battlefield art");
            var applicator = root.AddComponent<PlanetContentApplicator>();
            try
            {
                broken.ConfigureBattlefieldForEditor(true, missingTerrain ? null : original.BattlefieldTerrain,
                    missingTerrain ? original.BattlefieldSurfaces : null);
                Assert.That(applicator.TryApplyResolvedProfile("NEPTUNE", broken, out string error), Is.False);
                Assert.That(error, Does.Contain("no SF/background fallback"));
                Assert.That(applicator.ActiveEnvironment, Is.Null); Assert.That(applicator.ActiveBattlefield, Is.Null);
            }
            finally { applicator.Clear(); Object.DestroyImmediate(root); Object.DestroyImmediate(broken); }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void MissingOrTooShortPathFailsBeforeAllocatingGround(int missingLane)
        {
            var slots = Enumerable.Range(0, 24).Select(i => new Vector3(i % 6, 0f, i / 6)).ToArray();
            Vector3[][] paths = { new[] { Vector3.zero, Vector3.right }, new[] { Vector3.forward, Vector3.one },
                new[] { Vector3.left, Vector3.right } };
            paths[missingLane] = missingLane == 0 ? null : new[] { Vector3.zero };
            Assert.Throws<ArgumentException>(() => new PlanetBattlefieldLayout(slots, slots, paths[0], paths[1], paths[2]));
        }

        [Test]
        public void ActualBattleThemePreservesBoardsPathsPhysicsAndRestoresSfUnderlayAcrossReplacementFailureAndClear()
        {
            Scene scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Battle.unity");
            var root = new GameObject("Battlefield lifecycle test"); SceneManager.MoveGameObjectToScene(root, scene);
            var applicator = root.AddComponent<PlanetContentApplicator>();
            var baseline = root.AddComponent<BattleBoardSurfaceView>();
            PlanetContentProfile invalid = null, legacy = null;
            try
            {
                Transform p1 = scene.GetRootGameObjects().Single(go => go.name == "GridManager").transform;
                Transform p2 = scene.GetRootGameObjects().Single(go => go.name == "EnemyGridParent").transform;
                Transform path = scene.GetRootGameObjects().Single(go => go.GetComponent<PathManager>() != null).transform;
                Camera camera = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Camera>(true)).First();
                applicator.ConfigureForTests(null, null, camera);
                Assert.That(baseline.Configure(p1, p2, Resources.Load<BattleUnitArtCatalog>(BattleUnitArtCatalog.ResourcePath)), Is.True);
                var unchanged = p1.GetComponentsInChildren<Transform>(true).Concat(p2.GetComponentsInChildren<Transform>(true))
                    .Concat(path.GetComponentsInChildren<Transform>(true)).ToArray();
                Vector3[] positions = unchanged.Select(item => item.position).ToArray();
                Quaternion[] rotations = unchanged.Select(item => item.rotation).ToArray();
                Collider[] colliders = unchanged.SelectMany(item => item.GetComponents<Collider>()).ToArray();
                bool[] collisionStates = colliders.Select(item => item.enabled).ToArray();
                var sf = unchanged.SelectMany(item => item.GetComponents<MeshRenderer>()).Where(item => item.name == "Painted tile top").ToArray();
                Assert.That(sf.Length, Is.EqualTo(48));
                Assert.That(applicator.TryApplyResolvedProfile("NEPTUNE", Profile("NEPTUNE"), out string error), Is.True, error);
                PlanetBattlefieldView first = applicator.ActiveBattlefield;
                Assert.That(first.TileCount, Is.EqualTo(48));
                Assert.That(first.Root.GetComponentsInChildren<MeshRenderer>().Length, Is.EqualTo(2), "Two additional combined renderers; the existing SF underlay is retained.");
                Assert.That(first.Root.GetComponentsInChildren<Collider>(), Is.Empty);
                Assert.That(first.Root.GetComponentsInChildren<Fusion.NetworkObject>(), Is.Empty);
                Assert.That(applicator.ActiveEnvironment.transform.Find("PlanetBackgroundSprite"), Is.Null);
                Mesh[] firstMeshes = first.Root.GetComponentsInChildren<MeshFilter>().Select(item => item.sharedMesh).ToArray();
                Material[] firstMaterials = first.Root.GetComponentsInChildren<MeshRenderer>().Select(item => item.sharedMaterial).ToArray();
                Assert.That(applicator.TryApplyResolvedProfile("NEPTUNE", Profile("NEPTUNE"), out error), Is.True, error);
                Assert.That(applicator.ActiveBattlefield, Is.SameAs(first));
                Assert.That(applicator.TryApplyResolvedProfile("MARS", Profile("MARS"), out error), Is.True, error);
                Assert.That(firstMeshes.All(mesh => mesh == null), Is.True);
                Assert.That(firstMaterials.All(material => material == null), Is.True);
                PlanetBattlefieldView mars = applicator.ActiveBattlefield;
                invalid = Object.Instantiate(Profile("MARS")); invalid.ConfigureBattlefieldForEditor(true, null, invalid.BattlefieldSurfaces);
                Assert.That(applicator.TryApplyResolvedProfile("MARS", invalid, out _), Is.False);
                Assert.That(applicator.ActiveBattlefield, Is.SameAs(mars), "Invalid replacement preserves the complete previous theme.");
                legacy = Object.Instantiate(Profile("MARS")); legacy.ConfigureBattlefieldForEditor(false, null, null);
                Assert.That(applicator.TryApplyResolvedProfile("MARS", legacy, out error), Is.True, error);
                Assert.That(applicator.ActiveBattlefield, Is.Null); Assert.That(mars.Root == null, Is.True);
                Assert.That(sf.All(renderer => renderer != null && renderer.enabled), Is.True);
                for (int i = 0; i < unchanged.Length; i++)
                { Assert.That(unchanged[i].position, Is.EqualTo(positions[i])); Assert.That(unchanged[i].rotation, Is.EqualTo(rotations[i])); }
                for (int i = 0; i < colliders.Length; i++) Assert.That(colliders[i].enabled, Is.EqualTo(collisionStates[i]));
                Assert.That(applicator.TryApplyResolvedProfile("NEPTUNE", Profile("NEPTUNE"), out error), Is.True, error);
                PlanetBattlefieldView final = applicator.ActiveBattlefield;
                applicator.Clear(); Assert.That(final.Root == null, Is.True); Assert.That(sf.All(renderer => renderer.enabled), Is.True);
                Assert.That(Profile("NEPTUNE").BattlefieldTerrain, Is.Not.Null, "Owned cleanup must never destroy source assets.");
            }
            finally
            {
                applicator.Clear(); baseline.ReleaseSurfaces();
                if (invalid != null) Object.DestroyImmediate(invalid); if (legacy != null) Object.DestroyImmediate(legacy);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void ContentShowroomUsesTheSameThemeWithFourNonCombatUnitsAndSixtyDegreePreview()
        {
            const string key = "MyDefense.BattleShowroom.v1.Content";
            bool hadSettings = PlayerPrefs.HasKey(key); string prior = PlayerPrefs.GetString(key);
            Scene scene = EditorSceneManager.OpenPreviewScene(BattleShowroomScenes.Content);
            var content = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<BattleShowroomContent>(true)).Single();
            var applicator = content.GetComponent<PlanetContentApplicator>();
            try
            {
                BattleShowroomPreferences.Save("Content", new BattleShowroomContent.Settings
                    { mapId = "NEPTUNE", showGeometry = false, showComparisonUnits = true });
                Invoke(content, "Start"); Invoke(content, "LateUpdate");
                Assert.That(applicator.ActiveBattlefield, Is.Not.Null);
                Assert.That(applicator.ActiveBattlefield.TileCount, Is.EqualTo(48));
                BattleUnitArtView[] units = content.GetComponentsInChildren<BattleUnitArtView>();
                Assert.That(units.Length, Is.EqualTo(4));
                Assert.That(units.Select(unit => unit.Visual), Is.EquivalentTo(Enum.GetValues(typeof(BattleUnitVisual))));
                Assert.That(units.Single(unit => unit.Visual == BattleUnitVisual.Diad).transform.position.z
                    - units.Single(unit => unit.Visual == BattleUnitVisual.Puppeteer1).transform.position.z, Is.GreaterThan(2f));
                Assert.That(units.Single(unit => unit.Visual == BattleUnitVisual.Mecharon).transform.position.z
                    - units.Single(unit => unit.Visual == BattleUnitVisual.Puppeteer2).transform.position.z, Is.GreaterThan(2f),
                    "Comparison rows need enough ground separation for the elevated body silhouettes.");
                Assert.That(units.All(unit => unit.GetComponent<UnitAttack>() == null && unit.GetComponent<UnitData>().specId == 0), Is.True);
                Assert.That(scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Fusion.NetworkRunner>(true)), Is.Empty);
                Camera camera = (Camera)typeof(BattleShowroomContent).GetField("_battleCamera", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(content);
                Assert.That(Mathf.DeltaAngle(camera.transform.eulerAngles.x, 60f), Is.EqualTo(0f).Within(.01f));
                Assert.That(Mathf.DeltaAngle(camera.transform.eulerAngles.y, 0f), Is.EqualTo(0f).Within(.01f));
                var settings = (BattleShowroomContent.Settings)typeof(BattleShowroomContent).GetField("_settings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(content);
                Assert.That(settings.showGeometry, Is.False);
                BattleUnitArtCatalog artwork = units[0].Catalog;
                settings.mapId = "MARS"; Assert.That(Invoke(content, "ApplyMap"), Is.EqualTo(true));
                Assert.That(content.GetComponentsInChildren<BattleUnitArtView>().All(unit => unit.Catalog == artwork), Is.True);
            }
            finally
            {
                Invoke(content, "ClearComparisonUnits"); applicator.Clear(); EditorSceneManager.ClosePreviewScene(scene);
                if (hadSettings) PlayerPrefs.SetString(key, prior); else PlayerPrefs.DeleteKey(key); PlayerPrefs.Save();
            }
        }

        private static object Invoke(object target, string name)
            => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
}
