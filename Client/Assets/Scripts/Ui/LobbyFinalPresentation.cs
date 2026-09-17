using UnityEngine;
using UnityEngine.UI;

/// <summary>Lobby presentation only. Does not own navigation or economy requests.</summary>
public sealed class LobbyFinalPresentation : MonoBehaviour
{
    public RectTransform navigation;
    public RectTransform collectionScroll;
    public Image mascot;
    public Shader iconShader;
    public LobbyNavIconGraphic[] symbols;
    public GameObject[] indicators;
    public Image[] navigationImages;
    public Sprite[] activeNavigationSprites;
    public Sprite[] inactiveNavigationSprites;
    private Material mascotMaterial;
    private int selectedIndex = 2;
    private float previousSafeBottom = -1f;

    private void OnEnable() { SelectTab(selectedIndex); UpdateSafeArea(); }
    private void Update() { UpdateSafeArea(); }
    private void OnDestroy()
    {
        if (mascotMaterial != null)
        {
            if (Application.isPlaying) Destroy(mascotMaterial); else DestroyImmediate(mascotMaterial);
        }
    }

    public void SelectTab(int index)
    {
        selectedIndex = index;
        if (navigationImages != null)
            for (int i = 0; i < navigationImages.Length; i++)
            {
                var image = navigationImages[i];
                var sprites = i == index ? activeNavigationSprites : inactiveNavigationSprites;
                if (image == null || sprites == null || i >= sprites.Length || sprites[i] == null) continue;
                image.sprite = sprites[i];
                image.color = Color.white;
            }
        if (indicators != null)
            for (int i = 0; i < indicators.Length; i++)
                if (indicators[i] != null) indicators[i].SetActive(i == index);
        if (symbols != null)
            foreach (var symbol in symbols)
                if (symbol != null) symbol.color = symbol.TabIndex == index
                    ? new Color(.47f,.88f,.95f) : new Color(.46f,.51f,.55f);
        if (mascot != null && iconShader != null)
        {
            // Runtime-only transient material: never serialize a DontSave material into the Scene.
            if (Application.isPlaying)
            {
                if (mascotMaterial == null) mascotMaterial = new Material(iconShader) { hideFlags = HideFlags.HideAndDontSave };
                mascotMaterial.SetFloat("_Saturation", index == 1 ? 1f : 0f);
                mascot.material = mascotMaterial;
            }
            else mascot.material = null;
            mascot.color = index == 1 ? Color.white : new Color(.64f,.64f,.64f,1f);
        }
    }

    private void UpdateSafeArea()
    {
        if (navigation == null) return;
        var canvas = navigation.GetComponentInParent<Canvas>();
        float bottom = Screen.safeArea.yMin / Mathf.Max(.01f, canvas == null ? 1f : canvas.scaleFactor);
        if (Mathf.Abs(bottom - previousSafeBottom) < .1f) return;
        previousSafeBottom = bottom;
        ApplySafeBottom(bottom);
    }

    public void ApplySafeBottom(float bottom)
    {
        bottom = Mathf.Max(0,bottom);
        if (navigation == null) return;
        navigation.offsetMin = new Vector2(navigation.offsetMin.x, bottom);
        navigation.offsetMax = new Vector2(navigation.offsetMax.x, bottom + 180f);
        if (collectionScroll != null) collectionScroll.offsetMin = new Vector2(collectionScroll.offsetMin.x,225f+bottom);
    }

