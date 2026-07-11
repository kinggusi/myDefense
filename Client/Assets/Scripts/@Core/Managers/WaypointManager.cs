using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode] 
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
        DrawPath();
    }

    private void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
    }

    private void DrawPath()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        
        waypoints.Clear();
        foreach (Transform child in transform) waypoints.Add(child);

        if (waypoints.Count < 2) return;

        lineRenderer.positionCount = waypoints.Count;
        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 pos = waypoints[i].position;
            pos.y = 0.1f; 
            lineRenderer.SetPosition(i, pos);
        }
    }
}