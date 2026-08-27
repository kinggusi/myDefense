using System;
using System.Collections.Generic;
using MyDefense.Battle.Balance.Canonical;
using UnityEngine;

namespace MyDefense.Battle.Runtime
{
    [CreateAssetMenu(
        fileName = "PlanetContentCatalog",
        menuName = "MyDefense/Battle/Planet Content Catalog")]
    public sealed class PlanetContentCatalog : ScriptableObject
    {
        public const string ResourcesPath = "Battle/PlanetContent/PlanetContentCatalog";

        private static readonly string[] CanonicalMapIdValues =
        {
            "NEPTUNE",
            "URANUS",
            "SATURN",
            "JUPITER",
            "MARS",
            "EARTH",
            "VENUS",
            "MERCURY",
            "SUN"
        };

        private static readonly IReadOnlyList<string> ReadOnlyCanonicalMapIds =
            Array.AsReadOnly(CanonicalMapIdValues);

        [SerializeField] private List<PlanetContentProfile> _profiles = new();

        public static IReadOnlyList<string> CanonicalMapIds => ReadOnlyCanonicalMapIds;
        public IReadOnlyList<PlanetContentProfile> Profiles => _profiles;

        public bool TryResolve(string mapId, out PlanetContentProfile profile, out string error)
        {
            profile = null;
            IReadOnlyList<string> errors = PlanetContentValidator.ValidateCatalog(this);
            if (errors.Count > 0)
            {
                error = string.Join(Environment.NewLine + " - ", errors);
                return false;
            }

            if (string.IsNullOrWhiteSpace(mapId))
            {
                error = "Authoritative BattleSessionContext.MapId is required.";
                return false;
            }

            for (int index = 0; index < _profiles.Count; index++)
            {
                PlanetContentProfile candidate = _profiles[index];
                if (candidate != null && string.Equals(candidate.MapId, mapId, StringComparison.Ordinal))
                {
                    profile = candidate;
                    error = null;
                    return true;
                }
            }

            error = "Unknown or non-canonical PlanetContent mapId: '" + mapId + "'.";
            return false;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(IEnumerable<PlanetContentProfile> profiles)
        {
            _profiles = profiles == null
                ? new List<PlanetContentProfile>()
                : new List<PlanetContentProfile>(profiles);
        }
#endif
    }

    public static class PlanetContentValidator
    {
        public static IReadOnlyList<string> ValidateCatalogAgainstCanonical(
            PlanetContentCatalog catalog,
            CanonicalPlanetBattleRegistry canonicalPlanets)
        {
            var errors = new List<string>(ValidateCatalog(catalog));
            if (canonicalPlanets == null)
            {
                errors.Add("Canonical PlanetBattle registry is required.");
                return errors;
            }

            var canonicalIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<CanonicalPlanetBattle> planets = canonicalPlanets.All;
            for (int index = 0; index < planets.Count; index++)
            {
                CanonicalPlanetBattle planet = planets[index];
                if (planet == null || string.IsNullOrWhiteSpace(planet.MapId))
                {
                    errors.Add("Canonical PlanetBattle contains an invalid row at index " + index + ".");
                    continue;
                }
                if (!planet.Enabled)
                    errors.Add("Canonical PlanetBattle is disabled: '" + planet.MapId + "'.");
                if (!canonicalIds.Add(planet.MapId))
                    errors.Add("Canonical PlanetBattle contains duplicate mapId: '" + planet.MapId + "'.");
            }

            foreach (string expected in PlanetContentCatalog.CanonicalMapIds)
            {
                if (!canonicalIds.Contains(expected))
                    errors.Add("Canonical PlanetBattle is missing required mapId: '" + expected + "'.");
            }
            foreach (string actual in canonicalIds)
            {
                if (!ContainsOrdinal(PlanetContentCatalog.CanonicalMapIds, actual))
                    errors.Add("Canonical PlanetBattle contains unsupported mapId: '" + actual + "'.");
            }
            return errors;
        }

