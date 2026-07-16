using System.Collections;
using System.Linq;
using GachaShop.Core;
using UnityEngine;
using UnityEngine.UI;

public sealed class GachaShopController : MonoBehaviour
{
    [Header("Screen Navigation")]
    [SerializeField] private GameObject gachaScreen;
    [SerializeField] private Button enterGachaButton;
    [SerializeField] private Button backButton;

    [Header("Purchase Buttons")]
    [SerializeField] private Button singlePurchaseButton;
    [SerializeField] private Button tenPurchaseButton;
    [SerializeField] private Button retryButton;

    [Header("Machine Reveal")]
    [SerializeField] private RectTransform mascotRect;
    [SerializeField] private RectTransform machineRect;
    [SerializeField] private Image flashImage;
    [SerializeField] private Text speechText;

    [Header("Status")]
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private Text errorText;

    [Header("Result")]
    [SerializeField] private GameObject resultArea;
    [SerializeField] private GameObject singleResultContainer;
    [SerializeField] private GameObject tenResultGrid;
    [SerializeField] private GachaResultCardView singleResultCard;
    [SerializeField] private GachaResultCardView[] tenResultCards;
    [SerializeField] private Text resultSummaryText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button closeResultButton;

    [Header("Lobby")]
    [SerializeField] private LobbyManager lobbyManager;

    private readonly PurchaseRequestContext requestContext = new PurchaseRequestContext();
    private readonly GachaRevealState revealState = new GachaRevealState();
    private Coroutine revealCoroutine;
    private GachaPurchaseResponse currentResponse;
    private bool lobbyRefreshPending;

    private bool IsRequesting => requestContext.State == PurchaseRequestState.Requesting;

    private void Awake()
    {
        backButton.onClick.AddListener(CloseGachaScreen);
        singlePurchaseButton.onClick.AddListener(StartSinglePurchase);
        tenPurchaseButton.onClick.AddListener(StartTenPurchase);
        retryButton.onClick.AddListener(RetryPurchase);
        skipButton.onClick.AddListener(SkipReveal);
        closeResultButton.onClick.AddListener(CloseResult);
        ResetRevealState();
        gachaScreen.SetActive(false);
    }

    private void OnDestroy()
    {
        backButton.onClick.RemoveListener(CloseGachaScreen);
        singlePurchaseButton.onClick.RemoveListener(StartSinglePurchase);
        tenPurchaseButton.onClick.RemoveListener(StartTenPurchase);
        retryButton.onClick.RemoveListener(RetryPurchase);
        skipButton.onClick.RemoveListener(SkipReveal);
        closeResultButton.onClick.RemoveListener(CloseResult);
        StopRevealCoroutine();
    }

    public void OpenGachaScreen()
    {
        if (IsRequesting || revealState.IsRevealing)
        {
            return;
        }

        gachaScreen.SetActive(true);
        ResetRevealState();
        ShowIdle();
    }

    public void CloseGachaScreen()
    {
        if (IsRequesting ||
            revealState.IsResultVisible ||
            requestContext.State == PurchaseRequestState.RetryableFailure)
        {
            return;
        }

        StopRevealCoroutine();
        gachaScreen.SetActive(false);
    }

    public void StartSinglePurchase()
    {
        StartNewPurchase(GachaPurchasePresenter.SingleProductId);
    }

    public void StartTenPurchase()
    {
        StartNewPurchase(GachaPurchasePresenter.TenProductId);
    }

    public void RetryPurchase()
    {
        if (revealState.IsResultVisible || !requestContext.TryRetry())
        {
            return;
        }

        SendCurrentRequest();
    }

    public void SkipReveal()
    {
        if (!revealState.IsRevealing || currentResponse == null)
        {
            return;
        }

        StopRevealCoroutine();
        revealState.Skip();
        SetFlashAlpha(0f);
        ShowEveryResultCard(currentResponse);
        FinishReveal();
    }

    public void CloseResult()
    {
        if (IsRequesting || revealState.IsRevealing || !revealState.IsResultVisible)
        {
            return;
        }

        revealState.Close();
        resultArea.SetActive(false);
        currentResponse = null;
        SetPurchaseButtons(true);
        backButton.interactable = true;

        if (lobbyRefreshPending)
        {
            lobbyRefreshPending = false;
            lobbyManager.LoadLobbyData();
        }
    }

