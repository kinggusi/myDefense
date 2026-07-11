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

        // P1 기존 타일들 파괴
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

        // EnemyGridParent에도 GridManager가 붙어있다면 설정값 동기화
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

        // 4. 변경된 씬 저장
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("fields.unity 씬 저장 완료!");
    }
}
