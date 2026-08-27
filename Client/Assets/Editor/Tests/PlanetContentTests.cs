using System;
using System.Collections.Generic;
using System.Linq;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyDefense.Battle.Tests
{
    public sealed class PlanetContentCatalogTests
    {
        private PlanetContentTestFactory _fixture;

        [SetUp]
        public void SetUp()
        {
            _fixture = new PlanetContentTestFactory();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void Resolve_UsesExactCanonicalMapIdWithoutFallback()
        {
            Assert.That(_fixture.Catalog.TryResolve("NEPTUNE", out PlanetContentProfile profile, out string error),
                Is.True, error);
            Assert.That(profile.MapId, Is.EqualTo("NEPTUNE"));

            Assert.That(_fixture.Catalog.TryResolve("neptune", out _, out error), Is.False);
            Assert.That(error, Does.Contain("Unknown or non-canonical"));
            Assert.That(_fixture.Catalog.TryResolve("UNKNOWN", out _, out _), Is.False);
            Assert.That(_fixture.Catalog.TryResolve(null, out _, out _), Is.False);
        }

        [Test]
        public void Validate_DisabledProfileFailsEntireCatalog()
        {
            _fixture.SetEnabled("EARTH", false);

            IReadOnlyList<string> errors = PlanetContentValidator.ValidateCatalog(_fixture.Catalog);

            Assert.That(errors, Has.Some.Contains("disabled: 'EARTH'"));
            Assert.That(_fixture.Catalog.TryResolve("NEPTUNE", out _, out _), Is.False,
                "A partially valid catalog must not apply a different planet as fallback.");
        }

        [Test]
        public void Validate_DuplicateProfileFailsEntireCatalog()
        {
            PlanetContentProfile duplicate = _fixture.CreateProfile("NEPTUNE", true);
            _fixture.Catalog.ConfigureForEditor(_fixture.Profiles.Concat(new[] { duplicate }));

            IReadOnlyList<string> errors = PlanetContentValidator.ValidateCatalog(_fixture.Catalog);

            Assert.That(errors, Has.Some.Contains("Duplicate PlanetContentProfile mapId: 'NEPTUNE'"));
        }

        [Test]
        public void Validate_MissingProfileFailsEntireCatalog()
        {
            _fixture.Catalog.ConfigureForEditor(
                _fixture.Profiles.Where(profile => profile.MapId != "SUN"));

            IReadOnlyList<string> errors = PlanetContentValidator.ValidateCatalog(_fixture.Catalog);

            Assert.That(errors, Has.Some.Contains("missing canonical mapId: 'SUN'"));
        }

        [Test]
        public void Validate_ProductionCanonicalRegistryMatchesAllNineProfilesExactly()
        {
            CanonicalPlanetBattleRegistry canonical = PlanetContentTestFactory.LoadCanonicalPlanets();

            IReadOnlyList<string> errors = PlanetContentValidator.ValidateCatalogAgainstCanonical(
                _fixture.Catalog,
                canonical);

            Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));
            Assert.That(canonical.All.Select(planet => planet.MapId),
                Is.EqualTo(PlanetContentCatalog.CanonicalMapIds));
        }

        [Test]
        public void ResourcesCatalogAssets_LoadNineReferencedProfilesAndPresentationPrefabs()
        {
            const string catalogPath =
                "Assets/Resources/Battle/PlanetContent/PlanetContentCatalog.asset";
            PlanetContentCatalog catalog =
                AssetDatabase.LoadAssetAtPath<PlanetContentCatalog>(catalogPath);
            Assert.That(catalog, Is.Not.Null, "Missing generated Resources catalog: " + catalogPath);

            CanonicalPlanetBattleRegistry canonical = PlanetContentTestFactory.LoadCanonicalPlanets();
            IReadOnlyList<string> errors =
                PlanetContentValidator.ValidateCatalogAgainstCanonical(catalog, canonical);
            Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));
            Assert.That(catalog.Profiles, Has.Count.EqualTo(9));

            foreach (string mapId in PlanetContentCatalog.CanonicalMapIds)
            {
                string profilePath =
                    "Assets/Resources/Battle/PlanetContent/Profiles/" + mapId + ".asset";
                string environmentPath =
                    "Assets/Prefabs/Battle/PlanetContent/" + mapId + "_Environment.prefab";
                string effectPath =
                    "Assets/Prefabs/Battle/PlanetContent/" + mapId + "_AmbientEffect.prefab";
                PlanetContentProfile profile =
                    AssetDatabase.LoadAssetAtPath<PlanetContentProfile>(profilePath);
                GameObject environment = AssetDatabase.LoadAssetAtPath<GameObject>(environmentPath);
                GameObject effect = AssetDatabase.LoadAssetAtPath<GameObject>(effectPath);

                Assert.That(profile, Is.Not.Null, "Missing Profile: " + profilePath);
                Assert.That(environment, Is.Not.Null, "Missing environment Prefab: " + environmentPath);
                Assert.That(effect, Is.Not.Null, "Missing effect Prefab: " + effectPath);
                Assert.That(profile.MapId, Is.EqualTo(mapId));
                Assert.That(profile.Enabled, Is.True);
                Assert.That(profile.EnvironmentPrefab, Is.SameAs(environment));
                Assert.That(profile.EnvironmentEffects, Has.Length.EqualTo(1));
                Assert.That(profile.EnvironmentEffects[0], Is.SameAs(effect));
                Assert.That(AssetDatabase.GetAssetPath(profile.EnvironmentMaterial),
                    Is.EqualTo("Assets/Materials/Battle/PlanetContent/" + mapId + "_Environment.mat"));
                Assert.That(PlanetContentValidator.ValidatePresentationPrefab(environment), Is.Empty);
                Assert.That(PlanetContentValidator.ValidatePresentationPrefab(effect), Is.Empty);
            }
        }

        [Test]
        public void Validate_EnvironmentPrefabRejectsBattleAndNetworkRuntimeComponents()
        {
            var forbidden = new GameObject("ForbiddenPlanetEnvironment");
            try
            {
                forbidden.AddComponent<PathManager>();

                IReadOnlyList<string> errors =
                    PlanetContentValidator.ValidatePresentationPrefab(forbidden, "fixture");

                Assert.That(errors, Has.Some.Contains(typeof(PathManager).FullName));
                Assert.That(errors, Has.Some.Contains("PlanetEnvironmentContent"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(forbidden);
            }
        }

        [Test]
        public void Validate_PresentationAllowlistAcceptsMarkerMeshAndParticlesOnly()
        {
            var root = new GameObject("AllowedPlanetEnvironment");
            var mesh = new GameObject("Mesh");
            try
            {
                root.AddComponent<PlanetEnvironmentContent>();
                mesh.transform.SetParent(root.transform, false);
                mesh.AddComponent<MeshFilter>();
                mesh.AddComponent<MeshRenderer>();
                root.AddComponent<ParticleSystem>();

                IReadOnlyList<string> errors =
                    PlanetContentValidator.ValidatePresentationPrefab(root, "allowed-fixture");

                Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Validate_MarkerDoesNotPermitArbitraryMonoBehaviour()
        {
            var root = new GameObject("MarkedButGameplayEnvironment");
            try
            {
                root.AddComponent<PlanetEnvironmentContent>();
                root.AddComponent<PlanetContentNonPresentationProbe>();

                IReadOnlyList<string> errors =
                    PlanetContentValidator.ValidatePresentationPrefab(root, "marked-gameplay-fixture");

                Assert.That(errors, Has.Some.Contains("non-presentation component"));
                Assert.That(errors, Has.Some.Contains(typeof(PlanetContentNonPresentationProbe).FullName));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }

    public sealed class PlanetContentApplicatorTests
    {
        private PlanetContentTestFactory _fixture;
        private GameObject _applicatorObject;
        private PlanetContentApplicator _applicator;

        [SetUp]
        public void SetUp()
        {
            _fixture = new PlanetContentTestFactory();
            _applicatorObject = new GameObject("PlanetContentApplicator_Test");
            _applicator = _applicatorObject.AddComponent<PlanetContentApplicator>();
            _applicator.ConfigureForTests(_fixture.Catalog);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_applicatorObject);
            _fixture.Dispose();
        }

        [Test]
        public void Apply_IsIdempotent_ReplacesOnMapChange_AndPreservesOnFailure()
        {
            CanonicalPlanetBattleRegistry canonical = PlanetContentTestFactory.LoadCanonicalPlanets();

            Assert.That(_applicator.TryApply("NEPTUNE", canonical, out string error), Is.True, error);
            GameObject first = _applicator.ActiveEnvironment;
            Assert.That(first, Is.Not.Null);

            Assert.That(_applicator.TryApply("NEPTUNE", canonical, out error), Is.True, error);
            Assert.That(_applicator.ActiveEnvironment, Is.SameAs(first));

            Assert.That(_applicator.TryApply("UNKNOWN", canonical, out error), Is.False);
            Assert.That(_applicator.ActiveEnvironment, Is.SameAs(first));
            Assert.That(_applicator.ActiveMapId, Is.EqualTo("NEPTUNE"));

            Assert.That(_applicator.TryApply("EARTH", canonical, out error), Is.True, error);
            Assert.That(_applicator.ActiveEnvironment, Is.Not.SameAs(first));
            Assert.That(first == null, Is.True, "The replaced environment must be destroyed in EditMode.");
            Assert.That(_applicator.ActiveMapId, Is.EqualTo("EARTH"));
        }

        [Test]
        public void Clear_RestoresCameraLightingAndAmbientState()
        {
            var cameraObject = new GameObject("PlanetContent_MainCamera");
            var lightObject = new GameObject("PlanetContent_DirectionalLight");
            try
            {
                cameraObject.tag = "MainCamera";
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.backgroundColor = new Color(0.12f, 0.13f, 0.14f, 1f);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.magenta;
                light.intensity = 2.25f;
                light.transform.rotation = Quaternion.Euler(11f, 22f, 33f);
                Color ambient = new(0.21f, 0.22f, 0.23f, 1f);
                float ambientIntensity = 1.7f;
                RenderSettings.ambientLight = ambient;
                RenderSettings.ambientIntensity = ambientIntensity;
                _applicator.ConfigureForTests(_fixture.Catalog, null, camera, light);

                Assert.That(_applicator.TryApply(
                    "NEPTUNE",
                    PlanetContentTestFactory.LoadCanonicalPlanets(),
                    out string error), Is.True, error);
                Assert.That(camera.backgroundColor, Is.Not.EqualTo(new Color(0.12f, 0.13f, 0.14f, 1f)));

                _applicator.Clear();

                Assert.That(camera.backgroundColor, Is.EqualTo(new Color(0.12f, 0.13f, 0.14f, 1f)));
                Assert.That(light.color, Is.EqualTo(Color.magenta));
                Assert.That(light.intensity, Is.EqualTo(2.25f));
                Assert.That(light.transform.rotation.eulerAngles.x, Is.EqualTo(11f).Within(0.01f));
                Assert.That(RenderSettings.ambientLight, Is.EqualTo(ambient));
                Assert.That(RenderSettings.ambientIntensity, Is.EqualTo(ambientIntensity));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }
    }

    public sealed class PlanetContentSessionGateTests
    {
        [Test]
        public void Adapter_UnknownMapFailsBeforeWaveSessionInitialization()
        {
            using var fixture = new PlanetContentTestFactory();
            var root = new GameObject("PlanetContentSessionGate_Test");
            var executorObject = new GameObject("PlanetContentSessionGate_Executor");
            try
            {
                BattleWaveExecutor executor = executorObject.AddComponent<BattleWaveExecutor>();
                BattleSceneSessionAdapter adapter = root.AddComponent<BattleSceneSessionAdapter>();
                PlanetContentApplicator applicator = root.AddComponent<PlanetContentApplicator>();
                applicator.ConfigureForTests(fixture.Catalog);
                SetPrivate(adapter, "_waveExecutor", executor);
                var session = new BattleSessionContext(
                    "unknown-map-session",
                    "canonical",
                    "canonical-hash",
                    "battle",
                    "battle-hash",
                    1,
                    "UNKNOWN");

                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                    "\\[PlanetContent\\] Battle initialization failed closed.*UNKNOWN"));
                Assert.That(adapter.Initialize(
                    session,
                    new BattlePlayerIdentityMap("p1", "p2"),
                    LaneType.Player1Lane), Is.False);

                Assert.That(adapter.IsInitialized, Is.False);
                Assert.That(executor.RuntimeSession, Is.Null);
                Assert.That(adapter.LastInitializationError, Does.Contain("Unknown or non-canonical"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(executorObject);
            }
        }

        [Test]
        public void Adapter_NetworkRunnerWaitsForSpawnedMapWhileOfflineFixtureDoesNot()
        {
            Assert.That(BattleSceneSessionAdapter.RequiresSpawnedAuthorityMap(true, true, false), Is.True);
            Assert.That(BattleSceneSessionAdapter.RequiresSpawnedAuthorityMap(true, true, true), Is.False);
            Assert.That(BattleSceneSessionAdapter.RequiresSpawnedAuthorityMap(false, true, false), Is.False,
                "Offline EditMode fixtures keep the existing local initialization path.");
            Assert.That(BattleSceneSessionAdapter.RequiresSpawnedAuthorityMap(true, false, false), Is.True,
                "A running Fusion session must not bypass the map gate while its authority component is unresolved.");
        }

        [Test]
        public void Adapter_AlreadyInitializedRaceStillWaitsForSpawnedAuthorityMap()
        {
            bool earlyReturnAllowed = !BattleSceneSessionAdapter.RequiresSpawnedAuthorityMap(
                runnerIsRunning: true,
                hasStateAuthorityComponent: true,
                stateAuthorityIsSpawned: false);

            Assert.That(earlyReturnAllowed, Is.False,
                "The IsInitialized fast path must retry until replicated authoritative map state is available.");
        }

        private static void SetPrivate(object target, string name, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }

    public sealed class PlanetContentNonPresentationProbe : MonoBehaviour
    {
    }

    internal sealed class PlanetContentTestFactory : IDisposable
    {
        private readonly List<UnityEngine.Object> _owned = new();

        public PlanetContentCatalog Catalog { get; }
        public List<PlanetContentProfile> Profiles { get; } = new();

        public PlanetContentTestFactory()
        {
            Catalog = ScriptableObject.CreateInstance<PlanetContentCatalog>();
            _owned.Add(Catalog);
            foreach (string mapId in PlanetContentCatalog.CanonicalMapIds)
                Profiles.Add(CreateProfile(mapId, true));
            Catalog.ConfigureForEditor(Profiles);
        }

        public PlanetContentProfile CreateProfile(string mapId, bool enabled)
        {
            var environment = new GameObject(mapId + "_TestEnvironment");
            environment.AddComponent<PlanetEnvironmentContent>();
            _owned.Add(environment);
            PlanetContentProfile profile = ScriptableObject.CreateInstance<PlanetContentProfile>();
            profile.name = mapId;
            profile.ConfigureForEditor(
                mapId,
                enabled,
                environment,
                null,
                MapColor(mapId),
                null,
                new PlanetLightingSettings(
                    MapColor(mapId),
                    0.75f,
                    Color.white,
                    0.8f,
                    new Vector3(45f, -25f, 0f)),
                Array.Empty<GameObject>());
            _owned.Add(profile);
            return profile;
        }

        public void SetEnabled(string mapId, bool enabled)
        {
            PlanetContentProfile profile = Profiles.Single(value => value.MapId == mapId);
            profile.ConfigureForEditor(
                profile.MapId,
                enabled,
                profile.EnvironmentPrefab,
                profile.BackgroundSprite,
                profile.BackgroundColor,
                profile.EnvironmentMaterial,
                profile.Lighting,
                profile.EnvironmentEffects);
        }

        public static CanonicalPlanetBattleRegistry LoadCanonicalPlanets()
        {
            CanonicalBalanceLoadResult result = CanonicalBalanceLoader.Load(
                new StreamingAssetsCanonicalBalanceFileSource(),
                new ExistingMonsterPrefabRuntimeMapping());
            Assert.That(result.IsValid, Is.True, string.Join(Environment.NewLine, result.Errors));
            Assert.That(result.Bundle?.PlanetBattles, Is.Not.Null);
            return result.Bundle.PlanetBattles;
        }

        public void Dispose()
        {
            for (int index = _owned.Count - 1; index >= 0; index--)
            {
                if (_owned[index] != null)
                    UnityEngine.Object.DestroyImmediate(_owned[index]);
            }
            _owned.Clear();
        }

        private static Color MapColor(string mapId)
        {
            int index = Math.Max(0, PlanetContentCatalog.CanonicalMapIds.ToList().IndexOf(mapId));
            float channel = 0.1f + index * 0.07f;
            return new Color(channel, 0.2f + index * 0.03f, 0.4f, 1f);
        }
    }
}
