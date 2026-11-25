#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace MyDefense.Battle.Showroom
{
    [Serializable]
    public sealed class BattleShowroomEffectAtlas
    {
        [SerializeField] private Texture2D _texture;
        [SerializeField] private RectInt[] _frames;

        public bool IsAvailable => _texture != null && _frames != null && _frames.Length == 6
            && Array.TrueForAll(_frames, frame => frame.width > 0 && frame.height > 0);

        public BattleShowroomEffectAtlas(Texture2D texture, RectInt[] frames)
        {
            _texture = texture;
            _frames = frames;
        }

        public Sprite CreateSprite(int index)
        {
            if (_texture == null || _frames == null || index < 0 || index >= _frames.Length)
                return null;
            RectInt rect = _frames[index];
            if (rect.width == 0 || rect.height == 0)
                return null;
            return Sprite.Create(_texture, new Rect(rect.x, rect.y, rect.width, rect.height), Vector2.one * 0.5f,
                Mathf.Max(rect.width, rect.height), 0, SpriteMeshType.FullRect);
        }

        // Authoring rows are top-to-bottom, while the texture API counts y from the bottom.
        public static RectInt[] FindFrames(Color32[] pixels, int width, int height)
        {
            if (pixels == null || width < 3 || height < 2 || pixels.Length != width * height)
                return Array.Empty<RectInt>();

            var frames = new RectInt[6];
            for (int index = 0; index < frames.Length; index++)
            {
                int left = width * (index % 3) / 3;
                int right = width * (index % 3 + 1) / 3;
                int bottom = height * (1 - index / 3) / 2;
                int top = height * (2 - index / 3) / 2;
                int minX = right, maxX = left - 1, minY = top, maxY = bottom - 1;
                for (int y = bottom; y < top; y++)
                for (int x = left; x < right; x++)
                {
                    if (pixels[y * width + x].a < 20)
                        continue;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }

                if (maxX >= minX && maxY >= minY)
                {
                    int x = Mathf.Max(left, minX - 3);
                    int y = Mathf.Max(bottom, minY - 3);
                    frames[index] = new RectInt(x, y, Mathf.Min(right, maxX + 4) - x, Mathf.Min(top, maxY + 4) - y);
                }
            }
            return frames;
        }
    }

    internal sealed class BattleShowroomRelief
    {
        private readonly MeshFilter _filter;
        private readonly MeshRenderer _renderer;
        private readonly MaterialPropertyBlock _properties = new();

        public Transform Transform => _renderer.transform;

        public BattleShowroomRelief(Transform parent, string name, Material material)
        {
            var model = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            model.transform.SetParent(parent, false);
            _filter = model.GetComponent<MeshFilter>();
            _renderer = model.GetComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            _renderer.enabled = false;
        }

        public void Show(Mesh mesh, Texture texture)
        {
            if (mesh == null || texture == null)
            {
                Hide();
                return;
            }
            if (_filter.sharedMesh != mesh)
            {
                _filter.sharedMesh = mesh;
                _properties.SetTexture("_MainTex", texture);
                _renderer.SetPropertyBlock(_properties);
            }
            _renderer.enabled = true;
        }

        public void Hide() => _renderer.enabled = false;

        public static Mesh CreateMesh(Sprite pose)
            => MyDefense.Battle.Presentation.BattleSpriteGeometry.CreateRelief(pose);
    }
}
#endif
