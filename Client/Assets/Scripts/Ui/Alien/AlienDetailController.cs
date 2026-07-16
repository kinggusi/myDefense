using System.Collections;
using AlienUpgrade.Core;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public sealed class AlienDetailController : MonoBehaviour
{
    private const float UpgradeRearmDelaySeconds = 0.5f;

    [Header("화면")]
    public GameObject alienDetailScreen;
    public GameObject bottomNavigation;
    public GameObject sharedStatusBar;

public Button backButton;
    public Button upgradeButton;
    public GameObject loadingObject;
    public Text errorText;

    [Header("Alien")]
    public Image alienImage;
    public Sprite placeholderSprite;
    public Text alienNameText;
    public Text gradeText;

    [Header("능력치")]
    public Text levelText;
    public Text attackText;
    public Text mpText;
    public Text attackSpeedText;
    public Text rangeText;

    [Header("강화 재화")]
    public Text piecesText;
    public Text goldText;
    public Text growthCellText;
    public Text universalPieceText;

    [Header("연결")]
    public LobbyManager lobbyManager;

    private readonly AlienUpgradeFlow flow = new AlienUpgradeFlow();
    private bool upgradeSubmittedForCurrentOpen;
    private Coroutine upgradeRearmCoroutine;
    private void Awake()
    {
        if (alienDetailScreen == null)
        {
            alienDetailScreen = gameObject;
        }
    }

    private void OnEnable()
    {
        BindListeners();
    }

    private void OnDisable()
    {
        UnbindListeners();
        if (upgradeRearmCoroutine != null)
        {
            StopCoroutine(upgradeRearmCoroutine);
            upgradeRearmCoroutine = null;
        }
    }

    private void BindListeners()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(Close);
            backButton.onClick.AddListener(Close);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(Upgrade);
            upgradeButton.onClick.AddListener(Upgrade);
        }
    }

    private void UnbindListeners()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(Close);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(Upgrade);
        }
    }

