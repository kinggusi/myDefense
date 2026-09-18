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
    private string currentUsername;
    private bool initialized;

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
    private QuestController questController;



    void Start()
    {
        if (NetworkManager.AllowLegacyLobby && !MyDefense.Auth.DevelopmentLoginPolicy.IsRequested) InitializeForAccount(DefaultUsername, null);
        else MyDefense.Auth.AuthStartupController.Begin(this);
    }

    public void InitializeForAccount(string username, Action<bool> completed)
    {
        if (string.IsNullOrWhiteSpace(username)) { completed?.Invoke(false); return; }
        currentUsername = username;
        if (!initialized)
        {
        initialized = true;
        EnsureMaterialCurrencyUI();
        mythicBreedingController = GetComponent<MythicBreedingController>();
        if (mythicBreedingController == null)
        {
            mythicBreedingController = gameObject.AddComponent<MythicBreedingController>();
        }
        mythicBreedingController.Initialize(this);
        questController = GetComponent<QuestController>();
        if (questController == null)
        {
            questController = gameObject.AddComponent<QuestController>();
        }
        questController.Initialize(this);
        gameObject.AddComponent<MyDefense.Auth.AuthAccountPanel>().Install(this);
        // 1. 처음엔 메인 화면(2번 탭) 띄우기
        OpenTab(2);
        }
        LoadLobbyData(completed);
    }

    private void EnsureMaterialCurrencyUI()
    {
        Canvas canvas = viewObjects != null && viewObjects.Length > 1 && viewObjects[1] != null
                ? viewObjects[1].GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
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
        panelRect.sizeDelta = new Vector2(450f, 118f);

        HorizontalLayoutGroup layout = panel.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        text_UniversalPiece = EnsureMaterialCard(panel.transform, "WakjeoDnaCard", LobbyMaterialIconGraphic.MaterialKind.WakjeoDna);
        text_GrowthCell = EnsureMaterialCard(panel.transform, "GrowthCellCard", LobbyMaterialIconGraphic.MaterialKind.GrowthCell);

        Transform catalystCard = panel.transform.Find("MutationCatalystCard");
        if (catalystCard != null)
        {
            if (Application.isPlaying) Destroy(catalystCard.gameObject);
            else DestroyImmediate(catalystCard.gameObject);
        }
        LobbyFinalPresentation.StyleMaterials(panel.transform);
    }

    private Text EnsureMaterialCard(Transform parent, string cardName, LobbyMaterialIconGraphic.MaterialKind kind)
    {
        Transform existing = parent.Find(cardName);
        GameObject card = existing != null
                ? existing.gameObject
                : new GameObject(cardName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        card.layer = LayerMask.NameToLayer("UI");
        card.transform.SetParent(parent, false);
        card.GetComponent<Image>().color = Color.clear;
        card.GetComponent<LayoutElement>().preferredWidth = 220f;

        Transform border = card.transform.Find("LobbyNeonFrame");
        if (border == null)
        {
            var borderObject = new GameObject("LobbyNeonFrame", typeof(RectTransform), typeof(LayoutElement));
            borderObject.layer = card.layer;
            borderObject.transform.SetParent(card.transform, false);
            border = borderObject.transform;
        }
        var borderRect = (RectTransform)border;
        borderRect.anchorMin = Vector2.zero; borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = borderRect.offsetMax = Vector2.zero;
        border.SetAsFirstSibling();
        border.GetComponent<LayoutElement>().ignoreLayout = true;
        var materialFrame = border.GetComponent<LobbyNeonGraphic>();
        if (materialFrame == null) materialFrame = border.gameObject.AddComponent<LobbyNeonGraphic>();
        materialFrame.Configure(LobbyNeonGraphic.FrameStyle.Panel);
        materialFrame.raycastTarget = false;

        Transform iconTransform = card.transform.Find("MaterialIcon");
        var iconObject = iconTransform != null ? iconTransform.gameObject : new GameObject("MaterialIcon", typeof(RectTransform));
        iconObject.layer = card.layer;
        iconObject.transform.SetParent(card.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(.04f,.20f); iconRect.anchorMax = new Vector2(.29f,.80f);
        iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
        var icon = iconObject.GetComponent<LobbyMaterialIconGraphic>();
        if (icon == null) icon = iconObject.AddComponent<LobbyMaterialIconGraphic>();
        icon.Configure(kind);

        Transform plusTransform = card.transform.Find("PurchasePlus");
        var plusObject = plusTransform != null ? plusTransform.gameObject : new GameObject("PurchasePlus", typeof(RectTransform));
        plusObject.layer = card.layer;
        plusObject.transform.SetParent(card.transform, false);
        var plusRect = plusObject.GetComponent<RectTransform>();
        plusRect.anchorMin = new Vector2(.76f,.5f); plusRect.anchorMax = new Vector2(.93f,.5f);
        plusRect.offsetMin = plusRect.offsetMax = Vector2.zero;
        var square = plusObject.GetComponent<AspectRatioFitter>();
        if (square == null) square = plusObject.AddComponent<AspectRatioFitter>();
        square.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
        square.aspectRatio = 1f;
        var plusFrame = plusObject.GetComponent<LobbyNeonGraphic>();
        if (plusFrame == null) plusFrame = plusObject.AddComponent<LobbyNeonGraphic>();
        plusFrame.Configure(LobbyNeonGraphic.FrameStyle.Button);
        var plus = plusObject.GetComponent<Button>();
        if (plus == null) plus = plusObject.AddComponent<Button>();
        plus.targetGraphic = plusFrame;
        plus.onClick.RemoveAllListeners();
        plus.onClick.AddListener(() =>
        {
            var popup = GetComponent<LobbyMaterialPurchasePopup>();
            if (popup == null) popup = gameObject.AddComponent<LobbyMaterialPurchasePopup>();
            popup.Open(kind);
        });
        Transform plusLabel = plusObject.transform.Find("Label");
        if (plusLabel == null)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.layer = card.layer; labelObject.transform.SetParent(plusObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            Text label = labelObject.GetComponent<Text>(); label.text = "+";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 32;
            label.alignment = TextAnchor.MiddleCenter; label.color = new Color(.66f,.83f,.87f); label.raycastTarget = false;
        }

        Transform valueTransform = card.transform.Find("Value");
        GameObject value = valueTransform != null
                ? valueTransform.gameObject
                : new GameObject("Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        value.layer = LayerMask.NameToLayer("UI");
        value.transform.SetParent(card.transform, false);
        RectTransform rect = value.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.29f,.12f);
        rect.anchorMax = new Vector2(.74f,.88f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;

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
        text.text = "0";
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
        questController?.SetLobbyTab(index);
        GetComponent<LobbyMaterialPurchasePopup>()?.Close();
        GetComponent<LobbyFinalPresentation>()?.SelectTab(index);
        Debug.Log($"{index}번 탭으로 이동했습니다.");
    }

    // 서버에서 데이터를 가져오는 핵심 함수
    public void LoadLobbyData(Action<bool> onCompleted = null)
    {
        if (string.IsNullOrWhiteSpace(CurrentUsername)) { onCompleted?.Invoke(false); return; }
        Debug.Log("서버에 유저 정보를 요청합니다...");

        NetworkManager.Instance.Get($"/lobby/info/{CurrentUsername}",
            (json) => {
                try
                {
                // 성공: JSON 데이터를 C# 객체로 변환
                LobbyResponseDto data = JsonUtility.FromJson<LobbyResponseDto>(json);
                if (data == null || data.user == null || data.user.username != currentUsername)
                    throw new InvalidOperationException("Lobby account mismatch.");
                
                // 1. 상단 바 UI 갱신
                UpdateTopBarUI(data.user);

                // 2. 유닛 목록 생성
                SpawnMyUnits(data.aliens);

                mythicBreedingController?.RefreshStatus();
                questController?.RefreshStatus();

                Debug.Log($"{data.user.username}님 로비 로드 성공!");
                onCompleted?.Invoke(true);
                }
                catch (Exception)
                {
                    Debug.LogWarning("로비 응답을 적용하지 못했습니다. 다시 시도해 주세요.");
                    onCompleted?.Invoke(false);
                }
            }, 
            (error) => {
                Debug.LogWarning("로비 정보를 불러오지 못했습니다.");
                onCompleted?.Invoke(false);
            }
        );
    }

    public void UpdateRemainingDiamond(int remainingDiamond)
    {
        text_Diamond.text = remainingDiamond.ToString("N0");
    }

    public void UpdateQuestWallet(int gold, int universalPiece, int diamond)
    {
        if (text_Gold != null) text_Gold.text = gold.ToString("N0");
        if (text_Diamond != null) text_Diamond.text = diamond.ToString("N0");
        if (text_UniversalPiece != null)
            text_UniversalPiece.text = universalPiece.ToString("N0");
    }

    // 상단 재화 UI 업데이트
    void UpdateTopBarUI(UserDto user)
    {
        var session = MyDefense.Auth.AuthSession.Instance;
        text_UserName.text = FormatProfileName(session != null ? session.User : null, user.username);
        text_UserLevel.text = user.accountLevel.ToString();
        text_Heart.text = user.heart.ToString();
        text_Gold.text = user.gold.ToString("N0"); // 1,000 단위 콤마
        text_Diamond.text = user.diamond.ToString("N0");
        if (text_UniversalPiece != null)
            text_UniversalPiece.text = user.universalPiece.ToString("N0");
        if (text_GrowthCell != null)
            text_GrowthCell.text = user.growthCell.ToString("N0");
    }

    public static string FormatProfileName(MyDefense.Auth.AuthUser authenticatedUser, string username)
    {
        return authenticatedUser != null && authenticatedUser.isGuest && authenticatedUser.userId > 0
            ? "Guest-" + authenticatedUser.userId.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : username;
    }

    // 서버에서 받은 리스트만큼 카드 생성
    void SpawnMyUnits(List<AlienInventoryDto> aliens)
    {
        if (unitGridContent == null || unitCardPrefab == null) return;
        LobbyCollectionGrid collectionGrid = unitGridContent.GetComponent<LobbyCollectionGrid>();
        if (collectionGrid == null) collectionGrid = unitGridContent.gameObject.AddComponent<LobbyCollectionGrid>();
        collectionGrid.RefreshLayout();
        for (int i = unitGridContent.childCount - 1; i >= 0; i--)
        {
            Transform child = unitGridContent.GetChild(i);
            child.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
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

        foreach (long alienId in AlienCollectionOrdering.MainSectionAlienIds(collectionItems))
        {
            CreateUnitCard(aliensById[alienId]);
        }
        var lockedIds = AlienCollectionOrdering.LockedMythicAlienIds(collectionItems);
        if (lockedIds.Count > 0)
        {
            CreateLockedMythicHeader(lockedIds.Count);
            foreach (long alienId in lockedIds) CreateUnitCard(aliensById[alienId]);
        }
        LayoutRebuilder.MarkLayoutForRebuild((RectTransform)unitGridContent);
    }

    private void CreateLockedMythicHeader(int count)
    {
        var header = new GameObject(LobbySectionGridLayout.LockedHeaderName, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(LobbyNeonGraphic));
        header.layer = unitGridContent.gameObject.layer;
        header.transform.SetParent(unitGridContent, false);
        var frame = header.GetComponent<LobbyNeonGraphic>();
        frame.Configure(LobbyNeonGraphic.FrameStyle.Panel);
        frame.SetGrade("MYTHIC");
        frame.raycastTarget = false;
        var label = new GameObject("SectionTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        label.layer = header.layer;
        label.transform.SetParent(header.transform, false);
        var rect = (RectTransform)label.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(24f, 8f); rect.offsetMax = new Vector2(-24f, -8f);
        var text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "미해금 미스틱  ·  " + count + "종";
        text.fontSize = 28;
        text.color = new Color(.80f, .70f, .76f);
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
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

}
