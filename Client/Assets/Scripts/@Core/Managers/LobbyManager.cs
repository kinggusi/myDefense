using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using AlienUpgrade.Core;
using UnityEngine.UI;
using MyDefense.Lobby;

public class LobbyManager : MonoBehaviour
{
    private const string DefaultUsername = "sh1";
    private string currentUsername = DefaultUsername;

    public string CurrentUsername => currentUsername;
    public string CurrentDiamondText => text_Diamond != null ? text_Diamond.text : "0";

    [Header("화면 리스트 (Shop, Units, Main, Clan, Etc 순서)")]
    public GameObject[] viewObjects; 

    [Header("유저 정보 & 재화 UI")]
    public TMP_Text text_UserName;
    public TMP_Text text_UserLevel;
    public TMP_Text text_Heart;
    public TMP_Text text_Gold;
    public TMP_Text text_Diamond;
    public Text text_UniversalPiece;
    public Text text_GrowthCell;

    [Header("내 유닛 목록 UI")]
    public Transform unitGridContent; // 카드가 생성될 부모 (Grid_Content)
    public GameObject unitCardPrefab; // 만들어둔 Level_Block 프리팹
    [Header("Alien 상세 화면")]
    public AlienDetailController alienDetailController;
    private MythicBreedingController mythicBreedingController;



    void Start()
    {
        EnsureMaterialCurrencyUI();
        mythicBreedingController = GetComponent<MythicBreedingController>();
        if (mythicBreedingController == null)
        {
            mythicBreedingController = gameObject.AddComponent<MythicBreedingController>();
        }
        mythicBreedingController.Initialize(this);
        // 1. 처음엔 메인 화면(2번 탭) 띄우기
        OpenTab(2);

        // 2. 서버에서 데이터 로드 시작!
        LoadLobbyData();
    }

    private void EnsureMaterialCurrencyUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("로비 재화 UI를 생성할 Canvas를 찾지 못했습니다.");
            return;
        }

        Transform unitView = canvas.transform.Find("views/view_Unit");
        if (unitView == null)
        {
            unitView = canvas.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(child => child.name == "view_Unit");
        }
        if (unitView == null)
        {
            Debug.LogWarning("내 유닛 화면(view_Unit)을 찾지 못해 재료 재화 UI를 생성하지 않았습니다.");
            return;
        }

        Transform existing = unitView.Find("LobbyMaterialCurrencies");
        if (existing == null)
        {
            existing = canvas.transform.Find("LobbyMaterialCurrencies");
        }
        GameObject panel = existing != null
                ? existing.gameObject
                : new GameObject("LobbyMaterialCurrencies", typeof(RectTransform));
        panel.layer = LayerMask.NameToLayer("UI");
        panel.transform.SetParent(unitView, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(72f, -165f);
        panelRect.sizeDelta = new Vector2(310f, 118f);

        HorizontalLayoutGroup layout = panel.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        text_UniversalPiece = EnsureMaterialCard(panel.transform, "WakjeoDnaCard", "왹져 DNA",
                new Color(0.20f, 0.28f, 0.48f, 0.96f));
        text_GrowthCell = EnsureMaterialCard(panel.transform, "GrowthCellCard", "성장 세포",
                new Color(0.15f, 0.45f, 0.38f, 0.96f));

        Transform catalystCard = panel.transform.Find("MutationCatalystCard");
        if (catalystCard != null)
        {
            if (Application.isPlaying) Destroy(catalystCard.gameObject);
            else DestroyImmediate(catalystCard.gameObject);
        }
    }

    private static Text EnsureMaterialCard(Transform parent, string cardName, string label, Color color)
    {
        Transform existing = parent.Find(cardName);
        GameObject card = existing != null
                ? existing.gameObject
                : new GameObject(cardName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        card.layer = LayerMask.NameToLayer("UI");
        card.transform.SetParent(parent, false);
        card.GetComponent<Image>().color = color;
        card.GetComponent<LayoutElement>().preferredWidth = 150f;

        Transform valueTransform = card.transform.Find("Value");
        GameObject value = valueTransform != null
                ? valueTransform.gameObject
                : new GameObject("Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        value.layer = LayerMask.NameToLayer("UI");
        value.transform.SetParent(card.transform, false);
        RectTransform rect = value.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8f, 6f);
        rect.offsetMax = new Vector2(-8f, -6f);

        Text text = value.GetComponent<Text>();
        if (text == null)
        {
            TextMeshProUGUI oldText = value.GetComponent<TextMeshProUGUI>();
            if (oldText != null)
            {
                if (Application.isPlaying) Destroy(oldText);
                else DestroyImmediate(oldText);
            }
            text = value.AddComponent<Text>();
        }
        text.text = label + "  0";
        text.fontSize = 23;
        Canvas canvas = parent.GetComponentInParent<Canvas>();
        Text koreanText = canvas == null
                ? null
                : canvas.GetComponentsInChildren<Text>(true)
                        .FirstOrDefault(candidate => candidate != text
                                && candidate.font != null
                                && candidate.font.name == "LegacyRuntime");
        text.font = koreanText != null
                ? koreanText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 16;
        text.resizeTextMaxSize = 23;
        text.raycastTarget = false;
        return text;
    }

    public void OpenTab(int index)
    {
        for (int i = 0; i < viewObjects.Length; i++)
        {
            viewObjects[i].SetActive(i == index);
        }
        if (mythicBreedingController != null)
        {
            mythicBreedingController.SetLobbyTab(index);
        }
        Debug.Log($"{index}번 탭으로 이동했습니다.");
    }

    // 서버에서 데이터를 가져오는 핵심 함수
    public void LoadLobbyData(Action<bool> onCompleted = null)
    {
        Debug.Log("서버에 유저 정보를 요청합니다...");

        NetworkManager.Instance.Get($"/lobby/info/{CurrentUsername}",
            (json) => {
                // 성공: JSON 데이터를 C# 객체로 변환
                LobbyResponseDto data = JsonUtility.FromJson<LobbyResponseDto>(json);
                if (data != null && data.user != null && !string.IsNullOrWhiteSpace(data.user.username))
                {
                    currentUsername = data.user.username;
                }
                
                // 1. 상단 바 UI 갱신
                UpdateTopBarUI(data.user);

                // 2. 유닛 목록 생성
                SpawnMyUnits(data.aliens);

                mythicBreedingController?.RefreshStatus();

                Debug.Log($"{data.user.username}님 로비 로드 성공!");
                onCompleted?.Invoke(true);
            }, 
            (error) => {
                Debug.LogError("서버 연결 실패: " + error);
                onCompleted?.Invoke(false);
            }
        );
    }

    public void UpdateRemainingDiamond(int remainingDiamond)
    {
        text_Diamond.text = remainingDiamond.ToString("N0");
    }

    // 상단 재화 UI 업데이트
    void UpdateTopBarUI(UserDto user)
    {
        text_UserName.text = user.username;
        text_UserLevel.text = user.accountLevel.ToString();
        text_Heart.text = user.heart.ToString();
        text_Gold.text = user.gold.ToString("N0"); // 1,000 단위 콤마
        text_Diamond.text = user.diamond.ToString("N0");
        if (text_UniversalPiece != null)
            text_UniversalPiece.text = LobbyMaterialCurrencyFormatter.FormatUniversalPiece(user.universalPiece);
        if (text_GrowthCell != null)
            text_GrowthCell.text = LobbyMaterialCurrencyFormatter.FormatGrowthCell(user.growthCell);
    }

    // 서버에서 받은 리스트만큼 카드 생성
void SpawnMyUnits(List<AlienInventoryDto> aliens)
    {
        foreach (Transform child in unitGridContent)
        {
            Destroy(child.gameObject);
        }

        if (aliens == null)
        {
            return;
        }

        Dictionary<long, AlienInventoryDto> aliensById = aliens.ToDictionary(alien => alien.id);
        AlienCollectionItem[] collectionItems = aliens.Select(alien => new AlienCollectionItem
        {
            AlienId = alien.id,
            Grade = alien.grade,
            Level = alien.level,
            Pieces = alien.pieces,
            Owned = alien.owned
        }).ToArray();

        IReadOnlyList<long> ownedAlienIds = AlienCollectionOrdering.OwnedAlienIds(collectionItems);
        foreach (long alienId in ownedAlienIds)
        {
            CreateUnitCard(aliensById[alienId]);
        }

        IReadOnlyList<long> lockedMythicIds = AlienCollectionOrdering.LockedMythicAlienIds(collectionItems);
        if (lockedMythicIds.Count == 0)
        {
            return;
        }

        CreateLockedMythicSectionHeader(ownedAlienIds.Count);
        foreach (long alienId in lockedMythicIds)
        {
            CreateUnitCard(aliensById[alienId]);
        }
    }

    private void CreateUnitCard(AlienInventoryDto alien)
    {
        GameObject card = Instantiate(unitCardPrefab, unitGridContent);
        card.name = "UnitCard_" + alien.id;
        UnitCardUI cardScript = card.GetComponent<UnitCardUI>();

        if (cardScript != null)
        {
            cardScript.SetData(alien, alienId =>
            {
                if (alienDetailController != null)
                {
                    alienDetailController.Open(alienId);
                }
            });
        }
    }

    private void CreateLockedMythicSectionHeader(int precedingCardCount)
    {
        GridLayoutGroup grid = unitGridContent.GetComponent<GridLayoutGroup>();
        int columnCount = CalculateGridColumnCount(grid);
        int remainder = precedingCardCount % columnCount;
        if (remainder != 0)
        {
            for (int i = remainder; i < columnCount; i++)
            {
                CreateGridSpacer("SectionRowSpacer");
            }
        }

        int titleColumn = columnCount / 2;
        for (int column = 0; column < columnCount; column++)
        {
            if (column == titleColumn)
            {
                CreateSectionTitle();
            }
            else
            {
                CreateGridSpacer("SectionTitleSpacer");
            }
        }
    }

    private int CalculateGridColumnCount(GridLayoutGroup grid)
    {
        if (grid == null || !(unitGridContent is RectTransform contentRect))
        {
            return 1;
        }

        float availableWidth = contentRect.rect.width - grid.padding.horizontal;
        float cellAndSpacing = grid.cellSize.x + grid.spacing.x;
        return Mathf.Max(1, Mathf.FloorToInt((availableWidth + grid.spacing.x) / cellAndSpacing));
    }

    private void CreateSectionTitle()
    {
        GameObject titleObject = new GameObject("LockedMythicSectionTitle", typeof(RectTransform), typeof(Text));
        titleObject.transform.SetParent(unitGridContent, false);

        Text title = titleObject.GetComponent<Text>();
        title.text = "미해금 신화";
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 34;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        title.raycastTarget = false;
    }

    private void CreateGridSpacer(string objectName)
    {
        GameObject spacer = new GameObject(objectName, typeof(RectTransform));
        spacer.transform.SetParent(unitGridContent, false);
    }
}
