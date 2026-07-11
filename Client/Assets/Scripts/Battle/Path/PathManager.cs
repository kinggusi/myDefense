using UnityEngine;
using System.Collections.Generic;

namespace MyDefense.Battle
{
    public class PathManager : MonoBehaviour
    {
        public static PathManager Instance { get; private set; }

        [Header("웨이포인트 그룹들")]
        [SerializeField] private List<WaypointGroup> _waypointGroups = new List<WaypointGroup>();

        private Dictionary<LaneType, List<Transform>> _paths = new Dictionary<LaneType, List<Transform>>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializePaths();
        }

        public void InitializePaths()
        {
            _paths.Clear();
            
            // 등록된 그룹이 수동으로 지정되어 있지 않다면 씬 내에서 검색
            if (_waypointGroups.Count == 0)
            {
                _waypointGroups.AddRange(FindObjectsByType<WaypointGroup>(FindObjectsSortMode.None));
            }

            foreach (var group in _waypointGroups)
            {
                if (group == null) continue;
                
                // 해당 레인에 중복 등록을 피하거나 추가(동일 레인 경로가 여러 개일 경우)
                if (!_paths.ContainsKey(group.Lane))
                {
                    _paths[group.Lane] = new List<Transform>(group.Waypoints);
                }
                else
                {
                    Debug.LogWarning($"[PathManager] Lane {group.Lane} 이(가) 중복 등록되었습니다.");
                }
            }
        }

        /// <summary>
        /// 특정 레인의 웨이포인트 노드 리스트를 반환합니다.
        /// </summary>
        public List<Transform> GetPath(LaneType lane)
        {
            if (_paths.TryGetValue(lane, out var path))
            {
                return path;
            }

            Debug.LogError($"[PathManager] {lane} 에 해당하는 경로를 찾을 수 없습니다!");
            return null;
        }

        // 에디터 상에서 새로고침용 메서드
        public void RefreshGroupsInEditor()
        {
            _waypointGroups.Clear();
            _waypointGroups.AddRange(GetComponentsInChildren<WaypointGroup>());
            InitializePaths();
        }
    }
}
