using System.Collections.Generic;
using Fusion;
using MyDefense.Battle.Runtime;
using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Transient cosmetic notifications only. Initial snapshots create views but never replay birth/merge effects.</summary>
    [DefaultExecutionOrder(1400)]
    public sealed class BattleMonsterPresentationCoordinator:MonoBehaviour
    {
        private BattleRunnerLifecycle _lifecycle;
        private FusionKidnapBoardView _board;
        private BattleMonsterArtCatalog _catalog;
        private bool _subscribed;
        private readonly HashSet<BattleMonsterNetworkState> _ready=new();
        private readonly List<BattleMonsterNetworkState> _readyWork=new();
        private readonly Dictionary<BattleMonsterNetworkState,BattleMonsterArtView> _views=new();
        private readonly List<PendingSpawn> _spawns=new();
        private readonly List<PendingMerge> _merges=new();
        private readonly BattlePresentationEventGate _gate=new();
        private readonly Dictionary<LaneType,FxSlot> _portals=new();
        private readonly List<FxSlot> _mergePool=new();
        private BattlePresentationFrameCache.Lease[] _portalFrames,_mergeFrames;
        private Material _fxMaterial;
        private BattleSessionContext _context;
        private string _session;
        private BattleFieldPerspective _perspective;
        private bool _perspectiveSearched;
        public int SpawnEffectCount {get;private set;}
        public int MergeEffectCount {get;private set;}
        public void Initialize(BattleRunnerLifecycle lifecycle,FusionKidnapBoardView board){_lifecycle=lifecycle;_board=board;Resume();}
        private void OnEnable(){if(_lifecycle!=null)Resume();}
        private void OnDisable()=>Suspend();
        private void OnDestroy()=>Suspend();
        public void Resume()
        {
            if(_subscribed)return;
            _catalog=Resources.Load<BattleMonsterArtCatalog>(BattleMonsterArtCatalog.ResourcePath);
            if(_catalog==null||!_catalog.Available){Debug.LogWarning("[BattlePresentation] Monster/portal/merge artwork catalog unavailable. Generate Battle Presentation Refinement Assets.");return;}
            _fxMaterial=new Material(_catalog.EffectShader);
            _portalFrames=Frames(_catalog.Portal);_mergeFrames=Frames(_catalog.Merge);
            BattleMonsterNetworkState.PresentationReady+=Ready;BattleMonsterNetworkState.PresentationRemoved+=Removed;
            BattleWaveStateAuthority.PresentationReset+=ResetFromAuthority;
            BattleWaveStateAuthority.PresentationSpawnAccepted+=SpawnAccepted;BattleWaveStateAuthority.PresentationMergeAccepted+=MergeAccepted;
            _subscribed=true;
            foreach(GameObject root in gameObject.scene.GetRootGameObjects())
                foreach(var state in root.GetComponentsInChildren<BattleMonsterNetworkState>(true))
                    if(state.Object!=null&&state.Object.IsValid)Ready(state);
        }
        private static BattlePresentationFrameCache.Lease[] Frames(BattlePresentationAtlas atlas)
        {var result=new BattlePresentationFrameCache.Lease[atlas.Count];for(int i=0;i<result.Length;i++)result[i]=BattlePresentationFrameCache.Acquire(atlas,i,1);return result;}
        private bool SameSession(BattleWaveStateAuthority sender)=>sender!=null&&sender.gameObject.scene==gameObject.scene
            &&(_lifecycle?.Runner==null||sender.Runner==_lifecycle.Runner);
        private bool Current(BattleWaveStateAuthority sender,string session)=>SameSession(sender)&&!string.IsNullOrWhiteSpace(session)&&session==_lifecycle?.SessionContext?.BattleSessionId;
        private void SyncSession()
        {
            var context=_lifecycle?.SessionContext;if(ReferenceEquals(context,_context))return;
            ClearSession();_context=context;_session=context?.BattleSessionId;
            foreach(GameObject root in gameObject.scene.GetRootGameObjects())foreach(var state in root.GetComponentsInChildren<BattleMonsterNetworkState>(true))
                if(state.Object!=null&&state.Object.IsValid)Ready(state);
        }
        private void Ready(BattleMonsterNetworkState state)
        {if(this!=null&&state!=null&&state.gameObject.scene==gameObject.scene&&(_lifecycle?.Runner==null||state.Runner==_lifecycle.Runner))_ready.Add(state);}
        private void Removed(BattleMonsterNetworkState state)
        {if(this==null)return;_ready.Remove(state);if(_views.TryGetValue(state,out var view)){if(view!=null){view.ReleaseArtwork();view.enabled=false;}_views.Remove(state);}}
        private void ResetFromAuthority(BattleWaveStateAuthority sender){if(this!=null&&SameSession(sender))ClearSession();}
        private void SpawnAccepted(BattleWaveStateAuthority sender,string session,ulong sequence,NetworkId objectId,ulong runtimeId,int tick)
        {
            if(this==null)return;SyncSession();if(!Current(sender,session)||!_gate.AcceptSpawn(sequence,runtimeId))return;
            _spawns.Add(new PendingSpawn{Sender=sender,Session=session,ObjectId=objectId,RuntimeId=runtimeId,Tick=tick,Deadline=Time.unscaledTime+.7f});
        }
        private void MergeAccepted(BattleWaveStateAuthority sender,string session,ulong sequence,int player,int source,int target)
        {
            if(this==null)return;SyncSession();if(!Current(sender,session)||!_gate.AcceptMerge(sequence,source,target))return;
            _merges.Add(new PendingMerge{Sender=sender,Session=session,Player=player,Target=target,Deadline=Time.unscaledTime+.5f});
        }
        private void LateUpdate()
        {
            if(!_subscribed)return;
            SyncSession();
            _readyWork.Clear();_readyWork.AddRange(_ready);
            foreach(var state in _readyWork)
            {
                if(state==null){_ready.Remove(state);continue;}
                if(state.Object==null||!state.Object.IsValid||!state.IsInitialized)continue;
                if(!string.IsNullOrEmpty(_session)&&state.BattleSessionId.ToString()!=_session){_ready.Remove(state);continue;}
                if(_views.ContainsKey(state)){_ready.Remove(state);continue;}
                var view=state.GetComponent<BattleMonsterArtView>();if(view==null)view=state.gameObject.AddComponent<BattleMonsterArtView>();
                view.enabled=true;
                if(!view.Configure(_catalog,state.MonsterId.ToString(),state.PresentationScale,state))
                    Debug.LogWarning("[BattlePresentation] No authored Monster mapping for "+state.MonsterId);
                else _views.Add(state,view);
                _ready.Remove(state);
            }
            for(int i=_spawns.Count-1;i>=0;i--)
            {
                PendingSpawn pending=_spawns[i];
                if(!Current(pending.Sender,pending.Session)||pending.Sender.Runner==null||!pending.Sender.Runner.IsRunning){_spawns.RemoveAt(i);continue;}
                bool found=pending.Sender.Runner.TryFindObject(pending.ObjectId,out NetworkObject obj);
                var state=found?obj.GetComponent<BattleMonsterNetworkState>():null;
                bool initialized=state!=null&&state.Object!=null&&state.Object.IsValid&&state.IsInitialized;
                bool hasView=initialized&&_views.ContainsKey(state);
                float age=Mathf.Max(0,pending.Sender.Runner.Tick.Raw-pending.Tick)*pending.Sender.Runner.DeltaTime;
                LaneType lane=default;Vector3 anchor=default;
                bool placed=hasView&&TryPortalAnchor(pending.Sender,state,out lane,out anchor);
                var decision=BattlePresentationReadiness.Spawn(found,initialized,initialized&&state.RuntimeMonsterId==pending.RuntimeId,initialized&&state.IsDead,hasView,placed,age>.35f||Time.unscaledTime>pending.Deadline);
                if(decision==BattleVisualReadiness.Wait)continue;
                if(decision==BattleVisualReadiness.Skip){_spawns.RemoveAt(i);continue;}
                {
                    FxSlot portal=Portal(lane);portal.Position=anchor;portal.BurstAt=Time.unscaledTime-age;
                    if(age<.12f)_views[state].RevealLiveSpawn(age);SpawnEffectCount++;
                }
                _spawns.RemoveAt(i);
            }
            for(int i=_merges.Count-1;i>=0;i--)
            {
                PendingMerge merge=_merges[i];
                if(!Current(merge.Sender,merge.Session)){_merges.RemoveAt(i);continue;}
                Transform tile=_board==null?null:_board.TileAt(merge.Player,merge.Target);
                if(tile!=null){PlayMerge(tile.position+Vector3.up*.20f);_merges.RemoveAt(i);}
                else if(Time.unscaledTime>=merge.Deadline)_merges.RemoveAt(i);
            }
            foreach(var pair in _portals)
            {
                FxSlot slot=pair.Value;float age=Time.unscaledTime-slot.BurstAt;
                int index=age>=0&&age<.4f?4+Mathf.Min(3,(int)(age*10)):((int)(Time.unscaledTime*5)%4);
                slot.Draw(_portalFrames[index].Sprite,slot.Position,Quaternion.Euler(90,0,0),1.3f,1);
            }
            foreach(FxSlot slot in _mergePool)
            {
                float age=Time.unscaledTime-slot.BurstAt;
                if(age<0||age>.45f){slot.Hide();continue;}
                slot.Draw(_mergeFrames[Mathf.Min(5,(int)(age/.45f*6))].Sprite,slot.Position,Quaternion.Euler(25,0,0),1.45f,1-age*.4f);
            }
        }
        private bool TryPortalAnchor(BattleWaveStateAuthority authority,BattleMonsterNetworkState state,out LaneType lane,out Vector3 anchor)
        {
            lane=LaneType.BossSharedLane;anchor=default;
            int local=authority.GetNetworkedPlayerSlot(authority.Runner.LocalPlayer);
            if(!_perspectiveSearched)
            {
                _perspectiveSearched=true;
                foreach(GameObject root in gameObject.scene.GetRootGameObjects())
                {var found=root.GetComponentInChildren<BattleFieldPerspective>(true);if(found!=null){_perspective=found;break;}}
            }
            int ownerSlot=0;
            if(state.LanePolicy==BattleMonsterLanePolicy.EACH_FIELD)
            {
                string owner=state.FieldOwnerPlayerId.ToString();
                if(owner==authority.Player1UserId.ToString())ownerSlot=1;
                else if(owner==authority.Player2UserId.ToString())ownerSlot=2;else return false;
            }
            if(!BattlePresentationReadiness.TryLane(local,_perspective!=null&&_perspective.IsApplied,state.LanePolicy,ownerSlot,out lane))return false;
            var path=PathManager.Instance==null?null:PathManager.Instance.GetPath(lane);
            if(path==null||path.Count<2)return false;
            anchor=new Vector3(path[0].position.x,.027f,path[0].position.z);return true;
        }
        private FxSlot Portal(LaneType lane)
        {if(!_portals.TryGetValue(lane,out var slot)){slot=new FxSlot(transform,_fxMaterial,"Lane spawn portal",-2);_portals.Add(lane,slot);}return slot;}
        private void PlayMerge(Vector3 position)
        {
            FxSlot available=null;foreach(var slot in _mergePool)if(Time.unscaledTime-slot.BurstAt>.45f){available=slot;break;}
            if(available==null&&_mergePool.Count<8){available=new FxSlot(transform,_fxMaterial,"Accepted merge",5);_mergePool.Add(available);}
            if(available==null)return;available.Position=position;available.BurstAt=Time.unscaledTime;MergeEffectCount++;
        }
        public void ClearSession()
        {
            _gate.Clear();_ready.Clear();_spawns.Clear();_merges.Clear();
            foreach(var view in _views.Values)if(view!=null){view.ReleaseArtwork();view.enabled=false;}_views.Clear();
            foreach(var portal in _portals.Values)portal.Dispose();_portals.Clear();
            foreach(var slot in _mergePool)slot.Dispose();_mergePool.Clear();
        }
        public void Suspend()
        {
            if(_subscribed)
            {
                BattleMonsterNetworkState.PresentationReady-=Ready;BattleMonsterNetworkState.PresentationRemoved-=Removed;
                BattleWaveStateAuthority.PresentationReset-=ResetFromAuthority;BattleWaveStateAuthority.PresentationSpawnAccepted-=SpawnAccepted;
                BattleWaveStateAuthority.PresentationMergeAccepted-=MergeAccepted;
            }
            _subscribed=false;ClearSession();
            if(_portalFrames!=null)foreach(var frame in _portalFrames)frame?.Dispose();if(_mergeFrames!=null)foreach(var frame in _mergeFrames)frame?.Dispose();
            _portalFrames=null;_mergeFrames=null;BattleSpriteGeometry.Release(_fxMaterial);_fxMaterial=null;
        }
        private struct PendingSpawn{public BattleWaveStateAuthority Sender;public string Session;public NetworkId ObjectId;public ulong RuntimeId;public int Tick;public float Deadline;}
        private struct PendingMerge{public BattleWaveStateAuthority Sender;public string Session;public int Player,Target;public float Deadline;}
        private sealed class FxSlot
        {
            private readonly SpriteRenderer _renderer;
            public float BurstAt=-100;
            public Vector3 Position;
            public FxSlot(Transform parent,Material material,string name,int order)
            {var go=new GameObject(name);go.transform.SetParent(parent,false);_renderer=go.AddComponent<SpriteRenderer>();_renderer.sharedMaterial=material;_renderer.sortingOrder=order;_renderer.enabled=false;}
            public void Draw(Sprite sprite,Vector3 position,Quaternion rotation,float scale,float alpha)
            {_renderer.sprite=sprite;_renderer.color=new Color(1,1,1,alpha);_renderer.transform.SetPositionAndRotation(position,rotation);_renderer.transform.localScale=Vector3.one*scale;_renderer.enabled=true;}
            public void Hide()=>_renderer.enabled=false;
            public void Dispose()=>BattleSpriteGeometry.Release(_renderer.gameObject);
        }
    }
    public sealed class BattlePresentationEventGate
    {
        private readonly HashSet<ulong> _events=new(),_monsters=new();
        public bool AcceptMerge(ulong sequence,int source,int target)=>source>=0&&source<24&&target>=0&&target<24&&source!=target&&sequence>0&&_events.Add(sequence);
        public bool AcceptSpawn(ulong sequence,ulong runtime)=>sequence>0&&runtime>0&&!_monsters.Contains(runtime)&&_events.Add(sequence)&&_monsters.Add(runtime);
        public void Clear(){_events.Clear();_monsters.Clear();}
    }
    public enum BattleVisualReadiness {Wait,Skip,Ready}
    public static class BattlePresentationReadiness
    {
        public static BattleVisualReadiness Spawn(bool exists,bool initialized,bool sameIdentity,bool dead,bool view,bool placement,bool expired)
        {
            if(expired||initialized&&(!sameIdentity||dead))return BattleVisualReadiness.Skip;
            return exists&&initialized&&view&&placement?BattleVisualReadiness.Ready:BattleVisualReadiness.Wait;
        }
        public static bool TryLane(int local,bool perspective,BattleMonsterLanePolicy policy,int owner,out LaneType lane)
        {
            lane=LaneType.BossSharedLane;if(local!=1&&local!=2||local==2&&!perspective)return false;
            if(policy==BattleMonsterLanePolicy.BOSS_SHARED)return true;
            if(owner!=1&&owner!=2)return false;
            int displayed=local==2?3-owner:owner;lane=displayed==1?LaneType.Player1Lane:LaneType.Player2Lane;return true;
        }
    }
}
