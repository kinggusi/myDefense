using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MyDefense.Battle.Balance.Canonical;

namespace MyDefense.Battle
{
    /// <summary>
    /// Resolves the next-grade result from canonical alien-spec data.
    /// Legendary -> Mythic candidates and reroll policy are resolved from the
    /// canonical balance consumed by the State Authority.
    /// </summary>
    public static class BattleMergeResultResolver
    {
        private static readonly string[] Grades = { "NORMAL", "EPIC", "UNIQUE", "LEGEND", "MYTHIC" };

        public static bool TryResolveRandomNextGrade(byte sourceGrade, ulong seed, out long alienId, out byte resultGrade)
        {
            alienId = 0;
            resultGrade = 0;
            if (sourceGrade >= 3) return false;
            byte nextGrade = (byte)(sourceGrade + 1);
            var source = new StreamingAssetsCanonicalBalanceFileSource();
            if (!source.TryReadAllBytes("alien-spec.json", out byte[] bytes)) return false;
            string json = Encoding.UTF8.GetString(bytes);
            if (string.IsNullOrWhiteSpace(json)) return false;
            AlienSpecArray document;
            try { document = JsonUtility.FromJson<AlienSpecArray>("{\"items\":" + json + "}"); }
            catch (Exception) { return false; }
            AlienSpec[] candidates = (document?.items ?? Array.Empty<AlienSpec>())
                .Where(item => item != null && item.alienId > 0 && string.Equals(item.grade, Grades[nextGrade], StringComparison.Ordinal))
                .OrderBy(item => item.alienId)
                .ToArray();
            if (candidates.Length == 0) return false;
            int index = (int)(seed % (ulong)candidates.Length);
            alienId = candidates[index].alienId;
            resultGrade = nextGrade;
            return true;
        }

        public static bool TryResolveMythicCandidates(ulong seed, out long[] candidates)
            => TryResolveMythicCandidates(seed, Array.Empty<long>(), out candidates);

        public static bool TryResolveMythicCandidates(ulong seed, IReadOnlyCollection<long> previousCandidates, out long[] candidates)
        {
            candidates = null;
            var source = new StreamingAssetsCanonicalBalanceFileSource();
            if (!source.TryReadAllBytes("alien-spec.json", out byte[] bytes)) return false;
            if (!source.TryReadAllBytes("mythic-choice-balance.json", out byte[] choiceBytes)) return false;
            AlienSpecArray document;
            MythicChoiceDocument choice;
            try
            {
                document = JsonUtility.FromJson<AlienSpecArray>("{\"items\":" + Encoding.UTF8.GetString(bytes) + "}");
                choice = JsonUtility.FromJson<MythicChoiceDocument>(Encoding.UTF8.GetString(choiceBytes));
            }
            catch (Exception) { return false; }
            var excluded = new HashSet<long>(choice?.excludedAlienIds ?? Array.Empty<long>());
            long[] pool = (document?.items ?? Array.Empty<AlienSpec>())
                .Where(item => item != null && item.alienId > 0 && string.Equals(item.grade, "MYTHIC", StringComparison.Ordinal) && !excluded.Contains(item.alienId))
                .OrderBy(item => item.alienId)
                .Select(item => item.alienId)
                .ToArray();
            long[] available = pool.Where(id => previousCandidates == null || !previousCandidates.Contains(id)).ToArray();
            if (available.Length < 3) return false;
            int start = (int)(seed % (ulong)available.Length);
            candidates = new[] { available[start], available[(start + 1) % available.Length], available[(start + 2) % available.Length] };
            return true;
        }

        /// <summary>
        /// Returns the canonical mutation gate for a Mythic result. The current
        /// battle contract uses AlienSpec.isLocked as the release/unlock marker;
        /// per-player unlock snapshots can replace this lookup when entry
        /// snapshots become authoritative.
        /// </summary>
        public static bool TryGetMythicMutationEligibility(long alienId, out bool eligible)
        {
            eligible = false;
            if (alienId <= 0) return false;
            var source = new StreamingAssetsCanonicalBalanceFileSource();
            if (!source.TryReadAllBytes("alien-spec.json", out byte[] bytes)) return false;
            AlienSpecArray document;
            try
            {
                document = JsonUtility.FromJson<AlienSpecArray>("{\"items\":" + Encoding.UTF8.GetString(bytes) + "}");
            }
            catch (Exception)
            {
                return false;
            }

            AlienSpec spec = (document?.items ?? Array.Empty<AlienSpec>())
                .FirstOrDefault(item => item != null && item.alienId == alienId);
            if (spec == null || !string.Equals(spec.grade, "MYTHIC", StringComparison.Ordinal)) return false;
            eligible = !spec.isLocked;
            return true;
        }

        public static bool TryGetMythicRerollPolicy(out int freeCount, out int paidLimit, out int paidCost, out int timeoutSeconds)
        {
            freeCount = paidLimit = paidCost = timeoutSeconds = 0;
            var source = new StreamingAssetsCanonicalBalanceFileSource();
            if (!source.TryReadAllBytes("mythic-choice-balance.json", out byte[] bytes)) return false;
            try
            {
                MythicChoiceDocument document = JsonUtility.FromJson<MythicChoiceDocument>(Encoding.UTF8.GetString(bytes));
                MythicChoicePolicy policy = document?.mythicChoices?.FirstOrDefault();
                if (policy == null || !policy.enabled) return false;
                freeCount = policy.freeRerollCount;
                paidLimit = policy.paidRerollLimit;
                paidCost = policy.paidRerollCost;
                timeoutSeconds = policy.selectionTimeoutSeconds;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BattleMergeResultResolver] Mythic reroll policy load failed: {ex.Message}");
                return false;
            }
        }

        [Serializable] private sealed class AlienSpecArray { public AlienSpec[] items; }
        [Serializable] private sealed class AlienSpec { public long alienId; public string grade; public bool isLocked; }
        [Serializable] private sealed class MythicChoiceDocument { public long[] excludedAlienIds; public MythicChoicePolicy[] mythicChoices; }
        [Serializable] private sealed class MythicChoicePolicy
        {
            public int freeRerollCount;
            public int paidRerollLimit;
            public int paidRerollCost;
            public int selectionTimeoutSeconds;
            public bool enabled;
        }
    }
}
