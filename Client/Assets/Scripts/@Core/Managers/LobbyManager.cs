using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [Header("화면 리스트 (Shop, Units, Main, Clan, Etc 순서)")]

    public GameObject[] viewObjects; // 5개의 뷰를 담을 배열

    void Start()
    {
        // 게임 시작 시 첫 번째 화면(메인)만 켜기
        OpenTab(2);
    }

    // 버튼이 누를 함수 (index: 0~4)
    public void OpenTab(int index)
    {
        for (int i = 0; i < viewObjects.Length; i++)
        {
            if (i == index)
            {
                viewObjects[i].SetActive(true); // 선택된 놈은 켜고
            }
            else
            {
                viewObjects[i].SetActive(false); // 나머지는 끈다
            }
        }
        Debug.Log($"{index}번 탭으로 이동했습니다.");
    }
}
