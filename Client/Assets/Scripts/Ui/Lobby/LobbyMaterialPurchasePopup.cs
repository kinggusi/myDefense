using UnityEngine;
using UnityEngine.UI;

namespace MyDefense.Lobby
{
    /// <summary>Confirmation entry point. Currency is never granted or deducted by the UI.</summary>
    public sealed class LobbyMaterialPurchasePopup : MonoBehaviour
    {
        private GameObject root;
        private Text title;
        private Text description;
        private LobbyMaterialIconGraphic icon;
        public bool IsOpen => root != null && root.activeSelf;

        public void Open(LobbyMaterialIconGraphic.MaterialKind kind)
        {
            if (root == null) Build();
            if (root == null) return;
            title.text = kind == LobbyMaterialIconGraphic.MaterialKind.WakjeoDna ? "왹져 DNA 구매" : "성장 세포 구매";
            icon.Configure(kind);
            description.text = "다이아로 재료를 구매합니다.\n\n판매 수량 · 다이아 가격 · 구매 한도\n설정 후 이용할 수 있습니다.";
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        public void Close() { if (root != null) root.SetActive(false); }

        private void OnDestroy()
        {
            if (root == null) return;
            if (Application.isPlaying) Destroy(root);
            else DestroyImmediate(root);
        }

        private void Build()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) { Debug.LogWarning("[MaterialShop] 로비 Canvas가 없습니다."); return; }
            root = Rect("MaterialPurchasePopup", canvas.transform).gameObject;
            Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            Image shade = root.AddComponent<Image>();
            shade.color = new Color(0.015f, 0.025f, 0.05f, 0.84f);
            RectTransform panel = Rect("Panel", root.transform);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f,.5f);
            panel.sizeDelta = new Vector2(780f,650f);
            panel.gameObject.AddComponent<LobbyNeonGraphic>().Configure(LobbyNeonGraphic.FrameStyle.Panel);
            title = Label(panel,"Title",35,"",new Vector2(.07f,.82f),new Vector2(.93f,.96f));
            RectTransform iconRect = Rect("MaterialIcon",panel);
            Stretch(iconRect,new Vector2(.42f,.60f),new Vector2(.58f,.79f));
            icon = iconRect.gameObject.AddComponent<LobbyMaterialIconGraphic>();
            description = Label(panel,"Description",29,"",new Vector2(.10f,.29f),new Vector2(.90f,.59f));
            Button close = MakeButton(panel,"Close","닫기",new Vector2(.09f,.09f),new Vector2(.44f,.22f));
            close.onClick.AddListener(Close);
            Button buy = MakeButton(panel,"Purchase","판매 준비 중",new Vector2(.50f,.09f),new Vector2(.91f,.22f));
            // No invented exchange rate. Enable purchase only after canonical server products exist.
            buy.interactable = false;
        }

        private static RectTransform Rect(string name,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;
            go.transform.SetParent(parent,false);return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect,Vector2 min,Vector2 max)
        {
            rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        }

        private static Text Label(Transform parent,string name,int size,string content,Vector2 min,Vector2 max)
        {
            RectTransform rect=Rect(name,parent);Stretch(rect,min,max);
            Text text=rect.gameObject.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=size;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.80f,.89f,.93f);
            text.text=content;text.raycastTarget=false;return text;
        }

        private static Button MakeButton(Transform parent,string name,string caption,Vector2 min,Vector2 max)
        {
            RectTransform rect=Rect(name,parent);Stretch(rect,min,max);
            var frame=rect.gameObject.AddComponent<LobbyNeonGraphic>();frame.Configure(LobbyNeonGraphic.FrameStyle.Button);
            Button button=rect.gameObject.AddComponent<Button>();button.targetGraphic=frame;
            Label(rect,"Label",28,caption,Vector2.zero,Vector2.one);return button;
        }
    }
}
