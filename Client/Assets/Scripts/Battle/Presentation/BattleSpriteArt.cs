using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    // Presentation identities are deliberately independent of canonical Alien IDs.
    public enum BattleUnitVisual { Diad, Mecharon, Puppeteer1, Puppeteer2 }
    public enum BattleArtFacing { Front, Side, Rear }
    public enum BattleArtPose { Idle, Windup, Attack, Recover }

    [Serializable]
    public sealed class BattleDirectionArt
    {
        [SerializeField] private Texture2D _original;
        [SerializeField] private Texture2D _darkBold;
        [SerializeField] private RectInt[] _frames;
        [SerializeField] private float[] _feet;
        public bool Available => _original != null && _frames != null && _frames.Length == 3;
        public bool HasDarkBold => Available && _darkBold != null
            && _darkBold.width == _original.width && _darkBold.height == _original.height;

        public Sprite Create(BattleArtFacing facing, float height, bool darkBold)
        {
            if (!Available || darkBold && !HasDarkBold || height <= 0f) return null;
            int index = Mathf.Clamp((int)facing, 0, 2);
            RectInt r = _frames[index];
            if (r.width <= 0 || r.height <= 0) return null;
            float foot = _feet != null && _feet.Length == 3 ? _feet[index] : 0.5f;
            return Sprite.Create(darkBold ? _darkBold : _original,
                new Rect(r.x, r.y, r.width, r.height), new Vector2(foot, 0f), r.height / height,
                0, SpriteMeshType.FullRect);
        }

#if UNITY_EDITOR
        public BattleDirectionArt(Texture2D original, Texture2D darkBold, RectInt[] frames, float[] feet)
        { _original = original; _darkBold = darkBold; _frames = frames; _feet = feet; }
#endif
    }

    [Serializable]
    public sealed class BattleUnitPoseArt
    {
        [SerializeField] private BattleDirectionArt[] _poses;
        public BattleDirectionArt Pose(BattleArtPose pose)
            => _poses != null && (int)pose < _poses.Length ? _poses[(int)pose] : null;
        public bool Available(bool darkBold)
        {
            if (_poses == null || _poses.Length != 4) return false;
            foreach (BattleDirectionArt pose in _poses)
                if (pose == null || !pose.Available || darkBold && !pose.HasDarkBold) return false;
            return true;
        }
#if UNITY_EDITOR
        public BattleUnitPoseArt(BattleDirectionArt[] poses) => _poses = poses;
#endif
    }

    [Serializable]
    public sealed class BattleEffectArt
    {
        [SerializeField] private Texture2D _texture;
        [SerializeField] private RectInt[] _frames;
        public bool Available => _texture != null && _frames != null && _frames.Length == 6;
        public Sprite Create(int index)
        {
            if (!Available || index < 0 || index >= 6) return null;
            RectInt r = _frames[index];
            if (r.width <= 0 || r.height <= 0) return null;
            return Sprite.Create(_texture, new Rect(r.x, r.y, r.width, r.height), Vector2.one * .5f,
                Mathf.Max(r.width, r.height), 0, SpriteMeshType.FullRect);
        }
#if UNITY_EDITOR
        public BattleEffectArt(Texture2D texture, RectInt[] frames) { _texture = texture; _frames = frames; }
#endif
    }

    public static class BattleSpriteGeometry
    {
        public static Mesh CreateRelief(Sprite pose)
        {
            const int segments = 12;
            int stride = segments + 1;
            var vertices = new Vector3[stride * stride];
            var uv = new Vector2[vertices.Length];
            var indices = new int[segments * segments * 6];
            float height = pose.rect.height / pose.pixelsPerUnit;
            for (int row = 0; row <= segments; row++)
            for (int col = 0; col <= segments; col++)
            {
                float u = (float)col / segments, v = (float)row / segments;
                int i = row * stride + col;
                vertices[i] = new Vector3((u * pose.rect.width - pose.pivot.x) / pose.pixelsPerUnit,
                    (v * pose.rect.height - pose.pivot.y) / pose.pixelsPerUnit,
                    -height * .13f * Mathf.Sin(u * Mathf.PI) * Mathf.Sin(v * Mathf.PI));
                uv[i] = new Vector2((pose.rect.x + u * pose.rect.width) / pose.texture.width,
                    (pose.rect.y + v * pose.rect.height) / pose.texture.height);
            }
            int o = 0;
            for (int row = 0; row < segments; row++)
            for (int col = 0; col < segments; col++)
            {
                int l = row * stride + col, h = l + stride;
                indices[o++] = l; indices[o++] = h; indices[o++] = l + 1;
                indices[o++] = l + 1; indices[o++] = h; indices[o++] = h + 1;
            }
            var mesh = new Mesh { name = "Battle Artwork Relief" };
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = indices;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        public static void Release(UnityEngine.Object owned)
        {
            if (owned == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(owned);
            else UnityEngine.Object.DestroyImmediate(owned);
        }
    }

    /// <summary>Reference-counted pose geometry shared by matching units. Original textures are never owned here.</summary>
    public static class BattleUnitPoseCache
    {
        private sealed class Entry {public Sprite Sprite;public Mesh Mesh;public int References;}
        private static readonly Dictionary<(BattleUnitPoseArt art,BattleArtPose pose,BattleArtFacing facing,float height,bool dark,bool register),Entry> Entries=new();
        private static readonly Dictionary<Sprite,(BattleUnitPoseArt art,BattleArtPose pose,BattleArtFacing facing,float height,bool dark,bool register)> Keys=new();
        public static int PoseCount=>Entries.Count;
        public static int CreatedCount {get;private set;}
        public static Sprite Acquire(BattleUnitPoseArt art,BattleArtPose pose,BattleArtFacing facing,float height,bool dark,bool register,out Mesh mesh)
        {
            var key=(art,pose,facing,height,dark,register);
            if(!Entries.TryGetValue(key,out Entry entry))
            {
                Sprite sprite=art.Pose(pose)?.Create(facing,height,dark);if(sprite==null){mesh=null;return null;}
                mesh=BattleSpriteGeometry.CreateRelief(sprite);
                if(register)
                {
                    Sprite idle=art.Pose(BattleArtPose.Idle).Create(facing,height,dark);
                    float factor=(idle.rect.width/idle.pixelsPerUnit)/(sprite.rect.width/sprite.pixelsPerUnit);
                    Vector3[] vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i].x*=factor;
                    mesh.vertices=vertices;mesh.RecalculateBounds();BattleSpriteGeometry.Release(idle);
                }
                entry=new Entry{Sprite=sprite,Mesh=mesh};Entries.Add(key,entry);Keys.Add(sprite,key);CreatedCount++;
            }
            entry.References++;mesh=entry.Mesh;return entry.Sprite;
        }
        public static void Retain(Sprite sprite){if(sprite!=null&&Keys.TryGetValue(sprite,out var key))Entries[key].References++;}
        public static void Release(Sprite sprite)
        {
            if(sprite==null||!Keys.TryGetValue(sprite,out var key))return;
            Entry entry=Entries[key];if(--entry.References>0)return;
            Keys.Remove(sprite);Entries.Remove(key);BattleSpriteGeometry.Release(entry.Mesh);BattleSpriteGeometry.Release(entry.Sprite);
        }
    }
}
