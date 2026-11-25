#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Combat;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using UnityEngine;

namespace MyDefense.Battle.Showroom
{
    public sealed class BattleShowroomCombatProbe : MonoBehaviour
    {
        private const string PreferenceId = "CombatAttack";
        private static readonly string[] Grades = { "NORMAL", "EPIC", "UNIQUE", "LEGEND", "MYTHIC" };
        [SerializeField] private GameObject _alienUnit;
        [SerializeField] private Vector3[] _boardSlots;

        [Serializable]
        public sealed class Settings
        {
            public string damage = "10";
            public string rate = "1";
            public string range = "4";
            public string hp = "100";
        }

        private Settings _settings;
        private BattleWaveStateAuthority _authority;
        private BattleProjectileSpawner _spawner;
        private GameObject _unit;
        private int _unitSlot = -1;
        private long _unitAlienId;
        private AlienAttackSnapshot _snapshot;
        private int _shotCount;
        private int _hitCount;
        private float _lastShot;
        private float _lastShotInterval;
        private string _lastHit;
        private readonly HashSet<BattleProjectileNetworkState> _observedProjectiles = new();
        private string _message;
        private BattleMonsterNetworkState[] _targets = Array.Empty<BattleMonsterNetworkState>();
        private BattleMonsterNetworkState _target;
        private MonsterStat _targetStat;
        private float _previousTargetHp;
        private float _lastTargetHpLoss;
        private BattleMonsterMovement _frozen;
        private float _resumeSpeed;

        private void Awake() => _settings = BattleShowroomPreferences.Load<Settings>(PreferenceId);

        public void Bind(BattleWaveStateAuthority authority, BattleProjectileSpawner spawner)
        {
            ClearSession();
            _authority = authority;
            _spawner = spawner;
            if (_spawner != null)
                _spawner.DevelopmentProjectileSpawned += ObserveProjectile;
        }

        public void ClearSession()
        {
            if (_spawner != null)
                _spawner.DevelopmentProjectileSpawned -= ObserveProjectile;
            _authority = null;
            _spawner = null;
            ReleaseUnit();
            ReleaseMovementOverride();
            ReleaseTarget();
            _targets = Array.Empty<BattleMonsterNetworkState>();
            _message = null;
        }

        public void ResetPreferences()
        {
            BattleShowroomPreferences.Clear(PreferenceId);
            _settings = new Settings();
        }

        public void Draw(int selectedBoardSlot)
        {
            if (_settings == null)
                return;
            GUILayout.Space(8f);
            GUILayout.Label("Attack and target test setup", GUI.skin.box);
            GUILayout.Label("Enter an offline base Snapshot. No account stats are loaded.");
            GUILayout.BeginHorizontal();
            DrawInput("Damage", ref _settings.damage);
            DrawInput("Attacks/sec", ref _settings.rate);
            DrawInput("Range", ref _settings.range);
            GUILayout.EndHorizontal();
            if (GUILayout.Button($"Assign Snapshot to Slot {selectedBoardSlot + 1}"))
                ApplyUnit(selectedBoardSlot);
            if (_unit != null && GUILayout.Button("Remove Snapshot probe"))
                ReleaseUnit();

            if (GUILayout.Button("Find active targets in this Wave"))
                FindTargets();
            foreach (BattleMonsterNetworkState target in _targets)
            {
                if (target == null || target.Object == null || !target.Object.IsValid || target.IsDead)
                    continue;
                if (GUILayout.Button($"{target.MonsterId} | HP {target.CurrentHp:0.##}/{target.MaxHp:0.##} | #{target.RuntimeMonsterId}"))
                    SelectTarget(target);
            }
            DrawTargetControls();

            GUILayout.Space(8f);
            GUILayout.Label("Measured in this session", GUI.skin.box);
            if (_unit != null)
            {
                GUILayout.Label($"Applied Snapshot | Damage {_snapshot.Damage:0.##} | Attack interval {1f / _snapshot.AttackRate:0.###} s"
                    + $" | Range {_snapshot.Range:0.##} | {_snapshot.ActiveMutationType}");
                GUILayout.Label($"DoT {_snapshot.DotDamagePerTick:0.##} x{_snapshot.DotTickCount} | "
                    + $"Slow {_snapshot.SlowMultiplier:0.##} | Splash radius {_snapshot.SplashRadius:0.##}");
                GUILayout.Label($"Actual State Authority projectile shots {_shotCount} | Hits {_hitCount}"
                    + (_shotCount > 1 ? $" | Last shot interval {_lastShotInterval:0.###} s" : string.Empty));
                if (_lastHit != null)
                    GUILayout.Label(_lastHit);
            }
            if (_message != null)
                GUILayout.Label(_message);
            GUILayout.Label("Limits: canonical Wave targets. Defense, invulnerability, arbitrary counts and two-player Support Kill need additional fixtures.");
            GUILayout.Label("Time scale is fixed at 1. Fusion tick timing stays in its normal mode.");
        }

