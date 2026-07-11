using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class RebuildBoardsEditor
{
    [MenuItem("Tools/보드 4x6 재구성하기")]
    public static void RebuildAll()
    {
        // 1. 현재 fields 씬이 열려있는지 확인
        string currentScene = EditorSceneManager.GetActiveScene().path;
        if (!currentScene.Contains("fields.unity"))
        {
            EditorSceneManager.OpenScene("Assets/Pages/fields.unity");
        }

        // 2. P1 보드 (GameObject 명칭: "GridManager") 검색 및 갱신
        GameObject p1Obj = GameObject.Find("GridManager");
        if (p1Obj == null)
        {
            Debug.LogError("GridManager (P1) 게임오브젝트를 찾을 수 없습니다.");
            return;
        }

        GridManager p1Manager = p1Obj.GetComponent<GridManager>();
        if (p1Manager == null)
        {
            Debug.LogError("GridManager (P1) 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        p1Manager.rows = 4;
        p1Manager.cols = 6;
        p1Manager.tileSize = 1.1f;

        for (int i = p1Obj.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(p1Obj.transform.GetChild(i).gameObject);
        }

        p1Manager.GenerateGrid();
        Debug.Log("P1 보드 4x6 재생성 완료!");

        // 3. P2 보드 (EnemyGridParent) 검색 및 갱신
        GameObject p2Obj = GameObject.Find("EnemyGridParent");
        if (p2Obj == null)
        {
            Debug.LogError("EnemyGridParent (P2)를 찾을 수 없습니다.");
            return;
        }

        GridManager p2Manager = p2Obj.GetComponent<GridManager>();
        if (p2Manager != null)
        {
            p2Manager.rows = 4;
            p2Manager.cols = 6;
            p2Manager.tileSize = 1.1f;
        }

        for (int i = p2Obj.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(p2Obj.transform.GetChild(i).gameObject);
        }

        GameObject tilePrefab = p1Manager.tilePrefab;
        if (tilePrefab == null)
        {
            Debug.LogError("GridManager에 tilePrefab이 할당되어 있지 않습니다.");
            return;
        }

        float tileSize = p1Manager.tileSize;
        float startX = -((p1Manager.cols - 1) * tileSize) / 2;
        float startZ = -((p1Manager.rows - 1) * tileSize) / 2;

        for (int x = 0; x < p1Manager.cols; x++)
        {
            for (int z = 0; z < p1Manager.rows; z++)
            {
                Vector3 spawnPos = new Vector3(
                    startX + (x * tileSize), 
                    0, 
                    startZ + (z * tileSize)
                );

                GameObject newTile = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab);
                newTile.transform.position = p2Obj.transform.position + spawnPos;
                newTile.transform.rotation = Quaternion.identity;
                newTile.name = $"Tile_{x}_{z}";
                newTile.transform.SetParent(p2Obj.transform);
            }
        }
        Debug.Log("P2 보드 4x6 재생성 완료!");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem("Tools/배틀 경로 및 자동시작 설정하기")]
    public static void ConfigureBattlePathAndAutoStart()
    {
        string currentScene = EditorSceneManager.GetActiveScene().path;
        if (!currentScene.Contains("fields.unity"))
        {
            EditorSceneManager.OpenScene("Assets/Pages/fields.unity");
        }

        // 1. P1 사각형 순환 경로 가로축 보정 (X = -3.60 ~ 3.60)
        GameObject p1Lane = GameObject.Find("Player1Lane_WaypointGroup");
        if (p1Lane != null)
        {
            float minX = -3.60f;
            float maxX = 3.60f;
            for (int i = 0; i < p1Lane.transform.childCount; i++)
            {
                var wp = p1Lane.transform.GetChild(i);
                Vector3 pos = wp.position;
                string nameUpper = wp.name.ToUpper();
                if (nameUpper == "WP0" || nameUpper == "WP1") pos.x = minX;
                else if (nameUpper == "WP2" || nameUpper == "WP3") pos.x = maxX;
                wp.position = pos;
                EditorUtility.SetDirty(wp.gameObject);
            }
            Debug.Log("Player1Lane_WaypointGroup 가로폭 [-3.60, 3.60] 보정 완료!");
        }

        // 2. P2 사각형 순환 경로 가로축 보정 (X = -3.60 ~ 3.60)
        GameObject p2Lane = GameObject.Find("Player2Lane_WaypointGroup");
        if (p2Lane != null)
        {
            float minX = -3.60f;
            float maxX = 3.60f;
            for (int i = 0; i < p2Lane.transform.childCount; i++)
            {
                var wp = p2Lane.transform.GetChild(i);
                Vector3 pos = wp.position;
                string nameUpper = wp.name.ToUpper();
                if (nameUpper == "WP0" || nameUpper == "WP1") pos.x = minX;
                else if (nameUpper == "WP2" || nameUpper == "WP3") pos.x = maxX;
                wp.position = pos;
                EditorUtility.SetDirty(wp.gameObject);
            }
            Debug.Log("Player2Lane_WaypointGroup 가로폭 [-3.60, 3.60] 보정 완료!");
        }

        // 3. BattleWaveExecutor 를 찾아서 _autoStartOnPlay 를 true 로 세팅
        GameObject waveExecutorObj = GameObject.Find("BattleWaveExecutor");
        if (waveExecutorObj != null)
        {
            var waveExecutor = waveExecutorObj.GetComponent<MyDefense.Battle.BattleWaveExecutor>();
            if (waveExecutor != null)
            {
                SerializedObject so = new SerializedObject(waveExecutor);
                so.FindProperty("_autoStartOnPlay").boolValue = true;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(waveExecutorObj);
                Debug.Log("BattleWaveExecutor._autoStartOnPlay 활성화 완료!");
            }
        }

        // 4. 변경된 씬 저장
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("배틀 설정 완료 및 fields.unity 씬 저장 완료!");
    }
}