    public static void StyleMaterials(Transform panel)
    {
        var rect = (RectTransform)panel;
        rect.anchorMin = new Vector2(.04f,1f); rect.anchorMax = new Vector2(.51f,1f);
        rect.pivot = new Vector2(.5f,1f); rect.offsetMin = new Vector2(0,-310f); rect.offsetMax = new Vector2(0,-165f);
        var layout = panel.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) layout.enabled = false;
        Frame(panel.gameObject);
        string[] names = { "WakjeoDnaCard", "GrowthCellCard" };
        string[] labels = { "왹져DNA", "성장세포" };
        for (int i = 0; i < names.Length; i++)
        {
            var row = panel.Find(names[i]);
            if (row == null) continue;
            Place((RectTransform)row, .02f, i == 0 ? .50f : .04f, .98f, i == 0 ? .96f : .50f);
            var oldFrame = row.Find("LobbyNeonFrame");
            if (oldFrame != null) oldFrame.gameObject.SetActive(false);
            Place((RectTransform)row.Find("MaterialIcon"), .025f,.18f,.14f,.82f);
            var label = TextChild(row,"MaterialName", labels[i],25);
            Place(label.rectTransform,.18f,.08f,.60f,.92f); label.alignment = TextAnchor.MiddleLeft;
            var value = row.Find("Value").GetComponent<Text>();
            Place(value.rectTransform,.60f,.08f,.80f,.92f); value.alignment = TextAnchor.MiddleRight;
            value.resizeTextForBestFit = true; value.resizeTextMinSize = 12; value.resizeTextMaxSize = 24;
            var plus = (RectTransform)row.Find("PurchasePlus");
            Place(plus,.865f,.5f,.965f,.5f);
            var square = plus.GetComponent<AspectRatioFitter>();
            square.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight; square.aspectRatio = 1;
            plus.GetComponent<LobbyNeonGraphic>().UseFinalStyle();
        }
        var divider = Child(panel,"RowDivider").gameObject;
        Place((RectTransform)divider.transform,.06f,.498f,.94f,.502f);
        var image = Get<Image>(divider); image.color = new Color(.25f,.63f,.69f,.35f); image.raycastTarget = false;
    }

    public static void StyleBreeding(MythicBreedingShortcutView view)
    {
        var rect = (RectTransform)view.transform;
        rect.anchorMin = new Vector2(.55f,1f); rect.anchorMax = new Vector2(.96f,1f);
        rect.pivot = new Vector2(.5f,1f); rect.offsetMin = new Vector2(0,-310f); rect.offsetMax = new Vector2(0,-165f);
        Frame(view.gameObject);
        Place(view.titleText.rectTransform,.07f,.51f,.68f,.87f);
        view.titleText.fontSize = 32; view.titleText.alignment = TextAnchor.MiddleLeft;
        Place(view.statusText.rectTransform,.07f,.15f,.80f,.48f);
        view.statusText.fontSize = 23; view.statusText.alignment = TextAnchor.MiddleLeft;
        var arrow = TextChild(view.transform,"EntryArrow","›",46);
        Place(arrow.rectTransform,.85f,.2f,.96f,.8f); arrow.color = new Color(.40f,.81f,.88f);
        // Reserve a separate upper-row column for completed-slot count. It must never
        // share the entry arrow's hit/visual area, even when all three slots are ready.
        if (view.badgeObject != null)
        {
            Place((RectTransform)view.badgeObject.transform,.72f,.59f,.82f,.89f);
            var badgeImage = view.badgeObject.GetComponent<Image>();
            if (badgeImage != null) badgeImage.raycastTarget = false;
        }
        if (view.badgeText != null)
        {
            Place(view.badgeText.rectTransform,.06f,.05f,.94f,.95f);
            view.badgeText.raycastTarget = false;
            view.badgeText.alignment = TextAnchor.MiddleCenter;
            view.badgeText.resizeTextForBestFit = true;
            view.badgeText.resizeTextMinSize = 14;
            view.badgeText.resizeTextMaxSize = 24;
        }
    }

    public static RectTransform Child(Transform parent,string name)
    {
        var found = parent.Find(name);
        if (found != null) return (RectTransform)found;
        var go = new GameObject(name,typeof(RectTransform)); go.layer=parent.gameObject.layer;
        go.transform.SetParent(parent,false); return (RectTransform)go.transform;
    }
    public static T Get<T>(GameObject go) where T : Component
    { var component=go.GetComponent<T>(); return component != null ? component : go.AddComponent<T>(); }
    public static void Place(RectTransform r,float x0,float y0,float x1,float y1)
    { r.anchorMin=new Vector2(x0,y0); r.anchorMax=new Vector2(x1,y1); r.pivot=new Vector2(.5f,.5f); r.offsetMin=r.offsetMax=Vector2.zero; r.localScale=Vector3.one; }
    public static Text TextChild(Transform parent,string name,string text,int size)
    {
        var label=Get<Text>(Child(parent,name).gameObject);
        label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.text=text;
        label.fontSize=size; label.fontStyle=FontStyle.Bold; label.color=Color.white;
        label.alignment=TextAnchor.MiddleCenter; label.raycastTarget=false;
        label.resizeTextForBestFit=true; label.resizeTextMinSize=14; label.resizeTextMaxSize=size;
        return label;
    }
    public static LobbyNeonGraphic Frame(GameObject owner)
    {
        var rect=Child(owner.transform,"LobbyNeonFrame"); Place(rect,0,0,1,1); rect.SetAsFirstSibling();
        Get<LayoutElement>(rect.gameObject).ignoreLayout=true;
        var frame=Get<LobbyNeonGraphic>(rect.gameObject); frame.Configure(LobbyNeonGraphic.FrameStyle.Panel);
        frame.UseFinalStyle(); frame.raycastTarget=false;
        var image=owner.GetComponent<Image>(); if(image!=null) image.color=Color.clear;
        return frame;
    }
}
