using UnityEngine;
using System.Collections.Generic;

namespace MyDefense.Battle
{
    public class BattleMonsterMovement : MonoBehaviour
    {
        [Header("이동 설정")]
        [SerializeField] private LaneType _laneType = LaneType.Player1Lane;
        [SerializeField] private float _speed = 5f;

        private List<Transform> _waypoints;
        private int _targetIndex = 0;
        private bool _isInitialized = false;
        private bool _isPathCompleted = false;

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
            }
        }

        private void Start()
        {
            InitializePath();
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                InitializePath();
                if (!_isInitialized) return; // 경로 획득 실패 시 보류
            }

            if (_isPathCompleted) return; // 경로가 완료되어 대기 중이면 이동 연산 생략

            Move();
        }

        private void InitializePath()
        {
            if (PathManager.Instance == null) return;

            _waypoints = PathManager.Instance.GetPath(_laneType);
            if (_waypoints == null || _waypoints.Count == 0)
            {
                Debug.LogWarning($"[BattleMonsterMovement] {_laneType} 의 경로가 비어 있습니다.");
                return;
            }

            _targetIndex = 0;
            transform.position = _waypoints[0].position;
            _isInitialized = true;
            _isPathCompleted = false;
        }

        private void Move()
        {
            if (_waypoints == null || _waypoints.Count == 0) return;

            // 1. 목표 지점 가져오기
            Transform targetWaypoint = _waypoints[_targetIndex];

            // 2. 방향 계산 및 이동
            Vector3 dir = targetWaypoint.position - transform.position;
            transform.Translate(dir.normalized * _speed * Time.deltaTime, Space.World);

            // 3. 방향 전환 (목표 노드를 바라보기)
            if (dir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }

            // 4. 도착 판정 (거리가 0.15보다 가까워지면 다음 목표로)
            if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.15f)
            {
                GetNextWaypoint();
            }
        }

        private void GetNextWaypoint()
        {
            _targetIndex++;

            // 마지막 노드 도달 시 처리
            if (_targetIndex >= _waypoints.Count)
            {
                // V1 기획에 따른 일반 몬스터와 보스 이동 방식 분리
                if (_laneType == LaneType.BossSharedLane)
                {
                    OnBossPathCompleted();
                }
                else
                {
                    // 일반 몬스터는 개인 레인을 무한 순환하도록 함 (첫 노드로 이동 타겟 초기화)
                    _targetIndex = 0;
                }
            }
        }

        /// <summary>
        /// 보스가 공용 레인의 마지막 노드에 도착했을 때의 확장 지점입니다.
        /// 보스가 임의 삭제되지 않고 대기하도록 정지 상태로 전환합니다.
        /// </summary>
        protected virtual void OnBossPathCompleted()
        {
            _isPathCompleted = true;
            _speed = 0f;
            Debug.Log($"👹 [보스 공용 레인 완주] 보스가 최종 경로에 도착하여 대기 상태로 전환합니다. (보스 타이머 및 매치 영향도 확장 가능)");
        }
    }
}

