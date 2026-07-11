using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using MyDefense.Battle;

public class PathBuilder : EditorWindow
{
    [MenuItem("Battle/Build Path System")]
    public static void BuildPathSystem()
    {
        // 1. PathManager 생성 및 배치
        GameObject pathManagerObj = GameObject.Find("PathManager");
        if (pathManagerObj == null)
        {
            pathManagerObj = new GameObject("PathManager");
            Undo.RegisterCreatedObjectUndo(pathManagerObj, "Create PathManager");
        }

        PathManager pathManager = pathManagerObj.GetComponent<PathManager>();
        if (pathManager == null)
        {
            pathManager = pathManagerObj.AddComponent<PathManager>();
        }

        // 1.5. BattleWaveExecutor 생성 및 배치
        GameObject waveExecutorObj = GameObject.Find("BattleWaveExecutor");
        bool executorAlreadyExists = (waveExecutorObj != null);
        if (waveExecutorObj == null)
        {
            waveExecutorObj = new GameObject("BattleWaveExecutor");
            Undo.RegisterCreatedObjectUndo(waveExecutorObj, "Create BattleWaveExecutor");
        }

        BattleWaveExecutor waveExecutor = waveExecutorObj.GetComponent<BattleWaveExecutor>();
        if (waveExecutor == null)
        {
            waveExecutor = waveExecutorObj.AddComponent<BattleWaveExecutor>();
        }

        // 기존 레거시 WaveManager에서 프리팹 및 스폰 포인트 카피
        if (!executorAlreadyExists)
        {
            WaveManager legacyWave = FindFirstObjectByType<WaveManager>();
            if (legacyWave != null)
            {
                SerializedObject soExecutor = new SerializedObject(waveExecutor);
                SerializedObject soLegacy = new SerializedObject(legacyWave);
                var prefabProp = soLegacy.FindProperty("monsterPrefab");
                var spawnPointProp = soLegacy.FindProperty("spawnPoint");

                if (prefabProp != null && prefabProp.objectReferenceValue != null)
                {
                    soExecutor.FindProperty("_monsterPrefab").objectReferenceValue = prefabProp.objectReferenceValue;
                }
                if (spawnPointProp != null && spawnPointProp.objectReferenceValue != null)
                {
                    soExecutor.FindProperty("_spawnPoint").objectReferenceValue = spawnPointProp.objectReferenceValue;
                }
                
                soExecutor.ApplyModifiedProperties();
                Debug.Log("[PathBuilder] 레거시 WaveManager의 monsterPrefab 및 spawnPoint를 BattleWaveExecutor에 카피하였습니다.");
            }
        }

        // 2. 기존 WaypointManager를 탐색하여 Player1Lane으로 계층 구조 재설정
        WaypointManager oldManager = FindFirstObjectByType<WaypointManager>();
        Transform player1Parent = null;

        GameObject p1LaneObj = GameObject.Find("Player1Lane_WaypointGroup");
        bool p1AlreadyExists = (p1LaneObj != null);

        if (p1LaneObj == null)
        {
            p1LaneObj = new GameObject("Player1Lane_WaypointGroup");
            p1LaneObj.transform.SetParent(pathManagerObj.transform);
            Undo.RegisterCreatedObjectUndo(p1LaneObj, "Create Player1Lane Group");
        }

        WaypointGroup p1Group = p1LaneObj.GetComponent<WaypointGroup>();
        if (p1Group == null)
        {
            p1Group = p1LaneObj.AddComponent<WaypointGroup>();
        }

        SerializedObject so = new SerializedObject(p1Group);
        so.FindProperty("_laneType").enumValueIndex = (int)LaneType.Player1Lane;
        so.FindProperty("_gizmoColor").colorValue = Color.blue;
        so.ApplyModifiedProperties();

        if (p1AlreadyExists && p1LaneObj.transform.childCount > 0)
        {
            player1Parent = p1LaneObj.transform;
            Debug.Log("[PathBuilder] Player1Lane_WaypointGroup이 이미 존재하며 자식 노드가 있으므로 노드 복사/생성을 건너뜁니다.");
        }
        else if (oldManager != null)
        {
            List<Transform> children = new List<Transform>();
            foreach (Transform child in oldManager.transform)
            {
                children.Add(child);
            }

            foreach (var child in children)
            {
                child.SetParent(p1LaneObj.transform);
            }

            player1Parent = p1LaneObj.transform;
            oldManager.enabled = false; // 삭제하지 않고 비활성화
            Debug.Log("[PathBuilder] 기존 WaypointManager 노드들을 Player1Lane_WaypointGroup으로 이전하고 구형 매니저를 비활성화했습니다.");
        }
        else
        {
            // 신규 Player1Lane 생성 (P1 보드 외곽 사각형 순환)
            player1Parent = p1LaneObj.transform;
            CreateSquareWaypoints(p1LaneObj.transform, -4.2f, 4.2f, -6.69f, -1.37f);
        }

        // 3. Player2Lane 생성 (P2 보드 외곽 사각형 순환)
        Transform player2Parent = CreateSquareLane(pathManagerObj.transform, "Player2Lane_WaypointGroup", LaneType.Player2Lane, Color.red, -4.2f, 4.2f, -0.6f, 4.45f);

        // 4. BossSharedLane (공용 보스 경로) 생성 - 두 보드 사이 수평 좌우 횡단
        Transform bossParent = CreateBossLane(pathManagerObj.transform, "BossSharedLane_WaypointGroup", LaneType.BossSharedLane, Color.magenta, -4.2f, 4.2f, 0.5f, -1.06f);

        // 5. PathManager 새로고침
        pathManager.RefreshGroupsInEditor();

        EditorUtility.SetDirty(pathManagerObj);
        
        Debug.Log("[PathBuilder] 배틀 경로 시스템 구축 완료! 각 WaypointGroup의 세부 좌표는 씬 뷰에서 조정해 주십시오.");
    }

