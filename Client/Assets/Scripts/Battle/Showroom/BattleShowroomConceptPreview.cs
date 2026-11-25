#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

namespace MyDefense.Battle.Showroom
{
    public sealed partial class BattleShowroomConceptPreview : MonoBehaviour
    {
        private static readonly Color Magenta = new(0.91f, 0.45f, 0.67f);
        private static readonly Color Violet = new(0.66f, 0.58f, 0.91f);
        private static readonly Color Background = new(0.055f, 0.068f, 0.083f);
        private const float CameraPitch = 60f;
        private const float CameraYaw = 0f;
        private const float CharacterPitch = 25f;
        private const float HeroScale = 1.6f;
        private const float DollScale = 1.3f;
        private static readonly Vector2 Actor = new(158f, 298f);
        private static readonly Vector2 Boss = new(432f, 269f);
        private static readonly Vector2[] Targets = { new(365f, 178f), Boss, new(382f, 368f) };
        private static readonly Vector2[] DollHome = { new(222f, 233f), new(254f, 307f), new(221f, 362f) };
        private const float EffectUnits = 0.00335f;
        private const int MuzzleFlash = 0;
        private const int CannonShell = 1;
        private const int Missile = 2;
        private const int Exhaust = 3;
        private const int ShellImpact = 4;
        private const int Scorch = 5;
        private const int SummonGlyph = 0;
        private const int PuppetSlash = 1;
        private const int PuppetImpact = 2;
        private const int HandMagic = 3;
        private const int PuppetExplosion = 4;
        private const int PuppetTrail = 5;
        private const int DiadCharge = 0;
        private const int DiadLaser = 1;
        private const int DiadSlash = 2;
        private const int DiadScythe = 3;
        private const int DiadImpact = 4;
        private const int DiadTrail = 5;

        [SerializeField] private Camera _camera;
        [SerializeField] private MeshRenderer _tile;
        [SerializeField] private MeshRenderer[] _boardTiles;
        [SerializeField] private MeshRenderer _boardFrame;
        [SerializeField] private Texture2D _paintedBoardTexture;
        [SerializeField] private Texture2D _darkBoldBoardTexture;
        [SerializeField] private Texture2D[] _darkBoldUnitTextures;
        [SerializeField] private string _darkBoldValidationError;
        [SerializeField] private Shader _reliefShader;
        [SerializeField] private GameObject _stage;
        [SerializeField] private BattleShowroomTileAtlas _diad;
        [SerializeField] private BattleShowroomTileAtlas _mecharon;
        [SerializeField] private BattleShowroomTileAtlas _puppeteer;
        [SerializeField] private BattleShowroomTileAtlas _doll;
        [SerializeField] private BattleShowroomTileAtlas _diadAttack;
        [SerializeField] private BattleShowroomTileAtlas _mecharonAttack;
        [SerializeField] private BattleShowroomTileAtlas _puppeteerAttack;
        [SerializeField] private BattleShowroomTileAtlas _dollAttack;
        [SerializeField] private BattleShowroomTileAtlas _diadWindup;
        [SerializeField] private BattleShowroomTileAtlas _mecharonWindup;
        [SerializeField] private BattleShowroomTileAtlas _puppeteerWindup;
        [SerializeField] private BattleShowroomTileAtlas _dollWindup;
        [SerializeField] private BattleShowroomTileAtlas _diadRecover;
        [SerializeField] private BattleShowroomTileAtlas _mecharonRecover;
        [SerializeField] private BattleShowroomTileAtlas _puppeteerRecover;
        [SerializeField] private BattleShowroomTileAtlas _dollRecover;
        [SerializeField] private BattleShowroomEffectAtlas _diadEffects;
        [SerializeField] private BattleShowroomEffectAtlas _mecharonEffects;
        [SerializeField] private BattleShowroomEffectAtlas _puppeteerEffects;

        private readonly BattleShowroomConceptPlayback _playback = new();
        private readonly Dictionary<(BattleShowroomTileAtlas, BattleShowroomFacing, BattleShowroomLook), Sprite> _frames = new();
        private readonly Dictionary<string, Texture2D> _darkBoldTextures = new();
        private readonly List<LineRenderer> _lines = new();
        private readonly Vector3[] _ribbonPoints = new Vector3[20];
        private readonly BattleShowroomRelief[] _dolls = new BattleShowroomRelief[6];
        private readonly SpriteRenderer[] _dollShadows = new SpriteRenderer[6];
        private readonly Dictionary<Sprite, Mesh> _meshes = new();
        private readonly Dictionary<(BattleShowroomEffectAtlas, int), Sprite> _effectFrames = new();
        private readonly List<SpriteRenderer> _effectRenderers = new();
        private Material _reliefMaterial;
        private BattleShowroomRelief _body;
        private Bounds _boardBounds;
        private readonly List<Mesh> _boardSurfaces = new();
        private Material _boardMaterial;
        private SpriteRenderer _shadow;
        private Sprite _softGlow;
        private Texture2D _glowTexture;
        private Sprite _idleSprite;
        private Sprite _attackSprite;
        private Sprite _windupSprite;
        private Sprite _recoverSprite;
        private Sprite _dollSprite;
        private Sprite _dollAttackSprite;
        private Sprite _dollWindupSprite;
        private Sprite _dollRecoverSprite;
        private Bounds _actorArtBounds;
        private int _effectCursor;
        private Vector3 _bodyOffset;
        private Color _canonicalBackground;
        private BattleShowroomFacing _facing;
        private BattleShowroomLook _look;
        private bool _loop;
        private bool _auto;
        private int _lineCursor;
        private int _dollCursor;
        private Vector3 _foot;