        public static IReadOnlyList<string> ValidateCatalog(PlanetContentCatalog catalog)
        {
            var errors = new List<string>();
            if (catalog == null)
            {
                errors.Add("PlanetContentCatalog is required.");
                return errors;
            }

            IReadOnlyList<PlanetContentProfile> profiles = catalog.Profiles;
            var byMapId = new Dictionary<string, PlanetContentProfile>(StringComparer.Ordinal);
            for (int index = 0; index < profiles.Count; index++)
            {
                PlanetContentProfile profile = profiles[index];
                if (profile == null)
                {
                    errors.Add("PlanetContentCatalog contains a null profile at index " + index + ".");
                    continue;
                }

                string mapId = profile.MapId;
                if (string.IsNullOrWhiteSpace(mapId))
                {
                    errors.Add("PlanetContentProfile mapId is required at index " + index + ".");
                    continue;
                }
                if (!string.Equals(mapId, mapId.Trim(), StringComparison.Ordinal))
                    errors.Add("PlanetContentProfile mapId must not contain surrounding whitespace: '" + mapId + "'.");
                if (!byMapId.TryAdd(mapId, profile))
                    errors.Add("Duplicate PlanetContentProfile mapId: '" + mapId + "'.");
                if (!profile.Enabled)
                    errors.Add("PlanetContentProfile is disabled: '" + mapId + "'.");

                ValidatePresentationPrefab(profile.EnvironmentPrefab, mapId + " environmentPrefab", errors);
                GameObject[] effects = profile.EnvironmentEffects;
                for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
                    ValidatePresentationPrefab(effects[effectIndex], mapId + " environmentEffect[" + effectIndex + "]", errors);
            }

            var canonicalIds = new HashSet<string>(PlanetContentCatalog.CanonicalMapIds, StringComparer.Ordinal);
            foreach (string canonicalMapId in PlanetContentCatalog.CanonicalMapIds)
            {
                if (!byMapId.ContainsKey(canonicalMapId))
                    errors.Add("PlanetContentCatalog is missing canonical mapId: '" + canonicalMapId + "'.");
            }

            foreach (string mapId in byMapId.Keys)
            {
                if (!canonicalIds.Contains(mapId))
                    errors.Add("PlanetContentCatalog contains non-canonical mapId: '" + mapId + "'.");
            }

            return errors;
        }

        public static IReadOnlyList<string> ValidatePresentationPrefab(GameObject prefab, string label = null)
        {
            var errors = new List<string>();
            ValidatePresentationPrefab(prefab, label ?? "Planet content prefab", errors);
            return errors;
        }

        private static void ValidatePresentationPrefab(GameObject prefab, string label, List<string> errors)
        {
            if (prefab == null)
            {
                errors.Add(label + " is required.");
                return;
            }

            PlanetEnvironmentContent marker = prefab.GetComponent<PlanetEnvironmentContent>();
            if (marker == null)
                errors.Add(label + " must have PlanetEnvironmentContent on its root.");
            if (prefab.GetComponentsInChildren<PlanetEnvironmentContent>(true).Length != 1)
                errors.Add(label + " must contain exactly one PlanetEnvironmentContent marker.");

            Component[] components = prefab.GetComponentsInChildren<Component>(true);
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component == null)
                {
                    errors.Add(label + " contains a missing script reference.");
                    continue;
                }

                Type type = component.GetType();
                if (!IsAllowedPresentationComponent(component))
                    errors.Add(label + " contains non-presentation component: " + type.FullName + ".");
            }
        }

        private static bool IsAllowedPresentationComponent(Component component)
        {
            return component is Transform
                || component is PlanetEnvironmentContent
                || component is Renderer
                || component is MeshFilter
                || component is ParticleSystem
                || component is ParticleSystemForceField
                || component is Animator
                || component is Animation
                || component is Light
                || component is AudioSource
                || component is LODGroup
                || component is SpriteMask;
        }

        private static bool ContainsOrdinal(IReadOnlyList<string> values, string candidate)
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (string.Equals(values[index], candidate, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
