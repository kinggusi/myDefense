using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    [Header("화면 리스트 (Shop, Units, Main, Clan, Etc 순서)")]
    public GameObject[] viewObjects; 

    [Header("유저 정보 & 재화 UI")]
    public TMP_Text text_UserName;
    public TMP_Text text_UserLevel;
    public TMP_Text text_Heart;
    public TMP_Text text_Gold;
    public TMP_Text text_Diamond;

    [Header("내 유닛 목록 UI")]
    public Transform unitGridContent; // 카드가 생성될 부모 (Grid_Content)
    public GameObject unitCardPrefab; // 만들어둔 Level_Block 프리팹

    void Start()
    {
        // 1. 처음엔 메인 화면(2번 탭) 띄우기
        OpenTab(2);

        // 2. 서버에서 데이터 로드 시작!
        LoadLobbyData();
    }

    public void OpenTab(int index)
    {
        for (int i = 0; i < viewObjects.Length; i++)
        {
            viewObjects[i].SetActive(i == index);
        }
        Debug.Log($"{index}번 탭으로 이동했습니다.");
    }

    // 서버에서 데이터를 가져오는 핵심 함수
    public void LoadLobbyData() 
    {
        Debug.Log("서버에 유저 정보를 요청합니다...");

        NetworkManager.Instance.Get("/lobby/info/sh1", 
            (json) => {
                // 성공: JSON 데이터를 C# 객체로 변환
                LobbyResponseDto data = JsonUtility.FromJson<LobbyResponseDto>(json);
                
                // 1. 상단 바 UI 갱신
                UpdateTopBarUI(data.user);

                // 2. 유닛 목록 생성
                SpawnMyUnits(data.aliens);

                Debug.Log($"{data.user.username}님 로비 로드 성공!");
            }, 
            (error) => {
                Debug.LogError("서버 연결 실패: " + error);
            }
        );
    }

    // 상단 재화 UI 업데이트
    void UpdateTopBarUI(UserDto user)
    {
        text_UserName.text = user.username;
        // 서버 UserDto에 level이 없다면 일단 1로 고정하거나 추가해야 합니다.
        text_UserLevel.text = "1"; 
        text_Heart.text = user.heart.ToString();
        text_Gold.text = user.gold.ToString("N0"); // 1,000 단위 콤마
        text_Diamond.text = user.diamond.ToString("N0");
    }

    // 서버에서 받은 리스트만큼 카드 생성
    void SpawnMyUnits(List<AlienInventoryDto> aliens)
    {
        // 1. 기존 카드 청소
        foreach (Transform child in unitGridContent) Destroy(child.gameObject);

        // 2. 서버 데이터만큼 반복 생성
        foreach (var alien in aliens)
        {
            // 프리팹 복사본 생성 (Instantiate)
            GameObject card = Instantiate(unitCardPrefab, unitGridContent);
            
            // 생성된 카드의 '뇌(UnitCardUI)'를 가져옵니다. (GetComponent)
            UnitCardUI cardScript = card.GetComponent<UnitCardUI>();
            
            if (cardScript != null)
            {
                // 서버에서 온 데이터(alien)를 뇌에 전달! 
                // 뇌가 알아서 UI 텍스트들을 바꿉니다.
                cardScript.SetData(alien);
            }
        }
    }
}