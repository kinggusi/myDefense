using UnityEngine;

public class MonsterMovement : MonoBehaviour
{
    [Header("이동 설정")]
    public float speed = 5f; // 몬스터 이동 속도

    private int targetIndex = 0; // 현재 가야할 목표 지점 번호 (WP0, WP1...)

    private void Start()
    {
        // 게임 시작 시, 몬스터를 강제로 첫 번째 지점(WP0)으로 이동시킵니다.
        if (WaypointManager.Instance != null && WaypointManager.Instance.waypoints.Count > 0)
        {
            Transform startPoint = WaypointManager.Instance.waypoints[0];
            transform.position = startPoint.position;
        }
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        // 웨이포인트가 없으면 아무것도 안 함 (에러 방지)
        if (WaypointManager.Instance == null || WaypointManager.Instance.waypoints.Count == 0) return;

        // 1. 목표 지점 가져오기
        Transform targetWaypoint = WaypointManager.Instance.waypoints[targetIndex];

        // 2. 방향 계산 (목표지점 - 내위치)
        Vector3 dir = targetWaypoint.position - transform.position;
        
        // 3. 이동 (초당 speed만큼)
        // normalized를 해줘야 거리가 멀다고 빨리 가지 않고 일정한 속도로 갑니다.
        transform.Translate(dir.normalized * speed * Time.deltaTime);

        // 4. 도착 판정 (거리가 0.1보다 가까워지면 도착했다고 침)
        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
        {
            GetNextWaypoint();
        }
    }

    private void GetNextWaypoint()
    {
        // 다음 번호로 넘기기
        targetIndex++;

        // 만약 마지막 지점(WP3)까지 왔다면?
        if (targetIndex >= WaypointManager.Instance.waypoints.Count)
        {
            targetIndex = 0; // 다시 처음(WP0)으로 돌아가서 뺑뺑이 (무한 루프)
            // 나중에는 여기서 '기지에 데미지 줌' + '몬스터 삭제' 로직이 들어갑니다.
        }
    }
}