using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("설정")]
    public GameObject tilePrefab; // 타일 도장
    public int rows = 4;          // 세로 줄 수 (4)
    public int cols = 6;          // 가로 칸 수 (6)
    public float tileSize = 1.1f; // 타일 간격 (Gap 포함 크기)

    [ContextMenu("그리드 생성하기")] // 컴포넌트 메뉴에서 클릭 한 번으로 실행!
    public void GenerateGrid()
    {
        // 기존에 있던 타일 싹 지우기 (초기화)
        // 안전하게 뒤에서부터 삭제
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // 중앙 정렬을 위한 오프셋 계산 (CSS의 margin: 0 auto 원리)
        float startX = -((cols - 1) * tileSize) / 2;
        float startZ = -((rows - 1) * tileSize) / 2;

        for (int x = 0; x < cols; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                // 위치 계산
                Vector3 spawnPos = new Vector3(
                    startX + (x * tileSize), 
                    0, // Y는 바닥(0)
                    startZ + (z * tileSize)
                );

                // 생성 및 부모 설정
                GameObject newTile = Instantiate(tilePrefab, transform.position + spawnPos, Quaternion.identity);
                newTile.name = $"Tile_{x}_{z}"; // 이름 이쁘게 (Tile_0_0)
                newTile.transform.SetParent(this.transform); // GridManager의 자식으로 넣기
            }
        }
    }
}