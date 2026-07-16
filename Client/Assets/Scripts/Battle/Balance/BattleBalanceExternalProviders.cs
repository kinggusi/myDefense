namespace MyDefense.Battle.Balance
{
    public interface IMonsterDefinitionProvider
    {
        bool TryGet(string monsterId, out BattleMonsterDefinition definition);
    }

    public sealed class BattleMonsterDefinition
    {
        public string MonsterId { get; }
        public string MonsterType { get; }
        public float BaseMaxHp { get; }
        public float MoveSpeed { get; }
        public string PrefabKey { get; }
        public bool CountsTowardLaneLimit { get; }

        public BattleMonsterDefinition(string monsterId, string monsterType, float baseMaxHp, float moveSpeed, string prefabKey, bool countsTowardLaneLimit)
        {
            MonsterId = monsterId;
            MonsterType = monsterType;
            BaseMaxHp = baseMaxHp;
            MoveSpeed = moveSpeed;
            PrefabKey = prefabKey;
            CountsTowardLaneLimit = countsTowardLaneLimit;
        }
    }

    public interface IAlienIdProvider
    {
        bool Contains(long alienId);
    }
}