        private static void DrawInput(string title, ref string value)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(title);
            value = GUILayout.TextField(value ?? string.Empty, 8);
            GUILayout.EndVertical();
        }

        private void DrawTargetControls()
        {
            if (_target == null || _target.Object == null || !_target.Object.IsValid || _target.IsDead)
                return;
            GUILayout.Label($"Selected HP {_target.CurrentHp:0.##}/{_target.MaxHp:0.##}"
                + $" | Status speed x{_target.CurrentMoveSpeedMultiplier:0.##}"
                + $"\nLast measured HP loss {_lastTargetHpLoss:0.##}");
            GUILayout.BeginHorizontal();
            DrawInput("Fresh HP", ref _settings.hp);
            if (GUILayout.Button("Set HP before attacking"))
            {
                if (_unit != null || (_targetStat != null && _targetStat.LastDamageAttackerId != 0))
                    _message = "Use a fresh target before assigning an attack Snapshot.";
                else if (TryPositive(_settings.hp, 100000f, out float hp) && hp >= 1f && _targetStat != null)
                {
                    _targetStat.InitializeHp(hp);
                    _previousTargetHp = _targetStat.CurrentHp;
                    _lastTargetHpLoss = 0f;
                    BattleShowroomPreferences.Save(PreferenceId, _settings);
                }
                else
                    _message = "Enter target HP 1–100000.";
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button(_frozen == null ? "Stop target movement" : "Resume target movement"))
            {
                if (_frozen != null)
                    ReleaseMovementOverride();
                else if (_target.TryGetComponent(out BattleMonsterMovement movement))
                {
                    _frozen = movement;
                    _resumeSpeed = movement.Speed;
                    movement.Speed = 0f;
                }
            }
        }

        private void ApplyUnit(int slot)
        {
            _message = null;
            if (_authority == null || !_authority.HasStateAuthority
                || !_authority.IsBoardOccupied(1, slot) || _authority.IsBoardInjector(1, slot))
            {
                _message = "Select an occupied Alien slot on the local board.";
                return;
            }
            if (_alienUnit == null || _spawner == null || _spawner.ProjectilePrefab == null
                || _boardSlots == null || slot >= _boardSlots.Length)
            {
                _message = "Battle Alien, Projectile or slot reference is missing.";
                return;
            }
            if (!TryPositive(_settings.damage, 100000f, out float damage)
                || !TryPositive(_settings.rate, 20f, out float rate)
                || !TryPositive(_settings.range, 100f, out float range))
            {
                _message = "Enter positive Damage, Attacks/sec and Range within the fixture limits.";
                return;
            }
            long id = _authority.GetBoardAlienId(1, slot);
            if (!_authority.Executor.TryGetCanonicalBasicProjectile(id, out _))
            {
                _message = $"Alien {id} has no canonical basic projectile.";
                return;
            }
            ReleaseUnit();
            var position = _boardSlots[slot] + Vector3.up * 0.4f;
            _unit = Instantiate(_alienUnit, position, Quaternion.identity, transform);
            if (!_unit.TryGetComponent(out UnitData data) || !_unit.TryGetComponent(out UnitAttack attack))
            {
                ReleaseUnit();
                _message = "Authored Alien prefab is missing UnitData or UnitAttack.";
                return;
            }
            _unitSlot = slot;
            _unitAlienId = id;
            byte grade = _authority.GetBoardGrade(1, slot);
            data.specId = id;
            data.serverId = (1L << 32) | (uint)(slot + 1);
            data.grade = grade < Grades.Length ? Grades[grade] : string.Empty;
            data.activeMutationType = _authority.GetBoardMutationState(1, slot) == (byte)BattleMutationState.ACTIVE
                ? _authority.GetBoardMutationType(1, slot)
                : null;
            _snapshot = AlienAttackSnapshot.FromCalculatedStats(data.serverId, damage, rate, range, data.activeMutationType);
            _authority.Executor.TryApplyCanonicalResonance(grade,
                _authority.GetResonanceLevel(1, CanonicalResonanceTrack.NORMAL),
                _authority.GetResonanceLevel(1, CanonicalResonanceTrack.MYTHIC), _snapshot, out _snapshot);
            if (!string.IsNullOrWhiteSpace(data.activeMutationType)
                && _authority.Executor.TryGetCanonicalMutationSpec(data.activeMutationType, out CanonicalMutationSpec mutation))
            {
                _snapshot = MutationAttackSnapshotCalculator.Apply(_snapshot, mutation);
            }
            if (_authority.Executor.TryApplyActiveDailyBattleStatus(_snapshot, out AlienAttackSnapshot daily))
                _snapshot = daily;
            attack.ApplyAttackSnapshot(_snapshot);
            BattleShowroomPreferences.Save(PreferenceId, _settings);
            _message = "This probe uses the assigned Snapshot. Reapply after changing buffs or Wave.";
        }

