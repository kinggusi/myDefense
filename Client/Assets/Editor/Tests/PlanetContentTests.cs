using System;
using System.Collections.Generic;
using System.Linq;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
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

                AssertGeneratedPresentationIsSafe(mapId, environment, effect);
                AssertPlanetFeatureSignature(mapId, environment);
            }
        }

        [Test]
        public void ResourcesPlanetPlaceholders_UseCameraSafeGutterAndHorizontalBackground()
        {
            foreach (string mapId in PlanetContentCatalog.CanonicalMapIds)
            {
                GameObject environment = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Battle/PlanetContent/" + mapId + "_Environment.prefab");
                Assert.That(environment, Is.Not.Null, mapId);

                Transform background = environment.transform.Find("PlanetBackground");
                Assert.That(background, Is.Not.Null, mapId + " needs an XZ background.");
                Assert.That(Mathf.DeltaAngle(background.localEulerAngles.x, 90f),
                    Is.EqualTo(0f).Within(0.1f), mapId + " background must face the top-down camera.");
                Assert.That(background.localPosition.y, Is.LessThan(-0.5f));

                Renderer[] renderers = environment.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.gameObject.name == "PlanetBackground")
                        continue;

                    Bounds bounds = renderer.bounds;
                    Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(4.9f),
                        mapId + "/" + renderer.name + " intrudes into the reserved Board/Lane region.");
                    Assert.That(bounds.max.x, Is.LessThanOrEqualTo(10.6f),
                        mapId + "/" + renderer.name + " can be cropped by a 4:3 camera.");
                }
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

        private static void AssertGeneratedPresentationIsSafe(
            string mapId,
            GameObject environment,
            GameObject effect)
        {
            Assert.That(environment.GetComponentsInChildren<Collider>(true), Is.Empty,
                mapId + " environment must be presentation-only and collider-free.");
            Assert.That(effect.GetComponentsInChildren<Collider>(true), Is.Empty,
                mapId + " effect must be presentation-only and collider-free.");

            foreach (GameObject root in new[] { environment, effect })
            {
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                Assert.That(behaviours.All(value => value is PlanetEnvironmentContent), Is.True,
                    mapId + " generated content contains a gameplay/network MonoBehaviour.");

                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Assert.That(renderer.sharedMaterial, Is.Not.Null,
                        mapId + "/" + renderer.name + " has no material and can render pink.");
                    Assert.That(renderer.sharedMaterial.shader, Is.Not.Null,
                        mapId + "/" + renderer.name + " has no shader.");
                    Assert.That(renderer.sharedMaterial.shader.name,
                        Is.Not.EqualTo("Hidden/InternalErrorShader"), mapId + "/" + renderer.name);
                    Assert.That(renderer.sharedMaterial.shader.isSupported, Is.True,
                        mapId + "/" + renderer.name + " uses an unsupported shader.");
                }
            }

            ParticleSystemRenderer particles = effect.GetComponent<ParticleSystemRenderer>();
            Assert.That(particles, Is.Not.Null, mapId + " effect needs a ParticleSystemRenderer.");
            Assert.That(AssetDatabase.GetAssetPath(particles.sharedMaterial),
                Is.EqualTo("Assets/Materials/Battle/PlanetContent/" + mapId + "_Particle.mat"));

            Material particleMaterial = particles.sharedMaterial;
            Assert.That(particleMaterial.GetTag("RenderType", false), Is.EqualTo("Transparent"), mapId);
            Assert.That(particleMaterial.renderQueue, Is.EqualTo((int)RenderQueue.Transparent), mapId);
            Assert.That(particleMaterial.HasProperty("_ZWrite"), Is.True, mapId);
            Assert.That(particleMaterial.GetFloat("_ZWrite"), Is.EqualTo(0f), mapId);
            Assert.That(particleMaterial.HasProperty("_DstBlend"), Is.True, mapId);
            Assert.That(particleMaterial.GetFloat("_DstBlend"),
                Is.EqualTo((float)BlendMode.OneMinusSrcAlpha), mapId);
            if (particleMaterial.HasProperty("_Mode"))
            {
                Assert.That(particleMaterial.GetFloat("_Mode"), Is.EqualTo(3f),
                    mapId + " built-in particle material must use Transparent mode.");
                Assert.That(particleMaterial.GetFloat("_SrcBlend"), Is.EqualTo((float)BlendMode.One), mapId);
                Assert.That(particleMaterial.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"), Is.True, mapId);
            }
        }

        private static void AssertPlanetFeatureSignature(string mapId, GameObject environment)
        {
            Transform[] transforms = environment.GetComponentsInChildren<Transform>(true);
            Assert.That(transforms.Count(value => value.name == "PlanetBody"), Is.EqualTo(1), mapId);

            switch (mapId)
            {
                case "NEPTUNE":
                    AssertPrefixCount(transforms, "StormBand_", 2, mapId);
                    AssertPrefixCount(transforms, "AtmosphereSegment_", 12, mapId);
                    break;
                case "URANUS":
                    AssertPrefixCount(transforms, "IceBand_", 1, mapId);
                    AssertPrefixCount(transforms, "PolarRingSegment_", 12, mapId);
                    break;
                case "SATURN":
                    AssertPrefixCount(transforms, "RingSegment_", 16, mapId);
                    AssertPrefixCount(transforms, "InnerRingSegment_", 12, mapId);
                    break;
                case "JUPITER":
                    AssertPrefixCount(transforms, "Band_", 4, mapId);
                    Assert.That(transforms.Any(value => value.name == "GreatRedSpot"), Is.True, mapId);
                    break;
                case "MARS":
                    AssertPrefixCount(transforms, "Crater_", 2, mapId);
                    Assert.That(transforms.Any(value => value.name == "PolarCap"), Is.True, mapId);
                    break;
                case "EARTH":
                    AssertPrefixCount(transforms, "Land_", 2, mapId);
                    AssertPrefixCount(transforms, "CloudBand_", 1, mapId);
                    break;
                case "VENUS":
                    AssertPrefixCount(transforms, "CloudBand_", 3, mapId);
                    AssertPrefixCount(transforms, "AtmosphereSegment_", 12, mapId);
                    break;
                case "MERCURY":
                    AssertPrefixCount(transforms, "Crater_", 3, mapId);
                    break;
                case "SUN":
                    AssertPrefixCount(transforms, "CoronaSegment_", 16, mapId);
                    AssertPrefixCount(transforms, "OuterCoronaSegment_", 16, mapId);
                    break;
                default:
                    Assert.Fail("Missing generated feature assertion for " + mapId);
                    break;
            }
        }

        private static void AssertPrefixCount(
            IEnumerable<Transform> transforms,
            string prefix,
            int minimum,
            string mapId)
        {
            Assert.That(transforms.Count(value => value.name.StartsWith(prefix, StringComparison.Ordinal)),
                Is.GreaterThanOrEqualTo(minimum), mapId + " missing visual feature " + prefix);
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

        [Test]
        public void Apply_PreservesExplicitAccentAndFillsOnlyBackgroundOrMissingMaterial()
        {
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            Assert.That(shader, Is.Not.Null);
            var fallback = new Material(shader) { name = "PlanetFallback_Test" };
            var accent = new Material(shader) { name = "PlanetAccent_Test" };
            PlanetContentProfile profile = _fixture.Profiles.Single(value => value.MapId == "NEPTUNE");
            GameObject source = profile.EnvironmentPrefab;
            var background = new GameObject("PlanetBackground");
            var explicitAccent = new GameObject("ExplicitAccent");
            var missingMaterial = new GameObject("MissingMaterial");
            try
            {
                background.transform.SetParent(source.transform, false);
                background.AddComponent<MeshFilter>();
                background.AddComponent<MeshRenderer>().sharedMaterial = accent;
                explicitAccent.transform.SetParent(source.transform, false);
                explicitAccent.AddComponent<MeshFilter>();
                explicitAccent.AddComponent<MeshRenderer>().sharedMaterial = accent;
                missingMaterial.transform.SetParent(source.transform, false);
                missingMaterial.AddComponent<MeshFilter>();
                missingMaterial.AddComponent<MeshRenderer>().sharedMaterial = null;
                profile.ConfigureForEditor(
                    profile.MapId,
                    profile.Enabled,
                    profile.EnvironmentPrefab,
                    profile.BackgroundSprite,
                    profile.BackgroundColor,
                    fallback,
                    profile.Lighting,
                    profile.EnvironmentEffects);

                Assert.That(_applicator.TryApply(
                    "NEPTUNE",
                    PlanetContentTestFactory.LoadCanonicalPlanets(),
                    out string error), Is.True, error);

                Renderer appliedBackground = _applicator.ActiveEnvironment.transform
                    .Find("PlanetBackground").GetComponent<Renderer>();
                Renderer appliedAccent = _applicator.ActiveEnvironment.transform
                    .Find("ExplicitAccent").GetComponent<Renderer>();
                Renderer appliedMissing = _applicator.ActiveEnvironment.transform
                    .Find("MissingMaterial").GetComponent<Renderer>();
                Assert.That(appliedBackground.sharedMaterial, Is.SameAs(fallback));
                Assert.That(appliedAccent.sharedMaterial, Is.SameAs(accent));
                Assert.That(appliedMissing.sharedMaterial, Is.SameAs(fallback));
            }
            finally
            {
                _applicator.Clear();
                UnityEngine.Object.DestroyImmediate(fallback);
                UnityEngine.Object.DestroyImmediate(accent);
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
