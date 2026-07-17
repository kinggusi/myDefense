using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MyDefense.Battle.Balance
{
    public interface IBattleBalanceTextSource
    {
        bool TryLoad(string resourcePath, out string json);
    }

    public static class BattleBalanceResourcePaths
    {
        public const string Manifest = "Balance/Battle/battle-balance-manifest";
        public const string WaveSpec = "Balance/Battle/wave-spec";
        public const string WaveSpawnSpec = "Balance/Battle/wave-spawn-spec";
        public const string BossPatternSpec = "Balance/Battle/boss-pattern-spec";
        public const string SkillSpec = "Balance/Battle/skill-spec";
        public const string AlienSkillLinks = "Balance/Battle/alien-skill-links";
        public const string ProjectileSpec = "Balance/Battle/projectile-spec";
        public const string SkillEffectSpec = "Balance/Battle/skill-effect-spec";

        private static readonly IReadOnlyList<string> RequiredPathsValue = Array.AsReadOnly(new[]
        {
            WaveSpec, WaveSpawnSpec, BossPatternSpec, SkillSpec,
            AlienSkillLinks, ProjectileSpec, SkillEffectSpec
        });

        public static IReadOnlyList<string> RequiredDocumentPaths => RequiredPathsValue;

        public static bool HasFileExtension(string resourcePath)
        {
            return string.IsNullOrWhiteSpace(resourcePath) || Path.HasExtension(resourcePath);
        }
    }

    public sealed class ResourcesBattleBalanceTextSource : IBattleBalanceTextSource
    {
        public bool TryLoad(string resourcePath, out string json)
        {
            json = null;
            if (BattleBalanceResourcePaths.HasFileExtension(resourcePath)) return false;
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null) return false;
            json = asset.text;
            return !string.IsNullOrWhiteSpace(json);
        }
    }

    public sealed class InMemoryBattleBalanceTextSource : IBattleBalanceTextSource
    {
        private readonly Dictionary<string, string> _documents;
        private readonly List<string> _requestedPaths = new List<string>();

        public IReadOnlyList<string> RequestedPaths => _requestedPaths.AsReadOnly();

        public InMemoryBattleBalanceTextSource(IDictionary<string, string> documents)
        {
            _documents = documents == null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(documents, StringComparer.Ordinal);
        }

        public bool TryLoad(string resourcePath, out string json)
        {
            _requestedPaths.Add(resourcePath);
            json = null;
            if (BattleBalanceResourcePaths.HasFileExtension(resourcePath)) return false;
            return _documents.TryGetValue(resourcePath, out json) && !string.IsNullOrWhiteSpace(json);
        }
    }
}
