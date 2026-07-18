using Fusion;
using MyDefense.Battle.Runtime;
using UnityEngine;

namespace MyDefense.Battle
{
    /// <summary>
    /// Fusion state mirror for the authoritative Monster runtime identity and health.
    /// MonsterStat remains the local damage/view adapter; State Authority is the only
    /// peer that publishes changes to these Networked properties.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleMonsterNetworkState : NetworkBehaviour
    {
        [Networked] public NetworkString<_64> BattleSessionId { get; private set; }
        [Networked] public ulong RuntimeMonsterId { get; private set; }
        [Networked] public NetworkString<_32> MonsterId { get; private set; }
        [Networked] public int LanePolicyValue { get; private set; }
        [Networked] public NetworkString<_64> FieldOwnerPlayerId { get; private set; }
        [Networked] public int SpawnWave { get; private set; }
        [Networked] public float CurrentHp { get; private set; }
        [Networked] public float MaxHp { get; private set; }
        [Networked] public NetworkBool IsDead { get; private set; }

        private MonsterStat _monsterStat;

        public BattleMonsterLanePolicy LanePolicy => (BattleMonsterLanePolicy)LanePolicyValue;
        public bool IsInitialized => RuntimeMonsterId != 0 && !string.IsNullOrEmpty(BattleSessionId.ToString());

        public override void Spawned()
        {
            _monsterStat = GetComponent<MonsterStat>();
            if (_monsterStat == null)
                return;

            _monsterStat.OnHpInitialized += HandleHpInitialized;
            _monsterStat.OnHpChanged += HandleHpChanged;
            _monsterStat.OnDied += HandleDied;

            if (HasStateAuthority)
                SyncHealthFromLocal();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_monsterStat == null)
                return;

            _monsterStat.OnHpInitialized -= HandleHpInitialized;
            _monsterStat.OnHpChanged -= HandleHpChanged;
            _monsterStat.OnDied -= HandleDied;
            _monsterStat = null;
        }

        public override void Render()
        {
            if (HasStateAuthority || _monsterStat == null)
                return;

            _monsterStat.ApplyNetworkState(CurrentHp, MaxHp, IsDead);
        }

        public bool InitializeRuntimeIdentity(BattleMonsterRuntimeIdentity identity)
        {
            if (!HasStateAuthority || identity == null)
                return false;

            BattleSessionId = identity.BattleSessionId;
            RuntimeMonsterId = identity.RuntimeMonsterId;
            MonsterId = identity.MonsterId;
            LanePolicyValue = (int)identity.LanePolicy;
            FieldOwnerPlayerId = identity.FieldOwnerPlayerId ?? string.Empty;
            SpawnWave = identity.SpawnWave;
            return true;
        }

        public void SyncHealthFromLocal()
        {
            if (!HasStateAuthority || _monsterStat == null)
                return;

            MaxHp = _monsterStat.MaxHp;
            CurrentHp = _monsterStat.CurrentHp;
            IsDead = _monsterStat.IsDead;
        }

        private void HandleHpInitialized(float currentHp, float maxHp)
        {
            if (!HasStateAuthority)
                return;

            CurrentHp = currentHp;
            MaxHp = maxHp;
            IsDead = false;
        }

        private void HandleHpChanged(float currentHp, float maxHp)
        {
            if (!HasStateAuthority)
                return;

            CurrentHp = currentHp;
            MaxHp = maxHp;
        }

        private void HandleDied()
        {
            if (HasStateAuthority)
                IsDead = true;
        }
    }
}
