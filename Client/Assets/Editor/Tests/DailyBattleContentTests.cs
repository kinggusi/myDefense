using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyDefense.Battle.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MyDefense.Battle.Tests
{
    public sealed class DailyBattleContentTests
    {
        private DailyContentTestFactory _fixture;

        [SetUp]
        public void SetUp()
        {
            _fixture = new DailyContentTestFactory();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void Catalog_ContainsExactlyTheTwoApprovedDailyMaps()
        {
            Assert.That(DailyBattleContentCatalog.RequiredMapIds, Is.EqualTo(new[]
            {
                DailyBattleExecutionPlanBuilder.CultivationMapId,
                DailyBattleExecutionPlanBuilder.MutationLabMapId
            }));
            Assert.That(_fixture.Catalog.Profiles, Has.Count.EqualTo(2));
            Assert.That(DailyBattleContentValidator.ValidateCatalog(_fixture.Catalog), Is.Empty);
        }

        [Test]
        public void Catalog_FailsClosedForMissingDuplicateDisabledUnknownAndNullProfiles()
        {
            DailyBattleContentCatalog catalog = _fixture.Catalog;
            PlanetContentProfile cultivation = _fixture.Profiles[0];
            PlanetContentProfile mutation = _fixture.Profiles[1];

            catalog.ConfigureForEditor(new[] { cultivation });
            Assert.That(DailyBattleContentValidator.ValidateCatalog(catalog),
                Has.Some.Contains("missing required mapId"));

            catalog.ConfigureForEditor(new[] { cultivation, cultivation });
            Assert.That(DailyBattleContentValidator.ValidateCatalog(catalog),
                Has.Some.Contains("Duplicate"));

            mutation.ConfigureForEditor(
                mutation.MapId, false, mutation.EnvironmentPrefab, mutation.BackgroundSprite,
                mutation.BackgroundColor, mutation.EnvironmentMaterial, mutation.Lighting,
                mutation.EnvironmentEffects);
            catalog.ConfigureForEditor(new[] { cultivation, mutation });
            Assert.That(DailyBattleContentValidator.ValidateCatalog(catalog),
                Has.Some.Contains("disabled"));

            PlanetContentProfile unknown = _fixture.CreateProfile("DAILY_UNKNOWN", true, Color.red);
            catalog.ConfigureForEditor(new[] { cultivation, unknown });
            Assert.That(DailyBattleContentValidator.ValidateCatalog(catalog),
                Has.Some.Contains("unsupported mapId"));

            catalog.ConfigureForEditor(new PlanetContentProfile[] { cultivation, null });
            Assert.That(DailyBattleContentValidator.ValidateCatalog(catalog),
                Has.Some.Contains("null profile"));
        }

        [Test]
        public void Catalog_UnknownMapDoesNotResolveOrFallback()
        {
            Assert.That(_fixture.Catalog.TryResolve("DAILY_UNKNOWN", out PlanetContentProfile profile,
                out string error), Is.False);
            Assert.That(profile, Is.Null);
            Assert.That(error, Does.Contain("Unknown"));
        }

        [Test]
        public void Profiles_HaveDistinctEnvironmentEffectMaterialAndColors()
        {
            PlanetContentProfile cultivation = _fixture.Profiles.Single(
                value => value.MapId == DailyBattleExecutionPlanBuilder.CultivationMapId);
            PlanetContentProfile mutation = _fixture.Profiles.Single(
                value => value.MapId == DailyBattleExecutionPlanBuilder.MutationLabMapId);

            Assert.That(cultivation.EnvironmentPrefab, Is.Not.SameAs(mutation.EnvironmentPrefab));
            Assert.That(cultivation.EnvironmentEffects.Single(),
                Is.Not.SameAs(mutation.EnvironmentEffects.Single()));
            Assert.That(cultivation.EnvironmentMaterial, Is.Not.SameAs(mutation.EnvironmentMaterial));
            Assert.That(cultivation.BackgroundColor, Is.Not.EqualTo(mutation.BackgroundColor));
            Assert.That(cultivation.Lighting.AmbientColor, Is.Not.EqualTo(mutation.Lighting.AmbientColor));
        }

        [Test]
        public void GeneratedPlaceholders_HaveLargeDistinctNonParticleSilhouettesInSafeEdges()
        {
            const string catalogPath =
                "Assets/Resources/Battle/DailyContent/DailyBattleContentCatalog.asset";
            DailyBattleContentCatalog catalog =
                AssetDatabase.LoadAssetAtPath<DailyBattleContentCatalog>(catalogPath);
            Assert.That(catalog, Is.Not.Null,
                "Run DailyBattleContentAssetGenerator.GeneratePlaceholderContent before validation.");
            Assert.That(DailyBattleContentValidator.ValidateCatalog(catalog), Is.Empty);
            Assert.That(catalog.TryResolve(
                DailyBattleExecutionPlanBuilder.CultivationMapId,
                out PlanetContentProfile cultivation,
                out string error), Is.True, error);
            Assert.That(catalog.TryResolve(
                DailyBattleExecutionPlanBuilder.MutationLabMapId,
                out PlanetContentProfile mutation,
                out error), Is.True, error);

            Transform[] cultivationVisuals =
                cultivation.EnvironmentPrefab.GetComponentsInChildren<Transform>(true);
            Transform[] mutationVisuals =
                mutation.EnvironmentPrefab.GetComponentsInChildren<Transform>(true);
            Transform capsule = cultivationVisuals.Single(value => value.name == "CultivationCapsuleCore");
            Transform growthSeed = cultivationVisuals.Single(value => value.name == "CultivationGrowthSeed");
            Transform warningTop = mutationVisuals.Single(value => value.name == "MutationWarningFrame_Top");

            Assert.That(cultivationVisuals.Count(value =>
                value.name.StartsWith("CultivationCapsuleShell_", StringComparison.Ordinal)),
                Is.GreaterThanOrEqualTo(20));
            Assert.That(cultivationVisuals.Count(value =>
                value.name.StartsWith("CultivationGrowthRingOuter_", StringComparison.Ordinal)),
                Is.GreaterThanOrEqualTo(18));
            Assert.That(cultivationVisuals.Count(value =>
                value.name.StartsWith("CultivationOrganicCurve_", StringComparison.Ordinal)),
                Is.GreaterThanOrEqualTo(5));
            Assert.That(mutationVisuals.Count(value =>
                value.name.StartsWith("MutationCoil", StringComparison.Ordinal)),
                Is.GreaterThanOrEqualTo(44));
            Assert.That(mutationVisuals.Count(value =>
                value.name.StartsWith("MutationCrystal_", StringComparison.Ordinal)),
                Is.EqualTo(5));
            Assert.That(mutationVisuals.Count(value =>
                value.name.StartsWith("MutationElectricArc_", StringComparison.Ordinal)),
                Is.GreaterThanOrEqualTo(5));
            Assert.That(mutationVisuals.Count(value =>
                value.name.StartsWith("MutationWarningFrame_", StringComparison.Ordinal)),
                Is.EqualTo(3));

            Assert.That(capsule.localPosition.x, Is.LessThan(-7f));
            Assert.That(capsule.localPosition.z, Is.GreaterThan(1.5f));
            Assert.That(capsule.localScale.x, Is.GreaterThan(4f));
            Assert.That(growthSeed.localPosition.x, Is.GreaterThan(7f));
            Assert.That(warningTop.localPosition.z, Is.GreaterThan(4.5f));
            Assert.That(warningTop.localScale.x, Is.GreaterThan(20f));
            Assert.That(ReadMaterialColor(capsule.GetComponent<Renderer>().sharedMaterial).maxColorComponent,
                Is.GreaterThan(0.7f));
            Assert.That(ReadMaterialColor(warningTop.GetComponent<Renderer>().sharedMaterial).maxColorComponent,
                Is.GreaterThan(0.7f));
            Assert.That(cultivation.EnvironmentPrefab.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(mutation.EnvironmentPrefab.GetComponentsInChildren<Collider>(true), Is.Empty);
        }

        [Test]
        public void Catalog_RejectsBattleRuntimeComponentsInsideEnvironmentPrefab()
        {
            PlanetContentProfile cultivation = _fixture.Profiles[0];
            cultivation.EnvironmentPrefab.AddComponent<PathManager>();

            IReadOnlyList<string> errors = DailyBattleContentValidator.ValidateCatalog(_fixture.Catalog);

            Assert.That(errors, Has.Some.Contains("non-presentation component"));
            Assert.That(errors, Has.Some.Contains(typeof(PathManager).FullName));
        }

        [Test]
        public void Applicator_SwitchesDailyProfilesAndClearRestoresGlobalPresentation()
        {
            var root = new GameObject("DailyContentApplicator_Test");
            var cameraObject = new GameObject("DailyContentCamera_Test");
            var lightObject = new GameObject("DailyContentLight_Test");
            try
            {
                PlanetContentApplicator applicator = root.AddComponent<PlanetContentApplicator>();
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.backgroundColor = new Color(0.11f, 0.12f, 0.13f, 1f);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.yellow;
                light.intensity = 2.1f;
                light.transform.rotation = Quaternion.Euler(13f, 23f, 33f);
                Color ambient = new(0.21f, 0.22f, 0.23f, 1f);
                float ambientIntensity = 1.6f;
                RenderSettings.ambientLight = ambient;
                RenderSettings.ambientIntensity = ambientIntensity;
                applicator.ConfigureForTests(null, null, camera, light);

                Assert.That(_fixture.Catalog.TryResolve(
                    DailyBattleExecutionPlanBuilder.CultivationMapId,
                    out PlanetContentProfile cultivation, out string error), Is.True, error);
                Assert.That(applicator.TryApplyResolvedProfile(cultivation.MapId, cultivation, out error),
                    Is.True, error);
                GameObject first = applicator.ActiveEnvironment;
                Assert.That(first, Is.Not.Null);

                Assert.That(_fixture.Catalog.TryResolve(
                    DailyBattleExecutionPlanBuilder.MutationLabMapId,
                    out PlanetContentProfile mutation, out error), Is.True, error);
                Assert.That(applicator.TryApplyResolvedProfile(mutation.MapId, mutation, out error),
                    Is.True, error);
                Assert.That(applicator.ActiveEnvironment, Is.Not.SameAs(first));
                Assert.That(first == null, Is.True);
                Assert.That(camera.backgroundColor, Is.EqualTo(mutation.BackgroundColor));

                applicator.Clear();
                Assert.That(applicator.ActiveEnvironment, Is.Null);
                Assert.That(camera.backgroundColor, Is.EqualTo(new Color(0.11f, 0.12f, 0.13f, 1f)));
                Assert.That(light.color, Is.EqualTo(Color.yellow));
                Assert.That(light.intensity, Is.EqualTo(2.1f));
                Assert.That(light.transform.rotation.eulerAngles.x, Is.EqualTo(13f).Within(0.01f));
                Assert.That(RenderSettings.ambientLight, Is.EqualTo(ambient));
                Assert.That(RenderSettings.ambientIntensity, Is.EqualTo(ambientIntensity));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        [Test]
        public void Adapter_DailyMapMismatchFailsBeforePresentationApply()
        {
            var root = new GameObject("DailyContentAdapter_Test");
            try
            {
                BattleSceneSessionAdapter adapter = root.AddComponent<BattleSceneSessionAdapter>();
                PlanetContentApplicator applicator = root.AddComponent<PlanetContentApplicator>();
                SetField(adapter, "_dailyBattleContentCatalog", _fixture.Catalog);
                SetField(adapter, "_planetContentApplicator", applicator);
                MethodInfo apply = typeof(BattleSceneSessionAdapter).GetMethod(
                    "TryApplyDailyContent", BindingFlags.Instance | BindingFlags.NonPublic);
                object[] args =
                {
                    DailyBattleExecutionPlanBuilder.CultivationMapId,
                    DailyBattleExecutionPlanBuilder.MutationLabMapId,
                    null
                };

                Assert.That(apply, Is.Not.Null);
                Assert.That(apply.Invoke(adapter, args), Is.False);
                Assert.That(args[2], Does.Contain("does not match"));
                Assert.That(applicator.ActiveEnvironment, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Adapter_PostContentFailureRollsBackEnvironmentLightingCameraAndSoloState()
        {
            var owned = new List<GameObject>();
            var targets = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (string objectName in DailyBattleSoloPresentationController.PlayerTwoOnlyObjectNames)
            {
                var target = new GameObject(objectName);
                owned.Add(target);
                targets.Add(objectName, target);
            }
            targets["EnemyWaypointGroup"].SetActive(false);
            var root = new GameObject("DailyContentRollbackAdapter_Test");
            var cameraObject = new GameObject("DailyContentRollbackCamera_Test");
            var lightObject = new GameObject("DailyContentRollbackLight_Test");
            owned.Add(root);
            owned.Add(cameraObject);
            owned.Add(lightObject);
            try
            {
                BattleSceneSessionAdapter adapter = root.AddComponent<BattleSceneSessionAdapter>();
                DailyBattleSoloPresentationController solo =
                    root.AddComponent<DailyBattleSoloPresentationController>();
                PlanetContentApplicator applicator = root.AddComponent<PlanetContentApplicator>();
                Camera camera = cameraObject.AddComponent<Camera>();
                Color originalCamera = new(0.13f, 0.14f, 0.15f, 1f);
                camera.backgroundColor = originalCamera;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.cyan;
                light.intensity = 2.4f;
                light.transform.rotation = Quaternion.Euler(14f, 24f, 34f);
                Color originalAmbient = new(0.24f, 0.25f, 0.26f, 1f);
                float originalAmbientIntensity = 1.45f;
                RenderSettings.ambientLight = originalAmbient;
                RenderSettings.ambientIntensity = originalAmbientIntensity;
                applicator.ConfigureForTests(null, null, camera, light);
                Assert.That(_fixture.Catalog.TryResolve(
                    DailyBattleExecutionPlanBuilder.CultivationMapId,
                    out PlanetContentProfile profile,
                    out string error), Is.True, error);
                Assert.That(applicator.TryApplyResolvedProfile(profile.MapId, profile, out error),
                    Is.True, error);
                Assert.That(solo.SetSoloPlayerOneMode(true, out error), Is.True, error);
                Assert.That(solo.SoloEnabled, Is.True);
                Assert.That(applicator.ActiveEnvironment, Is.Not.Null);
                SetField(adapter, "_dailySoloPresentation", solo);
                SetField(adapter, "_planetContentApplicator", applicator);
                MethodInfo rollback = typeof(BattleSceneSessionAdapter).GetMethod(
                    "RollbackDailyPresentationAndFail",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                UnityEngine.TestTools.LogAssert.Expect(
                    LogType.Warning,
                    "[DailyBattle] initialization waiting/failed: forced post-content failure");

                Assert.That(rollback, Is.Not.Null);
                Assert.That(rollback.Invoke(adapter, new object[] { "forced post-content failure" }), Is.False);

                Assert.That(solo.SoloEnabled, Is.False);
                Assert.That(targets.Where(pair => pair.Key != "EnemyWaypointGroup")
                    .All(pair => pair.Value.activeSelf), Is.True);
                Assert.That(targets["EnemyWaypointGroup"].activeSelf, Is.False);
                Assert.That(applicator.ActiveEnvironment, Is.Null);
                Assert.That(applicator.ActiveMapId, Is.Null);
                Assert.That(camera.backgroundColor, Is.EqualTo(originalCamera));
                Assert.That(light.color, Is.EqualTo(Color.cyan));
                Assert.That(light.intensity, Is.EqualTo(2.4f));
                Assert.That(light.transform.rotation.eulerAngles.x, Is.EqualTo(14f).Within(0.01f));
                Assert.That(RenderSettings.ambientLight, Is.EqualTo(originalAmbient));
                Assert.That(RenderSettings.ambientIntensity, Is.EqualTo(originalAmbientIntensity));
            }
            finally
            {
                for (int index = owned.Count - 1; index >= 0; index--)
                {
                    if (owned[index] != null)
                        UnityEngine.Object.DestroyImmediate(owned[index]);
                }
            }
        }

        [Test]
        public void DailyCatalog_DoesNotChangeNinePlanetCatalogContract()
        {
            using var planetFixture = new PlanetContentTestFactory();
            Assert.That(PlanetContentCatalog.CanonicalMapIds, Has.Count.EqualTo(9));
            Assert.That(PlanetContentCatalog.CanonicalMapIds.Intersect(
                DailyBattleContentCatalog.RequiredMapIds, StringComparer.Ordinal), Is.Empty);
            Assert.That(PlanetContentValidator.ValidateCatalog(planetFixture.Catalog), Is.Empty);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static Color ReadMaterialColor(Material material)
        {
            Assert.That(material, Is.Not.Null);
            if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color")) return material.GetColor("_Color");
            Assert.Fail("Daily placeholder Material has no readable color property: " + material.name);
            return Color.black;
        }

        private sealed class DailyContentTestFactory : IDisposable
        {
            private readonly List<UnityEngine.Object> _owned = new();
            public DailyBattleContentCatalog Catalog { get; }
            public List<PlanetContentProfile> Profiles { get; } = new();

            public DailyContentTestFactory()
            {
                Catalog = ScriptableObject.CreateInstance<DailyBattleContentCatalog>();
                _owned.Add(Catalog);
                Profiles.Add(CreateProfile(
                    DailyBattleExecutionPlanBuilder.CultivationMapId,
                    true,
                    new Color(0.05f, 0.24f, 0.09f, 1f)));
                Profiles.Add(CreateProfile(
                    DailyBattleExecutionPlanBuilder.MutationLabMapId,
                    true,
                    new Color(0.22f, 0.04f, 0.32f, 1f)));
                Catalog.ConfigureForEditor(Profiles);
            }

            public PlanetContentProfile CreateProfile(string mapId, bool enabled, Color color)
            {
                var environment = new GameObject(mapId + "_Environment_Test");
                environment.AddComponent<PlanetEnvironmentContent>();
                var effect = new GameObject(mapId + "_Effect_Test");
                effect.AddComponent<PlanetEnvironmentContent>();
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
                Assert.That(shader, Is.Not.Null);
                var material = new Material(shader) { name = mapId + "_Material_Test" };
                var profile = ScriptableObject.CreateInstance<PlanetContentProfile>();
                profile.name = mapId;
                profile.ConfigureForEditor(
                    mapId,
                    enabled,
                    environment,
                    null,
                    color,
                    material,
                    new PlanetLightingSettings(
                        color, 0.8f, Color.Lerp(color, Color.white, 0.5f), 1f,
                        new Vector3(50f, -30f, 0f)),
                    new[] { effect });
                _owned.Add(environment);
                _owned.Add(effect);
                _owned.Add(material);
                _owned.Add(profile);
                return profile;
            }

            public void Dispose()
            {
                for (int index = _owned.Count - 1; index >= 0; index--)
                {
                    if (_owned[index] != null)
                        UnityEngine.Object.DestroyImmediate(_owned[index]);
                }
            }
        }
    }
}
