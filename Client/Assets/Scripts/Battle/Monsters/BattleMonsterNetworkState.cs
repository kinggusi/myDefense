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
        [Networked] public NetworkBool IsBoss { get; private set; }
        [Networked] public float CurrentHp { get; private set; }
        [Networked] public float MaxHp { get; private set; }
        [Networked] public NetworkBool IsDead { get; private set; }
        [Networked] public float PresentationScale { get; private set; }
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
        private NetworkTransform _networkTransform;
        private static readonly HashSet<int> ReportedPresentationMappings = new();
        private bool _hasPresentationPosition;
        private bool _presentationMappingReported;
        private Vector3 _lastPresentationPosition;

        public BattleMonsterLanePolicy LanePolicy => (BattleMonsterLanePolicy)LanePolicyValue;
        public bool IsInitialized => RuntimeMonsterId != 0 && !string.IsNullOrEmpty(BattleSessionId.ToString());
        public float CurrentMoveSpeedMultiplier => MutationMoveSpeedMultiplier <= 0f ? 1f : MutationMoveSpeedMultiplier;

        public override void Spawned()
        {
            _hasPresentationPosition = false;
            _networkTransform = GetComponent<NetworkTransform>();
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

            bool targetDied = IsDead || _monsterStat == null || _monsterStat.IsDead;
            DotTickPlan tickPlan = ResolveDotTickAfterDamage(targetDied, MutationDotTicksRemaining);
            if (tickPlan.ClearAllEffects)
            {
                ClearMutationEffects();
                return;
            }

            MutationDotTicksRemaining = tickPlan.RemainingTicks;
            MutationDotTimer = tickPlan.ScheduleTimer
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
            _networkTransform = null;
            if (_monsterStat == null)
                return;

            _monsterStat.OnHpInitialized -= HandleHpInitialized;
            _monsterStat.OnHpChanged -= HandleHpChanged;
            _monsterStat.OnDied -= HandleDied;
            _monsterStat = null;
        }

        public override void Render()
        {
            if (HasStateAuthority)
                return;

            transform.localScale = Vector3.one * ResolvePresentationScale(PresentationScale);
            _monsterStat?.ApplyNetworkState(CurrentHp, MaxHp, IsDead);
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

            // Read the canonical interpolated snapshot directly. The root Transform
            // is overwritten below for Player 2 presentation, so using its current
            // value as the next canonical sample can intermittently freeze or fold
            // the proxy path when Fusion does not write a new Transform that frame.
            if (!TryGetCanonicalRenderPosition(out Vector3 canonicalPosition))
                return;

            // Regular lanes are closed loops. Include the last-to-first segment;
            // omitting it pins the remapped proxy to a corner while the authority
            // monster traverses that closing edge.
            float progress = ProjectProgress(canonicalPosition, sourcePath, true);
            _lastPresentationPosition = EvaluateProgress(targetPath, progress, true);
            _hasPresentationPosition = true;
            transform.position = _lastPresentationPosition;
            if (!_presentationMappingReported)
            {
                _presentationMappingReported = true;
                int mappingKey = ((int)sourceLane << 8) | (int)targetLane;
                if (ReportedPresentationMappings.Add(mappingKey))
                    Debug.Log($"[BattleMonsterNetworkState] Local lane presentation remapped: source={sourceLane}, target={targetLane}.");
            }
        }

        private bool TryGetCanonicalRenderPosition(out Vector3 position)
        {
            _networkTransform ??= GetComponent<NetworkTransform>();
            if (_networkTransform == null)
            {
                position = transform.position;
                return false;
            }

            if (_networkTransform.TryGetSnapshotsBuffers(
                    out NetworkBehaviourBuffer fromBuffer,
                    out NetworkBehaviourBuffer toBuffer,
                    out float interpolationAlpha))
            {
                NetworkTRSPData from = fromBuffer.ReinterpretState<NetworkTRSPData>();
                NetworkTRSPData to = toBuffer.ReinterpretState<NetworkTRSPData>();
                position = InterpolateCanonicalPosition(from.Position, to.Position, interpolationAlpha);
                return true;
            }

            if (_networkTransform.StateBufferIsValid)
            {
                position = _networkTransform.Data.Position;
                return true;
            }

            position = transform.position;
            return false;
        }

        public static Vector3 InterpolateCanonicalPosition(Vector3 from, Vector3 to, float alpha)
            => Vector3.Lerp(from, to, Mathf.Clamp01(alpha));

        public bool TryGetPresentationPosition(out Vector3 position)
        {
            if (_hasPresentationPosition)
            {
                position = _lastPresentationPosition;
                return true;
            }

            position = transform.position;
            return false;
        }

        public static float ResolvePresentationScale(float replicatedScale)
            => replicatedScale > 0f && !float.IsNaN(replicatedScale) && !float.IsInfinity(replicatedScale)
                ? replicatedScale
                : 1f;

        public static float ProjectProgress(Vector3 position, IReadOnlyList<Transform> path, bool closedLoop)
        {
            float totalLength = PathLength(path, closedLoop);
            if (totalLength <= 0.0001f)
                return 0f;

            float walked = 0f;
            float bestDistance = float.MaxValue;
            float bestProgress = 0f;
            int segmentCount = closedLoop ? path.Count : path.Count - 1;
            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 start = path[i].position;
                Vector3 end = path[(i + 1) % path.Count].position;
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

        public static Vector3 EvaluateProgress(IReadOnlyList<Transform> path, float progress, bool closedLoop)
        {
            float totalLength = PathLength(path, closedLoop);
            if (totalLength <= 0.0001f)
                return path[0].position;

            float targetDistance = Mathf.Clamp01(progress) * totalLength;
            float walked = 0f;
            int segmentCount = closedLoop ? path.Count : path.Count - 1;
            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 start = path[i].position;
                Vector3 end = path[(i + 1) % path.Count].position;
                float segmentLength = Vector3.Distance(start, end);
                if (targetDistance <= walked + segmentLength || i == segmentCount - 1)
                {
                    float t = segmentLength <= 0.0001f ? 0f : (targetDistance - walked) / segmentLength;
                    return Vector3.Lerp(start, end, Mathf.Clamp01(t));
                }
                walked += segmentLength;
            }
            return path[path.Count - 1].position;
        }

        public static float PathLength(IReadOnlyList<Transform> path, bool closedLoop)
        {
            float length = 0f;
            int segmentCount = closedLoop ? path.Count : path.Count - 1;
            for (int i = 0; i < segmentCount; i++)
                length += Vector3.Distance(path[i].position, path[(i + 1) % path.Count].position);
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
            IsBoss = identity.IsBoss;
            return true;
        }

        public bool InitializePresentationScale(float scale)
        {
            if (!HasStateAuthority || scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale))
                return false;

            PresentationScale = scale;
            transform.localScale = Vector3.one * scale;
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
            ClearMutationEffects();
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
            ClearMutationEffects();
            BattleWaveStateAuthority authority = FindFirstObjectByType<BattleWaveStateAuthority>();
            if (authority == null)
                return;

            authority.TryAwardMonsterKill(this);
        }

        private void ClearMutationEffects()
        {
            MutationDotDamage = 0f;
            MutationDotTicksRemaining = 0;
            MutationDotTimer = default;
            MutationDotIntervalSeconds = 0f;
            MutationDotAttackerId = 0;
            MutationDotType = "NONE";
            MutationSlowTimer = default;
            MutationMoveSpeedMultiplier = 1f;
        }

        private static DotTickPlan ResolveDotTickAfterDamage(bool targetDied, int ticksRemainingBeforeHit)
        {
            if (targetDied)
                return new DotTickPlan(0, scheduleTimer: false, clearAllEffects: true);

            int remainingTicks = Mathf.Max(0, ticksRemainingBeforeHit - 1);
            return new DotTickPlan(
                remainingTicks,
                scheduleTimer: remainingTicks > 0,
                clearAllEffects: false);
        }

        private readonly struct DotTickPlan
        {
            public DotTickPlan(int remainingTicks, bool scheduleTimer, bool clearAllEffects)
            {
                RemainingTicks = remainingTicks;
                ScheduleTimer = scheduleTimer;
                ClearAllEffects = clearAllEffects;
            }

            public int RemainingTicks { get; }
            public bool ScheduleTimer { get; }
            public bool ClearAllEffects { get; }
        }
    }
}