    private static void CreateSquareWaypoints(Transform parent, float minX, float maxX, float minZ, float maxZ)
    {
        // 4개 모퉁이 순환 노드 배치
        Vector3[] coords = new Vector3[] {
            new Vector3(minX, 0.5f, minZ), // WP0 (시작)
            new Vector3(minX, 0.5f, maxZ), // WP1
            new Vector3(maxX, 0.5f, maxZ), // WP2
            new Vector3(maxX, 0.5f, minZ)  // WP3
        };

        for (int i = 0; i < coords.Length; i++)
        {
            GameObject node = new GameObject($"WP{i}");
            node.transform.SetParent(parent);
            node.transform.position = coords[i];
            Undo.RegisterCreatedObjectUndo(node, $"Create WP{i}");
        }
    }

    private static Transform CreateSquareLane(Transform parent, string name, LaneType laneType, Color color, float minX, float maxX, float minZ, float maxZ)
    {
        GameObject laneObj = GameObject.Find(name);
        if (laneObj == null)
        {
            laneObj = new GameObject(name);
            laneObj.transform.SetParent(parent);
            Undo.RegisterCreatedObjectUndo(laneObj, $"Create {name}");

            WaypointGroup group = laneObj.AddComponent<WaypointGroup>();
            SerializedObject so = new SerializedObject(group);
            so.FindProperty("_laneType").enumValueIndex = (int)laneType;
            so.FindProperty("_gizmoColor").colorValue = color;
            so.ApplyModifiedProperties();

            CreateSquareWaypoints(laneObj.transform, minX, maxX, minZ, maxZ);
        }
        return laneObj.transform;
    }

    private static Transform CreateBossLane(Transform parent, string name, LaneType laneType, Color color, float startX, float endX, float posY, float posZ)
    {
        GameObject laneObj = GameObject.Find(name);
        bool alreadyExists = (laneObj != null);
        if (laneObj == null)
        {
            laneObj = new GameObject(name);
            laneObj.transform.SetParent(parent);
            Undo.RegisterCreatedObjectUndo(laneObj, $"Create {name}");

            WaypointGroup group = laneObj.AddComponent<WaypointGroup>();
            SerializedObject so = new SerializedObject(group);
            so.FindProperty("_laneType").enumValueIndex = (int)laneType;
            so.FindProperty("_gizmoColor").colorValue = color;
            so.ApplyModifiedProperties();
        }

        // 보스는 수평 좌우 경로 (3개 노드)
        float midX = (startX + endX) / 2f;
        Vector3[] coords = new Vector3[] {
            new Vector3(startX, posY, posZ), // WP0
            new Vector3(midX, posY, posZ),   // WP1
            new Vector3(endX, posY, posZ)    // WP2
        };

        for (int i = 0; i < coords.Length; i++)
        {
            string childName = $"WP{i}";
            Transform childTrans = laneObj.transform.Find(childName);
            GameObject node;
            if (childTrans == null)
            {
                node = new GameObject(childName);
                node.transform.SetParent(laneObj.transform);
                Undo.RegisterCreatedObjectUndo(node, $"Create {childName}");
            }
            else
            {
                node = childTrans.gameObject;
            }
            node.transform.position = coords[i];
            EditorUtility.SetDirty(node);
        }

        EditorUtility.SetDirty(laneObj);
        return laneObj.transform;
    }
}
