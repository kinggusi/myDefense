using UnityEngine;
using System.Collections.Generic;
using Fusion;

namespace MyDefense.Battle
{
    public class BattleMonsterMovement : MonoBehaviour
    {
        [Header("이동 설정")]
        [SerializeField] private LaneType _laneType = LaneType.Player1Lane;
        [SerializeField] private float _speed = 5f;

        private List<Transform> _waypoints;
        private int _targetIndex = 0;
        private int _travelDirection = 1;
        private bool _isInitialized = false;
        private bool _isPathCompleted = false;
        private bool _pathInitializationAttempted = false;
        private bool _invalidPathReported = false;

        private bool HasMovementAuthority()
        {
            NetworkObject networkObject = GetComponent<NetworkObject>();
            return networkObject == null
                || networkObject.Runner == null
                || networkObject.HasStateAuthority;
        }

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        public LaneType Lane
        {
            get => _laneType;
            set
            {
                _laneType = value;
                _isInitialized = false; // 새로운 레인으로 지정 시 재초기화 유도
                _isPathCompleted = false;
                _pathInitializationAttempted = false;
                _invalidPathReported = false;
                _targetIndex = 0;
                _travelDirection = 1;
                _waypoints = null;
            }
        }

        private void Start()
        {
            if (!HasMovementAuthority()) return;
            InitializePath();
        }

        private void Update()
        {
            if (!HasMovementAuthority()) return;

            if (!_isInitialized)
            {
                if (!_pathInitializationAttempted)
                {
                    InitializePath();
                }
                if (!_isInitialized) return; // 경로 획득 실패 시 보류
            }

            if (_isPathCompleted) return; // 경로가 완료되어 대기 중이면 이동 연산 생략

            Move();
        }

        private void InitializePath()
        {
            if (!HasMovementAuthority()) return;
            if (PathManager.Instance == null) return;

            _pathInitializationAttempted = true;
            TryInitializePath(PathManager.Instance.GetPath(_laneType));
        }

        private bool TryInitializePath(List<Transform> waypoints)
        {
            _waypoints = waypoints;
            if (_waypoints == null || _waypoints.Count < 2)
            {
                ReportInvalidPathOnce(_waypoints?.Count ?? 0, "at least two waypoints are required");
                _isPathCompleted = true;
                return false;
            }

            for (int i = 0; i < _waypoints.Count; i++)
            {
                if (_waypoints[i] != null) continue;

                ReportInvalidPathOnce(_waypoints.Count, $"waypoint at index {i} is null");
                _isPathCompleted = true;
                return false;
            }

            transform.position = _waypoints[0].position;
            _targetIndex = 1;
            _travelDirection = 1;
            _isInitialized = true;
            _isPathCompleted = false;
            _invalidPathReported = false;
            return true;
        }

        private void ReportInvalidPathOnce(int waypointCount, string reason)
        {
            if (_invalidPathReported) return;

            _invalidPathReported = true;
            Debug.LogError($"[BattleMonsterMovement] Cannot move on {_laneType}: invalid path ({waypointCount} waypoints; {reason}). Movement stopped.");
        }

        private void Move()
        {
            if (_waypoints == null || _waypoints.Count == 0) return;

            // 1. 목표 지점 가져오기
            Transform targetWaypoint = _waypoints[_targetIndex];

            // 2. 방향 계산 및 이동
            Vector3 dir = targetWaypoint.position - transform.position;
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetWaypoint.position,
                Mathf.Max(0f, _speed) * Time.deltaTime);

            // 3. 방향 전환 (목표 노드를 바라보기)
            if (dir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }

            // 4. 도착 판정 (거리가 0.15보다 가까워지면 다음 목표로)
            if ((transform.position - targetWaypoint.position).sqrMagnitude < 0.0225f)
            {
                GetNextWaypoint();
            }
        }

        private void GetNextWaypoint()
        {
            if (_waypoints == null || _waypoints.Count < 2) return;

            if (_laneType != LaneType.BossSharedLane)
            {
                _targetIndex = (_targetIndex + 1) % _waypoints.Count;
                return;
            }

            if (_travelDirection > 0 && _targetIndex >= _waypoints.Count - 1)
            {
                OnBossPathCompleted();
            }
            else if (_travelDirection < 0 && _targetIndex <= 0)
            {
                _travelDirection = 1;
                _targetIndex = 1;
            }
            else
            {
                _targetIndex += _travelDirection;
            }
        }

        /// <summary>
        /// 보스가 공용 레인의 마지막 노드에 도착하면 역방향 순찰로 전환합니다.
        /// </summary>
        protected virtual void OnBossPathCompleted()
        {
            _travelDirection = -1;
            _targetIndex = _waypoints.Count - 2;
        }
    }
}