        private void FindTargets()
        {
            string session = _authority?.Executor?.RuntimeSession?.BattleSessionId;
            _targets = string.IsNullOrEmpty(session) ? Array.Empty<BattleMonsterNetworkState>()
                : FindObjectsByType<BattleMonsterNetworkState>(FindObjectsSortMode.None)
                    .Where(target => target.Object != null && target.Object.IsValid && !target.IsDead
                        && string.Equals(session, target.BattleSessionId.ToString(), StringComparison.Ordinal))
                    .OrderBy(target => target.RuntimeMonsterId)
                    .Take(24)
                    .ToArray();
            if (_targets.Length == 0)
                _message = "Start a Wave and wait for a target.";
        }

        private void ObserveProjectile(BattleProjectileNetworkState projectile)
        {
            if (_unit == null || projectile.AttackerServerId != _snapshot.AttackerServerId)
                return;
            float now = Time.unscaledTime;
            if (_shotCount > 0)
                _lastShotInterval = now - _lastShot;
            _lastShot = now;
            _shotCount++;
            _observedProjectiles.RemoveWhere(item => item == null);
            if (_observedProjectiles.Add(projectile))
                projectile.AuthoritativeHit += ObserveHit;
        }

        private void ObserveHit(HitEvent hit)
        {
            if (_unit == null || hit.AttackerId != _snapshot.AttackerServerId)
                return;
            _hitCount++;
            _lastHit = $"Last authoritative hit | target {hit.TargetRuntimeId} | Payload damage {hit.Payload.Amount:0.##}"
                + $" | Critical {hit.Payload.IsCritical}";
        }

        private void SelectTarget(BattleMonsterNetworkState target)
        {
            ReleaseMovementOverride();
            ReleaseTarget();
            _target = target;
            _targetStat = target.GetComponent<MonsterStat>();
            if (_targetStat == null)
                return;
            _previousTargetHp = _targetStat.CurrentHp;
            _targetStat.OnHpChanged += ObserveTargetHp;
        }

        private void ObserveTargetHp(float hp, float maxHp)
        {
            float loss = _previousTargetHp - hp;
            if (loss > 0f)
                _lastTargetHpLoss = loss;
            _previousTargetHp = hp;
        }

        private void ReleaseTarget()
        {
            if (_targetStat != null)
                _targetStat.OnHpChanged -= ObserveTargetHp;
            _target = null;
            _targetStat = null;
            _lastTargetHpLoss = 0f;
            _previousTargetHp = 0f;
        }

        private void Update()
        {
            if (_unit != null && (_authority == null || !_authority.IsBoardOccupied(1, _unitSlot)
                || _authority.GetBoardAlienId(1, _unitSlot) != _unitAlienId))
            {
                ReleaseUnit();
                _message = "Board slot changed. Assign a Snapshot to the current Alien.";
            }
        }

        private void ReleaseUnit()
        {
            foreach (BattleProjectileNetworkState projectile in _observedProjectiles)
                if (projectile != null)
                    projectile.AuthoritativeHit -= ObserveHit;
            _observedProjectiles.Clear();
            _snapshot = default;
            _shotCount = 0;
            _hitCount = 0;
            _lastHit = null;
            _lastShot = 0f;
            _lastShotInterval = 0f;
            _unitSlot = -1;
            _unitAlienId = 0;
            if (_unit == null)
                return;
            _unit.SetActive(false);
            Destroy(_unit);
            _unit = null;
        }

        private void ReleaseMovementOverride()
        {
            if (_frozen != null)
                _frozen.Speed = _resumeSpeed;
            _frozen = null;
        }

        private static bool TryPositive(string text, float max, out float value)
            => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && value > 0f && value <= max;

        private void OnDestroy() => ClearSession();

#if UNITY_EDITOR
        public void ConfigureForEditor(GameObject alienUnit, Vector3[] boardSlots)
        {
            _alienUnit = alienUnit;
            _boardSlots = boardSlots;
        }
#endif
    }
}
#endif
