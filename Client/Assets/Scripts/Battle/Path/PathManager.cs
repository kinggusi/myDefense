using UnityEngine;
using System;
using System.Collections.Generic;

namespace MyDefense.Battle
{
    public class PathManager : MonoBehaviour
    {
        public static PathManager Instance { get; private set; }

        [Header("웨이포인트 그룹들")]
        [SerializeField] private List<WaypointGroup> _waypointGroups = new List<WaypointGroup>();

        private readonly Dictionary<LaneType, List<Transform>> _paths = new Dictionary<LaneType, List<Transform>>();
        private readonly HashSet<LaneType> _reportedMissingPaths = new HashSet<LaneType>();

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
            _reportedMissingPaths.Clear();
            
            // 등록된 그룹이 수동으로 지정되어 있지 않다면 씬 내에서 검색
            if (_waypointGroups.Count == 0)
            {
                _waypointGroups.AddRange(FindObjectsByType<WaypointGroup>(FindObjectsSortMode.None));
            }

            var seenActiveLanes = new HashSet<LaneType>();
            var rejectedLanes = new HashSet<LaneType>();

            for (int groupIndex = 0; groupIndex < _waypointGroups.Count; groupIndex++)
            {
                WaypointGroup group = _waypointGroups[groupIndex];
                if (group == null)
                {
                    Debug.LogError($"[PathManager] WaypointGroup list contains a null entry at index {groupIndex}.");
                    continue;
                }

                if (!IsGroupActive(group))
                {
                    Debug.LogError($"[PathManager] WaypointGroup '{group.name}' for {group.Lane} is inactive and cannot be registered.");
                    continue;
                }

                LaneType lane = group.Lane;
                if (!seenActiveLanes.Add(lane))
                {
                    Debug.LogError($"[PathManager] Duplicate active WaypointGroup detected for {lane}. The lane path is rejected.");
                    rejectedLanes.Add(lane);
                    _paths.Remove(lane);
                    continue;
                }

                List<Transform> waypoints = group.Waypoints;
                if (!ValidateWaypoints(group, waypoints))
                {
                    rejectedLanes.Add(lane);
                    continue;
                }

                _paths[lane] = new List<Transform>(waypoints);
            }

            foreach (LaneType lane in Enum.GetValues(typeof(LaneType)))
            {
                if (rejectedLanes.Contains(lane))
                {
                    _paths.Remove(lane);
                }

                if (_paths.ContainsKey(lane)) continue;

                Debug.LogError($"[PathManager] No valid active path is registered for {lane}.");
                _reportedMissingPaths.Add(lane);
            }
        }

        private static bool IsGroupActive(WaypointGroup group)
        {
            if (!group.enabled) return false;

            Transform current = group.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf) return false;
                current = current.parent;
            }

            return true;
        }

        private static bool ValidateWaypoints(WaypointGroup group, List<Transform> waypoints)
        {
            if (waypoints == null || waypoints.Count < 2)
            {
                int count = waypoints?.Count ?? 0;
                Debug.LogError($"[PathManager] WaypointGroup '{group.name}' for {group.Lane} requires at least two waypoints, but found {count}.");
                return false;
            }

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null)
                {
                    Debug.LogError($"[PathManager] WaypointGroup '{group.name}' for {group.Lane} contains a null waypoint at index {i}.");
                    return false;
                }

                for (int j = 0; j < i; j++)
                {
                    if ((waypoints[i].position - waypoints[j].position).sqrMagnitude > 0.000001f) continue;

                    Debug.LogError($"[PathManager] WaypointGroup '{group.name}' for {group.Lane} contains duplicate waypoint positions at indices {j} and {i}.");
                    return false;
                }
            }

            return true;
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

            if (_reportedMissingPaths.Add(lane))
            {
                Debug.LogError($"[PathManager] No valid path is available for {lane}.");
            }
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
