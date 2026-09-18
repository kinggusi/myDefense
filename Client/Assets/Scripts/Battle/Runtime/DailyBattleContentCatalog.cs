using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyDefense.Battle.Runtime
{
    [CreateAssetMenu(
        fileName = "DailyBattleContentCatalog",
        menuName = "MyDefense/Battle/Daily Battle Content Catalog")]
    public sealed class DailyBattleContentCatalog : ScriptableObject
    {
        public const string ResourcesPath = "Battle/DailyContent/DailyBattleContentCatalog";

        private static readonly string[] RequiredMapIdValues =
        {
            DailyBattleExecutionPlanBuilder.CultivationMapId,
            DailyBattleExecutionPlanBuilder.MutationLabMapId
        };

        private static readonly IReadOnlyList<string> ReadOnlyRequiredMapIds =
            Array.AsReadOnly(RequiredMapIdValues);

        [SerializeField] private List<PlanetContentProfile> _profiles = new();

        public static IReadOnlyList<string> RequiredMapIds => ReadOnlyRequiredMapIds;
        public IReadOnlyList<PlanetContentProfile> Profiles => _profiles;

        public bool TryResolve(string authoritativeMapId, out PlanetContentProfile profile, out string error)
        {
            profile = null;
            IReadOnlyList<string> errors = DailyBattleContentValidator.ValidateCatalog(this);
            if (errors.Count > 0)
            {
                error = string.Join(Environment.NewLine + " - ", errors);
                return false;
            }
            if (string.IsNullOrWhiteSpace(authoritativeMapId))
            {
                error = "Authoritative Daily Battle mapId is required.";
                return false;
            }

            for (int index = 0; index < _profiles.Count; index++)
            {
                PlanetContentProfile candidate = _profiles[index];
                if (string.Equals(candidate.MapId, authoritativeMapId, StringComparison.Ordinal))
                {
                    profile = candidate;
                    error = null;
                    return true;
                }
            }

            error = "Unknown DailyBattleContent mapId: '" + authoritativeMapId + "'.";
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

    public static class DailyBattleContentValidator
    {
        public static IReadOnlyList<string> ValidateCatalog(DailyBattleContentCatalog catalog)
        {
            var errors = new List<string>();
            if (catalog == null)
            {
                errors.Add("DailyBattleContentCatalog is required.");
                return errors;
            }

            IReadOnlyList<PlanetContentProfile> profiles = catalog.Profiles;
            var byMapId = new Dictionary<string, PlanetContentProfile>(StringComparer.Ordinal);
            for (int index = 0; index < profiles.Count; index++)
            {
                PlanetContentProfile profile = profiles[index];
                if (profile == null)
                {
                    errors.Add("DailyBattleContentCatalog contains a null profile at index " + index + ".");
                    continue;
                }
                string mapId = profile.MapId;
                if (string.IsNullOrWhiteSpace(mapId))
                {
                    errors.Add("Daily Battle Profile mapId is required at index " + index + ".");
                    continue;
                }
                if (!string.Equals(mapId, mapId.Trim(), StringComparison.Ordinal))
                    errors.Add("Daily Battle Profile mapId must not contain surrounding whitespace: '" + mapId + "'.");
                if (!byMapId.TryAdd(mapId, profile))
                    errors.Add("Duplicate Daily Battle Profile mapId: '" + mapId + "'.");
                if (!profile.Enabled)
                    errors.Add("Daily Battle Profile is disabled: '" + mapId + "'.");

                errors.AddRange(PlanetContentValidator.ValidatePresentationPrefab(
                    profile.EnvironmentPrefab,
                    mapId + " environmentPrefab"));
                GameObject[] effects = profile.EnvironmentEffects;
                for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
                {
                    errors.AddRange(PlanetContentValidator.ValidatePresentationPrefab(
                        effects[effectIndex],
                        mapId + " environmentEffect[" + effectIndex + "]"));
                }
            }

            var required = new HashSet<string>(DailyBattleContentCatalog.RequiredMapIds, StringComparer.Ordinal);
            foreach (string mapId in DailyBattleContentCatalog.RequiredMapIds)
            {
                if (!byMapId.ContainsKey(mapId))
                    errors.Add("DailyBattleContentCatalog is missing required mapId: '" + mapId + "'.");
            }
            foreach (string mapId in byMapId.Keys)
            {
                if (!required.Contains(mapId))
                    errors.Add("DailyBattleContentCatalog contains unsupported mapId: '" + mapId + "'.");
            }
            if (profiles.Count != DailyBattleContentCatalog.RequiredMapIds.Count)
            {
                errors.Add("DailyBattleContentCatalog must contain exactly "
                    + DailyBattleContentCatalog.RequiredMapIds.Count + " Profiles.");
            }

            return errors;
        }
    }
}
