using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode] // 에디터 모드에서도 선이 보이게 합니다.
public class WaypointManager : MonoBehaviour
{
    public static WaypointManager Instance { get; private set; }
    public List<Transform> waypoints = new List<Transform>();
    private LineRenderer lineRenderer;

    private void Awake()
    {
        Instance = this;
        SetupLineRenderer();
    }

    private void Update()
    {
        // 실시간으로 점을 옮기면 선도 따라오게 만듭니다.
        DrawPath();
    }

    private void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        // 선이 바닥에 파묻히지 않게 레이어 순서를 올리거나 Y값을 조절합니다.
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
    }

    private void DrawPath()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        
        // 자식 오브젝트(WP)들을 리스트에 갱신
        waypoints.Clear();
        foreach (Transform child in transform) waypoints.Add(child);

        if (waypoints.Count < 2) return;

        lineRenderer.positionCount = waypoints.Count;
        for (int i = 0; i < waypoints.Count; i++)
        {
            // 바닥(Y=0)보다 아주 살짝 위(0.1)에 선을 그립니다.
            Vector3 pos = waypoints[i].position;
            pos.y = 0.1f; 
            lineRenderer.SetPosition(i, pos);
        }
    }
}