        public BattleShowroomConceptAction Action => _playback.Action;
        public float NextAutomaticIn => _playback.NextAutomaticIn;
        public bool IsAutomatic => _playback.IsAutomatic;
        public BattleShowroomConceptDefinition Definition => _playback.Definition;
        public int ActiveDollCount => _playback.ActiveDollCount;
        public BattleShowroomLook Look => _look;
        public string LookUnavailableReason => _artOwner != null ? _artOwner.LookUnavailableReason
            : string.IsNullOrEmpty(_darkBoldValidationError)
            ? "Dark + Bold artwork is unavailable. Refresh the Character scene after importing the comparison assets."
            : _darkBoldValidationError;
        public bool IsDarkBoldAvailable => _artOwner != null ? _artOwner.IsDarkBoldAvailable
            : string.IsNullOrEmpty(_darkBoldValidationError)
            && _darkBoldBoardTexture != null && _darkBoldUnitTextures != null && _darkBoldUnitTextures.Length > 0
            && System.Array.TrueForAll(_darkBoldUnitTextures, texture => texture != null);
        public bool IsArtAvailable => _camera != null && _stage != null && _tile != null && _reliefShader != null
            && ResolveAtlas(Definition.Art, BattleShowroomConceptPose.Idle)?.IsAvailable == true
            && (Definition.DollCount == 0 || ArtOwner._doll?.IsAvailable == true);
        public bool IsAttackPoseAvailable => ResolveAtlas(Definition.Art, BattleShowroomConceptPose.Attack)?.IsAvailable == true
            && ResolveAtlas(Definition.Art, BattleShowroomConceptPose.Windup)?.IsAvailable == true
            && ResolveAtlas(Definition.Art, BattleShowroomConceptPose.Recover)?.IsAvailable == true
            && (Definition.DollCount == 0 || ArtOwner._dollAttack?.IsAvailable == true
                && ArtOwner._dollWindup?.IsAvailable == true && ArtOwner._dollRecover?.IsAvailable == true);
        public bool IsBoardArtAvailable => _paintedBoardTexture != null;
        public bool AreEffectAssetsAvailable => Definition.Art switch
        {
            BattleShowroomConceptArt.Mecharon => _mecharonEffects?.IsAvailable == true,
            BattleShowroomConceptArt.Diad => _diadEffects?.IsAvailable == true,
            BattleShowroomConceptArt.Puppeteer => _puppeteerEffects?.IsAvailable == true,
            _ => true
        };

        private void Awake()
        {
            if (_camera != null)
                _canonicalBackground = _camera.backgroundColor;
        }

        public void Select(BattleShowroomConcept concept)
        {
            _playback.Select(concept);
            bool showTile = concept != BattleShowroomConcept.Canonical;
            if (_stage != null)
                _stage.SetActive(showTile);
            if (!_actorOnly && _camera != null)
                _camera.backgroundColor = showTile ? Background : _canonicalBackground;
            if (!showTile || _stage == null || _camera == null)
                return;
            EnsureStageObjects();
            ResetEffects();
            if (IsArtAvailable)
                RefreshSprites();
            else
                _body?.Hide();
            FrameTileCamera(Mathf.Max(0.1f, _camera.aspect), 1f, CameraPitch);
        }

        public void Play(BattleShowroomConceptAction action)
        {
            if (action == BattleShowroomConceptAction.Deploy && Concept != BattleShowroomConcept.PuppeteerPlan1)
                return;
            _playback.Play(action);
        }

        public bool SetLook(BattleShowroomLook look)
        {
            if (look != BattleShowroomLook.Original && look != BattleShowroomLook.DarkBold)
                return false;
            if (look == BattleShowroomLook.DarkBold && !IsDarkBoldAvailable)
                return false;
            if (_look == look)
                return true;
            _darkBoldTextures.Clear();
            if (_darkBoldUnitTextures != null)
            {
                foreach (Texture2D texture in _darkBoldUnitTextures)
                {
                    if (texture != null)
                        _darkBoldTextures[texture.name] = texture;
                }
            }
            _look = look;
            if (IsArtAvailable && _body != null)
                RefreshSprites();
            ApplyBoardLook();
            foreach (BattleShowroomConceptPreview actor in _placedActors.Values)
                actor.SetLook(look);
            return true;
        }

        public void SetFacing(BattleShowroomFacing facing)
        {
            if (_facing == facing)
                return;
            _facing = facing;
            if (IsArtAvailable && _body != null)
                RefreshSprites();
        }

        public void SetPlaybackOptions(bool loop, bool auto)
        {
            _loop = loop;
            _auto = auto;
        }

