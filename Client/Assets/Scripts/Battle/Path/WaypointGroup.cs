using UnityEngine;
using System.Collections.Generic;

namespace MyDefense.Battle
{
    public class WaypointGroup : MonoBehaviour
    {
        [Header("경로 설정")]
        [SerializeField] private LaneType _laneType;
        [SerializeField] private Color _gizmoColor = Color.green;

        public LaneType Lane => _laneType;

        private List<Transform> _waypoints = new List<Transform>();

        public List<Transform> Waypoints
        {
            get
            {
                UpdateWaypoints();
                return _waypoints;
            }
        }

        private void Awake()
        {
            UpdateWaypoints();
        }

        public void UpdateWaypoints()
        {
            _waypoints.Clear();
            foreach (Transform child in transform)
            {
                _waypoints.Add(child);
            }
        }

        private void OnDrawGizmos()
        {
            UpdateWaypoints();
            if (_waypoints.Count < 2) return;

            Gizmos.color = _gizmoColor;
            for (int i = 0; i < _waypoints.Count - 1; i++)
            {
                if (_waypoints[i] != null && _waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
                    Gizmos.DrawSphere(_waypoints[i].position, 0.2f);
                }
            }
            if (_waypoints[_waypoints.Count - 1] != null)
            {
                Gizmos.DrawSphere(_waypoints[_waypoints.Count - 1].position, 0.2f);
            }
        }
    }
}
