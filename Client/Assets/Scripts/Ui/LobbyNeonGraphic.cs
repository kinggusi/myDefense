using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent lobby frame. Uses the UI material, stencil and clipping pipeline.</summary>
[AddComponentMenu("UI/MyDefense/Lobby Neon Graphic")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class LobbyNeonGraphic : MaskableGraphic
{
    public enum FrameStyle { Card, Panel, Button }

    [SerializeField] private FrameStyle style = FrameStyle.Card;
    [SerializeField] private bool owned = true;
    [SerializeField] private Color background = new Color(0.025f, 0.045f, 0.095f, 0.98f);
    [SerializeField] private Color cyan = new Color(0.31f, 0.91f, 0.96f, 1f);
    [SerializeField] private Color violet = new Color(0.69f, 0.36f, 0.95f, 1f);
    [SerializeField] private bool gradeTint;
    [SerializeField] private bool finalStyle;

    public bool IsOwned => owned;
    public FrameStyle Style => style;
    public Color BorderTint => cyan;

    public void SetGrade(string grade)
    {
        gradeTint = true;
        cyan = GradeBorderColor(grade);
        violet = Color.Lerp(cyan, new Color(0.60f, 0.69f, 0.78f), 0.18f);
        SetVerticesDirty();
    }

    public static Color GradeBorderColor(string grade)
    {
        switch ((grade ?? string.Empty).ToUpperInvariant())
        {
            case "NORMAL": return new Color32(0xDC, 0xE5, 0xE8, 255);
            case "EPIC": return new Color32(0xBD, 0x65, 0xF2, 255);
            case "UNIQUE": return new Color32(0xF5, 0xC4, 0x51, 255);
            case "LEGEND":
            case "LEGENDARY": return new Color32(0x42, 0xE8, 0x87, 255);
            case "MYTHIC":
            case "MYSTIC": return new Color32(0xFF, 0x4F, 0x83, 255);
            default: return new Color(0.55f, 0.71f, 0.76f);
        }
    }

    public void UseFinalStyle()
    {
        finalStyle = true;
        background = new Color(.012f, .038f, .064f, .93f);
        if (!gradeTint) { cyan = new Color(.22f,.65f,.71f); violet = cyan; }
        SetVerticesDirty();
    }

    public void Configure(FrameStyle frameStyle)
    {
        style = frameStyle;
        color = Color.white;
        SetVerticesDirty();
    }

    public void SetOwned(bool value)
    {
        if (owned == value) return;
        owned = value;
        SetVerticesDirty();
    }

    public static float FinalOuterStrokeWidth(float nominalWidth, float canvasScaleFactor)
    {
        return Mathf.Max(nominalWidth, 1.25f / Mathf.Max(.001f, canvasScaleFactor));
    }

    public static float FinalCardStrokeWidth(float nominalWidth, float canvasScaleFactor)
    {
        // A 1.25 px stroke becomes less than half a pixel in the 0.37x Game View.
        // Keep every final-style card edge at least 3 canvas pixels (1.11 preview pixels),
        // rather than letting its position on the pixel grid hide one opposite edge.
        return Mathf.Max(nominalWidth, 3f / Mathf.Max(.001f, canvasScaleFactor));
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect bounds = GetPixelAdjustedRect();
        if (bounds.width < 12f || bounds.height < 12f) return;
        float unit = Mathf.Clamp(Mathf.Min(bounds.width, bounds.height) / 180f, 0.65f, 2f);
        float cut = Mathf.Min(Mathf.Min(bounds.width, bounds.height) * 0.14f,
            (style == FrameStyle.Panel ? 26f : 15f) * unit);
        Color rim = owned ? cyan : (gradeTint ? Color.Lerp(new Color(0.25f, 0.30f, 0.36f), cyan, 0.40f) : new Color(0.30f, 0.41f, 0.48f, 1f));
        Color accent = owned ? violet : (gradeTint ? rim : new Color(0.38f, 0.35f, 0.47f, 1f));
        Color fill = owned ? background : new Color(0.035f, 0.043f, 0.066f, 0.99f);

        Fill(mesh, Inset(bounds, 3f * unit), Mathf.Max(1f, cut - 3f * unit), fill);
        if (finalStyle)
        {
            float canvasScale = canvas != null ? canvas.scaleFactor : 1f;
            bool card = style == FrameStyle.Card;
            float outerWidth = card ? FinalCardStrokeWidth(1.5f * unit, canvasScale)
                : FinalOuterStrokeWidth(1.5f * unit, canvasScale);
            Ring(mesh, Inset(bounds, 2f * unit), cut, outerWidth, Alpha(rim, .90f));
            Ring(mesh, Inset(bounds, 6f * unit), Mathf.Max(0f, cut - 4f * unit),
                card ? FinalCardStrokeWidth(.7f * unit, canvasScale) : .7f * unit, Alpha(rim, .22f));
            if (card)
                Line(mesh, new Vector2(bounds.xMin + bounds.width * .09f, bounds.yMin + bounds.height * .19f),
                    new Vector2(bounds.xMax - bounds.width * .09f, bounds.yMin + bounds.height * .19f),
                    FinalCardStrokeWidth(unit, canvasScale), Alpha(rim, .80f));
            return;
        }
        if (gradeTint) Ring(mesh, bounds, cut, 7f * unit, Alpha(rim, 0.045f));
        Ring(mesh, bounds, cut, 4f * unit, Alpha(rim, 0.11f));
        Ring(mesh, Inset(bounds, 3f * unit), Mathf.Max(1f, cut - 3f * unit), 1.5f * unit, Alpha(rim, 0.88f));
        Ring(mesh, Inset(bounds, 8f * unit), Mathf.Max(1f, cut - 5f * unit), unit, Alpha(rim, 0.32f));

        // Short corner circuits give the reference's cyan / violet split without blooming over labels.
        float arm = Mathf.Min(bounds.width * 0.25f, 54f * unit);
        Vector2[] edge = Corners(Inset(bounds, unit), Mathf.Max(1f, cut - unit));
        Line(mesh, edge[0], edge[1], 2.5f * unit, rim);
        Line(mesh, edge[1], edge[1] + Vector2.right * arm, 2.5f * unit, rim);
        Line(mesh, edge[4], edge[5], 2.5f * unit, accent);
        Line(mesh, edge[5], edge[5] + Vector2.left * arm, 2.5f * unit, accent);

        if (style == FrameStyle.Card)
        {
            float footerY = bounds.yMin + bounds.height * 0.23f;
            Line(mesh, new Vector2(bounds.xMin + 13f * unit, footerY),
                new Vector2(bounds.xMax - 13f * unit, footerY), unit, Alpha(rim, 0.28f));
            Rect pedestal = new Rect(bounds.xMin + 14f * unit, bounds.yMin + 12f * unit,
                bounds.width - 28f * unit, Mathf.Max(10f, bounds.height * 0.105f));
            Fill(mesh, pedestal, 4f * unit, owned ? new Color(0.055f, 0.19f, 0.22f, 1f)
                : new Color(0.08f, 0.10f, 0.14f, 1f));
        }
    }

    private static Rect Inset(Rect rect, float amount) => new Rect(rect.xMin + amount, rect.yMin + amount,
        Mathf.Max(1f, rect.width - amount * 2f), Mathf.Max(1f, rect.height - amount * 2f));

    private static Color Alpha(Color value, float alpha) { value.a *= alpha; return value; }

    private static Vector2[] Corners(Rect r, float cut)
    {
        cut = Mathf.Clamp(cut, 0f, Mathf.Min(r.width, r.height) * 0.45f);
        return new[] { new Vector2(r.xMin, r.yMax - cut), new Vector2(r.xMin + cut, r.yMax),
            new Vector2(r.xMax - cut, r.yMax), new Vector2(r.xMax, r.yMax - cut),
            new Vector2(r.xMax, r.yMin + cut), new Vector2(r.xMax - cut, r.yMin),
            new Vector2(r.xMin + cut, r.yMin), new Vector2(r.xMin, r.yMin + cut) };
    }

    private void Fill(VertexHelper mesh, Rect rect, float cut, Color tint)
    {
        Vector2[] points = Corners(rect, cut);
        int start = mesh.currentVertCount;
        AddVertex(mesh, rect.center, tint);
        foreach (Vector2 point in points) AddVertex(mesh, point, tint);
        for (int i = 0; i < points.Length; i++) mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % points.Length);
    }

    private void Ring(VertexHelper mesh, Rect rect, float cut, float width, Color tint)
    {
        Vector2[] outer = Corners(rect, cut);
        Vector2[] inner = Corners(Inset(rect, width), Mathf.Max(0f, cut - width * 0.58f));
        int start = mesh.currentVertCount;
        for (int i = 0; i < outer.Length; i++) { AddVertex(mesh, outer[i], tint); AddVertex(mesh, inner[i], tint); }
        for (int i = 0; i < outer.Length; i++)
        {
            int next = (i + 1) % outer.Length;
            mesh.AddTriangle(start + i * 2, start + next * 2, start + i * 2 + 1);
            mesh.AddTriangle(start + i * 2 + 1, start + next * 2, start + next * 2 + 1);
        }
    }

    private void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 d = (b - a).normalized;
        Vector2 n = new Vector2(-d.y, d.x) * width * 0.5f;
        int start = mesh.currentVertCount;
        AddVertex(mesh, a - n, tint); AddVertex(mesh, a + n, tint);
        AddVertex(mesh, b + n, tint); AddVertex(mesh, b - n, tint);
        mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
    }

    private void AddVertex(VertexHelper mesh, Vector2 position, Color tint)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = tint * color;
        mesh.AddVert(vertex);
    }
}