        public void FrameTileCamera(float aspect, float zoom, float elevation = CameraPitch)
        {
            if (_actorOnly || _camera == null)
                return;
            Quaternion rotation = Quaternion.Euler(Mathf.Clamp(elevation, 50f, 80f), CameraYaw, 0f);
            Matrix4x4 viewRotation = Matrix4x4.Rotate(Quaternion.Inverse(rotation));
            Bounds board = TransformBounds(_boardBounds, viewRotation);
            Bounds actor = ProjectedActorBounds(viewRotation);
            if (_boardPlacementMode)
            {
                foreach (BattleShowroomConceptPreview placed in _placedActors.Values)
                    board.Encapsulate(placed.ProjectedActorBounds(viewRotation));
                BattleShowroomConceptPreview selected = GetPlacedUnit(_selectedPlacementSlot);
                MeshRenderer selectedTile = GetPlacementTile(_selectedPlacementSlot);
                if (selected != null)
                    actor = selected.ProjectedActorBounds(viewRotation);
                else if (selectedTile != null)
                    actor = TransformBounds(new Bounds(selectedTile.bounds.center + Vector3.up * 0.5f,
                        new Vector3(1f, 1f, 1f)), viewRotation);
            }
            else
                board.Encapsulate(actor);
            float clampedZoom = Mathf.Clamp(zoom, 0.7f, 2.6f);
            float focus = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.05f, 2.1f, clampedZoom));
            Vector3 projectedCenter = Vector3.Lerp(board.center, actor.center, focus);
            Vector3 center = rotation * projectedCenter;
            _camera.transform.SetPositionAndRotation(center - rotation * Vector3.forward * 14f, rotation);
            _camera.orthographic = true;
            float boardFit = Mathf.Max(board.extents.y + 0.23f,
                (board.extents.x + 0.23f) / Mathf.Max(0.1f, aspect));
            float detailFit = Mathf.Max(actor.extents.y + 0.23f,
                (actor.extents.x + 0.23f) / Mathf.Max(0.1f, aspect));
            _camera.orthographicSize = Mathf.Max(detailFit, boardFit / clampedZoom);
        }

        public static Bounds TransformBounds(Bounds bounds, Matrix4x4 transform)
        {
            Vector3 center = transform.MultiplyPoint3x4(bounds.center);
            Vector3 right = transform.MultiplyVector(Vector3.right * bounds.extents.x);
            Vector3 up = transform.MultiplyVector(Vector3.up * bounds.extents.y);
            Vector3 forward = transform.MultiplyVector(Vector3.forward * bounds.extents.z);
            Vector3 extent = new(Mathf.Abs(right.x) + Mathf.Abs(up.x) + Mathf.Abs(forward.x),
                Mathf.Abs(right.y) + Mathf.Abs(up.y) + Mathf.Abs(forward.y),
                Mathf.Abs(right.z) + Mathf.Abs(up.z) + Mathf.Abs(forward.z));
            return new Bounds(center, extent * 2f);
        }

        private static Quaternion CharacterRotation => Quaternion.Euler(CharacterPitch, CameraYaw, 0f);

        private void Update()
        {
            if (_boardPlacementMode || _playback.Concept == BattleShowroomConcept.Canonical || !IsArtAvailable || _body == null)
                return;
            _playback.Advance(Time.unscaledDeltaTime, _loop, _auto);
            ResetEffects();
            Bounds tileBounds = _tile.bounds;
            _foot = new Vector3(tileBounds.center.x, tileBounds.max.y + 0.012f, tileBounds.center.z);

            float phase = _playback.Elapsed;
            bool attacking = Action == BattleShowroomConceptAction.Basic || Action == BattleShowroomConceptAction.Skill;
            BattleShowroomConceptPose pose = BattleShowroomConceptPlayback.PoseAtElapsed(_playback.Concept, Action, phase);
            _bodyOffset = BattleShowroomConceptPlayback.BodyMotionOffset(Action, phase, AttackHeading(_facing));
            float lift = Definition.Art == BattleShowroomConceptArt.Mecharon ? 0f : 0.025f;
            DrawRelief(_body, SelectPose(pose, _idleSprite, _windupSprite, _attackSprite, _recoverSprite),
                _foot + _bodyOffset + Vector3.up * lift);
            PlaceShadow(_shadow, _foot + _bodyOffset, 0.72f * HeroScale, lift);
            if (_playback.Concept == BattleShowroomConcept.PuppeteerPlan1)
                DrawPuppetFormation(phase);
            else if (_playback.Concept == BattleShowroomConcept.PuppeteerPlan2)
                DrawReturningPuppet(phase, attacking);
            else if (attacking && _playback.Concept == BattleShowroomConcept.Diad)
                DrawDiad(phase);
            else if (attacking)
                DrawMecharon(phase);
        }

        private BattleShowroomTileAtlas ResolveAtlas(BattleShowroomConceptArt art, BattleShowroomConceptPose pose)
            => _artOwner != null ? _artOwner.ResolveAtlas(art, pose) : (art, pose) switch
        {
            (BattleShowroomConceptArt.Diad, BattleShowroomConceptPose.Windup) => _diadWindup,
            (BattleShowroomConceptArt.Diad, BattleShowroomConceptPose.Attack) => _diadAttack,
            (BattleShowroomConceptArt.Diad, BattleShowroomConceptPose.Recover) => _diadRecover,
            (BattleShowroomConceptArt.Diad, _) => _diad,
            (BattleShowroomConceptArt.Mecharon, BattleShowroomConceptPose.Windup) => _mecharonWindup,
            (BattleShowroomConceptArt.Mecharon, BattleShowroomConceptPose.Attack) => _mecharonAttack,
            (BattleShowroomConceptArt.Mecharon, BattleShowroomConceptPose.Recover) => _mecharonRecover,
            (BattleShowroomConceptArt.Mecharon, _) => _mecharon,
            (BattleShowroomConceptArt.Puppeteer, BattleShowroomConceptPose.Windup) => _puppeteerWindup,
            (BattleShowroomConceptArt.Puppeteer, BattleShowroomConceptPose.Attack) => _puppeteerAttack,
            (BattleShowroomConceptArt.Puppeteer, BattleShowroomConceptPose.Recover) => _puppeteerRecover,
            (BattleShowroomConceptArt.Puppeteer, _) => _puppeteer,
            _ => null
        };

        private static Sprite SelectPose(BattleShowroomConceptPose pose, Sprite idle,
            Sprite windup, Sprite attack, Sprite recover) => pose switch
        {
            BattleShowroomConceptPose.Windup => windup != null ? windup : idle,
            BattleShowroomConceptPose.Attack => attack != null ? attack : idle,
            BattleShowroomConceptPose.Recover => recover != null ? recover : idle,
            _ => idle
        };

        private void RefreshSprites()
        {
            BattleShowroomConceptArt art = Definition.Art;
            float height = (art == BattleShowroomConceptArt.Mecharon ? 0.85f : 0.90f) * HeroScale;
            _idleSprite = GetSprite(ResolveAtlas(art, BattleShowroomConceptPose.Idle), height);
            _windupSprite = GetSprite(ResolveAtlas(art, BattleShowroomConceptPose.Windup), height);
            _attackSprite = GetSprite(ResolveAtlas(art, BattleShowroomConceptPose.Attack), height);
            _recoverSprite = GetSprite(ResolveAtlas(art, BattleShowroomConceptPose.Recover), height);
            _dollSprite = GetSprite(ArtOwner._doll, 0.35f * DollScale);
            _dollWindupSprite = GetSprite(ArtOwner._dollWindup, 0.35f * DollScale);
            _dollAttackSprite = GetSprite(ArtOwner._dollAttack, 0.35f * DollScale);
            _dollRecoverSprite = GetSprite(ArtOwner._dollRecover, 0.35f * DollScale);
            _actorArtBounds = _idleSprite != null ? _idleSprite.bounds : new Bounds(Vector3.up * 0.7f, Vector3.one);
            foreach (Sprite pose in new[] { _windupSprite, _attackSprite, _recoverSprite })
            {
                if (pose != null)
                    _actorArtBounds.Encapsulate(TransformBounds(pose.bounds,
                        Matrix4x4.Scale(new Vector3(RegisteredPoseWidthScale(pose), 1f, 1f))));
            }
            _actorArtBounds.Expand(new Vector3(0f, 0f, height * 0.26f));
        }

        private Sprite GetSprite(BattleShowroomTileAtlas atlas, float height)
        {
            if (atlas?.IsAvailable != true)
                return null;
            var key = (atlas, _facing, _look);
            if (!_frames.TryGetValue(key, out Sprite sprite))
            {
                Texture2D texture = atlas.Texture;
                if (_look == BattleShowroomLook.DarkBold
                    && !_darkBoldTextures.TryGetValue(atlas.Texture.name, out texture))
                {
                    _darkBoldValidationError = "Dark + Bold artwork is missing for " + atlas.Texture.name + ".";
                    Debug.LogWarning("[BattleShowroom] " + _darkBoldValidationError);
                    return null;
                }
                sprite = atlas.CreateSprite(_facing, height, texture);
                if (sprite == null)
                {
                    _darkBoldValidationError = "Dark + Bold canvas dimensions differ for " + atlas.Texture.name + ".";
                    Debug.LogWarning("[BattleShowroom] " + _darkBoldValidationError);
                    return null;
                }
                _frames.Add(key, sprite);
            }
            return sprite;
        }

        private void EnsureStageObjects()
        {
            if (_softGlow != null)
                return;

            CreateGlowSprite();
            _boardBounds = _boardFrame != null ? _boardFrame.bounds : new Bounds(Vector3.zero, Vector3.one);
            if (_paintedBoardTexture != null && _boardFrame != null)
            {
                _boardMaterial = new Material(_boardFrame.sharedMaterial) { name = "Showroom SF Deck" };
                _boardMaterial.SetColor("_Color", Color.white);
                _boardMaterial.SetFloat("_Glossiness", 0.04f);
                _boardMaterial.SetColor("_EmissionColor", Color.white * 0.22f);
                _boardMaterial.EnableKeyword("_EMISSION");
                ApplyBoardLook();
                PaintBoardSurface(_boardFrame, 3, 0f);
            }
            if (_boardTiles != null)
            {
                for (int index = 0; index < _boardTiles.Length; index++)
                {
                    MeshRenderer boardTile = _boardTiles[index];
                    if (boardTile == null)
                        continue;
                    _boardBounds.Encapsulate(boardTile.bounds);
                    if (_boardMaterial != null)
                        PaintBoardSurface(boardTile, boardTile == _tile ? 2 : (index + index / 6) % 2, 0.16f);
                }
            }
            if (_reliefShader == null)
                return;
            _reliefMaterial = new Material(_reliefShader) { name = "Concept Artwork Relief" };
            _body = new BattleShowroomRelief(_stage.transform, "Concept Body", _reliefMaterial);
            _shadow = CreateShadow("Actor contact shadow");
            for (int index = 0; index < _dolls.Length; index++)
            {
                _dolls[index] = new BattleShowroomRelief(_stage.transform, "Puppet " + (index + 1), _reliefMaterial);
                _dollShadows[index] = CreateShadow("Puppet contact shadow " + (index + 1));
            }
        }

        private void DrawRelief(BattleShowroomRelief model, Sprite pose, Vector3 position)
        {
            if (pose == null)
            {
                model.Hide();
                return;
            }
            if (!_meshes.TryGetValue(pose, out Mesh mesh))
            {
                mesh = BattleShowroomRelief.CreateMesh(pose);
                float widthScale = RegisteredPoseWidthScale(pose);
                if (!Mathf.Approximately(widthScale, 1f))
                {
                    Vector3[] vertices = mesh.vertices;
                    for (int index = 0; index < vertices.Length; index++)
                        vertices[index].x *= widthScale;
                    mesh.vertices = vertices;
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                }
                _meshes.Add(pose, mesh);
            }
            model.Transform.SetPositionAndRotation(position, CharacterRotation);
            model.Transform.localScale = Vector3.one;
            model.Show(mesh, pose.texture);
        }

        private float RegisteredPoseWidthScale(Sprite pose)
        {
            if (Definition.Art != BattleShowroomConceptArt.Mecharon || _idleSprite == null
                || pose != _windupSprite && pose != _recoverSprite)
                return 1f;
            return _idleSprite.bounds.size.x / Mathf.Max(0.0001f, pose.bounds.size.x);
        }

        private void PaintBoardSurface(MeshRenderer original, int cell, float seamPadding)
        {
            Bounds bounds = original.bounds;
            float halfWidth = (bounds.size.x + seamPadding) * 0.5f;
            float halfDepth = (bounds.size.z + seamPadding) * 0.5f;
            var mesh = new Mesh { name = "Painted " + original.name };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, 0f, -halfDepth), new Vector3(-halfWidth, 0f, halfDepth),
                new Vector3(halfWidth, 0f, halfDepth), new Vector3(halfWidth, 0f, -halfDepth)
            };
            SetBoardSurfaceUV(mesh, cell);
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.RecalculateBounds();
            _boardSurfaces.Add(mesh);
            _paintedTileMeshes[original] = mesh;
            var surface = new GameObject("Painted " + original.name, typeof(MeshFilter), typeof(MeshRenderer));
            surface.transform.SetParent(_stage.transform, false);
            surface.transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.001f, bounds.center.z);
            surface.GetComponent<MeshFilter>().sharedMesh = mesh;
            surface.GetComponent<MeshRenderer>().sharedMaterial = _boardMaterial;
            original.enabled = false;
        }

        private void SetBoardSurfaceUV(Mesh mesh, int cell)
        {
            if (_paintedBoardTexture == null)
                return;
            float inset = 2f / Mathf.Min(_paintedBoardTexture.width, _paintedBoardTexture.height);
            float left = cell % 2 * 0.5f + inset;
            float bottom = (1 - cell / 2) * 0.5f + inset;
            float right = left + 0.5f - inset * 2f;
            float top = bottom + 0.5f - inset * 2f;
            mesh.uv = new[] { new Vector2(left, bottom), new Vector2(left, top), new Vector2(right, top), new Vector2(right, bottom) };
        }

        private void ApplyBoardLook()
        {
            if (_boardMaterial == null)
                return;
            Texture2D texture = _look == BattleShowroomLook.DarkBold ? _darkBoldBoardTexture : _paintedBoardTexture;
            _boardMaterial.SetTexture("_MainTex", texture);
            _boardMaterial.SetTexture("_EmissionMap", texture);
        }

        private SpriteRenderer CreateShadow(string name)
        {
            SpriteRenderer shadow = CreateRenderer(name, 0);
            shadow.sprite = _softGlow;
            return shadow;
        }

        private static void PlaceShadow(SpriteRenderer shadow, Vector3 ground, float size, float lift)
        {
            float softness = Mathf.Clamp01(lift * 1.2f);
            shadow.transform.SetPositionAndRotation(ground - Vector3.up * 0.006f
                + new Vector3(0.05f, 0f, 0.045f) * (1f + softness), Quaternion.Euler(90f, 0f, -15f));
            shadow.transform.localScale = new Vector3(size * (1f + softness), size * (0.74f + softness), 1f);
            shadow.color = new Color(0f, 0f, 0f, 0.76f - softness * 0.44f);
            shadow.enabled = true;
        }

        private SpriteRenderer CreateRenderer(string name, int sortingOrder)
        {
            var root = new GameObject(name);
            root.transform.SetParent(_stage.transform, false);
            var sprite = root.AddComponent<SpriteRenderer>();
            sprite.sortingOrder = sortingOrder;
            sprite.enabled = false;
            return sprite;
        }

        private void CreateGlowSprite()
        {
            const int size = 48;
            _glowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Tile Preview Soft Glow" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 offset = new((x + 0.5f) * (2f / size) - 1f, (y + 0.5f) * (2f / size) - 1f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - offset.magnitude), 1.6f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
            _glowTexture.SetPixels32(pixels);
            _glowTexture.Apply(false, true);
            _softGlow = Sprite.Create(_glowTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void ResetEffects()
        {
            for (int index = 0; index < _lineCursor; index++)
                _lines[index].enabled = false;
            for (int index = 0; index < _dollCursor; index++)
            {
                _dolls[index].Hide();
                _dollShadows[index].enabled = false;
            }
            for (int index = 0; index < _effectCursor; index++)
                _effectRenderers[index].enabled = false;
            _effectCursor = 0;
            _lineCursor = 0;
            _dollCursor = 0;
        }

        private void DrawDiad(float time)
        {
            bool skill = Action == BattleShowroomConceptAction.Skill;
            float release = skill ? 0.43f : 0.28f;
            float depthSign = _facing == BattleShowroomFacing.Rear ? 1f : -1f;
            Vector3 core = ArtworkAnchor(new Vector3(0f, 0.48f, 0.075f * depthSign));
            Vector3 face = ArtworkAnchor(new Vector3(_playback.IsAlternateBasic ? 0.19f : -0.20f, 0.65f, 0.065f * depthSign));
            Vector3 target = _foot + AttackHeading(_facing) * 1.75f + Vector3.up * 0.22f;
            if (time < release)
            {
                float charge = Mathf.SmoothStep(0f, 1f, time / release);
                PaintEffect(_diadEffects, DiadCharge, skill ? core : face,
                    Mathf.Lerp(0.23f, skill ? 0.89f : 0.55f, charge), 0.30f + charge * 0.65f, _camera.transform.rotation);
            }

            if (skill)
            {
                DrawDiadScythe(core, target, time - release, 0.55f, true);
                PaintPulse(_diadEffects, DiadImpact, target, time - release - 0.55f,
                    0.48f, 1.55f, _camera.transform.rotation);
                return;
            }
            if (_playback.IsAlternateBasic)
            {
                DrawDiadScythe(face, target, time - release, 0.48f, false);
                PaintPulse(_diadEffects, DiadImpact, target, time - release - 0.48f,
                    0.35f, 0.72f, _camera.transform.rotation);
                return;
            }

            float beamAge = time - release;
            if (beamAge >= 0f && beamAge <= 0.48f)
            {
                Vector3 end = Vector3.Lerp(face, target, Mathf.Clamp01(beamAge / 0.12f));
                Vector3 center = (face + end) * 0.5f;
                Vector3 path = end - face;
                float width = Vector3.ProjectOnPlane(path, _camera.transform.forward).magnitude;
                float opacity = Mathf.Clamp01((0.48f - beamAge) / 0.17f);
                PaintEffect(_diadEffects, DiadLaser, center, Mathf.Max(0.04f, width), opacity, EffectRotation(path));
            }
            PaintPulse(_diadEffects, DiadImpact, target, beamAge - 0.12f,
                0.34f, 0.67f, _camera.transform.rotation);
        }

        private void DrawDiadScythe(Vector3 origin, Vector3 target, float age, float travel, bool unified)
        {
            if (age < 0f || age > travel)
                return;
            float progress = Mathf.SmoothStep(0f, 1f, age / travel);
            Vector3 path = target - origin;
            Vector3 point = Vector3.Lerp(origin, target, progress);
            Quaternion rotation = EffectRotation(path);
            float size = unified ? Mathf.Lerp(1.05f, 1.70f, progress) : Mathf.Lerp(0.56f, 0.92f, progress);
            PaintEffect(_diadEffects, unified ? DiadScythe : DiadSlash, point, size, 1f, rotation);
            PaintEffect(_diadEffects, DiadTrail, point - path.normalized * (unified ? 0.43f : 0.22f),
                size * 0.82f, 0.70f, rotation);
        }

        private Vector3 ArtworkAnchor(Vector3 localPoint)
            => _foot + _bodyOffset + CharacterRotation * (localPoint * HeroScale);

        private void DrawMecharon(float time)
        {
            Vector3 heading = AttackHeading(_facing);
            Vector3 across = Vector3.Cross(Vector3.up, heading);
            float distance = _facing == BattleShowroomFacing.Front ? 1.40f : 2.00f;
            Vector3 center = _foot + heading * distance;
            bool missiles = Action == BattleShowroomConceptAction.Skill;
            int shotCount = missiles ? 7 : 5;
            float interval = missiles ? 0.094f : 0.215f;
            float start = missiles ? 0.33f : 0.19f;
            for (int shot = 0; shot < shotCount; shot++)
            {
                float firedAt = start + shot * interval;
                float age = time - firedAt;
                float spread = missiles ? (shot - 3) * 0.35f : 0f;
                Vector3 aim = center + across * spread;
                DrawMechaProjectile(CannonMuzzle(shot), aim, age, missiles, shot);
            }
        }

        private Vector3 CannonMuzzle(int shot)
        {
            if (_facing == BattleShowroomFacing.Side)
                return ArtworkAnchor(new Vector3(0.33f, 0.39f, -0.055f));
            float lateral = shot % 2 == 0 ? -0.275f : 0.275f;
            float depth = _facing == BattleShowroomFacing.Rear ? 0.065f : -0.065f;
            return ArtworkAnchor(new Vector3(lateral, 0.40f, depth));
        }

        private void DrawMechaProjectile(Vector3 muzzle, Vector3 landing, float age, bool missile, int shot)
        {
            float travel = missile ? 0.65f + (shot % 3) * 0.08f : 0.34f;
            if (age < 0f || age > travel + 0.71f)
                return;

            if (age < 0.14f)
            {
                float flash = age / 0.14f;
                Vector3 aim = (landing + Vector3.up * 0.30f - muzzle).normalized;
                PaintEffect(_mecharonEffects, MuzzleFlash, muzzle + aim * 0.11f, 0.52f + flash * 0.10f,
                    1f - flash, EffectRotation(aim));
            }
            if (age < travel)
            {
                float progress = age / travel;
                Vector3 head = MechaFlightPoint(muzzle, landing, progress, missile);
                Vector3 next = MechaFlightPoint(muzzle, landing, Mathf.Min(1f, progress + 0.045f), missile);
                Vector3 direction = (next - head).normalized;
                Quaternion rotation = EffectRotation(direction);
                PaintEffect(_mecharonEffects, missile ? Missile : CannonShell, head,
                    missile ? 0.61f : 0.40f, 1f, rotation);
                PaintEffect(_mecharonEffects, Exhaust, head - direction * (missile ? 0.32f : 0.23f),
                    missile ? 0.68f : 0.43f, Mathf.Lerp(0.48f, 0.92f, Mathf.Sin(progress * Mathf.PI)), rotation);
            }

            Vector3 scorch = landing + Vector3.up * 0.015f;
            PaintPulse(_mecharonEffects, ShellImpact, landing + Vector3.up * 0.13f, age - travel,
                0.39f, missile ? 0.88f : 0.57f, _camera.transform.rotation);
            PaintPulse(_mecharonEffects, Scorch, scorch, age - travel - 0.055f,
                0.62f, missile ? 0.66f : 0.45f, Quaternion.Euler(90f, 0f, shot * 37f));
        }

        private static Vector3 MechaFlightPoint(Vector3 muzzle, Vector3 landing, float progress, bool missile)
        {
            float apex = missile ? 0.92f : 0.11f;
            return Vector3.Lerp(muzzle, landing + Vector3.up * 0.095f, progress)
                + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * apex);
        }

        private void DrawPuppetFormation(float time)
        {
            const int groupSize = 3;
            const float burstTime = BattleShowroomConceptPlayback.SynchronizedBurstTime;
            bool skill = Action == BattleShowroomConceptAction.Skill;
            int count = skill ? _playback.DollsAtSkillStart : _playback.ActiveDollCount;
            int usedTargets = 0;
            for (int index = 0; index < count; index++)
            {
                int slot = index % groupSize;
                if (slot == 0)
                    usedTargets = 0;
                Vector2 home = DollHome[slot];
                int targetIndex = BattleShowroomConceptPlayback.ClosestAvailableTarget(home, Targets, usedTargets);
                usedTargets |= 1 << targetIndex;
                Vector2 target = Targets[targetIndex];
                Vector2 stationed = StationedDollPosition(target, index, _playback.Repositioned);
                Vector2 point = stationed;
                float lift = 0f;
                BattleShowroomConceptPose dollPose = BattleShowroomConceptPose.Idle;
                float tether = 0.14f;
                Color color = index < groupSize ? Violet : Magenta;
                if (skill)
                {
                    Vector2 impact = target + new Vector2(index < groupSize ? -9f : 9f, index < groupSize ? -7f : 7f);
                    float approach = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / burstTime));
                    point = Vector2.Lerp(stationed, impact, approach);
                    tether = 0.45f;
                    dollPose = time < 0.37f ? BattleShowroomConceptPose.Windup : BattleShowroomConceptPose.Attack;
                    if (time >= burstTime)
                    {
                        PaintPulse(_puppeteerEffects, PuppetExplosion, DollPoint(impact, 0.23f),
                            time - burstTime, 0.66f, 1.05f, _camera.transform.rotation);
                        PaintPulse(_puppeteerEffects, SummonGlyph, DollPoint(impact, 0f),
                            time - burstTime, 0.58f, 0.72f, Quaternion.Euler(90f, 0f, 0f));
                        continue;
                    }
                }
                else if (Action == BattleShowroomConceptAction.Deploy && index >= _playback.FirstDeployingDoll)
                {
                    float age = time - slot * 0.11f;
                    if (age < 0f)
                        continue;
                    float approach = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 0.71f));
                    point = Vector2.Lerp(home, stationed, approach);
                    tether = 0.61f * (1f - approach);
                    dollPose = BattleShowroomConceptPlayback.PoseAtElapsed(_playback.Concept, Action, age);
                    PaintPulse(_puppeteerEffects, SummonGlyph, DollPoint(home, 0f), age,
                        0.37f, 0.58f, Quaternion.Euler(90f, 0f, 0f));
                    PaintPulse(_puppeteerEffects, HandMagic, DollPoint(stationed, 0.16f), age - 0.61f,
                        0.26f, 0.43f, _camera.transform.rotation);
                }
                else if (Action == BattleShowroomConceptAction.Basic)
                {
                    float age = BattleShowroomConceptPlayback.DollStrikeAge(time, index);
                    point = BattleShowroomConceptPlayback.DollStrikePosition(stationed, target, age);
                    float swing = Mathf.Clamp01((age - 0.25f) / 0.46f);
                    float weight = Mathf.Sin(swing * Mathf.PI);
                    tether = 0.14f + weight * 0.42f;
                    dollPose = BattleShowroomConceptPlayback.DollPoseAtElapsed(age);
                    Vector3 targetPoint = DollPoint(target, 0.12f);
                    Quaternion swingDirection = EffectRotation(DollPoint(target, 0f) - DollPoint(stationed, 0f));
                    float impactAge = age - BattleShowroomConceptPlayback.DollStrikeImpact;
                    PaintPulse(_puppeteerEffects, PuppetSlash, targetPoint, impactAge + 0.09f,
                        0.25f, 0.67f, swingDirection);
                    PaintPulse(_puppeteerEffects, PuppetImpact, targetPoint, impactAge,
                        0.32f, 0.56f, _camera.transform.rotation);
                }
                else if (Action == BattleShowroomConceptAction.Move)
                {
                    Vector2 previous = StationedDollPosition(target, index, !_playback.Repositioned);
                    float approach = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / 0.95f));
                    point = Vector2.Lerp(previous, stationed, approach);
                    tether = 0.14f + Mathf.Sin(approach * Mathf.PI) * 0.30f;
                }
                DrawDoll(point, lift, color, dollPose, tether);
            }

            if (Action == BattleShowroomConceptAction.Basic || Action == BattleShowroomConceptAction.Skill)
                PaintPulse(_puppeteerEffects, HandMagic, PuppetHand(), time - 0.12f,
                    0.42f, 0.42f, _camera.transform.rotation);
        }

        private static Vector2 StationedDollPosition(Vector2 target, int index, bool repositioned)
        {
            bool firstGroup = index < 3;
            float reach = firstGroup ? -67f : 15f;
            if (target == Boss)
                reach += 27f;
            float side = firstGroup ? -14f : 15f;
            return target + new Vector2(reach + (repositioned ? -34f : 0f), side + (repositioned ? 28f : 0f));
        }

        private void DrawReturningPuppet(float time, bool attacking)
        {
            Vector2 home = DollHome[1] + new Vector2(-13f, 31f);
            Vector2 point = home;
            float lift = 0f;
            if (attacking)
            {
                bool skill = Action == BattleShowroomConceptAction.Skill;
                Vector2 destination = skill ? Boss + new Vector2(-14f, 3f) : Boss;
                const float apexLift = 0.78f;
                float impact = skill ? 0.95f : 0.57f;
                float returnStart = skill ? impact + 0.28f : impact;
                if (time < impact)
                {
                    float approach = Mathf.Clamp01(time / impact);
                    point = skill ? Vector2.Lerp(home, destination, Mathf.SmoothStep(0f, 1f, approach))
                        : Curve(home, destination, -63f, approach);
                    lift = skill ? RiseToApex(approach, apexLift) : Hop(approach, 0.23f);
                }
                else
                {
                    Vector3 burst = DollPoint(destination, (skill ? apexLift : 0f) + 0.17f);
                    PaintPulse(_puppeteerEffects, skill ? PuppetExplosion : PuppetImpact, burst,
                        time - impact, skill ? 0.50f : 0.36f, skill ? 1.30f : 0.72f, _camera.transform.rotation);
                    if (skill)
                        PaintPulse(_puppeteerEffects, SummonGlyph, DollPoint(destination, 0f),
                            time - impact, 0.59f, 1.09f, Quaternion.Euler(90f, 0f, 0f));
                    if (skill && time < returnStart)
                        return;
                    float returning = Mathf.Clamp01((time - returnStart) / (skill ? 0.77f : 0.68f));
                    point = skill ? Vector2.Lerp(destination, home, Mathf.SmoothStep(0f, 1f, returning))
                        : Curve(destination, home, 70f, returning);
                    lift = skill ? RiseToApex(1f - returning, apexLift) : Hop(returning, 0.22f);
                }
                Vector3 trail = DollPoint(point, lift + 0.13f);
                Quaternion direction = EffectRotation(DollPoint(time < impact ? destination : home, lift) - trail);
                PaintEffect(_puppeteerEffects, PuppetTrail, trail, skill ? 0.80f : 0.63f,
                    Mathf.Clamp01(time * 7f), direction);
            }
            BattleShowroomConceptPose pose = BattleShowroomConceptPlayback.PoseAtElapsed(_playback.Concept, Action, time);
            DrawDoll(point, lift, Violet, pose, attacking ? 0.47f : 0.13f);
        }

        private Vector3 DollPoint(Vector2 point, float lift)
        {
            Vector3 heading = AttackHeading(_facing);
            Vector3 across = Vector3.Cross(Vector3.up, heading);
            float distance = (point.x - Actor.x) * 0.0047f;
            float lateral = (point.y - Boss.y) * 0.0072f;
            return _foot + heading * distance + across * lateral + Vector3.up * (0.025f + lift);
        }

        private Vector3 PuppetHand()
        {
            float beside = _facing == BattleShowroomFacing.Rear ? -0.21f : 0.21f;
            if (_facing == BattleShowroomFacing.Side)
                beside = 0.31f;
            return ArtworkAnchor(new Vector3(beside, 0.56f, -0.045f));
        }

        private void DrawDoll(Vector2 point, float lift, Color color, BattleShowroomConceptPose pose, float tether)
        {
            if (_dollCursor == _dolls.Length || _dollSprite == null)
                return;
            int index = _dollCursor++;
            Vector3 feet = DollPoint(point, lift);
            DrawRelief(_dolls[index], SelectPose(pose, _dollSprite, _dollWindupSprite, _dollAttackSprite,
                _dollRecoverSprite), feet);
            PlaceShadow(_dollShadows[index], DollPoint(point, 0f), 0.30f * DollScale, lift);
            Vector3 crown = feet + CharacterRotation * new Vector3(0f, 0.25f * DollScale, -0.02f);
            DrawMarionetteRibbon(PuppetHand(), crown, color, tether, index);
        }

        private void DrawMarionetteRibbon(Vector3 start, Vector3 end, Color color, float alpha, int index)
        {
            Vector3 chord = end - start;
            Vector3 cross = Vector3.Cross(Vector3.up, chord).normalized;
            float pulse = _playback.Clock * 8f + index * 0.7f;
            for (int point = 0; point < _ribbonPoints.Length; point++)
            {
                float t = (float)point / (_ribbonPoints.Length - 1);
                float belly = Mathf.Sin(t * Mathf.PI);
                float wave = Mathf.Sin(t * 7f - pulse) * 0.068f;
                Vector3 arc = Vector3.Lerp(start, end, t);
                arc += cross * ((0.105f + wave) * belly) - Vector3.up * (0.18f * belly);
                _ribbonPoints[point] = arc;
            }
            float opacity = Mathf.Lerp(0.82f, 1f, Mathf.Clamp01((alpha - 0.14f) / 0.47f));
            for (int layer = 0; layer < 3; layer++)
            {
                Color tint = layer == 0 ? new Color(0.61f, 0.16f, 1f, 0.34f)
                    : layer == 1 ? new Color(0.055f, 0.018f, 0.10f, 0.96f)
                    : new Color(0.94f, 0.74f, 1f, opacity);
                float pixels = layer == 0 ? 6.2f : layer == 1 ? 3.5f : 1.6f;
                LineRenderer line = UseLine(_ribbonPoints.Length, 1f, tint);
                line.sortingOrder = layer + 2;
                line.startWidth = ScreenPixelsToWorld(pixels);
                line.endWidth = line.startWidth;
                line.SetPositions(_ribbonPoints);
            }
            float flow = Mathf.Repeat(_playback.Clock * 0.60f + index * 0.173f, 1f) * (_ribbonPoints.Length - 1);
            int segment = Mathf.Min(_ribbonPoints.Length - 2, Mathf.FloorToInt(flow));
            Vector3 spark = Vector3.Lerp(_ribbonPoints[segment], _ribbonPoints[segment + 1], flow - segment);
            PaintEffect(_puppeteerEffects, HandMagic, spark, ScreenPixelsToWorld(7f), 0.93f, _camera.transform.rotation);
            PaintEffect(_puppeteerEffects, HandMagic, end, ScreenPixelsToWorld(9f), 0.78f, _camera.transform.rotation);
            if (index == 0)
                PaintEffect(_puppeteerEffects, HandMagic, start, ScreenPixelsToWorld(12f), 0.86f, _camera.transform.rotation);
        }

        private float ScreenPixelsToWorld(float pixels)
            => pixels * (2f * _camera.orthographicSize / Mathf.Max(1, _camera.pixelHeight));

        private Quaternion EffectRotation(Vector3 worldDirection)
        {
            float x = Vector3.Dot(worldDirection, _camera.transform.right);
            float y = Vector3.Dot(worldDirection, _camera.transform.up);
            float angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            return Quaternion.AngleAxis(angle, _camera.transform.forward) * _camera.transform.rotation;
        }

        private void PaintPulse(BattleShowroomEffectAtlas atlas, int index, Vector3 position, float age,
            float life, float size, Quaternion rotation)
        {
            if (age < 0f || age >= life)
                return;
            float progress = age / life;
            float scale = Mathf.Lerp(0.62f, 1.18f, Mathf.Sqrt(progress));
            float opacity = (1f - progress) * Mathf.Min(1f, 0.44f + progress * 9f);
            PaintEffect(atlas, index, position, size * scale, opacity, rotation);
        }

        private void PaintEffect(BattleShowroomEffectAtlas atlas, int index, Vector3 position,
            float size, float opacity, Quaternion rotation)
        {
            if (atlas == null || opacity < 0.015f)
                return;
            if (!_effectFrames.TryGetValue((atlas, index), out Sprite sprite))
            {
                sprite = atlas.CreateSprite(index);
                if (sprite == null)
                    return;
                _effectFrames.Add((atlas, index), sprite);
            }
            if (_effectCursor == _effectRenderers.Count)
                _effectRenderers.Add(CreateRenderer("Painted combat effect", 3));
            SpriteRenderer effect = _effectRenderers[_effectCursor++];
            effect.sprite = sprite;
            effect.color = WithAlpha(Color.white, opacity);
            effect.transform.SetPositionAndRotation(position, rotation);
            effect.transform.localScale = Vector3.one * size;
            effect.enabled = true;
        }

        private static float Hop(float progress, float height) => Mathf.Sin(progress * Mathf.PI) * height;

        private static float RiseToApex(float progress, float height)
            => BattleShowroomConceptPlayback.MoveBetweenHomeAndApex(Vector2.zero, Vector2.up * height, progress, rising: true).y;


        private static Vector2 Curve(Vector2 from, Vector2 to, float height, float phase)
            => Vector2.Lerp(from, to, phase) + Vector2.up * (height * Mathf.Sin(phase * Mathf.PI));

        public static Vector3 AttackHeading(BattleShowroomFacing facing) => facing switch
        {
            BattleShowroomFacing.Side => Vector3.right,
            BattleShowroomFacing.Rear => Vector3.forward,
            _ => Vector3.back
        };

        private LineRenderer UseLine(int count, float width, Color color, bool loop = false)
        {
            if (_lineCursor == _lines.Count)
            {
                var root = new GameObject("Tile Preview Effect Line");
                root.transform.SetParent(_stage.transform, false);
                var created = root.AddComponent<LineRenderer>();
                created.sharedMaterial = _shadow.sharedMaterial;
                created.sortingOrder = 5;
                created.numCapVertices = 2;
                _lines.Add(created);
            }
            LineRenderer line = _lines[_lineCursor++];
            line.loop = loop;
            line.sortingOrder = 5;
            line.positionCount = count;
            line.startWidth = width * EffectUnits;
            line.endWidth = width * EffectUnits;
            line.startColor = color;
            line.endColor = color;
            line.enabled = true;
            return line;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private bool _resourcesReleased;

        private void OnDestroy() => ReleaseOwnedResources();

        private void ReleaseOwnedResources()
        {
            if (_resourcesReleased)
                return;
            _resourcesReleased = true;
            foreach (BattleShowroomConceptPreview actor in _placedActors.Values)
            {
                if (actor != null)
                    actor.ReleaseOwnedResources();
            }
            foreach (Sprite sprite in _frames.Values)
                DestroyOwnedObject(sprite);
            foreach (Mesh mesh in _meshes.Values)
                DestroyOwnedObject(mesh);
            foreach (Sprite effect in _effectFrames.Values)
                DestroyOwnedObject(effect);
            if (_reliefMaterial != null)
                DestroyOwnedObject(_reliefMaterial);
            foreach (Mesh surface in _boardSurfaces)
                DestroyOwnedObject(surface);
            if (_boardMaterial != null)
                DestroyOwnedObject(_boardMaterial);
            if (_softGlow != null)
                DestroyOwnedObject(_softGlow);
            if (_glowTexture != null)
                DestroyOwnedObject(_glowTexture);
            _placedActors.Clear();
            _paintedTileMeshes.Clear();
            _frames.Clear();
            _meshes.Clear();
            _effectFrames.Clear();
            _boardSurfaces.Clear();
        }

#if UNITY_EDITOR
        public void ConfigureLookVariantsForEditor(Texture2D boardTexture, Texture2D[] unitTextures, string validationError)
        {
            _darkBoldBoardTexture = boardTexture;
            _darkBoldUnitTextures = unitTextures;
            _darkBoldValidationError = validationError;
        }

        public void ConfigureForEditor(Camera camera, GameObject stage, MeshRenderer tile,
            MeshRenderer[] boardTiles, MeshRenderer boardFrame, Texture2D paintedBoardTexture, Shader reliefShader,
            BattleShowroomTileAtlas diad,
            BattleShowroomTileAtlas mecharon, BattleShowroomTileAtlas puppeteer, BattleShowroomTileAtlas doll,
            BattleShowroomTileAtlas diadAttack, BattleShowroomTileAtlas mecharonAttack,
            BattleShowroomTileAtlas puppeteerAttack, BattleShowroomTileAtlas dollAttack,
            BattleShowroomEffectAtlas mecharonEffects, BattleShowroomEffectAtlas puppeteerEffects,
            BattleShowroomTileAtlas diadWindup, BattleShowroomTileAtlas mecharonWindup,
            BattleShowroomTileAtlas puppeteerWindup, BattleShowroomTileAtlas dollWindup,
            BattleShowroomTileAtlas diadRecover, BattleShowroomTileAtlas mecharonRecover,
            BattleShowroomTileAtlas puppeteerRecover, BattleShowroomTileAtlas dollRecover,
            BattleShowroomEffectAtlas diadEffects)
        {
            _camera = camera;
            _stage = stage;
            _tile = tile;
            _boardTiles = boardTiles;
            _boardFrame = boardFrame;
            _paintedBoardTexture = paintedBoardTexture;
            _reliefShader = reliefShader;
            _diad = diad;
            _mecharon = mecharon;
            _puppeteer = puppeteer;
            _doll = doll;
            _diadAttack = diadAttack;
            _mecharonAttack = mecharonAttack;
            _puppeteerAttack = puppeteerAttack;
            _dollAttack = dollAttack;
            _mecharonEffects = mecharonEffects;
            _puppeteerEffects = puppeteerEffects;
            _diadWindup = diadWindup;
            _mecharonWindup = mecharonWindup;
            _puppeteerWindup = puppeteerWindup;
            _dollWindup = dollWindup;
            _diadRecover = diadRecover;
            _mecharonRecover = mecharonRecover;
            _puppeteerRecover = puppeteerRecover;
            _dollRecover = dollRecover;
            _diadEffects = diadEffects;
        }
#endif
    }
}
#endif
