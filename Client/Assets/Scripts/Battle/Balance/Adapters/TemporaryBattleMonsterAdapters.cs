using System;
using System.Collections.Generic;
using System.Text;
using MyDefense.Battle.Balance.Canonical;
using UnityEngine;

namespace MyDefense.Battle.Balance
{
    /// <summary>
    /// Battle-owned prefab lookup boundary. It intentionally resolves only explicitly
    /// configured prefab references and never assumes a Resources path from prefabKey.
    /// </summary>
    public interface IBattleMonsterPrefabResolver
    {
        bool TryResolve(string prefabKey, out GameObject prefab);
    }

    public sealed class ExplicitBattleMonsterPrefabResolver : IBattleMonsterPrefabResolver
    {
        private readonly Dictionary<string, GameObject> _prefabs;

        public ExplicitBattleMonsterPrefabResolver(string prefabKey, GameObject prefab)
        {
            _prefabs = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(prefabKey) && prefab != null)
            {
                _prefabs.Add(prefabKey, prefab);
            }
        }

        public bool TryResolve(string prefabKey, out GameObject prefab)
        {
            prefab = null;
            return prefabKey != null && _prefabs.TryGetValue(prefabKey, out prefab) && prefab != null;
        }
    }

    /// <summary>
    /// Temporary Battle adapter used until the Monster owner supplies a production
    /// MonsterDefinition provider. Both logical IDs reuse the existing serialized
    /// Monster prefab, while their type, speed, and lane-limit policy remain distinct.
    /// </summary>
    public sealed class TemporaryBattleMonsterDefinitionProvider : IMonsterDefinitionProvider
    {
        public const string NormalMonsterId = "MONSTER_NORMAL_DEFAULT";
        public const string BossMonsterId = "MONSTER_BOSS_DEFAULT";
        public const string ExistingMonsterPrefabKey = "Monster";

        private readonly Dictionary<string, BattleMonsterDefinition> _definitions;

        private TemporaryBattleMonsterDefinitionProvider(float prefabBaseMaxHp)
        {
            _definitions = new Dictionary<string, BattleMonsterDefinition>(StringComparer.Ordinal)
            {
                {
                    NormalMonsterId,
                    new BattleMonsterDefinition(
                        NormalMonsterId,
                        "NORMAL",
                        prefabBaseMaxHp,
                        5f,
                        ExistingMonsterPrefabKey,
                        true)
                },
                {
                    BossMonsterId,
                    new BattleMonsterDefinition(
                        BossMonsterId,
                        "BOSS",
                        prefabBaseMaxHp,
                        2f,
                        ExistingMonsterPrefabKey,
                        false)
                }
            };
        }

        public static bool TryCreate(
            GameObject existingMonsterPrefab,
            out TemporaryBattleMonsterDefinitionProvider provider,
            out string error)
        {
            provider = null;
            error = null;
            if (existingMonsterPrefab == null)
            {
                error = "The existing serialized Monster prefab reference is missing.";
                return false;
            }

            MonsterStat stat = existingMonsterPrefab.GetComponent<MonsterStat>();
            if (stat == null)
            {
                error = "The existing Monster prefab must contain MonsterStat.";
                return false;
            }

            if (stat.hp <= 0f)
            {
                error = "The existing Monster prefab MonsterStat.hp must be greater than zero.";
                return false;
            }

            provider = new TemporaryBattleMonsterDefinitionProvider(stat.hp);
            return true;
        }

        public bool TryGet(string monsterId, out BattleMonsterDefinition definition)
        {
            definition = null;
            return monsterId != null && _definitions.TryGetValue(monsterId, out definition);
        }
    }

    public sealed class EmptyBattleAlienIdProvider : IAlienIdProvider
    {
        public static readonly EmptyBattleAlienIdProvider Instance = new EmptyBattleAlienIdProvider();

        private EmptyBattleAlienIdProvider()
        {
        }

        public bool Contains(long alienId)
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves the authoritative Alien ID set from canonical alien-spec.json.
    /// Battle-owned skill links use this adapter only for cross-contract validation;
    /// Alien stats and unlock policy remain owned by the canonical User balance.
    /// </summary>
    public sealed class CanonicalBattleAlienIdProvider : IAlienIdProvider
    {
        private readonly HashSet<long> _alienIds;

        private CanonicalBattleAlienIdProvider(IEnumerable<long> alienIds)
        {
            _alienIds = new HashSet<long>(alienIds ?? System.Array.Empty<long>());
        }

        public static bool TryCreate(out CanonicalBattleAlienIdProvider provider, out string error)
        {
            provider = null;
            error = null;
            var source = new StreamingAssetsCanonicalBalanceFileSource();
            if (!source.TryReadAllBytes("alien-spec.json", out byte[] bytes))
            {
                error = "Canonical alien-spec.json is missing.";
                return false;
            }

            try
            {
                AlienSpecArray document = JsonUtility.FromJson<AlienSpecArray>(
                    "{\"items\":" + Encoding.UTF8.GetString(bytes) + "}");
                var ids = new HashSet<long>();
                foreach (AlienSpec item in document?.items ?? System.Array.Empty<AlienSpec>())
                {
                    if (item == null || item.alienId <= 0 || !ids.Add(item.alienId))
                    {
                        error = "Canonical alien-spec.json contains an invalid or duplicate alienId.";
                        return false;
                    }
                }

                if (ids.Count == 0)
                {
                    error = "Canonical alien-spec.json contains no Alien IDs.";
                    return false;
                }

                provider = new CanonicalBattleAlienIdProvider(ids);
                return true;
            }
            catch (System.Exception ex)
            {
                error = "Canonical alien-spec.json could not be parsed: " + ex.Message;
                return false;
            }
        }

        public bool Contains(long alienId) => _alienIds.Contains(alienId);

        [System.Serializable]
        private sealed class AlienSpecArray
        {
            public AlienSpec[] items;
        }

        [System.Serializable]
        private sealed class AlienSpec
        {
            public long alienId;
        }
    }
}
