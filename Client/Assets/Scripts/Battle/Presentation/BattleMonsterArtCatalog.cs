using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    [Serializable]
    public sealed class BattlePresentationAtlas
    {
        [SerializeField] private Texture2D _texture;
        [SerializeField] private RectInt[] _frames;
        [SerializeField] private Vector2[] _pivots;
        [SerializeField] private float _referenceHeight;
        [SerializeField] private bool _normalizeHeight;
        public int Count=>_frames?.Length??0;
        public Texture2D Texture=>_texture;
        public bool Available=>_texture!=null&&Count>0&&_referenceHeight>0;
        public Sprite Create(int index,float height)
        {
            if(!Available||index<0||index>=Count||height<=0)return null;
            RectInt frame=_frames[index];
            Vector2 pivot=_pivots!=null&&_pivots.Length==Count?_pivots[index]:Vector2.one*.5f;
            return Sprite.Create(_texture,new Rect(frame.x,frame.y,frame.width,frame.height),pivot,(_normalizeHeight?frame.height:_referenceHeight)/height,0,SpriteMeshType.FullRect);
        }
#if UNITY_EDITOR
        public BattlePresentationAtlas(Texture2D texture,RectInt[] frames,Vector2[] pivots,float referenceHeight,bool normalizeHeight=false)
        {_texture=texture;_frames=frames;_pivots=pivots;_referenceHeight=referenceHeight;_normalizeHeight=normalizeHeight;}
#endif
    }

    public sealed class BattleMonsterArtCatalog:ScriptableObject
    {
        public const string ResourcePath="Battle/Presentation/BattleMonsterArtCatalog";
        [SerializeField] private BattlePresentationAtlas _normal,_elite,_boss,_portal,_merge;
        [SerializeField] private Shader _shader;
        [SerializeField] private Shader _effectShader;
        public BattlePresentationAtlas Portal=>_portal;
        public BattlePresentationAtlas Merge=>_merge;
        public Shader Shader=>_shader;
        public Shader EffectShader=>_effectShader;
        public BattlePresentationAtlas Resolve(string id)=>id switch{"NORMAL_MONSTER"=>_normal,"ELITE_MONSTER"=>_elite,"WAVE_BOSS"=>_boss,_=>null};
        public bool Available=>_normal?.Count==12&&_elite?.Count==12&&_boss?.Count==12&&_portal?.Count==8&&_merge?.Count==6
            &&_normal.Available&&_elite.Available&&_boss.Available&&_portal.Available&&_merge.Available&&_shader!=null&&_effectShader!=null;
#if UNITY_EDITOR
        public void Configure(BattlePresentationAtlas normal,BattlePresentationAtlas elite,BattlePresentationAtlas boss,BattlePresentationAtlas portal,BattlePresentationAtlas merge,Shader shader,Shader effects)
        {_normal=normal;_elite=elite;_boss=boss;_portal=portal;_merge=merge;_shader=shader;_effectShader=effects;}
#endif
    }

    public static class BattlePresentationFrameCache
    {
        public sealed class Lease:IDisposable
        {
            internal readonly (BattlePresentationAtlas atlas,int frame,float height) Key;
            public Sprite Sprite {get;internal set;}
            public Mesh Mesh {get;internal set;}
            private bool _disposed;
            internal Lease((BattlePresentationAtlas,int,float) key){Key=key;}
            public void Dispose(){if(_disposed)return;_disposed=true;Release(Key);}
        }
        private sealed class Entry{public Sprite Sprite;public Mesh Mesh;public int Owners;}
        private static readonly Dictionary<(BattlePresentationAtlas,int,float),Entry> Entries=new();
        public static int Count=>Entries.Count;
        public static Lease Acquire(BattlePresentationAtlas atlas,int frame,float height)
        {
            var key=(atlas,frame,height);
            if(!Entries.TryGetValue(key,out Entry entry))
            {
                Sprite sprite=atlas?.Create(frame,height);if(sprite==null)return null;
                entry=new Entry{Sprite=sprite,Mesh=BattleSpriteGeometry.CreateRelief(sprite)};Entries.Add(key,entry);
            }
            entry.Owners++;return new Lease(key){Sprite=entry.Sprite,Mesh=entry.Mesh};
        }
        private static void Release((BattlePresentationAtlas,int,float) key)
        {
            if(!Entries.TryGetValue(key,out Entry entry)||--entry.Owners>0)return;
            Entries.Remove(key);BattleSpriteGeometry.Release(entry.Sprite);BattleSpriteGeometry.Release(entry.Mesh);
        }
    }
}