    public void ResetRevealState()
    {
        StopRevealCoroutine();
        revealState.Close();
        currentResponse = null;
        lobbyRefreshPending = false;
        resultArea.SetActive(false);
        singleResultContainer.SetActive(false);
        tenResultGrid.SetActive(false);
        singleResultCard.Hide();
        foreach (GachaResultCardView card in tenResultCards)
        {
            card.Hide();
        }

        SetFlashAlpha(0f);
        mascotRect.anchoredPosition = Vector2.zero;
        machineRect.localScale = Vector3.one;
        speechText.text = "어떤 왹져 친구를 만나게 될까요?";
    }

    private void StartNewPurchase(string productId)
    {
        if (!revealState.CanStartPurchase || !requestContext.TryStartNew(productId))
        {
            return;
        }

        SendCurrentRequest();
    }

    private void SendCurrentRequest()
    {
        if (lobbyManager == null || NetworkManager.Instance == null)
        {
            requestContext.MarkRetryableFailure();
            ShowRetryableError("네트워크 또는 로비 연결을 확인해 주세요.");
            return;
        }

        ShowRequesting();
        string uri = GachaPurchasePresenter.BuildPurchaseUri(
            lobbyManager.CurrentUsername,
            requestContext.ProductId,
            requestContext.PurchaseRequestId);

        NetworkManager.Instance.PostJsonAsync<EmptyRequestBody, GachaPurchaseResponse>(
            uri,
            new EmptyRequestBody(),
            HandlePurchaseResult);
    }

    private void HandlePurchaseResult(ApiResult<GachaPurchaseResponse> result)
    {
        if (result != null && result.IsSuccess && result.Data != null)
        {
            requestContext.MarkCompleted();
            currentResponse = result.Data;
            lobbyRefreshPending = true;

            GachaPurchaseViewModel viewModel = GachaPurchasePresenter.CreateViewModel(result.Data);
            lobbyManager.UpdateRemainingDiamond(viewModel.RemainingDiamond);
            resultSummaryText.text = viewModel.Summary + "\n\n" + viewModel.Details;
            loadingIndicator.SetActive(false);
            errorText.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(false);
            PlayReveal(result.Data);
            return;
        }

        HandleFailure(result);
    }

    private void HandleFailure(ApiResult<GachaPurchaseResponse> result)
    {
        string errorCode = result?.Error?.code;
        string networkError = result?.NetworkError;
        bool jsonParseFailed = !string.IsNullOrEmpty(networkError) &&
                               (networkError.Contains("JSON_PARSE_ERROR") ||
                                networkError.Contains("ERROR_JSON_PARSE_FAILED"));
        GachaPurchaseFailureKind failureKind = GachaPurchasePresenter.ClassifyFailure(
            result?.StatusCode ?? 0,
            errorCode,
            !string.IsNullOrEmpty(networkError),
            jsonParseFailed);
        string message = result?.Error?.message;
        if (string.IsNullOrWhiteSpace(message))
        {
            message = jsonParseFailed
                ? "서버 응답을 해석하지 못했습니다. 같은 요청으로 다시 시도해 주세요."
                : "구매 요청에 실패했습니다. 다시 시도해 주세요.";
        }

        loadingIndicator.SetActive(false);
        resultArea.SetActive(false);
        errorText.text = message;
        errorText.gameObject.SetActive(true);
        backButton.interactable = true;

        if (failureKind == GachaPurchaseFailureKind.Retryable)
        {
            requestContext.MarkRetryableFailure();
            ShowRetryableError(message);
        }
        else
        {
            requestContext.MarkFatalFailure();
            retryButton.gameObject.SetActive(false);
            SetPurchaseButtons(true);
        }
    }

    private void PlayReveal(GachaPurchaseResponse response)
    {
        StopRevealCoroutine();
        GachaDraw[] orderedDraws = GachaRevealState.OrderDraws(response.draws);
        if (orderedDraws.Length == 0)
        {
            errorText.text = "가챠 결과가 비어 있습니다.";
            errorText.gameObject.SetActive(true);
            SetPurchaseButtons(true);
            backButton.interactable = true;
            return;
        }

        revealState.Begin(orderedDraws.Length);
        resultArea.SetActive(true);
        singleResultContainer.SetActive(orderedDraws.Length == 1);
        tenResultGrid.SetActive(orderedDraws.Length > 1);
        skipButton.gameObject.SetActive(true);
        closeResultButton.gameObject.SetActive(false);
        SetPurchaseButtons(false);
        backButton.interactable = false;
        speechText.text = orderedDraws.Length == 1
            ? "새로운 친구를 찾아왔어!"
            : "친구들을 많이 찾아왔어!";
        revealCoroutine = StartCoroutine(orderedDraws.Length == 1
            ? PlaySingleReveal(response, orderedDraws[0])
            : PlayTenReveal(response, orderedDraws));
    }

