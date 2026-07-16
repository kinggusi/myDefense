using System;
using System.Collections.Generic;
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
}
