using UnityEngine;
namespace MyDefense.Auth
{
    public enum AccountEntryPolicy { GuestFirst, ProviderRequired }
    [CreateAssetMenu(menuName = "MyDefense/Startup Theme")]
    public sealed class StartupTheme : ScriptableObject
    {
        [Header("Presentation only — replace assets without changing authentication")]
        public Texture2D background;
        public Sprite logo, mascot;
        public Font font;
        public Color accent = new Color(.29f, .88f, .92f);
        public Color text = new Color(.90f, .95f, .98f);
        public Color muted = new Color(.54f, .66f, .72f);
        public Color error = new Color(1f, .53f, .59f);
        public string title = "왹져 디펜스";
        public string subtitle = "우주를 지켜낼 왹져들을 만나보세요";
        public string startLabel = "게임 시작";
        [TextArea] public string guestNotice = "게스트 접속 정보는 이 기기에 보관됩니다.\n기기 변경 전 Google · Apple 계정 연동을 권장합니다.";
        [Header("Provider adapters required before changing policy")]
        public AccountEntryPolicy entryPolicy = AccountEntryPolicy.GuestFirst;
    }
}
