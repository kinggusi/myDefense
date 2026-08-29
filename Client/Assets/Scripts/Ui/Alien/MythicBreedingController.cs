using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public sealed class MythicBreedingController : MonoBehaviour
{
    private const string ScreenResource = "Prefabs/Lobby/MythicBreeding/MythicBreedingScreen";
    private const string ShortcutResource = "Prefabs/Lobby/MythicBreeding/MythicBreedingShortcut";
    private const string CombinationRelativePath = "Balance/generated/mythic-breeding-results.json";

    private readonly List<long> selectedParents = new List<long>(2);
    private readonly Dictionary<string, string> pendingRequestIds = new Dictionary<string, string>();
    private readonly Dictionary<int, Text> slotLabelTexts = new Dictionary<int, Text>();
    private readonly List<MythicBreedingShortcutView> shortcuts = new List<MythicBreedingShortcutView>();
    private LobbyManager lobby;
    private MythicBreedingUiView view;
    private MythicBreedingSlotsResponse slots;
    private MythicBreedingCandidatesResponse candidates;
    private int selectedSlot = 1;
    private bool requesting;
    private bool summaryRequesting;
    private bool combinationLoaded;
    private float nextCountdownRefreshAt;
    private bool readyRefreshRequested;

    public void Initialize(LobbyManager manager)
    {
        if (lobby != null) return;
        lobby = manager;
        BuildFromPrefabs();
    }

    private void BuildFromPrefabs()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        GameObject screenPrefab = Resources.Load<GameObject>(ScreenResource);
        GameObject shortcutPrefab = Resources.Load<GameObject>(ShortcutResource);
        if (canvas == null || screenPrefab == null || shortcutPrefab == null)
        {
            Debug.LogWarning("[Breeding] 정식 Breeding UI Prefab을 찾지 못했습니다.");
            return;
        }

        GameObject screen = Instantiate(screenPrefab, canvas.transform, false);
        screen.name = "MythicBreedingScreen";
        view = screen.GetComponent<MythicBreedingUiView>();
        if (view == null)
        {
            Debug.LogError("[Breeding] MythicBreedingUiView 참조가 없습니다.");
            Destroy(screen);
            return;
        }
        screen.SetActive(false);
        WireScreenEvents();

        CreateShortcut(shortcutPrefab, lobby.viewObjects[1].transform, "신화 교배", "부모 2종으로 24시간 교배", new Vector2(-24f, -165f));
    }

    private void CreateShortcut(GameObject prefab, Transform parent, string title, string subtitle, Vector2 position)
    {
        MythicBreedingShortcutView shortcut = Instantiate(prefab, parent, false).GetComponent<MythicBreedingShortcutView>();
        shortcut.name = "MythicBreedingShortcut";
        shortcut.SetContent(title, subtitle, 0);
        shortcut.button.onClick.AddListener(Open);
        RectTransform rect = shortcut.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = position;
        shortcuts.Add(shortcut);
    }

    private void WireScreenEvents()
    {
        view.backButton.onClick.AddListener(Close);
        view.combinationButton.onClick.AddListener(OpenCombinationTable);
        view.combinationCloseButton.onClick.AddListener(() => view.combinationPanel.SetActive(false));
        view.primaryActionButton.onClick.AddListener(PrimaryAction);
        view.accelerateButton.onClick.AddListener(() => Accelerate(1));
        view.instantButton.onClick.AddListener(AccelerateInstant);
        view.combinationPanel.SetActive(false);
    }

    public void SetLobbyTab(int index)
    {
        if (view != null && view.gameObject.activeSelf) Close();
        if (index == 1) RefreshStatus();
    }

    public void RefreshStatus()
    {
        if (summaryRequesting || lobby == null || NetworkManager.Instance == null) return;
        summaryRequesting = true;
        string user = UnityWebRequest.EscapeURL(lobby.CurrentUsername);
        NetworkManager.Instance.Get(MythicBreedingClientContract.SlotsPath(user), json =>
        {
            summaryRequesting = false;
            slots = JsonUtility.FromJson<MythicBreedingSlotsResponse>(json);
            UpdateShortcuts();
        }, _ => summaryRequesting = false);
    }

    public void Open()
    {
        if (view == null || requesting) return;
        view.gameObject.SetActive(true);
        view.transform.SetAsLastSibling();
        selectedParents.Clear();
        LoadAll();
    }

    public void Close()
    {
        if (requesting || view == null) return;
        view.combinationPanel.SetActive(false);
        view.gameObject.SetActive(false);
    }

    private void LoadAll()
    {
        requesting = true;
        SetBusy(true);
        SetStatus("교배 정보를 불러오는 중...");
        string user = UnityWebRequest.EscapeURL(lobby != null ? lobby.CurrentUsername : string.Empty);
        NetworkManager.Instance.Get(MythicBreedingClientContract.SlotsPath(user), json =>
        {
            slots = JsonUtility.FromJson<MythicBreedingSlotsResponse>(json);
            NetworkManager.Instance.Get(MythicBreedingClientContract.CandidatesPath(user), candidateJson =>
            {
                candidates = JsonUtility.FromJson<MythicBreedingCandidatesResponse>(candidateJson);
                requesting = false;
                Render();
            }, Fail);
        }, Fail);
    }

    private void Render()
    {
        ClearGenerated(view.slotRoot, view.slotButtonTemplate.gameObject);
        ClearGenerated(view.candidateRoot, view.candidateButtonTemplate.gameObject);
        slotLabelTexts.Clear();
        readyRefreshRequested = false;
        if (slots?.slots != null)
            foreach (MythicBreedingSlotDto slot in slots.slots)
            {
                int slotNo = slot.slotNo;
                Button button = Instantiate(view.slotButtonTemplate, view.slotRoot, false);
                button.name = "Slot" + slotNo;
                button.gameObject.SetActive(true);
                Text label = button.GetComponentInChildren<Text>();
                label.text = "슬롯 " + slotNo + "\n" + SlotLabel(slot, DateTime.UtcNow);
                slotLabelTexts[slotNo] = label;
                button.onClick.AddListener(() =>
                {
                    if (requesting) return;
                    selectedSlot = slotNo;
                    selectedParents.Clear();
                    Render();
                });
            }

        MythicBreedingSlotDto active = ActiveSlot();
        bool canSelectParents = active != null && MythicBreedingClientContract.CanSelectParents(active.status);
        if (candidates?.candidates != null)
            foreach (MythicBreedingCandidateDto candidate in candidates.candidates)
            {
                MythicBreedingCandidateDto captured = candidate;
                bool selected = selectedParents.Contains(candidate.userAlienId);
                Button button = Instantiate(view.candidateButtonTemplate, view.candidateRoot, false);
                button.name = "Candidate" + candidate.alienId;
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<Text>().text = "신화 " + (candidate.alienId - 28) + "\n" + candidate.name + "  Lv." + candidate.level + (selected ? "  ✓" : string.Empty);
                button.interactable = canSelectParents && candidate.selectable;
                button.onClick.AddListener(() => ToggleParent(captured));
            }

        view.parentSelectionText.text = selectedParents.Count == 0
            ? "서로 다른 보유 신화 2종을 선택하세요."
            : "선택 부모: " + string.Join(" + ", selectedParents);

        bool breeding = active != null && active.status == "BREEDING";
        view.primaryActionButton.gameObject.SetActive(!breeding);
        view.accelerateButton.gameObject.SetActive(breeding);
        view.instantButton.gameObject.SetActive(breeding);
        view.accelerateButton.GetComponentInChildren<Text>().text = slots == null
            ? "10분 단축"
            : "10분 단축 (" + slots.accelerationUnitDiamondCost.ToString("N0") + ")";
        if (active == null)
        {
            view.primaryActionButton.interactable = false;
            SetBusy(false);
            return;
        }
        Text actionText = view.primaryActionButton.GetComponentInChildren<Text>();
        if (active.status == "LOCKED")
        {
            actionText.text = UnlockLabel(active.slotNo);
            view.primaryActionButton.interactable = true;
        }
        else if (active.status == "AVAILABLE")
        {
            actionText.text = "교배 시작";
            view.primaryActionButton.interactable = selectedParents.Count == 2;
        }
        else if (active.status == "REWARD_READY")
        {
            actionText.text = "보상 수령";
            view.primaryActionButton.interactable = true;
        }
        UpdateShortcuts();
        SetStatus("부모는 소비되지 않으며 전투·강화에 계속 사용할 수 있습니다.");
        SetBusy(false);
    }

    private void UpdateShortcuts()
    {
        int ready = MythicBreedingClientContract.CountRewardReady(slots);
        string status = MythicBreedingClientContract.BuildShortcutStatus(slots);
        for (int i = 0; i < shortcuts.Count; i++)
        {
            shortcuts[i].SetContent("신화 교배", status, ready);
        }
    }

    private void ToggleParent(MythicBreedingCandidateDto candidate)
    {
        if (requesting || !MythicBreedingClientContract.CanSelectParents(ActiveSlot()?.status) || !candidate.selectable) return;
        if (selectedParents.Contains(candidate.userAlienId)) selectedParents.Remove(candidate.userAlienId);
        else
        {
            if (selectedParents.Count == 2) selectedParents.RemoveAt(0);
            selectedParents.Add(candidate.userAlienId);
        }
        Render();
    }

    private void PrimaryAction()
    {
        MythicBreedingSlotDto slot = ActiveSlot();
        if (slot == null || requesting) return;
        if (slot.status == "LOCKED")
        {
            string key = MythicBreedingClientContract.IntentKey("unlock", selectedSlot);
            Post("unlock", new MythicBreedingUnlockRequest { requestId = RequestIdFor(key) }, key,
                _ => lobby.LoadLobbyData(__ => LoadAll()));
        }
        else if (slot.status == "AVAILABLE" && selectedParents.Count == 2)
        {
            string key = MythicBreedingClientContract.IntentKey("start", selectedSlot, selectedParents[0], selectedParents[1]);
            Post("start", new MythicBreedingStartRequest { parentUserAlienIdA = selectedParents[0], parentUserAlienIdB = selectedParents[1], requestId = RequestIdFor(key) }, key, _ => LoadAll());
        }
        else if (slot.status == "REWARD_READY")
        {
            string key = MythicBreedingClientContract.IntentKey("claim", selectedSlot);
            Post("claim", new MythicBreedingClaimRequest { requestId = RequestIdFor(key) }, key, json =>
            {
                MythicBreedingClaimResponse reward = JsonUtility.FromJson<MythicBreedingClaimResponse>(json);
                SetStatus("신화 " + (reward.resultAlienId - 28) + " 보상을 수령했습니다.");
                lobby.LoadLobbyData(_ => LoadAll());
            });
        }
    }

    private void Accelerate(int units)
    {
        if (units <= 0 || requesting) return;
        string key = MythicBreedingClientContract.IntentKey("accelerate", selectedSlot, units);
        Post("accelerate", new MythicBreedingAccelerateRequest { requestId = RequestIdFor(key), units = units }, key, json =>
        {
            MythicBreedingAccelerateResponse response = JsonUtility.FromJson<MythicBreedingAccelerateResponse>(json);
            lobby.UpdateRemainingDiamond(response.remainingDiamond);
            LoadAll();
        });
    }

    private void AccelerateInstant()
    {
        MythicBreedingSlotDto slot = ActiveSlot();
        if (slot == null || slots == null || !DateTime.TryParse(slot.readyAt, null, DateTimeStyles.RoundtripKind, out DateTime ready)) return;
        Accelerate(MythicBreedingClientContract.CalculateAccelerationUnits(ready, DateTime.UtcNow, slots.accelerationUnitSeconds));
    }

    private void OpenCombinationTable()
    {
        if (view == null) return;
        view.combinationPanel.SetActive(true);
        if (!combinationLoaded) StartCoroutine(LoadCombinationTable());
    }

    private IEnumerator LoadCombinationTable()
    {
        view.combinationText.text = "공개 조합표를 불러오는 중...";
        string path = Path.Combine(Application.streamingAssetsPath, CombinationRelativePath).Replace('\\', '/');
        using UnityWebRequest request = UnityWebRequest.Get(path);
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            view.combinationText.text = "조합표를 불러오지 못했습니다.\n" + request.error;
            yield break;
        }
        MythicBreedingRecipeDocument document = JsonUtility.FromJson<MythicBreedingRecipeDocument>(request.downloadHandler.text);
        view.combinationText.text = MythicBreedingClientContract.BuildCombinationTable(document);
        combinationLoaded = true;
    }

    private void Post<T>(string action, T body, string intentKey, Action<string> success)
    {
        requesting = true;
        SetBusy(true);
        string user = UnityWebRequest.EscapeURL(lobby.CurrentUsername);
        NetworkManager.Instance.PostJson(MythicBreedingClientContract.SlotActionPath(selectedSlot, action, user), JsonUtility.ToJson(body), json =>
        {
            requesting = false;
            pendingRequestIds.Remove(intentKey);
            success(json);
        }, Fail);
    }

    private string RequestIdFor(string intentKey)
    {
        if (!pendingRequestIds.TryGetValue(intentKey, out string requestId))
        {
            requestId = Guid.NewGuid().ToString();
            pendingRequestIds[intentKey] = requestId;
        }
        return requestId;
    }

    private void Fail(string error)
    {
        requesting = false;
        SetBusy(false);
        SetStatus("요청 실패: " + error);
    }

    private MythicBreedingSlotDto ActiveSlot()
    {
        if (slots?.slots == null) return null;
        foreach (MythicBreedingSlotDto slot in slots.slots)
            if (slot.slotNo == selectedSlot) return slot;
        return null;
    }

    private string UnlockLabel(int slotNo)
    {
        int price = slotNo == 2 ? slots.slot2GemPrice : slots.slot3GemPrice;
        return slotNo == 2
            ? "Lv." + slots.slot2UnlockLevel + " 또는 " + price.ToString("N0") + " 다이아"
            : price.ToString("N0") + " 다이아로 해금";
    }

    private static string SlotLabel(MythicBreedingSlotDto slot, DateTime nowUtc)
    {
        if (slot.status == "LOCKED") return "잠김";
        if (slot.status == "AVAILABLE") return "사용 가능";
        if (slot.status == "REWARD_READY") return "보상 수령 가능";
        if (!DateTime.TryParse(slot.readyAt, null, DateTimeStyles.RoundtripKind, out DateTime readyAt)) return "교배 중";
        return "교배 중\n" + MythicBreedingClientContract.FormatRemainingTime(readyAt, nowUtc);
    }

    private void Update()
    {
        if (view == null || !view.gameObject.activeSelf || slots?.slots == null || requesting ||
            Time.unscaledTime < nextCountdownRefreshAt) return;

        nextCountdownRefreshAt = Time.unscaledTime + 0.25f;
        DateTime nowUtc = DateTime.UtcNow;
        bool becameReady = false;
        foreach (MythicBreedingSlotDto slot in slots.slots)
        {
            if (slot != null && slotLabelTexts.TryGetValue(slot.slotNo, out Text label))
                label.text = "슬롯 " + slot.slotNo + "\n" + SlotLabel(slot, nowUtc);
            if (slot?.status == "BREEDING" &&
                DateTime.TryParse(slot.readyAt, null, DateTimeStyles.RoundtripKind, out DateTime readyAt) &&
                readyAt.ToUniversalTime() <= nowUtc)
                becameReady = true;
        }

        if (becameReady && !readyRefreshRequested)
        {
            readyRefreshRequested = true;
            LoadAll();
        }
    }

    private void SetBusy(bool busy)
    {
        if (view == null) return;
        if (view.inputCanvasGroup != null)
        {
            view.inputCanvasGroup.interactable = !busy;
            view.inputCanvasGroup.blocksRaycasts = true;
        }
        view.backButton.interactable = !busy;
        view.combinationButton.interactable = !busy;
        if (busy)
        {
            view.primaryActionButton.interactable = false;
            view.accelerateButton.interactable = false;
            view.instantButton.interactable = false;
        }
        else
        {
            view.accelerateButton.interactable = true;
            view.instantButton.interactable = true;
        }
    }

    private void SetStatus(string message)
    {
        if (view != null && view.statusText != null) view.statusText.text = message;
    }

    private static void ClearGenerated(Transform root, GameObject template)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            if (root.GetChild(i).gameObject != template) Destroy(root.GetChild(i).gameObject);
    }
}
