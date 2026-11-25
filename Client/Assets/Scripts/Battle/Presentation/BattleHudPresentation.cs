using System;
using System.Collections.Generic;
using MyDefense.Shared.Contracts;
using MyDefense.Battle.Balance.Canonical;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Actual Battle presentation only. Commands remain owned by FusionBattleUiController.</summary>
    [DefaultExecutionOrder(1100)]
    public sealed class BattleHudPresentation : MonoBehaviour
    {
        public sealed class DisplayState
        {
            public string Map = "", Status = "연결 대기", LocalIdentity = "참가자 확인 중", OtherIdentity = "참가자 확인 중";
            public string LocalCount = "—", OtherCount = "—", MutationDetail = "신화 유닛이 없습니다", MutationAction = "생체변이";
            public string NormalResonance = "일반 공명", MythicResonance = "신화 공명", Reroll = "후보 리롤";
            public string ChoiceHint = "후보 3종 중 하나를 선택하세요";
            public string SelectionDetail="";
            public readonly string[] Choices = { "", "", "" };
            public Color LocalCountColor = BattleHudGraphic.Cyan, OtherCountColor = BattleHudGraphic.Cyan;
            public bool OtherInactive;
            public int Wave, Gold, KidnapCost;
            public bool CanKidnap, CanMutate, CanSelectMythic, CanNormalResonance, CanMythicResonance, CanReroll, ChoiceActive, CanChoose;
        }

        /// <summary>Current replicated counts interpreted using the existing canonical display thresholds.</summary>
        public readonly struct PlayerDisplay
        {
            public readonly string Identity;
            public readonly int Count;
            public readonly PlayerBattleState State;
            public readonly bool Warning, Danger;
            public PlayerDisplay(string identity,int count,PlayerBattleState state,bool warning,bool danger)
            { Identity=identity; Count=count; State=state; Warning=warning; Danger=danger; }
            public static PlayerDisplay FromReplicatedCount(string identity,int count,PlayerBattleState state,int warningThreshold,int dangerThreshold)
                => new(identity,count,state,count>=warningThreshold,count>=dangerThreshold);
            public Color CountColor => State != PlayerBattleState.ACTIVE ? new Color(1f,.30f,.35f)
                : Danger ? new Color(1f,.54f,.24f) : Warning ? new Color(1f,.84f,.31f) : BattleHudGraphic.Cyan;
            public string CountText(int limit)
            {
                string status=State==PlayerBattleState.SPECTATING?"관전":State==PlayerBattleState.ELIMINATED?"탈락":Danger?"위험":Warning?"경고":"MONSTER";
                return status+" · "+Count+" / "+limit;
            }
        }

        public static void AssignPlayerDisplays(DisplayState display,int localSlot,bool solo,int limit,PlayerDisplay player1,PlayerDisplay player2)
        {
            PlayerDisplay local=localSlot==2?player2:player1, other=localSlot==2?player1:player2;
            display.LocalIdentity=string.IsNullOrWhiteSpace(local.Identity)?"참가자 확인 중":local.Identity;
            display.LocalCount=local.CountText(limit); display.LocalCountColor=local.CountColor;
            display.OtherInactive=solo;
            display.OtherIdentity=solo?"솔로 콘텐츠 · 비활성":string.IsNullOrWhiteSpace(other.Identity)?"참가자 확인 중":other.Identity;
            display.OtherCount=solo?"—":other.CountText(limit);
            display.OtherCountColor=solo?new Color(.42f,.52f,.59f):other.CountColor;
        }

        private static readonly List<BattleHudPresentation> Active = new();
        private static readonly List<RaycastResult> UiHits = new();
        private static PointerEventData _uiPointer;
        private static EventSystem _uiEvents;
        private readonly List<(Graphic graphic, bool enabled)> _legacyGraphics = new();
        private readonly List<(Selectable selectable, bool enabled)> _legacySelectables = new();
        private readonly Dictionary<Renderer, bool> _debugLabels = new();
        private FusionBattleUiController _controller;
        private RectTransform _root, _header, _footer, _worldFrame, _drawer, _modal, _shade;
        private RectTransform _topMatte, _bottomMatte, _leftMatte, _rightMatte;
        private RectTransform _localCard, _otherCard;
        private Text _map, _wave, _status, _localIdentity, _otherIdentity, _localCount, _otherCount, _gold, _mutationDetail;
        private Text _selectionDetail;
        private BattlePresentationQuality _quality;
        private Button _kidnap, _mutation, _normal, _mythic, _reroll, _previous, _next;
        private readonly Button[] _choices = new Button[3];
        private readonly List<Button> _buttons = new();
        private Button _mutationTab, _resonanceTab;
        private Camera _camera;
        private Rect _originalCameraRect;
        private float _unreservedCameraSize, _appliedCameraSize;
        private bool _cameraCaptured, _built, _visible, _drawerOpen, _mutationTabSelected = true, _modalOpen;
        private GameObject _ownedEventSystem;
        private BattleHudLayout _layout;
        private DisplayState _state = new();
        private Font _font;
        private Font _numberFont;
        private BattleHudStyleCatalog _style;
        private Text _kidnapPrice;
        private bool _ownsFont;
        private static int _developmentDrawer = -1;
        private static Rect _developmentInputRect;
        private BattleSpectatorCameraController _spectator;
        private int _layoutWidth, _layoutHeight;
        private Rect _layoutSafeArea;
        private bool _layoutReady;
        private float _nextSpectatorResolve;
        public bool IsVisible => _visible;
        public bool IsModalOpen => _modalOpen;
        public BattleHudLayout Layout => _layout;
        public static bool HasActiveHud => Active.Exists(hud => hud != null && hud._visible);
        public static bool HasActiveModal => Active.Exists(hud => hud != null && hud._visible && hud._modalOpen);

        public void Initialize(FusionBattleUiController controller)
        {
            _controller = controller;
            if (!_built) Build();
            Show();
        }

        private void OnEnable() { if (_built) Show(); }
        private void OnDisable() { Hide(); }
        private void OnDestroy()
        {
            Hide();
            Release(_root == null ? null : _root.gameObject);
            Release(_ownedEventSystem);
            if (_ownsFont) Release(_font);
        }

        public void Show()
        {
            if (!_built || _visible || !isActiveAndEnabled) return;
            _visible = true;
            if (!Active.Contains(this)) Active.Add(this);
            HideLegacyGraphics();
            foreach(UnitData unit in FindObjectsByType<UnitData>(FindObjectsSortMode.None))
                if(unit.gameObject.scene==gameObject.scene)
                    foreach(string marker in new[]{"GradeLabel","MutationLabel"})
                        SuppressWorldMarker(unit.transform.Find(marker)?.GetComponent<Renderer>());
            _root.gameObject.SetActive(true);
            if(_ownedEventSystem!=null)_ownedEventSystem.SetActive(true);
            ApplyLayoutForScreen(Screen.width, Screen.height, Screen.safeArea, SceneCamera());
        }

        public void Hide()
        {
            _quality?.EndScope();
            _visible = false; Active.Remove(this);
            if (_root != null) _root.gameObject.SetActive(false);
            if(_ownedEventSystem!=null)_ownedEventSystem.SetActive(false);
            foreach (var entry in _legacyGraphics) if (entry.graphic != null) entry.graphic.enabled = entry.enabled;
            _legacyGraphics.Clear();
            foreach (var entry in _legacySelectables) if (entry.selectable != null) entry.selectable.enabled = entry.enabled;
            _legacySelectables.Clear();
            foreach (var entry in _debugLabels) if (entry.Key != null) entry.Key.enabled = entry.Value;
            _debugLabels.Clear();
            if (_cameraCaptured && _camera != null)
            {
                _camera.rect = _originalCameraRect;
                _camera.orthographicSize = _unreservedCameraSize;
            }
            _cameraCaptured = false;
            if (Active.Count == 0) { _developmentDrawer = -1; _developmentInputRect = default; }
        }

        private void LateUpdate()
        {
            if (!_visible) return;
            ApplyLayoutForScreen(Screen.width, Screen.height, Screen.safeArea, SceneCamera());
        }

        private void HideLegacyGraphics()
        {
            Scene scene = gameObject.scene;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "UI_Coop_HUD" && child.name != "P1MonsterCountText" && child.name != "P2MonsterCountText") continue;
                foreach (Graphic graphic in child.GetComponentsInChildren<Graphic>(true))
                {
                    if (_legacyGraphics.Exists(saved => saved.graphic == graphic)) continue;
                    _legacyGraphics.Add((graphic, graphic.enabled)); graphic.enabled = false;
                }
                foreach (Selectable selectable in child.GetComponentsInChildren<Selectable>(true))
                {
                    if (_legacySelectables.Exists(saved => saved.selectable == selectable)) continue;
                    _legacySelectables.Add((selectable,selectable.enabled)); selectable.enabled=false;
                }
            }
        }

        private Camera SceneCamera()
        {
            Camera main=Camera.main;
            if(main!=null && main.gameObject.scene==gameObject.scene)return main;
            foreach(GameObject root in gameObject.scene.GetRootGameObjects())
                foreach(Camera candidate in root.GetComponentsInChildren<Camera>(true))
                    if(candidate.CompareTag("MainCamera"))return candidate;
            return null;
        }

        private void Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (string family in Font.GetOSInstalledFontNames())
            {
                if (!string.Equals(family,"Malgun Gothic",StringComparison.OrdinalIgnoreCase) && family != "맑은 고딕") continue;
                _font = Font.CreateDynamicFontFromOSFont(family,24); _ownsFont = _font != null;
                break;
            }
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _style=Resources.Load<BattleHudStyleCatalog>(BattleHudStyleCatalog.ResourcePath);
            if(_style!=null&&_style.Available){if(_ownsFont)Release(_font);_ownsFont=false;_font=_style.Korean;_numberFont=_style.Numbers;}
            else _numberFont=_font;
            GameObject canvasObject = new("BattleHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasObject, gameObject.scene);
            _root = (RectTransform)canvasObject.transform;
            Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 60;
            canvas.pixelPerfect = true;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            if (EventSystem.current == null)
            {
                _ownedEventSystem = new GameObject("BattleHudEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(_ownedEventSystem, gameObject.scene);
            }
            _topMatte=Matte("HeaderBackdrop"); _bottomMatte=Matte("FooterBackdrop");
            _leftMatte=Matte("LeftSafeBackdrop"); _rightMatte=Matte("RightSafeBackdrop");
            _header = Panel(_root, "MissionHeader");
            _map = Label(_header, "Planet", 17, BattleHudGraphic.Cyan);
            _wave = Label(_header, "Wave", 29, Color.white, TextAnchor.MiddleRight);
            _wave.font=_numberFont;
            _status = Label(_header, "BattleStatus", 14, new Color(.65f,.76f,.81f), TextAnchor.MiddleCenter);
            _status.verticalOverflow=VerticalWrapMode.Overflow;_status.horizontalOverflow=HorizontalWrapMode.Overflow;
            _localCard = Panel(_header, "LocalPlayer"); _otherCard = Panel(_header, "OtherPlayer");
            _localIdentity = Label(_localCard, "Identity", 18, Color.white,TextAnchor.MiddleCenter);
            _otherIdentity = Label(_otherCard, "Identity", 18, new Color(.79f,.85f,.88f),TextAnchor.MiddleCenter);
            _localCount = Label(_localCard, "Count", 19, BattleHudGraphic.Cyan, TextAnchor.MiddleCenter);
            _otherCount = Label(_otherCard, "Count", 19, BattleHudGraphic.Cyan, TextAnchor.MiddleCenter);
            _worldFrame = Panel(_root, "TacticalFrame", false); _worldFrame.GetComponent<BattleHudGraphic>().OutlineOnly = true;
            _selectionDetail=Label(_worldFrame,"SelectedUnitDetail",15,Color.white,TextAnchor.MiddleCenter);
            _quality=GetComponent<BattlePresentationQuality>();if(_quality==null)_quality=gameObject.AddComponent<BattlePresentationQuality>();
            _footer = Panel(_root, "BattleControls");
            _gold = Label(_footer, "BattleGold", 25, new Color(.96f,.86f,.60f));
            _kidnap = MakeButton(_footer, "Kidnap", () => _controller?.OnClickKidnap(), true);
            _mutationTab = MakeButton(_footer, "MutationTab", () => ToggleDrawer(true));
            _resonanceTab = MakeButton(_footer, "ResonanceTab", () => ToggleDrawer(false));
            Caption(_mutationTab, "생체변이"); Caption(_resonanceTab, "공명");
            _drawer = Panel(_root, "UnitUpgradeDrawer");
            _mutationDetail = Label(_drawer, "MutationDetail", 17, Color.white);
            _previous = MakeButton(_drawer, "PreviousMythic", () => _controller?.SelectMythic(-1)); Caption(_previous, "‹");
            _next = MakeButton(_drawer, "NextMythic", () => _controller?.SelectMythic(1)); Caption(_next, "›");
            _mutation = MakeButton(_drawer, "MutationAction", () => _controller?.OnClickMutation());
            _normal = MakeButton(_drawer, "NormalResonance", () => _controller?.OnClickResonance(CanonicalResonanceTrack.NORMAL));
            _mythic = MakeButton(_drawer, "MythicResonance", () => _controller?.OnClickResonance(CanonicalResonanceTrack.MYTHIC));
            _shade = Panel(_root, "ChoiceInputBlocker");
            _shade.GetComponent<BattleHudGraphic>().color = new Color(.2f,.3f,.4f,.84f);
            _modal = Panel(_root, "MythicChoiceModal");
            Label(_modal, "Title", 25, Color.white).text = "신화 선택";
            Label(_modal, "Hint", 15, new Color(.70f,.81f,.85f)).text = "후보 3종 중 하나를 선택하세요";
            for (int i = 0; i < 3; i++) { int index = i; _choices[i] = MakeButton(_modal, "Choice" + i, () => _controller?.OnClickMythicChoice(index)); }
            _reroll = MakeButton(_modal, "Reroll", () => _controller?.OnClickMythicReroll());
            _built = true;
            Render(_state);
        }

        public void Render(DisplayState state)
        {
            _state = state ?? new DisplayState(); if (!_built) return;
            _map.text = string.IsNullOrWhiteSpace(_state.Map) ? "BATTLE · 행성 확인 중" : "BATTLE / " + _state.Map;
            _selectionDetail.text=_state.SelectionDetail;
            _wave.text = "WAVE " + _state.Wave.ToString("000"); _status.text = _state.Status;
            _localIdentity.text = _state.LocalIdentity; _otherIdentity.text = _state.OtherIdentity;
            _localCount.text = _state.LocalCount; _otherCount.text = _state.OtherCount;
            _localCount.color=_state.LocalCountColor; _otherCount.color=_state.OtherCountColor;
            _otherIdentity.color=_state.OtherInactive?new Color(.42f,.52f,.59f):new Color(.79f,.85f,.88f);
            _gold.text = "전투 Gold\n" + _state.Gold.ToString("N0");
            Caption(_kidnap, "왹져 납치");if(_kidnapPrice!=null)_kidnapPrice.text=_state.KidnapCost.ToString("N0")+" G"; _kidnap.interactable = _state.CanKidnap;
            _mutationDetail.text = _state.MutationDetail;
            Caption(_mutation, _state.MutationAction); _mutation.interactable = _state.CanMutate;
            _previous.interactable = _next.interactable = _state.CanSelectMythic;
            Caption(_normal, _state.NormalResonance); _normal.interactable = _state.CanNormalResonance;
            Caption(_mythic, _state.MythicResonance); _mythic.interactable = _state.CanMythicResonance;
            _modalOpen = _state.ChoiceActive;
            _modal.Find("Hint").GetComponent<Text>().text=_state.ChoiceHint;
            _modal.gameObject.SetActive(_modalOpen); _shade.gameObject.SetActive(_modalOpen);
            for (int i = 0; i < 3; i++) { Caption(_choices[i], _state.Choices[i]); _choices[i].interactable = _state.CanChoose && !string.IsNullOrWhiteSpace(_state.Choices[i]); }
            Caption(_reroll, _state.Reroll); _reroll.interactable = _state.CanReroll;
            RefreshDrawer();
            foreach(Button button in _buttons)
                button.GetComponentInChildren<Text>(true).color = !button.interactable ? new Color(.43f,.55f,.62f)
                    : button==_kidnap ? new Color(.025f,.09f,.13f) : Color.white;
            if(_kidnapPrice!=null)_kidnapPrice.color=_kidnap.interactable?new Color(.04f,.14f,.17f):new Color(.32f,.43f,.47f);
        }

        private void ToggleDrawer(bool mutation)
        {
            _developmentDrawer=-1; _developmentInputRect=default;
            _drawerOpen = !_drawerOpen || _mutationTabSelected != mutation; _mutationTabSelected = mutation; RefreshDrawer();
        }

        private void RefreshDrawer()
        {
            _drawer.gameObject.SetActive(_drawerOpen && !_modalOpen);
            Caption(_mutationTab,(_drawerOpen && _mutationTabSelected ? "− " : "+ ")+"생체변이");
            Caption(_resonanceTab,(_drawerOpen && !_mutationTabSelected ? "− " : "+ ")+"공명");
            _mutationDetail.gameObject.SetActive(_mutationTabSelected); _previous.gameObject.SetActive(_mutationTabSelected); _next.gameObject.SetActive(_mutationTabSelected);
            _mutation.gameObject.SetActive(_mutationTabSelected); _normal.gameObject.SetActive(!_mutationTabSelected); _mythic.gameObject.SetActive(!_mutationTabSelected);
        }

        public void ApplyLayoutForScreen(int width, int height, Rect safeArea, Camera camera)
        {
            if (!_built || !_visible) return;
            if(_layoutReady && _layoutWidth==width && _layoutHeight==height && _layoutSafeArea==safeArea)
            { ApplyCameraViewport(width,height,camera); return; }
            _layoutWidth=width; _layoutHeight=height; _layoutSafeArea=safeArea; _layoutReady=true;
            _layout = BattleHudLayout.Calculate(width, height, safeArea);
            Place(_topMatte,new Rect(0,_layout.World.yMax,width,height-_layout.World.yMax));
            Place(_bottomMatte,new Rect(0,0,width,_layout.World.yMin));
            Place(_leftMatte,new Rect(0,_layout.World.yMin,_layout.World.xMin,_layout.World.height));
            Place(_rightMatte,new Rect(_layout.World.xMax,_layout.World.yMin,width-_layout.World.xMax,_layout.World.height));
            Place(_header, _layout.Header); Place(_footer, _layout.Footer); Place(_worldFrame, _layout.World);
            Place(_drawer, _layout.Drawer); Place(_modal, _layout.Modal); Place(_shade, new Rect(0,0,width,height));
            Local(_selectionDetail.rectTransform,10,4,_layout.World.width-20,24);
            float s = _layout.Scale, w = _layout.Header.width;
            Local(_map.rectTransform, 17*s, _layout.Header.height - 32*s, w*.62f, 24*s);
            Local(_wave.rectTransform, w*.64f, _layout.Header.height - 32*s, w*.33f-12*s, 26*s);
            float cardHeight = (_layout.Landscape ? 48 : 72)*s;
            Local(_localCard, 12*s, 10*s, (w-32*s)/2, cardHeight);
            Local(_otherCard, w/2+4*s, 10*s, (w-32*s)/2, cardHeight);
            foreach (RectTransform card in new[] { _localCard, _otherCard })
            {
                Local(card.Find("Identity") as RectTransform, 10*s, cardHeight*.57f, card.rect.width-20*s, cardHeight*.33f);
                Local(card.Find("Count") as RectTransform, 10*s, cardHeight*.08f, card.rect.width-20*s, cardHeight*.36f);
            }
            _status.gameObject.SetActive(true);
            Local(_status.rectTransform, 14*s, cardHeight+16*s, w-28*s, 24*s);
            float fw = _layout.Footer.width, fh = _layout.Footer.height;
            Local(_gold.rectTransform, 18*s, 12*s, fw*.22f, fh-24*s);
            float ctaHeight=Mathf.Min(fh-28*s,fw*.43f/3f),ctaWidth=ctaHeight*3;
            Local(_kidnap.transform as RectTransform, fw*.475f-ctaWidth*.5f,(fh-ctaHeight)*.5f,ctaWidth,ctaHeight);
            Local(_mutationTab.transform as RectTransform, fw*.72f, fh*.52f, fw*.25f-8*s, fh*.36f);
            Local(_resonanceTab.transform as RectTransform, fw*.72f, fh*.12f, fw*.25f-8*s, fh*.36f);
            float dw = _layout.Drawer.width, dh = _layout.Drawer.height;
            Local(_mutationDetail.rectTransform, 58*s, dh*.43f, dw-116*s, dh*.51f);
            Local(_previous.transform as RectTransform, 10*s, dh*.55f, 40*s, 42*s);
            Local(_next.transform as RectTransform, dw-50*s, dh*.55f, 40*s, 42*s);
            Local(_mutation.transform as RectTransform, 14*s, 16*s, dw-28*s, 54*s);
            Local(_normal.transform as RectTransform, 14*s, dh*.53f, dw-28*s, dh*.35f);
            Local(_mythic.transform as RectTransform, 14*s, dh*.12f, dw-28*s, dh*.35f);
            float mw = _layout.Modal.width, mh = _layout.Modal.height, gap = 14*s, cw = (mw-gap*4)/3;
            Local(_modal.Find("Title") as RectTransform, 20*s, mh-62*s, mw-40*s, 42*s);
            Local(_modal.Find("Hint") as RectTransform, 20*s, mh-94*s, mw-40*s, 26*s);
            for (int i=0;i<3;i++) Local(_choices[i].transform as RectTransform, gap+i*(cw+gap), 100*s, cw, Mathf.Max(40*s,mh-210*s));
            Local(_reroll.transform as RectTransform, gap, 22*s, mw-gap*2, 55*s);
            foreach (Text text in _root.GetComponentsInChildren<Text>(true))
            { text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.Max(11, Mathf.RoundToInt(12*s)); text.resizeTextMaxSize = Mathf.RoundToInt((text.name == "BattleGold" ? 25 : text.name == "Wave" ? 29 : 20)*s); }
            _status.resizeTextForBestFit=false;_status.fontSize=Mathf.RoundToInt(14*s);
            ApplyCameraViewport(width, height, camera);
            _wave.text="WAVE "+_state.Wave.ToString("000");
        }

        private void ApplyCameraViewport(int width, int height, Camera camera)
        {
            if (camera == null || !camera.orthographic) return;
            if(Application.isPlaying)_quality?.BeginScope(camera);
            if (_cameraCaptured && _camera != camera)
            {
                if (_camera != null) { _camera.rect = _originalCameraRect; _camera.orthographicSize = _unreservedCameraSize; }
                _cameraCaptured = false;
            }
            if (!_cameraCaptured)
            { _camera = camera; _originalCameraRect = camera.rect; _unreservedCameraSize = camera.orthographicSize; _appliedCameraSize = _unreservedCameraSize; _cameraCaptured = true; }
            else if (!Mathf.Approximately(camera.orthographicSize, _appliedCameraSize))
                _unreservedCameraSize = camera.orthographicSize; // A legitimate external/art framer changed size. Never take its transform.
            Rect viewport = _layout.NormalizedWorld(width, height);
            if(camera.rect!=viewport)camera.rect = viewport;
            if (_spectator == null && Time.unscaledTime>=_nextSpectatorResolve)
            { _spectator = FindFirstObjectByType<BattleSpectatorCameraController>(); _nextSpectatorResolve=Time.unscaledTime+.5f; }
            if (_spectator != null && _spectator.IsSpectating) return;
            float originalAspect = width*_originalCameraRect.width/Mathf.Max(1,height*_originalCameraRect.height);
            float nextAspect = width*viewport.width/Mathf.Max(1,height*viewport.height);
            _appliedCameraSize = BattleHudLayout.PreserveVisibleBounds(_unreservedCameraSize, originalAspect, nextAspect);
            if(!Mathf.Approximately(camera.orthographicSize,_appliedCameraSize))camera.orthographicSize = _appliedCameraSize;
        }

        public static bool BlocksWorldInput(Vector2 screenPosition)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Vector2 guiPosition = new(screenPosition.x,Screen.height-screenPosition.y);
            if(HasActiveHud && (DevelopmentToggleRect(0).Contains(guiPosition) || DevelopmentToggleRect(1).Contains(guiPosition))) return true;
            if (_developmentDrawer >= 0 && _developmentInputRect.Contains(guiPosition)) return true;
#endif
            foreach (BattleHudPresentation hud in Active)
                if (hud != null && hud._visible && hud._layout.BlocksWorld(screenPosition, hud._drawerOpen, hud._modalOpen)) return true;
            EventSystem events = EventSystem.current;
            if (events == null) return false;
            if(_uiEvents!=events||_uiPointer==null){_uiEvents=events;_uiPointer=new PointerEventData(events);}
            _uiPointer.Reset();_uiPointer.position=screenPosition;UiHits.Clear();
            foreach(BaseRaycaster raycaster in RaycasterManager.GetRaycasters())
                if(raycaster is GraphicRaycaster graphic && graphic.isActiveAndEnabled)
                { graphic.Raycast(_uiPointer,UiHits);if(UiHits.Count>0)return true; }
            return false;
        }

        public static bool AllowsCommands(bool runnerRunning, MatchState match, PlayerBattleState player)
            => runnerRunning && match == MatchState.RUNNING && player == PlayerBattleState.ACTIVE;

        public static void SuppressWorldMarker(Renderer renderer)
        {
            if(renderer==null)return;
            foreach(BattleHudPresentation hud in Active)
            {
                if(hud==null || !hud._visible || renderer.gameObject.scene!=hud.gameObject.scene)continue;
                if(!hud._debugLabels.ContainsKey(renderer))hud._debugLabels.Add(renderer,renderer.enabled);
                renderer.enabled=false;
                return;
            }
        }

        public static Rect DevelopmentToggleRect(int index)
        {
            foreach (BattleHudPresentation hud in Active)
                if (hud != null && hud._visible) return new Rect(hud._layout.World.x+12+index*154, Screen.height-hud._layout.World.yMax+8, 148, 28);
            return new Rect(12+index*154, 12, 148, 28);
        }
        public static Rect DevelopmentDrawerRect(float preferredWidth, float preferredHeight)
        {
            Rect toggle = DevelopmentToggleRect(0);
            float height = Mathf.Min(preferredHeight,Screen.height-toggle.yMax-24);
            foreach(BattleHudPresentation hud in Active) if(hud!=null && hud._visible) height=Mathf.Min(height,hud._layout.World.height-50);
            return new Rect(toggle.x,toggle.yMax+8,Mathf.Min(preferredWidth,Screen.width-toggle.x-12),Mathf.Max(80,height));
        }
        public static bool ToggleDevelopmentDrawer(int index)
        {
            _developmentDrawer = _developmentDrawer == index ? -1 : index;
            _developmentInputRect=default;
            if(_developmentDrawer>=0)
                foreach(BattleHudPresentation hud in Active)
                    if(hud!=null && hud._visible) { hud._drawerOpen=false; hud.RefreshDrawer(); }
            return _developmentDrawer==index;
        }
        public static bool IsDevelopmentDrawerOpen(int index) => _developmentDrawer == index;
        public static void RegisterDevelopmentInput(Rect rect) { _developmentInputRect=rect; }

        private RectTransform Panel(Transform parent, string name, bool blocks = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(BattleHudGraphic)); go.transform.SetParent(parent,false);
            var frame = go.GetComponent<BattleHudGraphic>(); frame.raycastTarget = blocks; frame.color = Color.white;
            frame.Flat=name!="TacticalFrame";
            return (RectTransform)go.transform;
        }
        private RectTransform Matte(string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(_root,false);
            Image image=go.GetComponent<Image>();image.color=new Color(.009f,.022f,.04f,1);image.raycastTarget=false;
            return (RectTransform)go.transform;
        }
        private Text Label(Transform parent, string name, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(parent,false);
            Text text = go.GetComponent<Text>(); text.font = _font; text.fontSize = size; text.fontStyle = FontStyle.Normal;
            text.color = color; text.alignment = anchor; text.raycastTarget = false; text.supportRichText = false;
            return text;
        }
        private Button MakeButton(Transform parent, string name, Action click, bool primary = false)
        {
            RectTransform rect = Panel(parent,name); var graphic = rect.GetComponent<BattleHudGraphic>(); graphic.Accent = primary;
            if(primary)graphic.Flat=false;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = graphic; button.onClick.AddListener(() => click());
            _buttons.Add(button);
            ColorBlock colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1,1,1,1); colors.pressedColor = new Color(.7f,.9f,.95f); colors.disabledColor = new Color(.40f,.48f,.52f,.8f); button.colors = colors;
            Text text = Label(rect,"Label",primary?25:18,primary?new Color(.025f,.09f,.13f):Color.white,TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = new Vector2(8,4); text.rectTransform.offsetMax = new Vector2(-8,-4);
            if(primary)
            {
                if(_style!=null&&_style.Kidnap!=null)
                {
                    // One Graphic per GameObject: the skin must be a child of the existing code-native frame.
                    graphic.enabled=false;
                    var skin=new GameObject("Skin",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));skin.transform.SetParent(rect,false);skin.transform.SetAsFirstSibling();
                    var skinRect=(RectTransform)skin.transform;skinRect.anchorMin=Vector2.zero;skinRect.anchorMax=Vector2.one;skinRect.offsetMin=skinRect.offsetMax=Vector2.zero;
                    var image=skin.GetComponent<Image>();image.sprite=_style.Kidnap;image.type=Image.Type.Simple;image.preserveAspect=true;button.targetGraphic=image;
                }
                text.rectTransform.anchorMin=new Vector2(.28f,.47f);text.rectTransform.anchorMax=new Vector2(.92f,.83f);text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
                _kidnapPrice=Label(rect,"Price",19,new Color(.04f,.14f,.17f),TextAnchor.MiddleCenter);_kidnapPrice.font=_numberFont;
                _kidnapPrice.rectTransform.anchorMin=new Vector2(.28f,.16f);_kidnapPrice.rectTransform.anchorMax=new Vector2(.92f,.47f);_kidnapPrice.rectTransform.offsetMin=_kidnapPrice.rectTransform.offsetMax=Vector2.zero;
            }
            return button;
        }
        private static void Caption(Button button, string caption) { button.GetComponentInChildren<Text>(true).text = caption; }
        private static void Place(RectTransform rect, Rect pixelRect) { rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero; rect.anchoredPosition=pixelRect.position; rect.sizeDelta=pixelRect.size; }
        private static void Local(RectTransform rect, float x, float y, float width, float height) { if(rect!=null) Place(rect,new Rect(x,y,width,height)); }
        private static void Release(UnityEngine.Object owned) { if(owned==null)return; if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned); }
    }
}
