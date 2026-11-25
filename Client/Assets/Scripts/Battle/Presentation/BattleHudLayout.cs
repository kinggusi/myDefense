using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Pixel-space presentation reservations. Rects use bottom-left screen coordinates.</summary>
    public readonly struct BattleHudLayout
    {
        public readonly Rect Safe, Header, Footer, World, Drawer, Modal;
        public readonly bool Landscape;
        public readonly float Scale;

        private BattleHudLayout(Rect safe, Rect header, Rect footer, Rect world, Rect drawer, Rect modal, bool landscape, float scale)
        { Safe = safe; Header = header; Footer = footer; World = world; Drawer = drawer; Modal = modal; Landscape = landscape; Scale = scale; }

        public static BattleHudLayout Calculate(int width, int height, Rect safeArea)
        {
            width = Mathf.Max(1, width); height = Mathf.Max(1, height);
            float left = Mathf.Clamp(safeArea.xMin, 0, width), bottom = Mathf.Clamp(safeArea.yMin, 0, height);
            float right = Mathf.Clamp(safeArea.xMax, left + 1, width), top = Mathf.Clamp(safeArea.yMax, bottom + 1, height);
            Rect safe = Rect.MinMaxRect(left, bottom, right, top);
            bool landscape = safe.width > safe.height * 1.2f;
            float scale = Mathf.Clamp(Mathf.Min(safe.width / (landscape ? 960f : 800f), safe.height / (landscape ? 540f : 960f)), .65f, 1.5f);
            float gap = 10 * scale;
            float headerHeight = (landscape ? 126 : 154) * scale;
            float footerHeight = (landscape ? 94 : 130) * scale;
            Rect header = new(safe.x + gap, safe.yMax - gap - headerHeight, safe.width - gap * 2, headerHeight);
            Rect footer = new(safe.x + gap, safe.y + gap, safe.width - gap * 2, footerHeight);
            Rect world = Rect.MinMaxRect(safe.x, footer.yMax + gap, safe.xMax, header.yMin - gap);
            float drawerWidth = Mathf.Min(430 * scale, safe.width - gap * 2);
            Rect drawer = new(safe.xMax - drawerWidth - gap, footer.yMax + gap, drawerWidth,
                Mathf.Min(268 * scale, world.height - gap));
            Rect modal = new(safe.center.x - Mathf.Min(760 * scale, safe.width - 24 * scale) / 2,
                safe.center.y - Mathf.Min(475 * scale, safe.height - 24 * scale) / 2,
                Mathf.Min(760 * scale, safe.width - 24 * scale), Mathf.Min(475 * scale, safe.height - 24 * scale));
            return new(safe, header, footer, world, drawer, modal, landscape, scale);
        }

        public Rect NormalizedWorld(int width, int height)
            => new(World.x / width, World.y / height, World.width / width, World.height / height);

        public bool BlocksWorld(Vector2 point, bool drawerOpen, bool modalOpen)
            => modalOpen || !World.Contains(point) || (drawerOpen && Drawer.Contains(point));

        public static float PreserveVisibleBounds(float previousSize, float previousAspect, float nextAspect)
            => previousSize * Mathf.Max(1f, previousAspect / Mathf.Max(.01f, nextAspect));
    }
}
