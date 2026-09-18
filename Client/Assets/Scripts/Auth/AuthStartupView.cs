using UnityEngine;
using UnityEngine.UI;
namespace MyDefense.Auth
{
    public sealed class AuthStartupView : MonoBehaviour
    {
        public StartupTheme theme;
        public RectTransform safeArea;
        public RawImage background;
        public Image logo, mascot;
        public Text title, subtitle, status, notice, startLabel, googleLabel, appleLabel;
        public Button startButton, googleButton, appleButton;
        public Text[] stages;
        private Rect previousSafe;
        private Vector2 previousSize;
        private void Awake() { ApplyTheme(); ApplySafeArea(); }
        private void Update()
        {
            if (previousSafe != Screen.safeArea || previousSize != new Vector2(Screen.width, Screen.height)) ApplySafeArea();
        }
        public void ApplyTheme()
        {
            if (theme == null) return;
            background.texture = theme.background; background.raycastTarget = false;
            if (theme.background != null)
            {
                var fit = background.GetComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = (float)theme.background.width / theme.background.height;
            }
            logo.sprite = theme.logo; logo.gameObject.SetActive(theme.logo != null);
            title.gameObject.SetActive(theme.logo == null); title.text = theme.title;
            mascot.sprite = theme.mascot; mascot.gameObject.SetActive(theme.mascot != null);
            subtitle.text = theme.subtitle; notice.text = theme.guestNotice; startLabel.text = theme.startLabel;
            foreach (var label in GetComponentsInChildren<Text>(true))
            { if (theme.font != null) label.font = theme.font; label.color = theme.text; }
            title.color = theme.accent; subtitle.color = notice.color = theme.muted;
        }
        public void SetState(string message, bool busy, int stage = -1, bool failure = false)
        {
            status.text = message;
            status.color = failure && theme != null ? theme.error : theme != null ? theme.text : Color.white;
            startButton.interactable = !busy && theme != null && theme.entryPolicy == AccountEntryPolicy.GuestFirst;
            startLabel.text = failure ? "다시 시도" : theme != null ? theme.startLabel : "게임 시작";
            googleButton.interactable = appleButton.interactable = false;
            for (int i = 0; i < stages.Length; i++) stages[i].color = theme == null ? Color.white : i <= stage ? theme.accent : theme.muted;
        }
        public void SetProviderAvailability(bool google, bool apple)
        {
            googleButton.interactable = google; appleButton.interactable = apple;
            googleLabel.text = google ? "Google 계정" : "Google · 준비 중";
            appleLabel.text = apple ? "Apple 계정" : "Apple · 준비 중";
        }
        private void ApplySafeArea()
        {
            previousSafe = Screen.safeArea; previousSize = new Vector2(Screen.width, Screen.height);
            if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            safeArea.anchorMin = new Vector2(previousSafe.xMin / Screen.width, previousSafe.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(previousSafe.xMax / Screen.width, previousSafe.yMax / Screen.height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }
    }
}
