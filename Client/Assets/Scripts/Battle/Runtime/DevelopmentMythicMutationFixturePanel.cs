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

        public static bool CanStartValidationWave(
            bool isSpawned,
            bool isStateAuthority,
            bool isValidationArmed,
            bool isValidationStartConsumed)
            => isSpawned
                && isStateAuthority
                && isValidationArmed
                && !isValidationStartConsumed;

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

    public static class DevelopmentPartialSettlementFixtureRules
    {
        public static bool TryValidate(
            bool isSpawned,
            bool isStateAuthority,
            bool isP1ValidationArmed,
            MyDefense.Shared.Contracts.MatchState matchState,
            bool isWaveRunning,
            int currentWave,
            int highestClearedWave,
            int currentWaveSpawnCount,
            int currentWaveKillCount,
            int currentWaveUnresolvedSpawnCount,
            bool evidenceConsistent,
            out string reason)
        {
            if (!isSpawned)
                return Fail("Waiting for Fusion NetworkObject.Spawned().", out reason);
            if (!isStateAuthority)
                return Fail("Only State Authority may force the Development settlement failure.", out reason);
            if (isP1ValidationArmed)
                return Fail("P1VAL sessions suppress Settlement and cannot use this fixture.", out reason);
            if (matchState != MyDefense.Shared.Contracts.MatchState.RUNNING)
                return Fail("MatchState must be RUNNING.", out reason);
            if (!isWaveRunning)
                return Fail("The unfinished Wave must still be running.", out reason);
            if (currentWave <= 0 || currentWave != highestClearedWave + 1)
                return Fail("Current Wave must be exactly highestClearedWave + 1.", out reason);
            if (!evidenceConsistent)
                return Fail("Current Wave Kill evidence does not match its Spawn audit.", out reason);
            if (currentWaveSpawnCount <= 0)
                return Fail("The current Wave has no authoritative Spawn evidence.", out reason);
            if (currentWaveKillCount <= 0)
                return Fail("Kill at least one current-Wave Monster before forcing FAILED.", out reason);
            if (currentWaveUnresolvedSpawnCount <= 0)
                return Fail("At least one current-Wave Spawn must remain unresolved by the Kill audit.", out reason);

            reason = string.Empty;
            return true;
        }

        private static bool Fail(string message, out string reason)
        {
            reason = message;
            return false;
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
            const float panelHeight = 510f;
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

            BattleWaveExecutor executor = _authority.Executor;
            if (executor != null && executor.IsP1ValidationArmed)
            {
                bool canStartWave = DevelopmentMythicFixtureRules.CanStartValidationWave(
                    _authority.IsSpawnedForAccess,
                    _authority.IsAuthoritative,
                    executor.IsP1ValidationArmed,
                    executor.IsP1ValidationStartConsumed);
                previousEnabled = GUI.enabled;
                GUI.enabled = canStartWave;
                if (GUI.Button(new Rect(panel.x + 16f, panel.y + 304f, panelWidth - 32f, 44f),
                        $"Start P1 Wave {executor.P1ValidationTargetWave:D3} (Host only)"))
                {
                    _status = _authority.TryStartNextWave()
                        ? $"P1 Wave {executor.P1ValidationTargetWave:D3} started."
                        : "P1 Wave start was rejected by State Authority.";
                }
                GUI.enabled = previousEnabled;
            }

            previousEnabled = GUI.enabled;
            GUI.enabled = _authority.IsSpawnedForAccess && _authority.IsAuthoritative;
            if (GUI.Button(new Rect(panel.x + 16f, panel.y + 360f, panelWidth - 32f, 44f),
                    "Force FAILED for partial Settlement (Host only)"))
            {
                _status = _authority.TryForceDevelopmentPartialSettlementFailure(out string reason)
                    ? "FAILED committed from real partial-Wave Spawn/Kill evidence."
                    : reason;
            }
            GUI.enabled = previousEnabled;

            GUI.Label(new Rect(panel.x + 16f, panel.y + 416f, panelWidth - 32f, 76f),
                _authority.IsSpawnedForAccess
                    ? _status
                    : "Waiting for Fusion NetworkObject.Spawned().");
        }
    }
}
#endif
