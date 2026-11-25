#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace MyDefense.Battle.Showroom
{
    public enum BattleShowroomFacing
    {
        Front,
        Side,
        Rear
    }

    public enum BattleShowroomLook
    {
        Original,
        DarkBold
    }

    [Serializable]
    public sealed class BattleShowroomTileAtlas
    {
        [SerializeField] private Texture2D _texture;
        [SerializeField] private RectInt[] _frames;
        [SerializeField] private float[] _feet;

        public Texture2D Texture => _texture;
        public bool IsAvailable => _texture != null && _frames != null && _frames.Length == 3
            && _frames[0].width > 0 && _frames[1].width > 0 && _frames[2].width > 0;

        public BattleShowroomTileAtlas(Texture2D texture, RectInt[] frames, float[] feet)
        {
            _texture = texture;
            _frames = frames;
            _feet = feet;
        }

        public Sprite CreateSprite(BattleShowroomFacing facing, float visibleHeight, Texture2D textureOverride = null)
        {
            if (!IsAvailable)
                return null;
            Texture2D texture = textureOverride != null ? textureOverride : _texture;
            if (texture.width != _texture.width || texture.height != _texture.height)
                return null;
            int index = Mathf.Clamp((int)facing, 0, 2);
            RectInt frame = _frames[index];
            float foot = _feet != null && _feet.Length == 3 ? _feet[index] : 0.5f;
            return Sprite.Create(texture, new Rect(frame.x, frame.y, frame.width, frame.height),
                new Vector2(foot, 0f), frame.height / visibleHeight, 0, SpriteMeshType.FullRect);
        }

        public static RectInt[] FindAlphaFrames(Color32[] pixels, int width, int height)
        {
            if (pixels == null || width < 3 || width % 3 != 0 || height < 1 || pixels.Length != width * height)
                return Array.Empty<RectInt>();
            var result = new RectInt[3];
            int cellWidth = width / 3;
            for (int frame = 0; frame < 3; frame++)
            {
                int left = cellWidth * frame;
                int right = left + cellWidth;
                int minX = right, maxX = -1, minY = height, maxY = -1;
                for (int y = 0; y < height; y++)
                for (int x = left; x < right; x++)
                {
                    if (pixels[y * width + x].a < 72)
                        continue;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
                if (maxX < minX || maxY < minY)
                    continue;
                int paddedLeft = Mathf.Max(left, minX - 3);
                int paddedBottom = Mathf.Max(0, minY - 1);
                result[frame] = new RectInt(paddedLeft, paddedBottom,
                    Mathf.Min(right, maxX + 4) - paddedLeft, Mathf.Min(height, maxY + 4) - paddedBottom);
            }
            return result;
        }

        public static float[] FindFootPivots(Color32[] pixels, int width, RectInt[] frames)
        {
            var feet = new float[frames.Length];
            for (int index = 0; index < frames.Length; index++)
            {
                RectInt frame = frames[index];
                feet[index] = 0.5f;
                if (frame.width == 0 || frame.height == 0)
                    continue;
                int minimum = frame.xMax;
                int maximum = frame.xMin - 1;
                int cutoff = frame.yMin + Mathf.Max(1, Mathf.CeilToInt(frame.height * 0.09f));
                for (int y = frame.yMin; y < cutoff; y++)
                for (int x = frame.xMin; x < frame.xMax; x++)
                {
                    if (pixels[y * width + x].a < 110)
                        continue;
                    minimum = Mathf.Min(minimum, x);
                    maximum = Mathf.Max(maximum, x);
                }
                if (maximum >= minimum)
                    feet[index] = ((minimum + maximum + 1) * 0.5f - frame.xMin) / frame.width;
            }
            return feet;
        }
    }
}
#endif
