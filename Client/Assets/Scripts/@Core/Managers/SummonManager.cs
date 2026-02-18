using UnityEngine;
using System.Collections.Generic; // 리스트 사용을 위해 필요

public class SummonManager : MonoBehaviour
{
    [Header("연결 정보")]
    public GameObject unitPrefab;   // 소환할 유닛 프리팹 (AlienUnit)
    public Transform gridParent;    // 타일들이 모여있는 부모 객체 (GridManager)

    // 버튼 클릭 시 실행될 함수
    public void SummonUnit()
    {
        // 1. 현재 비어있는 타일들을 다 찾는다.
        List<Transform> emptyTiles = new List<Transform>();

        // GridManager의 자식(타일)들을 하나씩 검사
        foreach (Transform tile in gridParent)
        {
            // 타일 자식 개수가 0이면 비어있는 것! (유닛은 타일의 자식으로 들어가니까)
            if (tile.childCount == 0)
            {
                emptyTiles.Add(tile);
            }
        }

        // 2. 빈 곳이 없으면 소환 실패
        if (emptyTiles.Count == 0)
        {
            Debug.Log("빈 타일이 없습니다! (꽉 참)");
            return;
        }

        // 3. 랜덤하게 하나 뽑기 (운빨 요소)
        int randomIndex = Random.Range(0, emptyTiles.Count);
        Transform targetTile = emptyTiles[randomIndex];

        // 4. 유닛 소환!
        SpawnUnit(targetTile);
    }

    void SpawnUnit(Transform tileTransform)
    {
        // 타일 위치 + 약간 위(0.5)에 생성
        Vector3 spawnPos = tileTransform.position + new Vector3(0, 0.5f, 0);
        GameObject newUnit = Instantiate(unitPrefab, spawnPos, Quaternion.identity);

        // [중요] 유닛을 타일의 자식으로 넣어야 나중에 "빈 타일" 체크가 됨
        newUnit.transform.SetParent(tileTransform);
    }
}