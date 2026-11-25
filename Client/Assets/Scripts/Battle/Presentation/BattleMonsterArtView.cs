using System.Collections.Generic;
using MyDefense.Battle.Runtime;
using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Passive monster artwork, evaluated after the existing local lane remap.</summary>
    [DefaultExecutionOrder(1500)]
    public sealed class BattleMonsterArtView:MonoBehaviour
    {
        private BattleMonsterNetworkState _state;
        private MonsterStat _stat;
        private BattlePresentationAtlas _atlas;
        private Renderer _legacy;
        private bool _legacyEnabled;
        private Transform _stage;
        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private Material _material;
        private MonsterHealthView _health;
        private Renderer _healthPrevious;
        private readonly Dictionary<int,BattlePresentationFrameCache.Lease> _frames=new();
        private MaterialPropertyBlock _properties;
        private Vector3 _previous;
        private float _height,_distance,_lastHp,_flashUntil,_deathAt=-1,_revealAt=-100;
        private float _lastViewAt;
        private int _direction;
        private bool _mirror,_released=true;
        public Renderer BodyRenderer=>_renderer;
        public float Opacity {get;private set;}=1;
        public int HitFlashCount {get;private set;}
        public bool IsDying=>_deathAt>=0;
        public const float FootHeight=.035f; // Common Battle presentation ground; waypoint/collider heights remain unchanged.

        /// <summary>Current local-field socket from fixed artwork geometry, independent of LateUpdate order.</summary>
        public Vector3 ResolveHitPoint(Vector3 presentationRoot)
            =>_released||_renderer==null?presentationRoot:new Vector3(presentationRoot.x,FootHeight,presentationRoot.z)
                +Quaternion.Euler(25,0,0)*(Vector3.up*(_height*.52f));
        public static Vector3 ResolveHitPoint(Transform target,Vector3 presentationRoot)
        {
            var view=target==null?null:target.GetComponent<BattleMonsterArtView>();
            return view==null?presentationRoot:view.ResolveHitPoint(presentationRoot);
        }

        public bool Configure(BattleMonsterArtCatalog catalog,string monsterId,float scale,BattleMonsterNetworkState state=null)
        {
            BattlePresentationAtlas atlas=catalog==null?null:catalog.Resolve(monsterId);
            if(atlas==null||!atlas.Available||catalog.Shader==null)return false;
            // Unity native-backed objects cannot be constructed by a MonoBehaviour field initializer.
            _properties??=new MaterialPropertyBlock();
            ReleaseArtwork();_released=false;_state=state;_atlas=atlas;_height=(monsterId=="ELITE_MONSTER"?1f:.78f)*Mathf.Max(.01f,scale);
            _stat=GetComponent<MonsterStat>();_lastHp=_stat==null?0:_stat.CurrentHp;
            if(_stat!=null){_stat.OnHpChanged+=HealthChanged;_stat.OnHpInitialized+=HealthInitialized;}
            _legacy=GetComponent<Renderer>();if(_legacy!=null){_legacyEnabled=_legacy.enabled;_legacy.enabled=false;}
            _stage=new GameObject("Painted monster body",typeof(MeshFilter),typeof(MeshRenderer)).transform;_stage.SetParent(transform,false);
            _filter=_stage.GetComponent<MeshFilter>();_renderer=_stage.GetComponent<MeshRenderer>();_material=new Material(catalog.Shader);_renderer.sharedMaterial=_material;
            _health=GetComponentInChildren<MonsterHealthView>(true);if(_health!=null)_healthPrevious=_health.SetPresentationRenderer(_renderer);
            _previous=transform.position;_distance=0;_direction=0;_mirror=false;_deathAt=-1;_flashUntil=0;_revealAt=-100;_lastViewAt=Time.unscaledTime;Opacity=1;
            LateUpdate();return true;
        }
        public void RevealLiveSpawn(float age=0){if(!_released&&!IsDying)_revealAt=Time.unscaledTime-Mathf.Clamp(age,0,.12f);}
        private void HealthInitialized(float hp,float max){_lastHp=hp;_flashUntil=0;}
        private void HealthChanged(float hp,float max)
        {
            if(hp<_lastHp-.0001f&&hp>0&&!IsDying){_flashUntil=Time.unscaledTime+.09f;HitFlashCount++;}
            _lastHp=hp;
        }
        private void LateUpdate()
        {
            if(_released||_stage==null)return;
            bool networkReady=_state!=null&&_state.Object!=null&&_state.Object.IsValid;
            bool dead=networkReady?(bool)_state.IsDead:_stat!=null&&_stat.IsDead;
            UpdatePresentation(transform.position,dead,Time.unscaledTime);
        }
        public void UpdatePresentation(Vector3 position,bool dead,float now)
        {
            if(_released||_stage==null)return;
            Vector3 delta=position-_previous;delta.y=0;_previous=position;
            float deltaTime=Mathf.Clamp(now-_lastViewAt,0,.1f);_lastViewAt=now;
            if(dead&&_deathAt<0)_deathAt=now;
            if(!dead&&_deathAt<0&&delta.sqrMagnitude>.000001f)
            {
                _distance+=Mathf.Min(delta.magnitude/Mathf.Max(.14f,_height*.2f),deltaTime*10f);
                _direction=Mathf.Abs(delta.x)>Mathf.Abs(delta.z)*.65f?1:delta.z>0?2:0;
                _mirror=_direction==1&&delta.x<0;
            }
            int gait=IsDying?0:Mathf.FloorToInt(_distance)%4,index=gait*3+_direction;
            if(!_frames.TryGetValue(index,out var frame)){frame=BattlePresentationFrameCache.Acquire(_atlas,index,_height);if(frame==null)return;_frames.Add(index,frame);}
            _filter.sharedMesh=frame.Mesh;
            _stage.SetPositionAndRotation(new Vector3(position.x,FootHeight,position.z),Quaternion.Euler(25,0,0));
            Vector3 scale=transform.lossyScale;_stage.localScale=new Vector3((_mirror?-1:1)/Safe(scale.x),1/Safe(scale.y),1/Safe(scale.z));
            Opacity=IsDying?1-Mathf.Clamp01(now-_deathAt):Mathf.Clamp01((now-_revealAt)/.12f);
            if(IsDying&&_legacy!=null&&_legacy.sharedMaterial!=null)
            {
                Material legacy=_legacy.sharedMaterial;
                if(legacy.HasProperty("_Color"))Opacity=Mathf.Min(Opacity,legacy.GetColor("_Color").a);
                else if(legacy.HasProperty("_BaseColor"))Opacity=Mathf.Min(Opacity,legacy.GetColor("_BaseColor").a);
            }
            _properties.SetTexture("_MainTex",frame.Sprite.texture);_properties.SetFloat("_Opacity",Opacity);
            _properties.SetFloat("_Flash",!IsDying&&now<_flashUntil?1:0);_renderer.SetPropertyBlock(_properties);
            _health?.SetPresentationDead(IsDying);
        }
        private static float Safe(float x)=>Mathf.Abs(x)<.0001f?1:x;
        public void ReleaseArtwork()
        {
            if(_released)return;_released=true;
            if(_stat!=null){_stat.OnHpChanged-=HealthChanged;_stat.OnHpInitialized-=HealthInitialized;}
            if(_legacy!=null)_legacy.enabled=_legacyEnabled;
            if(_health!=null){_health.SetPresentationRenderer(_healthPrevious);_health.SetPresentationDead(_stat!=null&&_stat.IsDead);}
            foreach(var frame in _frames.Values)frame.Dispose();_frames.Clear();
            BattleSpriteGeometry.Release(_stage==null?null:_stage.gameObject);BattleSpriteGeometry.Release(_material);
            _stage=null;_renderer=null;_health=null;_state=null;
        }
        private void OnDisable()=>ReleaseArtwork();
        private void OnDestroy()=>ReleaseArtwork();
    }
}