    private IEnumerator PlaySingleReveal(GachaPurchaseResponse response, GachaDraw draw)
    {
        yield return PlayMachineShakeAndFlash();
        GachaReward reward = FindReward(response, draw.alienId);
        singleResultCard.Bind(draw, reward);
        yield return PopCard(singleResultCard.RectTransform);
        revealState.RevealNext();
        FinishReveal();
    }

    private IEnumerator PlayTenReveal(GachaPurchaseResponse response, GachaDraw[] orderedDraws)
    {
        yield return PlayMachineShakeAndFlash();
        int count = Mathf.Min(orderedDraws.Length, tenResultCards.Length);
        for (int i = 0; i < count; i++)
        {
            GachaDraw draw = orderedDraws[i];
            tenResultCards[i].Bind(draw, FindReward(response, draw.alienId));
            tenResultCards[i].RectTransform.localScale = Vector3.one * 0.5f;
            revealState.RevealNext();
            yield return PopCard(tenResultCards[i].RectTransform, 0.1f);
        }

        revealState.Skip();
        FinishReveal();
    }

    private IEnumerator PlayMachineShakeAndFlash()
    {
        Vector2 mascotStart = mascotRect.anchoredPosition;
        Vector3 machineStart = machineRect.localScale;
        const float duration = 0.35f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float shake = Mathf.Sin(elapsed * 70f) * 9f;
            mascotRect.anchoredPosition = mascotStart + Vector2.right * shake;
            machineRect.localScale = machineStart * (1f + Mathf.Sin(elapsed * 45f) * 0.025f);
            yield return null;
        }

        mascotRect.anchoredPosition = mascotStart;
        machineRect.localScale = machineStart;
        yield return FadeFlash(0f, 1f, 0.12f);
        yield return FadeFlash(1f, 0f, 0.18f);
    }

    private IEnumerator FadeFlash(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFlashAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetFlashAlpha(to);
    }

    private static IEnumerator PopCard(RectTransform card, float duration = 0.3f)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = t < 0.72f
                ? Mathf.Lerp(0.5f, 1.1f, t / 0.72f)
                : Mathf.Lerp(1.1f, 1f, (t - 0.72f) / 0.28f);
            card.localScale = Vector3.one * scale;
            yield return null;
        }

        card.localScale = Vector3.one;
    }

    private void ShowEveryResultCard(GachaPurchaseResponse response)
    {
        GachaDraw[] orderedDraws = GachaRevealState.OrderDraws(response.draws);
        if (orderedDraws.Length == 1)
        {
            singleResultCard.Bind(orderedDraws[0], FindReward(response, orderedDraws[0].alienId));
            singleResultCard.RectTransform.localScale = Vector3.one;
            return;
        }

        int count = Mathf.Min(orderedDraws.Length, tenResultCards.Length);
        for (int i = 0; i < count; i++)
        {
            tenResultCards[i].Bind(orderedDraws[i], FindReward(response, orderedDraws[i].alienId));
            tenResultCards[i].RectTransform.localScale = Vector3.one;
        }
    }

    private void FinishReveal()
    {
        revealCoroutine = null;
        skipButton.gameObject.SetActive(false);
        closeResultButton.gameObject.SetActive(true);
        backButton.interactable = false;
        SetPurchaseButtons(false);
    }

    private static GachaReward FindReward(GachaPurchaseResponse response, long alienId)
    {
        return (response.rewards ?? System.Array.Empty<GachaReward>())
            .FirstOrDefault(reward => reward.alienId == alienId);
    }

    private void ShowIdle()
    {
        loadingIndicator.SetActive(false);
        errorText.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);
        SetPurchaseButtons(true);
        backButton.interactable = true;
    }

    private void ShowRequesting()
    {
        SetPurchaseButtons(false);
        backButton.interactable = false;
        retryButton.gameObject.SetActive(false);
        loadingIndicator.SetActive(true);
        errorText.gameObject.SetActive(false);
        resultArea.SetActive(false);
    }

    private void ShowRetryableError(string message)
    {
        loadingIndicator.SetActive(false);
        SetPurchaseButtons(false);
        backButton.interactable = true;
        errorText.text = message;
        errorText.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(true);
    }

    private void SetPurchaseButtons(bool interactable)
    {
        singlePurchaseButton.interactable = interactable;
        tenPurchaseButton.interactable = interactable;
    }

    private void SetFlashAlpha(float alpha)
    {
        Color color = flashImage.color;
        color.a = alpha;
        flashImage.color = color;
    }

    private void StopRevealCoroutine()
    {
        if (revealCoroutine == null)
        {
            return;
        }

        StopCoroutine(revealCoroutine);
        revealCoroutine = null;
    }
}
