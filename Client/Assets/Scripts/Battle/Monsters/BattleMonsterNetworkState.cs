using Fusion;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using System.Collections.Generic;
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
        [Networked] public float MutationMoveSpeedMultiplier { get; private set; }
        [Networked] private float MutationDotDamage { get; set; }
        [Networked] private int MutationDotTicksRemaining { get; set; }
        [Networked] private TickTimer MutationDotTimer { get; set; }
        [Networked] private float MutationDotIntervalSeconds { get; set; }
        [Networked] private long MutationDotAttackerId { get; set; }
        [Networked] private NetworkString<_16> MutationDotType { get; set; }
        [Networked] private TickTimer MutationSlowTimer { get; set; }

        private MonsterStat _monsterStat;
        private BattleWaveStateAuthority _waveAuthority;
        private bool _presentationMappingReported;
        private bool _hasPresentationPosition;
        private Vector3 _lastPresentationPosition;

        public BattleMonsterLanePolicy LanePolicy => (BattleMonsterLanePolicy)LanePolicyValue;
        public bool IsInitialized => RuntimeMonsterId != 0 && !string.IsNullOrEmpty(BattleSessionId.ToString());
        public float CurrentMoveSpeedMultiplier => MutationMoveSpeedMultiplier <= 0f ? 1f : MutationMoveSpeedMultiplier;

        public override void Spawned()
        {
            _hasPresentationPosition = false;
            _monsterStat = GetComponent<MonsterStat>();
            if (_monsterStat == null)
                return;

            _monsterStat.OnHpInitialized += HandleHpInitialized;
            _monsterStat.OnHpChanged += HandleHpChanged;
            _monsterStat.OnDied += HandleDied;

            if (HasStateAuthority)
            {
                MutationMoveSpeedMultiplier = 1f;
                SyncHealthFromLocal();
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || Runner == null || !Runner.IsRunning || IsDead)
                return;
            if (MutationSlowTimer.IsRunning && MutationSlowTimer.Expired(Runner))
            {
                MutationSlowTimer = default;
                MutationMoveSpeedMultiplier = 1f;
            }
            if (!MutationDotTimer.IsRunning || !MutationDotTimer.Expired(Runner) || MutationDotTicksRemaining <= 0)
                return;

            _monsterStat ??= GetComponent<MonsterStat>();
            _monsterStat?.ApplyDamage(new DamagePayload
            {
                BattleSessionId = BattleSessionId.ToString(),
                TargetRuntimeId = RuntimeMonsterId,
                AttackerId = MutationDotAttackerId,
                Amount = MutationDotDamage,
                ActiveMutationType = MutationDotType.ToString()
            });
            MutationDotTicksRemaining--;
            MutationDotTimer = MutationDotTicksRemaining > 0
                ? TickTimer.CreateFromSeconds(Runner, MutationDotIntervalSeconds)
                : default;
        }

        public void ApplyMutationEffect(
            float dotDamage,
            int dotTicks,
            float dotIntervalSeconds,
            float slowMultiplier,
            float slowDurationSeconds,
            long attackerId,
            string mutationType)
        {
            if (!HasStateAuthority || Runner == null || !Runner.IsRunning || IsDead)
                return;
            if (dotDamage > 0f && dotTicks > 0 && dotIntervalSeconds > 0f)
            {
                MutationDotDamage = dotDamage;
                MutationDotTicksRemaining = dotTicks;
                MutationDotIntervalSeconds = dotIntervalSeconds;
                MutationDotAttackerId = attackerId;
                MutationDotType = mutationType ?? "NONE";
                MutationDotTimer = TickTimer.CreateFromSeconds(Runner, dotIntervalSeconds);
            }
            if (slowMultiplier > 0f && slowMultiplier < 1f && slowDurationSeconds > 0f)
            {
                MutationMoveSpeedMultiplier = Mathf.Min(CurrentMoveSpeedMultiplier, slowMultiplier);
                MutationSlowTimer = TickTimer.CreateFromSeconds(Runner, slowDurationSeconds);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _hasPresentationPosition = false;
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

        /// <summary>
        /// NetworkTransform replicates the canonical world path.  Player 2's
        /// local presentation swaps the two private lanes so that the local
        /// player's monsters always enter from the lower (local) side and the
        /// opponent's monsters enter from the upper side.  This is a view-only
        /// remap; State Authority movement, lane ownership, and kill rewards
        /// remain canonical and are never changed here.
        /// </summary>
        private void LateUpdate()
        {
            if (HasStateAuthority || LanePolicy != BattleMonsterLanePolicy.EACH_FIELD)
                return;

            if (_waveAuthority == null)
                _waveAuthority = FindFirstObjectByType<BattleWaveStateAuthority>();
            if (_waveAuthority == null || _waveAuthority.Runner == null ||
                _waveAuthority.Object == null || !_waveAuthority.Object.IsValid ||
                PathManager.Instance == null)
                return;

            int localSlot = _waveAuthority.GetNetworkedPlayerSlot(_waveAuthority.Runner.LocalPlayer);
            if (localSlot != 2)
                return;

            string ownerId = FieldOwnerPlayerId.ToString();
            string player1Id = _waveAuthority.Player1UserId.ToString();
            string player2Id = _waveAuthority.Player2UserId.ToString();
            LaneType sourceLane;
            if (string.Equals(ownerId, player1Id, System.StringComparison.Ordinal))
                sourceLane = LaneType.Player1Lane;
            else if (string.Equals(ownerId, player2Id, System.StringComparison.Ordinal))
                sourceLane = LaneType.Player2Lane;
            else
                return;

            LaneType targetLane = sourceLane == LaneType.Player1Lane
                ? LaneType.Player2Lane
                : LaneType.Player1Lane;
            List<Transform> sourcePath = PathManager.Instance.GetPath(sourceLane);
            List<Transform> targetPath = PathManager.Instance.GetPath(targetLane);
            if (sourcePath == null || targetPath == null || sourcePath.Count < 2 || targetPath.Count < 2)
                return;

            // NetworkTransform may leave the presentation value untouched on a
            // render frame where no new snapshot was applied. Re-projecting that
            // already remapped value against the canonical source path collapses
            // multiple monsters onto the same waypoint and makes them appear to
            // disappear. Only remap when Fusion has supplied a new canonical
            // position since the previous presentation pass.
            Vector3 canonicalPosition = transform.position;
            if (_hasPresentationPosition &&
                (canonicalPosition - _lastPresentationPosition).sqrMagnitude <= 0.000001f)
                return;

            float progress = ProjectProgress(canonicalPosition, sourcePath);
            _lastPresentationPosition = EvaluateProgress(targetPath, progress);
            _hasPresentationPosition = true;
            transform.position = _lastPresentationPosition;
            if (!_presentationMappingReported)
            {
                _presentationMappingReported = true;
                Debug.Log($"[BattleMonsterNetworkState] Local lane presentation remapped: owner={ownerId}, source={sourceLane}, target={targetLane}.");
            }
        }

        private static float ProjectProgress(Vector3 position, List<Transform> path)
        {
            float totalLength = PathLength(path);
            if (totalLength <= 0.0001f)
                return 0f;

            float walked = 0f;
            float bestDistance = float.MaxValue;
            float bestProgress = 0f;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 start = path[i].position;
                Vector3 end = path[i + 1].position;
                Vector3 segment = end - start;
                float segmentLength = segment.magnitude;
                if (segmentLength <= 0.0001f)
                    continue;
                float t = Mathf.Clamp01(Vector3.Dot(position - start, segment) / (segmentLength * segmentLength));
                float distance = (position - Vector3.Lerp(start, end, t)).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestProgress = (walked + segmentLength * t) / totalLength;
                }
                walked += segmentLength;
            }
            return Mathf.Clamp01(bestProgress);
        }

        private static Vector3 EvaluateProgress(List<Transform> path, float progress)
        {
            float totalLength = PathLength(path);
            if (totalLength <= 0.0001f)
                return path[0].position;

            float targetDistance = Mathf.Clamp01(progress) * totalLength;
            float walked = 0f;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 start = path[i].position;
                Vector3 end = path[i + 1].position;
                float segmentLength = Vector3.Distance(start, end);
                if (targetDistance <= walked + segmentLength || i == path.Count - 2)
                {
                    float t = segmentLength <= 0.0001f ? 0f : (targetDistance - walked) / segmentLength;
                    return Vector3.Lerp(start, end, Mathf.Clamp01(t));
                }
                walked += segmentLength;
            }
            return path[path.Count - 1].position;
        }

        private static float PathLength(List<Transform> path)
        {
            float length = 0f;
            for (int i = 0; i < path.Count - 1; i++)
                length += Vector3.Distance(path[i].position, path[i + 1].position);
            return length;
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
            if (!HasStateAuthority)
                return;

            IsDead = true;
            BattleWaveStateAuthority authority = FindFirstObjectByType<BattleWaveStateAuthority>();
            if (authority == null)
                return;

            authority.TryAwardMonsterKill(this);
        }
    }
}