public void Open(long alienId)
    {
        if (alienDetailScreen != null && alienDetailScreen.activeSelf)
        {
            return;
        }

        if (!flow.BeginStatusRequest(alienId))
        {
            return;
        }

        upgradeSubmittedForCurrentOpen = false;

        if (alienDetailScreen == null)
        {
            alienDetailScreen = gameObject;
        }

        alienDetailScreen.SetActive(true);
        if (bottomNavigation != null)
        {
            bottomNavigation.SetActive(false);
        }

        if (sharedStatusBar != null)
        {
            sharedStatusBar.SetActive(true);
            sharedStatusBar.transform.SetAsLastSibling();
        }

        if (alienImage != null)
        {
            alienImage.sprite = placeholderSprite;
        }

        SetRequestingUi(true);
        ClearError();
        RequestStatus(alienId);
    }

    public void Close()
    {
        if (!flow.CanClose)
        {
            return;
        }

        if (alienDetailScreen != null)
        {
            alienDetailScreen.SetActive(false);
        }

        if (bottomNavigation != null)
        {
            bottomNavigation.SetActive(true);
        }
    }

    public void Upgrade()
    {
        if (upgradeSubmittedForCurrentOpen)
        {
            return;
        }

        if (!flow.BeginUpgradeRequest())
        {
            return;
        }

        upgradeSubmittedForCurrentOpen = true;

        SetRequestingUi(true);
        ClearError();

        string username = lobbyManager != null ? lobbyManager.CurrentUsername : string.Empty;
        string uri = "/aliens/" + flow.View.AlienId + "/upgrade?username=" + UnityWebRequest.EscapeURL(username);

        if (NetworkManager.Instance == null)
        {
            HandleFailure("네트워크 연결을 확인할 수 없습니다.");
            return;
        }

        NetworkManager.Instance.PostJsonAsync<EmptyRequestBody, AlienUpgradeResponseDto>(
            uri,
            new EmptyRequestBody(),
            HandleUpgradeResult);
    }

    private void RequestStatus(long alienId)
    {
        string username = lobbyManager != null ? lobbyManager.CurrentUsername : string.Empty;
        string uri = "/aliens/" + alienId + "/upgrade-status?username=" + UnityWebRequest.EscapeURL(username);

        if (NetworkManager.Instance == null)
        {
            HandleFailure("네트워크 연결을 확인할 수 없습니다.");
            return;
        }

        NetworkManager.Instance.Get(
            uri,
            json =>
            {
                AlienUpgradeStatusDto response = JsonUtility.FromJson<AlienUpgradeStatusDto>(json);
                flow.CompleteStatus(response);
                SetRequestingUi(false);
                RefreshUi();
            },
            error => HandleFailure("Alien 강화 정보를 불러오지 못했습니다."));
    }

    private void HandleUpgradeResult(ApiResult<AlienUpgradeResponseDto> result)
    {
        if (result != null && result.IsSuccess && result.Data != null)
        {
            flow.CompleteUpgrade(result.Data);
            RefreshUi();

            if (flow.TryConsumeLobbyRefresh() && lobbyManager != null)
            {
                lobbyManager.LoadLobbyData(HandleLobbyRefreshCompleted);
            }
            else
            {
                HandleLobbyRefreshCompleted(lobbyManager != null);
            }

            return;
        }

        string code = result != null && result.Error != null ? result.Error.code : string.Empty;
        string fallback = result != null && result.Error != null ? result.Error.message : null;
        if (string.IsNullOrWhiteSpace(fallback) && result != null)
        {
            fallback = result.NetworkError;
        }

        bool requestDefinitelyRejected = result != null &&
                                         result.StatusCode >= 400 &&
                                         result.StatusCode < 500;
        string message = AlienUpgradeFlow.MessageForError(code, fallback);
        if (!requestDefinitelyRejected)
        {
            message += " 서버 처리 여부가 불확실하므로 상세 화면을 다시 열어 상태를 확인해 주세요.";
        }

        HandleFailure(message, requestDefinitelyRejected);
    }

    private void HandleLobbyRefreshCompleted(bool succeeded)
    {
        flow.CompleteLobbyRefresh(
            succeeded ? null : "강화는 완료됐지만 로비 정보를 갱신하지 못했습니다. 다시 강화하기 전에 상세 정보를 확인해 주세요.");
        SetRequestingUi(false);
        RefreshUi();

        if (succeeded && flow.View.CanUpgrade && upgradeRearmCoroutine == null)
        {
            upgradeRearmCoroutine = StartCoroutine(RearmUpgradeAfterDelay());
        }
    }

    private IEnumerator RearmUpgradeAfterDelay()
    {
        yield return new WaitForSecondsRealtime(UpgradeRearmDelaySeconds);

        upgradeRearmCoroutine = null;
        if (flow.RearmUpgrade())
        {
            upgradeSubmittedForCurrentOpen = false;
            RefreshUi();
        }
    }

    private void HandleFailure(string message, bool allowRetryInCurrentOpen = true)
    {
        flow.Fail(message);
        if (allowRetryInCurrentOpen)
        {
            upgradeSubmittedForCurrentOpen = false;
        }
        SetRequestingUi(false);
        RefreshUi();
    }

    private void SetRequestingUi(bool requesting)
    {
        if (loadingObject != null)
        {
            loadingObject.SetActive(requesting);
        }

        if (backButton != null)
        {
            backButton.interactable = !requesting;
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = !requesting && flow.CanStartUpgrade();
        }
    }

    private void RefreshUi()
    {
        AlienUpgradeViewModel view = flow.View;

        if (alienNameText != null) alienNameText.text = view.AlienName ?? string.Empty;
        if (gradeText != null) gradeText.text = view.Grade ?? string.Empty;
        if (levelText != null) levelText.text = "Level " + view.Level + " / " + view.MaxLevel;
        if (attackText != null) attackText.text = "Attack " + view.Attack.ToString("0.##");
        if (mpText != null) mpText.text = "MP " + view.Mp.ToString("0.##");
        if (attackSpeedText != null) attackSpeedText.text = "Attack Speed " + view.AttackSpeed.ToString("0.##");
        if (rangeText != null) rangeText.text = "Range " + view.Range.ToString("0.##");
        if (piecesText != null) piecesText.text = "Pieces " + view.CurrentPieces + " / " + view.RequiredPieces;
        if (goldText != null) goldText.text = "Gold " + view.Gold.ToString("N0") + " / " + view.RequiredGold.ToString("N0");
        if (growthCellText != null) growthCellText.text = "Growth Cell " + view.GrowthCell + " / " + view.RequiredGrowthCell;
        if (universalPieceText != null) universalPieceText.text = "Universal Piece " + view.UniversalPiece + " / " + view.RequiredUniversalPiece;

        if (upgradeButton != null)
        {
            upgradeButton.interactable = flow.CanStartUpgrade();
        }

        if (errorText != null)
        {
            errorText.text = flow.ErrorMessage ?? string.Empty;
            errorText.gameObject.SetActive(!string.IsNullOrWhiteSpace(errorText.text));
        }
    }

    private void ClearError()
    {
        if (errorText != null)
        {
            errorText.text = string.Empty;
            errorText.gameObject.SetActive(false);
        }
    }
}
