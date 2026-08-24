#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
using MyDefense.Battle;

namespace MyDefense.Battle.Runtime
{
    public static class DevelopmentMythicFixtureRules
    {
        public const string NoneMutation = "NONE";

        private static readonly string[] MutationIds =
        {
            NoneMutation, "GIANT", "BERSERK", "SWIFT", "TOXIC",
            "GREEDY", "OBESE", "FROZEN", "BLANK"
        };

        private static readonly HashSet<string> SupportedMutations =
            new(MutationIds, StringComparer.Ordinal);

        public static IReadOnlyList<string> Mutations => MutationIds;

        public static bool TryNormalize(
            long alienId,
            string mutationType,
            out string normalizedMutation,
            out string reason)
        {
            normalizedMutation = string.IsNullOrWhiteSpace(mutationType)
                ? NoneMutation
                : mutationType.Trim().ToUpperInvariant();

            if (!BattleMergeResultResolver.TryGetMythicMutationEligibility(alienId, out bool eligible))
            {
                reason = $"Alien {alienId} is not a canonical Mythic.";
                return false;
            }

            if (!eligible)
            {
                reason = $"Alien {alienId} is locked and cannot activate Mutation.";
                return false;
            }

            if (!SupportedMutations.Contains(normalizedMutation))
            {
                reason = $"Mutation {normalizedMutation} is not supported by the fixture.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// Development-only IMGUI panel for deterministic P1 Mutation/Snapshot validation.
    /// It is attached automatically when a BattleWaveStateAuthority exists, so no
    /// production Scene or Prefab serialization is required.
    /// </summary>
    public sealed class DevelopmentMythicMutationFixturePanel : MonoBehaviour
    {
        private const long DefaultMythicAlienId = 29;

        private BattleWaveStateAuthority _authority;
        private long _alienId = DefaultMythicAlienId;
        private string _alienIdText = DefaultMythicAlienId.ToString();
        private string _mutationType = DevelopmentMythicFixtureRules.NoneMutation;
        private string _status = "Select an unlocked Mythic and Mutation.";
        private bool _expanded;

        public void Initialize(BattleWaveStateAuthority authority)
        {
            _authority = authority;
        }

        private void Update()
        {
            if (_authority == null)
                _authority = FindFirstObjectByType<BattleWaveStateAuthority>(FindObjectsInactive.Include);
        }

        private void OnGUI()
        {
            if (_authority == null)
                return;

            Rect toggleRect = new Rect(12f, 12f, 220f, 40f);
            if (GUI.Button(toggleRect, _expanded ? "Hide P1 Fixture" : "Show P1 Fixture"))
                _expanded = !_expanded;
            if (!_expanded)
                return;

            const float panelWidth = 420f;
            const float panelHeight = 390f;
            Rect panel = new Rect(12f, 58f, panelWidth, panelHeight);
            GUI.Box(panel, "P1 MYTHIC / MUTATION FIXTURE");

            GUI.Label(new Rect(panel.x + 16f, panel.y + 36f, 110f, 28f), "Mythic ID");
            _alienIdText = GUI.TextField(new Rect(panel.x + 126f, panel.y + 34f, 100f, 30f), _alienIdText);
            if (GUI.Button(new Rect(panel.x + 236f, panel.y + 34f, 74f, 30f), "Apply ID"))
            {
                if (long.TryParse(_alienIdText, out long parsed) && parsed > 0)
                {
                    _alienId = parsed;
                    _status = $"Selected Mythic {_alienId}.";
                }
                else
                {
                    _status = "Mythic ID must be a positive number.";
                }
            }

            GUI.Label(new Rect(panel.x + 16f, panel.y + 75f, panelWidth - 32f, 24f),
                "Mutation (NONE creates a pure Mythic snapshot)");

            IReadOnlyList<string> mutations = DevelopmentMythicFixtureRules.Mutations;
            for (int index = 0; index < mutations.Count; index++)
            {
                int row = index / 3;
                int column = index % 3;
                string candidate = mutations[index];
                Rect button = new Rect(panel.x + 16f + column * 128f, panel.y + 104f + row * 42f, 118f, 34f);
                bool selected = string.Equals(_mutationType, candidate, StringComparison.Ordinal);
                if (GUI.Button(button, selected ? $"[{candidate}]" : candidate))
                    _mutationType = candidate;
            }

            bool previousEnabled = GUI.enabled;
            GUI.enabled = _authority.IsSpawnedForAccess;
            if (GUI.Button(new Rect(panel.x + 16f, panel.y + 244f, panelWidth - 32f, 48f),
                    $"Spawn Alien {_alienId} + {_mutationType}"))
            {
                if (!DevelopmentMythicFixtureRules.TryNormalize(
                        _alienId, _mutationType, out string normalized, out string reason))
                {
                    _status = reason;
                }
                else
                {
                    _authority.RequestDevelopmentMythicFixture(_alienId, normalized);
                    _status = "Fixture request sent to State Authority.";
                }
            }
            GUI.enabled = previousEnabled;

            GUI.Label(new Rect(panel.x + 16f, panel.y + 304f, panelWidth - 32f, 64f),
                _authority.IsSpawnedForAccess
                    ? _status
                    : "Waiting for Fusion NetworkObject.Spawned().");
        }
    }
}
#endif
