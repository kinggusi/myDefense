using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace MyDefense.Auth
{
    /// <summary>Account-link entry is confined to the main lobby tab, without modifying the profile HUD.</summary>
    public sealed class AuthAccountPanel : MonoBehaviour
    {
        private AuthStartupView view;
        private CanvasGroup parentGroup;
        private bool priorInteractable, busy;
        private ProvidersResponse availableProviders;
        public void Install(LobbyManager lobby)
        {
            if (AuthSession.Instance == null || lobby.viewObjects.Length <= 2) return;
            var root = new GameObject("AccountLinkShortcut", typeof(RectTransform), typeof(LobbyNeonGraphic), typeof(Button));
            root.transform.SetParent(lobby.viewObjects[2].transform, false);
            var rect = (RectTransform)root.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-28, -300); rect.sizeDelta = new Vector2(340, 100);
            var frame = root.GetComponent<LobbyNeonGraphic>(); frame.Configure(LobbyNeonGraphic.FrameStyle.Button); frame.UseFinalStyle();
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>(); label.transform.SetParent(root.transform, false);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 30;
            label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.text = "계정 연동"; label.raycastTarget = false;
            root.GetComponent<Button>().targetGraphic = frame;
            root.GetComponent<Button>().onClick.AddListener(() => Open(lobby));
        }
        private void Open(LobbyManager lobby)
        {
            if (view != null) return;
            var canvas = lobby.viewObjects[2].GetComponentInParent<Canvas>();
            parentGroup = canvas.GetComponent<CanvasGroup>();
            if (parentGroup == null) parentGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            priorInteractable = parentGroup.interactable; parentGroup.interactable = false;
            view = Instantiate(Resources.Load<GameObject>(AuthStartupController.ViewResource), canvas.transform, false).GetComponent<AuthStartupView>();
            view.name = "AccountLinkPanel";
            view.logo.gameObject.SetActive(false); view.title.gameObject.SetActive(true); view.title.text = "계정 연동";
            view.subtitle.text = "현재 왹져와 진행을 그대로 보존합니다";
            view.startButton.onClick.AddListener(Close);
            view.googleButton.onClick.AddListener(() => Link("GOOGLE")); view.appleButton.onClick.AddListener(() => Link("APPLE"));
            Show("계정 연동 설정 확인 중");
            StartCoroutine(RefreshProviders());
        }
        private IEnumerator RefreshProviders()
        {
            ProvidersResponse providers = null;
            yield return AuthSession.Instance.Providers(result => providers = result);
            if (view == null || busy) yield break;
            availableProviders = providers;
            Show(AuthSession.Instance.User.isGuest ? "게스트 계정 · 연동 준비 중" : "연결된 계정으로 플레이 중");
            view.SetProviderAvailability(Ready(providers, "GOOGLE"), Ready(providers, "APPLE"));
        }
        private static bool Ready(ProvidersResponse response, string provider)
            => response?.providers != null && response.providers.Any(p => p.provider == provider && p.available)
                && ExternalIdentityProviders.Find(provider)?.IsAvailable == true;
        private void Show(string message, bool error = false)
        {
            view.SetState(message, busy, -1, error); view.startLabel.text = "돌아가기"; view.startButton.interactable = !busy;
        }
        private void Link(string provider) { if (!busy) StartCoroutine(LinkFlow(provider)); }
        private IEnumerator LinkFlow(string provider)
        {
            busy = true; Show("현재 계정에 연결하는 중"); bool ok = false;
            yield return AuthSession.Instance.External(provider, true, value => ok = value);
            busy = false; Show(ok ? "계정이 연결되었습니다. 현재 진행은 그대로 유지됩니다." : AuthSession.Instance.LastError, !ok);
            view.SetProviderAvailability(Ready(availableProviders, "GOOGLE"), Ready(availableProviders, "APPLE"));
        }
        private void Close()
        {
            if (busy) return;
            if (view != null) Destroy(view.gameObject); view = null;
            if (parentGroup != null) parentGroup.interactable = priorInteractable;
        }
        private void OnDestroy() { busy = false; Close(); }
    }
}
