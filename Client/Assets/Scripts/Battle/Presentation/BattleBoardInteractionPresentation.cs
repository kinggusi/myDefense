using System;
using System.Collections.Generic;
using MyDefense.Battle.Combat;
using MyDefense.Shared.Contracts;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Picking, grade/merge hints and a render-only pending preview. Never changes board state or logical roots.</summary>
    [DefaultExecutionOrder(-40)]
    public sealed class BattleBoardInteractionPresentation : MonoBehaviour
    {
        private FusionKidnapBoardView _view;
        private BattleWaveStateAuthority _authority;
        private readonly Dictionary<GameObject,Badge> _badges=new();
        private readonly List<GameObject> _stale=new();
        private Canvas _canvas;
        private Font _font;
        private Font _hintFont;
        private int _pickFrame=-1;
        private Vector2 _pickPosition;
        private FusionKidnapUnitDrag _picked;
        private RenderPreview _preview;
        private int _selectedSlot=-1;
        private int _selectedPlayer;
        private uint _generation;
        private static readonly ProfilerMarker PickMarker=new("Battle.Board.Pick");
        public int PhysicsPickCount {get;private set;}
        public int SelectedSlot=>_selectedSlot;
        public string SelectionDetail {get;private set;}="";
        public bool HasPending=>_preview!=null&&_preview.Pending;
        public Vector3 PreviewPosition=>_preview==null?Vector3.zero:_preview.Position;

        public void Initialize(FusionKidnapBoardView view,BattleWaveStateAuthority authority)
        {
            if(_view!=null)_view.UnitViewChanged-=UnitChanged;
            _view=view;_authority=authority;
            if(_view!=null){_view.UnitViewChanged-=UnitChanged;_view.UnitViewChanged+=UnitChanged;}
            if(_canvas==null)
            {
                var go=new GameObject("Battle grade and merge hints",typeof(RectTransform),typeof(Canvas));
                go.transform.SetParent(transform,false);_canvas=go.GetComponent<Canvas>();_canvas.renderMode=RenderMode.ScreenSpaceOverlay;_canvas.sortingOrder=55;
                _font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var style=Resources.Load<BattleHudStyleCatalog>(BattleHudStyleCatalog.ResourcePath);
                _hintFont=style!=null&&style.Korean!=null?style.Korean:_font;
                if(style!=null&&style.Numbers!=null)_font=style.Numbers;
                _canvas.gameObject.SetActive(false);
            }
        }
        private void OnEnable(){BattleProjectileSpawner.PresentationSessionReset+=SessionReset;if(_view!=null){_view.UnitViewChanged-=UnitChanged;_view.UnitViewChanged+=UnitChanged;}}
        private void OnDisable(){BattleProjectileSpawner.PresentationSessionReset-=SessionReset;if(_view!=null)_view.UnitViewChanged-=UnitChanged;CancelAll();if(_canvas!=null)_canvas.gameObject.SetActive(false);}
        private void OnDestroy(){CancelAll();BattleSpriteGeometry.Release(_canvas==null?null:_canvas.gameObject);}
        private void SessionReset(BattleProjectileSpawner sender){if(sender!=null&&sender.gameObject.scene==gameObject.scene)CancelAll();}

        private bool LocalActive(out int player)
        {
            player=0;
            if(_authority==null||!_authority.IsSpawnedForAccess||_authority.Runner==null)return false;
            player=_authority.GetNetworkedPlayerSlot(_authority.Runner.LocalPlayer);
            if(player!=1&&player!=2)return false;
            PlayerBattleState state=player==1?_authority.Player1BattleState:_authority.Player2BattleState;
            return BattleHudPresentation.AllowsCommands(_authority.Runner.IsRunning,_authority.MatchState,state)&&!_authority.IsMythicChoiceActive(player);
        }
        public bool CanManipulate(int player)=>LocalActive(out int local)&&local==player;

        private void Update()
        {
            if(_view==null)return;
            if(!LocalActive(out int player))
            { if(_preview!=null||_selectedSlot>=0)CancelAll(); return; }
            TickPreview(Time.unscaledTime);
            if(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame)
            {
                FusionKidnapUnitDrag pick=Pick(Mouse.current.position.ReadValue());
                _selectedSlot=pick==null?-1:pick.SlotIndex;_selectedPlayer=player;
                if(pick!=null)_view.SelectMythicIfApplicable(player,pick.SlotIndex);
            }
        }
        private void UnitChanged(GameObject unit)
        {
            if(_preview==null||_preview.Drag==null||_preview.Drag.gameObject!=unit)return;
            if(_preview.IsStale||_preview.Drag.SlotIndex!=_preview.SourceSlot)
            {_selectedSlot=_preview.Drag.SlotIndex;CancelActivePreview();}
        }
        private void TickPreview(float now)
        {
            if(_preview==null)return;
            if(_preview.IsStale||_preview.Pending&&(now>=_preview.Deadline||_preview.Drag.SlotIndex!=_preview.SourceSlot))CancelActivePreview();
        }

        public FusionKidnapUnitDrag Pick(Vector2 position)
        {
            if(!LocalActive(out int player))return null;
            return ResolvePick(Camera.main,position,player);
        }
        private FusionKidnapUnitDrag ResolvePick(Camera camera,Vector2 position,int player)
        {
            if(BattleHudPresentation.BlocksWorldInput(position))return null;
            if(_pickFrame==Time.frameCount&&_pickPosition==position)return _picked;
            _pickFrame=Time.frameCount;_pickPosition=position;_picked=null;
            if(camera==null||!camera.pixelRect.Contains(position))return null;
            using(PickMarker.Auto())
            {
                PhysicsPickCount++;
                if(Physics.Raycast(camera.ScreenPointToRay(position),out RaycastHit hit))
                {
                    var drag=hit.collider.GetComponentInParent<FusionKidnapUnitDrag>();
                    if(drag!=null&&drag.PlayerSlot==player&&drag.gameObject.scene==gameObject.scene)_picked=drag;
                }
                if(_picked==null)
                {
                    float best=float.MaxValue;
                    foreach(GameObject unit in _view.VisibleUnits)
                    {
                        if(unit==null)continue;
                        var drag=unit.GetComponent<FusionKidnapUnitDrag>();if(drag==null||drag.PlayerSlot!=player)continue;
                        var art=unit.GetComponent<BattleUnitArtView>();
                        if(art==null||!art.TryGetBodyBounds(out Bounds body))continue;
                        Rect bounds=ScreenRect(camera,body);if(!bounds.Contains(position))continue;
                        float distance=((Vector2)camera.WorldToScreenPoint(unit.transform.position)-position).sqrMagnitude;
                        if(distance<best){best=distance;_picked=drag;}
                    }
                }
            }
            return _picked;
        }

        public bool BeginPreview(FusionKidnapUnitDrag drag)
        {
            if(HasPending)return false;
            if(drag==null||!LocalActive(out int player)||drag.PlayerSlot!=player)return false;
            EndPreview();_selectedSlot=drag.SlotIndex;_selectedPlayer=player;
            _preview=new RenderPreview(drag,_generation);return true;
        }
        public void MovePreview(FusionKidnapUnitDrag drag,Vector3 position)
        { if(_preview!=null&&_preview.Drag==drag)_preview.Move(position); }
        public void Await(FusionKidnapUnitDrag drag,int targetSlot)
        {
            if(_preview==null||_preview.Drag!=drag)return;
            Transform tile=_view.TileAt(drag.PlayerSlot,targetSlot);
            if(tile==null){EndPreview();return;}
            _preview.Move(new Vector3(tile.position.x,drag.transform.position.y,tile.position.z));
            _preview.Pending=true;_preview.TargetSlot=targetSlot;_preview.Deadline=Time.unscaledTime+1.5f;
        }
        public void Acknowledge(GameObject source,int player,int from,int to)
        {
            if(_preview==null||_preview.Generation!=_generation||_preview.Drag==null||_preview.Drag.gameObject!=source
                ||_preview.Drag.PlayerSlot!=player||_preview.SourceSlot!=from||_preview.TargetSlot!=to)return;
            EndPreview();_selectedSlot=to;_selectedPlayer=player;
        }
        public void CancelPreview(FusionKidnapUnitDrag drag)
        {if(_preview!=null&&_preview.Drag==drag)EndPreview();}
        private void EndPreview(){_preview?.Dispose();_preview=null;}
        private void CancelActivePreview(){FusionKidnapUnitDrag drag=_preview?.Drag;EndPreview();if(drag!=null)drag.CancelPresentationInput();}
        public void CancelAll(){_generation++;CancelActivePreview();if(_picked!=null)_picked.CancelPresentationInput();_picked=null;_selectedSlot=-1;_selectedPlayer=0;SelectionDetail="";_pickFrame=-1;}

        public static bool IsMergePartner(int source,int target,bool sourceOccupied,bool targetOccupied,long sourceId,long targetId,byte sourceGrade,byte targetGrade,
            bool localActive,bool choiceActive,bool sourceInjector,bool targetInjector)
            =>localActive&&!choiceActive&&!sourceInjector&&!targetInjector&&BattleWaveStateAuthority.CanMerge(source,target,sourceOccupied,targetOccupied,sourceId,targetId,sourceGrade,targetGrade);
        public static string GradeLabel(byte grade)=>grade switch{0=>"N",1=>"E",2=>"U",3=>"L",4=>"M",_=>"DNA"};
        public static Color GradeColor(byte grade)=>grade switch{1=>new Color(.74f,.40f,.95f),2=>new Color(.96f,.77f,.32f),3=>new Color(.26f,.91f,.53f),4=>new Color(1f,.31f,.51f),_=>new Color(.78f,.85f,.89f)};

        private void LateUpdate()
        {
            if(_canvas==null)return;
            if(!isActiveAndEnabled||_view==null||_authority==null||!_authority.IsSpawnedForAccess){_canvas.gameObject.SetActive(false);return;}
            _canvas.gameObject.SetActive(true);
            Camera camera=Camera.main;if(camera==null)return;
            bool canAct=LocalActive(out int local);int partners=0;
            _stale.Clear();foreach(var pair in _badges)if(pair.Key==null)_stale.Add(pair.Key);
            foreach(GameObject key in _stale){_badges[key].Dispose();_badges.Remove(key);}
            foreach(GameObject unit in _view.VisibleUnits)
            {
                if(unit==null)continue;
                var drag=unit.GetComponent<FusionKidnapUnitDrag>();if(drag==null)continue;
                if(!_badges.TryGetValue(unit,out Badge badge)){badge=new Badge(_canvas.transform,_font,_hintFont);_badges.Add(unit,badge);}
                int p=drag.PlayerSlot,slot=drag.SlotIndex;byte grade=_authority.GetBoardGrade(p,slot);
                bool selected=p==_selectedPlayer&&slot==_selectedSlot;
                bool partner=false;
                if(p==local&&_selectedPlayer==local&&_selectedSlot>=0)
                    partner=IsMergePartner(_selectedSlot,slot,_authority.IsBoardOccupied(p,_selectedSlot),_authority.IsBoardOccupied(p,slot),
                        _authority.GetBoardAlienId(p,_selectedSlot),_authority.GetBoardAlienId(p,slot),_authority.GetBoardGrade(p,_selectedSlot),grade,
                        canAct,_authority.IsMythicChoiceActive(p),_authority.IsBoardInjector(p,_selectedSlot),_authority.IsBoardInjector(p,slot));
                if(partner)partners++;
                Vector3 root=_preview!=null&&_preview.Drag==drag?_preview.Position:unit.transform.position;
                badge.Draw(camera,root,grade,selected,partner);
            }
            if(_selectedSlot>=0&&_selectedPlayer==local&&_authority.IsBoardOccupied(local,_selectedSlot))
                SelectionDetail=(_authority.IsBoardInjector(local,_selectedSlot)?"Mutation Injector · DNA":$"Alien #{_authority.GetBoardAlienId(local,_selectedSlot)} · {FusionKidnapBoardView.GradeNameForPresentation(_authority.GetBoardGrade(local,_selectedSlot))} · 합성 가능 {partners}")+(_preview!=null&&_preview.Pending?" · 승인 대기":"");
            else SelectionDetail="";
        }

        private static Rect ScreenRect(Camera camera,Bounds bounds)
        {
            Vector2 min=new(float.MaxValue,float.MaxValue),max=new(float.MinValue,float.MinValue);
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {Vector3 p=camera.WorldToScreenPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }

        private sealed class Badge
        {
            private readonly RectTransform _root,_outline,_underlay,_inner,_pill;
            private readonly Text _label,_hint;
            private readonly BattleHudGraphic _frame,_ring,_innerRing;
            public Badge(Transform parent,Font font,Font hintFont)
            {
                var go=new GameObject("Grade badge",typeof(RectTransform),typeof(CanvasRenderer),typeof(BattleHudGraphic));go.transform.SetParent(parent,false);
                _root=(RectTransform)go.transform;_frame=go.GetComponent<BattleHudGraphic>();_frame.raycastTarget=false;
                var text=new GameObject("Grade",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));text.transform.SetParent(go.transform,false);
                _label=text.GetComponent<Text>();_label.font=font;_label.fontSize=12;_label.fontStyle=FontStyle.Bold;_label.alignment=TextAnchor.MiddleCenter;_label.raycastTarget=false;
                var tr=(RectTransform)text.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
                var ring=new GameObject("Selected or merge tile",typeof(RectTransform),typeof(CanvasRenderer),typeof(BattleHudGraphic));ring.transform.SetParent(parent,false);
                _outline=(RectTransform)ring.transform;_ring=ring.GetComponent<BattleHudGraphic>();_ring.OutlineOnly=true;_ring.raycastTarget=false;
                _ring.OutlineWidth=2.5f;_ring.UseDirectOutlineColor=true;
                _outline.SetAsFirstSibling();
                var inner=new GameObject("Merge inner edge",typeof(RectTransform),typeof(CanvasRenderer),typeof(BattleHudGraphic));inner.transform.SetParent(_outline,false);
                _inner=(RectTransform)inner.transform;_inner.anchorMin=Vector2.zero;_inner.anchorMax=Vector2.one;_inner.offsetMin=new Vector2(4,4);_inner.offsetMax=new Vector2(-4,-4);
                _innerRing=inner.GetComponent<BattleHudGraphic>();_innerRing.OutlineOnly=true;_innerRing.OutlineWidth=1.4f;_innerRing.UseDirectOutlineColor=true;_innerRing.color=new Color(.72f,1f,.79f);_innerRing.raycastTarget=false;
                var under=new GameObject("Merge tile contrast",typeof(RectTransform),typeof(CanvasRenderer),typeof(BattleHudGraphic));under.transform.SetParent(parent,false);
                _underlay=(RectTransform)under.transform;var underGraphic=under.GetComponent<BattleHudGraphic>();underGraphic.OutlineOnly=true;underGraphic.OutlineWidth=7.5f;underGraphic.UseDirectOutlineColor=true;underGraphic.color=new Color(.008f,.015f,.022f);underGraphic.raycastTarget=false;_underlay.SetAsFirstSibling();
                var pill=new GameObject("Merge available pill",typeof(RectTransform),typeof(CanvasRenderer),typeof(BattleHudGraphic));pill.transform.SetParent(_outline,false);
                _pill=(RectTransform)pill.transform;_pill.anchorMin=_pill.anchorMax=new Vector2(.5f,0);_pill.pivot=new Vector2(.5f,1);_pill.sizeDelta=new Vector2(38,24);_pill.anchoredPosition=new Vector2(0,-4);
                var pillGraphic=pill.GetComponent<BattleHudGraphic>();pillGraphic.Flat=true;pillGraphic.color=new Color(.55f,.60f,.65f);pillGraphic.raycastTarget=false;
                var hint=new GameObject("Merge available",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));hint.transform.SetParent(_pill,false);
                _hint=hint.GetComponent<Text>();_hint.font=hintFont;_hint.fontSize=13;_hint.color=new Color(.80f,1f,.86f);_hint.alignment=TextAnchor.MiddleCenter;_hint.text="합성";_hint.raycastTarget=false;
                _hint.verticalOverflow=VerticalWrapMode.Overflow;_hint.horizontalOverflow=HorizontalWrapMode.Overflow;
                var hintRect=(RectTransform)hint.transform;hintRect.anchorMin=Vector2.zero;hintRect.anchorMax=Vector2.one;hintRect.offsetMin=new Vector2(3,0);hintRect.offsetMax=new Vector2(-3,0);
            }
            public void Draw(Camera camera,Vector3 root,byte grade,bool selected,bool partner)
            {
                Vector3 foot=root-Vector3.up*.735f;Vector3 screen=camera.WorldToScreenPoint(foot);
                _root.gameObject.SetActive(screen.z>0&&camera.pixelRect.Contains(screen));
                float width=grade>4?34:24;
                Place(_root,new Rect(screen.x-width*.5f,screen.y-11,width,16));_label.text=GradeLabel(grade);_label.color=GradeColor(grade);_frame.color=GradeColor(grade);
                _outline.gameObject.SetActive((selected||partner)&&screen.z>0);
                _underlay.gameObject.SetActive((selected||partner)&&screen.z>0);_pill.gameObject.SetActive(partner);_inner.gameObject.SetActive(partner);
                if(selected||partner)
                {
                    Bounds world=new Bounds(foot,new Vector3(1.02f,.01f,1.02f));Rect rect=ScreenRect(camera,world);Place(_outline,rect);
                    Place(_underlay,rect);
                    _ring.color=selected?new Color(.38f,.94f,1f):new Color(.42f,1f,.56f);
                }
            }
            public void Dispose(){BattleSpriteGeometry.Release(_root.gameObject);BattleSpriteGeometry.Release(_outline.gameObject);BattleSpriteGeometry.Release(_underlay.gameObject);}
            private static void Place(RectTransform rect,Rect value)
            {
                if(rect.anchorMin!=Vector2.zero||rect.anchorMax!=Vector2.zero||rect.pivot!=Vector2.zero)rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;
                if(rect.anchoredPosition!=value.position)rect.anchoredPosition=value.position;
                if(rect.sizeDelta!=value.size)rect.sizeDelta=value.size;
            }
        }

        private sealed class RenderPreview : IDisposable
        {
            public readonly FusionKidnapUnitDrag Drag;
            public readonly int SourceSlot;
            public readonly uint Generation;
            public int TargetSlot=-1;
            public bool Pending;
            public float Deadline;
            public Vector3 Position {get;private set;}
            private readonly long _species;
            private readonly byte _grade;
            private readonly GameObject _ghost;
            private readonly BattleUnitArtView _art;
            private readonly Renderer _legacy;
            private readonly MaterialPropertyBlock _original=new();
            private readonly Vector3 _origin;
            public bool IsStale=>Drag==null||Drag.GetComponent<UnitData>() is not UnitData data||data.specId!=_species||data.grade!=FusionKidnapBoardView.GradeNameForPresentation(_grade);
            public RenderPreview(FusionKidnapUnitDrag drag,uint generation)
            {
                Drag=drag;Generation=generation;SourceSlot=drag.SlotIndex;_origin=drag.transform.position;Position=_origin;
                UnitData data=drag.GetComponent<UnitData>();_species=data==null?0:data.specId;_grade=GradeCode(data?.grade);
                _art=drag.GetComponent<BattleUnitArtView>();
                _ghost=_art==null?null:_art.CreateRenderGhost();
                if(_ghost!=null)_art.SetPreviewDim(.38f);
                else
                {
                    MeshFilter source=drag.GetComponent<MeshFilter>();_legacy=drag.GetComponent<Renderer>();
                    if(source!=null&&_legacy!=null)
                    {
                        _ghost=new GameObject("Unit render-only drag preview",typeof(MeshFilter),typeof(MeshRenderer));
                        _ghost.GetComponent<MeshFilter>().sharedMesh=source.sharedMesh;
                        Renderer renderer=_ghost.GetComponent<Renderer>();renderer.sharedMaterials=_legacy.sharedMaterials;
                        _legacy.GetPropertyBlock(_original);renderer.SetPropertyBlock(_original);
                        var dim=new MaterialPropertyBlock();_legacy.GetPropertyBlock(dim);
                        dim.SetColor("_Color",Color.gray*.45f);dim.SetColor("_BaseColor",Color.gray*.45f);_legacy.SetPropertyBlock(dim);
                        _ghost.transform.SetPositionAndRotation(_origin,drag.transform.rotation);_ghost.transform.localScale=drag.transform.lossyScale;
                    }
                }
            }
            public void Move(Vector3 position){if(_ghost!=null)_ghost.transform.position+=position-Position;Position=position;}
            public void Dispose(){if(_art!=null)_art.SetPreviewDim(1);if(_legacy!=null&&!IsStale)_legacy.SetPropertyBlock(_original);if(_ghost!=null)_ghost.GetComponent<BattleRenderGhostLease>()?.ReleaseResources();BattleSpriteGeometry.Release(_ghost);}
            private static byte GradeCode(string grade)=>grade switch{"EPIC"=>1,"UNIQUE"=>2,"LEGEND"=>3,"MYTHIC"=>4,_=>0};
        }
    }
}
