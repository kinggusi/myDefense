using System.Collections.Generic;
using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Passive artwork only. It never reads or changes combat statistics.</summary>
    public sealed class BattleUnitArtView : MonoBehaviour
    {
        private BattleUnitArtCatalog _catalog;
        private BattleUnitVisual _kind;
        private bool _darkBold;
        private Transform _stage;
        private Material _material;
        private MeshRenderer _legacy;
        private bool _legacyEnabled;
        private readonly Dictionary<(BattleUnitPoseArt, BattleArtPose, BattleArtFacing), Sprite> _sprites = new();
        private readonly Dictionary<Sprite, Mesh> _meshes = new();
        private ArtNode _body;
        private readonly ArtNode[] _dolls = new ArtNode[3];
        private readonly LineRenderer[,] _strings = new LineRenderer[3, 3];
        private Material _stringMaterial;
        private readonly Vector3[] _curve = new Vector3[20];
        private readonly SpriteRenderer[] _ribbonPulses = new SpriteRenderer[3];
        private readonly SpriteRenderer[] _ribbonEnds = new SpriteRenderer[3];
        private SpriteRenderer _handGlow;
        private Sprite _handMagic;
        private readonly Dictionary<ulong, Vector3> _flights = new();
        private readonly HashSet<ulong> _observedShots = new();
        private readonly Dictionary<ulong, int> _shotOrdinals = new();
        private float _shotTime = -100f;
        private Vector3 _aim;
        private BattleArtFacing _facing;
        private bool _mirror;
        private bool _released;
        private Texture2D _shadowTexture;
        private Sprite _shadowSprite;
        private SpriteRenderer _shadow;
        private float _previewDim=1;
        public bool TryGetBodyBounds(out Bounds bounds){if(_body==null||!IsArtworkActive){bounds=default;return false;}bounds=_body.Bounds;return true;}
        public GameObject CreateRenderGhost()
        {
            if(_stage==null)return null;
            GameObject ghost=Instantiate(_stage.gameObject);ghost.name="Unit render-only drag preview";
            ghost.transform.SetPositionAndRotation(_stage.position,_stage.rotation);ghost.transform.localScale=_stage.lossyScale;
            ghost.AddComponent<BattleRenderGhostLease>().Initialize(_sprites.Values,_stage);
            return ghost;
        }
        public void SetPreviewDim(float value){_previewDim=Mathf.Clamp01(value);}
        public BattleUnitVisual Visual => _kind;
        public BattleUnitArtCatalog Catalog => _catalog;
        public bool DarkBold => _darkBold;
        public long BoundSpecId { get; private set; }
        public bool IsArtworkActive => !_released && _stage != null;
        public int ObservedShotOrdinal { get; private set; }
        public uint ShotGeneration { get; private set; }
        public Vector3 Foot => transform.position - Vector3.up * .735f;
        private static Quaternion ArtRotation => Quaternion.Euler(25f, 0f, 0f);

        public bool Configure(BattleUnitArtCatalog catalog, BattleUnitVisual kind, bool darkBold)
        {
            if (catalog == null || !catalog.Available(kind, darkBold)) return false;
            ReleaseArtwork();
            _released = false;
            _catalog = catalog; _kind = kind; _darkBold = darkBold;
            BoundSpecId = GetComponent<UnitData>()?.specId ?? 0L;
            _stage = new GameObject("Battle unit artwork").transform;
            _stage.SetParent(transform, false);
            _material = new Material(catalog.ReliefShader);
            _body = new ArtNode(_stage, "Unit", _material);
            _legacy = GetComponent<MeshRenderer>();
            if (_legacy != null) { _legacyEnabled = _legacy.enabled; _legacy.enabled = false; }
            CreateShadow();
            if (kind >= BattleUnitVisual.Puppeteer1)
            {
                _stringMaterial = new Material(Shader.Find("Sprites/Default"));
                _handMagic = catalog.Effects(kind).Create(3);
                _handGlow = CreateGlyph("Controller hand glow");
                for (int i = 0; i < (_kind == BattleUnitVisual.Puppeteer1 ? 3 : 1); i++)
                {
                    _dolls[i] = new ArtNode(_stage, "Doll " + i, _material);
                    _ribbonPulses[i] = CreateGlyph("Control ribbon pulse " + i);
                    _ribbonEnds[i] = CreateGlyph("Doll control contact " + i);
                    for (int layer = 0; layer < 3; layer++)
                    {
                        var line = new GameObject("Control ribbon " + i + " " + layer).AddComponent<LineRenderer>();
                        line.transform.SetParent(_stage, false);
                        line.sharedMaterial = _stringMaterial; line.positionCount = _curve.Length;
                        line.useWorldSpace = true; line.numCapVertices = 3; line.numCornerVertices = 2;
                        line.sortingOrder = layer + 1;
                        _strings[i, layer] = line;
                    }
                }
            }
            LateUpdate();
            return true;
        }

        public void ObserveShot(ulong projectileId, Vector3 target)
        {
            ReserveShot(projectileId);
            if (!_observedShots.Add(projectileId)) return;
            _shotTime = Time.time; _aim = target;
            Vector3 delta = target - Foot;
            _facing = Mathf.Abs(delta.x) > Mathf.Abs(delta.z) * .65f ? BattleArtFacing.Side
                : delta.z > 0f ? BattleArtFacing.Rear : BattleArtFacing.Front;
            _mirror = _facing == BattleArtFacing.Side && delta.x < 0f;
            if (_kind >= BattleUnitVisual.Puppeteer1) _flights[projectileId] = EmissionPoint;
        }

        public BattleUnitArtShot CaptureShot(ulong projectileId)
            => new(_catalog, _kind, _darkBold, BoundSpecId, Foot, transform.position + Vector3.up, ReserveShot(projectileId), ShotGeneration);

        public void ResetShotHistory()
        {
            // IDs restart at 1 in a new Runner session. Invalidate old optional
            // source references as well, so late cleanup cannot end a new flight.
            ShotGeneration++;
            _flights.Clear(); _observedShots.Clear(); _shotOrdinals.Clear();
            ObservedShotOrdinal = 0; _shotTime = -100f;
            _facing = BattleArtFacing.Front; _mirror = false;
        }

        private int ReserveShot(ulong projectileId)
        {
            if (!_shotOrdinals.TryGetValue(projectileId, out int ordinal))
            { ordinal = ++ObservedShotOrdinal; _shotOrdinals.Add(projectileId, ordinal); }
            return ordinal;
        }

        public void EndFlight(ulong projectileId) => _flights.Remove(projectileId);
        public void UpdateFlight(ulong projectileId, Vector3 point)
        { if (_flights.ContainsKey(projectileId)) _flights[projectileId] = point; }

        public Vector3 EmissionPoint
        {
            get
            {
                Vector3 local = _kind == BattleUnitVisual.Mecharon
                    ? new Vector3(_facing == BattleArtFacing.Side ? .30f : .23f, .55f, -.065f)
                    : _kind == BattleUnitVisual.Diad ? new Vector3(.05f, .54f, -.065f)
                    : new Vector3(.23f, .56f, -.045f);
                if (_facing == BattleArtFacing.Rear) local.z = .065f;
                if (_mirror) local.x = -local.x;
                return Foot + ArtRotation * (local * 1.6f);
            }
        }

        public static Vector3 ResolveEmissionPoint(BattleUnitVisual visual, Vector3 foot, Vector3 target)
        {
            Vector3 delta = target - foot;
            bool side = Mathf.Abs(delta.x) > Mathf.Abs(delta.z) * .65f;
            bool rear = !side && delta.z > 0f;
            Vector3 local = visual == BattleUnitVisual.Mecharon ? new Vector3(side ? .30f : .23f, .55f, -.065f)
                : visual == BattleUnitVisual.Diad ? new Vector3(.05f, .54f, -.065f) : new Vector3(.23f, .56f, -.045f);
            if (rear) local.z = .065f;
            if (side && delta.x < 0f) local.x = -local.x;
            return foot + ArtRotation * (local * 1.6f);
        }

        private void LateUpdate()
        {
            if (_stage == null || _catalog == null) return;
            _stage.SetPositionAndRotation(Foot, Quaternion.identity);
            Vector3 s = transform.lossyScale;
            _stage.localScale = new Vector3(Inverse(s.x), Inverse(s.y), Inverse(s.z));
            float age = Time.time - _shotTime;
            BattleArtPose pose = age < .045f ? BattleArtPose.Windup : age < .14f ? BattleArtPose.Attack
                : age < .32f ? BattleArtPose.Recover : BattleArtPose.Idle;
            Show(_body, _catalog.Unit(_kind), pose, _facing, Foot,
                (_kind == BattleUnitVisual.Mecharon ? .85f : .90f) * 1.6f, _mirror);
            for (int i = 0; i < _dolls.Length; i++)
            {
                if (_dolls[i] == null) continue;
                bool flying = false;
                Vector3 flyingPoint = _aim;
                foreach (var flight in _flights)
                    if ((int)(flight.Key % (ulong)(_kind == BattleUnitVisual.Puppeteer1 ? 3 : 1)) == i)
                    { flying = true; flyingPoint = flight.Value; }
                Vector3 at = Foot + new Vector3((i - (_kind == BattleUnitVisual.Puppeteer1 ? 1 : 0)) * .36f,
                    .055f, -.31f);
                if (flying) _dolls[i].Hide();
                else Show(_dolls[i], _catalog.Doll, BattleArtPose.Idle, _facing, at, .455f, _mirror);
                DrawRibbon(i, EmissionPoint, flying ? flyingPoint + Vector3.up * .15f : at + Vector3.up * .23f);
            }
            if (_shadow != null)
            {
                _shadow.color=new Color(1,1,1,_previewDim);
                _shadow.transform.SetPositionAndRotation(Foot + Vector3.up * .003f, Quaternion.Euler(90f, 0f, 0f));
                _shadow.transform.localScale = new Vector3(.88f, .48f, 1f);
            }
        }

        private void Show(ArtNode node, BattleUnitPoseArt art, BattleArtPose pose,
            BattleArtFacing facing, Vector3 position, float height, bool mirror)
        {
            var key = (art, pose, facing);
            if (!_sprites.TryGetValue(key, out Sprite sprite))
            {
                bool register=art==_catalog.Unit(_kind)&&_kind==BattleUnitVisual.Mecharon&&(pose==BattleArtPose.Windup||pose==BattleArtPose.Recover);
                sprite=BattleUnitPoseCache.Acquire(art,pose,facing,height,_darkBold,register,out Mesh mesh);
                if (sprite == null) { node.Hide(); return; }
                _sprites.Add(key, sprite);
                _meshes.Add(sprite, mesh);
            }
            node.Show(_meshes[sprite], sprite.texture, position, ArtRotation, mirror,_previewDim);
        }

        private void DrawRibbon(int index, Vector3 start, Vector3 end)
        {
            for (int i = 0; i < _curve.Length; i++)
            {
                float t = (float)i / (_curve.Length - 1);
                _curve[i] = Vector3.Lerp(start, end, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .22f);
            }
            Camera camera = Camera.main;
            float pixel = camera != null && camera.orthographic
                ? 2f * camera.orthographicSize / Mathf.Max(1, camera.pixelHeight) : .01f;
            for (int layer = 0; layer < 3; layer++)
            {
                LineRenderer line = _strings[index, layer];
                line.SetPositions(_curve);
                line.startWidth = line.endWidth = pixel * (layer == 0 ? 6.2f : layer == 1 ? 3.5f : 1.6f);
                Color color = layer == 0 ? new Color(.61f, .16f, 1f, .34f)
                    : layer == 1 ? new Color(.055f, .018f, .10f, .96f) : new Color(.94f, .74f, 1f, .88f);
                line.startColor = line.endColor = color;
            }
            float flow = Mathf.Repeat(Time.time * .60f + index * .173f, 1f) * (_curve.Length - 1);
            int segment = Mathf.Min(_curve.Length - 2, Mathf.FloorToInt(flow));
            Vector3 spark = Vector3.Lerp(_curve[segment], _curve[segment + 1], flow - segment);
            Quaternion rotation = camera != null ? camera.transform.rotation : Quaternion.Euler(60f, 0f, 0f);
            DrawGlyph(_ribbonPulses[index], spark, pixel * 7f, .93f, rotation);
            DrawGlyph(_ribbonEnds[index], end, pixel * 9f, .78f, rotation);
            if (index == 0) DrawGlyph(_handGlow, start, pixel * 12f, .86f, rotation);
        }

        private SpriteRenderer CreateGlyph(string name)
        {
            var glyph = new GameObject(name).AddComponent<SpriteRenderer>();
            glyph.transform.SetParent(_stage, false); glyph.sprite = _handMagic; glyph.sortingOrder = 4;
            return glyph;
        }

        private static void DrawGlyph(SpriteRenderer glyph, Vector3 position, float size, float alpha, Quaternion rotation)
        {
            if (glyph == null) return;
            glyph.transform.SetPositionAndRotation(position, rotation);
            glyph.transform.localScale = Vector3.one * size;
            glyph.color = new Color(1f, 1f, 1f, alpha);
        }

        private void CreateShadow()
        {
            _shadowTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color32[1024];
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float radius = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
                pixels[y * 32 + x] = new Color32(8, 7, 17, (byte)(Mathf.Pow(Mathf.Clamp01(1f - radius), 1.5f) * 110f));
            }
            _shadowTexture.SetPixels32(pixels); _shadowTexture.Apply();
            _shadowSprite = Sprite.Create(_shadowTexture, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32f);
            _shadow = new GameObject("Contact shadow").AddComponent<SpriteRenderer>();
            _shadow.transform.SetParent(_stage, false); _shadow.sprite = _shadowSprite;
        }

        public void ReleaseArtwork()
        {
            if (_released) return;
            _released = true;
            if (_legacy != null) _legacy.enabled = _legacyEnabled;
            foreach (Sprite sprite in _sprites.Values) BattleUnitPoseCache.Release(sprite);
            _meshes.Clear(); _sprites.Clear(); ResetShotHistory();
            BattleSpriteGeometry.Release(_stage != null ? _stage.gameObject : null);
            BattleSpriteGeometry.Release(_material); BattleSpriteGeometry.Release(_stringMaterial);
            BattleSpriteGeometry.Release(_shadowSprite); BattleSpriteGeometry.Release(_shadowTexture);
            BattleSpriteGeometry.Release(_handMagic); _handMagic = null; _handGlow = null;
            _stage = null; _legacy = null;
            for (int i = 0; i < 3; i++) { _dolls[i] = null; _ribbonPulses[i] = null; _ribbonEnds[i] = null; }
        }
        private void OnDestroy() => ReleaseArtwork();
        private static float Inverse(float value) => Mathf.Abs(value) > .0001f ? 1f / value : 1f;

        private sealed class ArtNode
        {
            private readonly MeshFilter _filter;
            private readonly MeshRenderer _renderer;
            private readonly MaterialPropertyBlock _properties = new();
            public Bounds Bounds=>_renderer.bounds;
            public ArtNode(Transform parent, string name, Material material)
            {
                var root = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                root.transform.SetParent(parent, false);
                _filter = root.GetComponent<MeshFilter>(); _renderer = root.GetComponent<MeshRenderer>();
                _renderer.sharedMaterial = material;
            }
            public void Hide() => _renderer.enabled = false;
            public void Show(Mesh mesh, Texture texture, Vector3 position, Quaternion rotation, bool mirror,float dim)
            {
                _filter.sharedMesh = mesh;
                _properties.SetTexture("_MainTex", texture);_properties.SetFloat("_PresentationDim",dim); _renderer.SetPropertyBlock(_properties);
                _renderer.transform.SetPositionAndRotation(position, rotation);
                _renderer.transform.localScale = new Vector3(mirror ? -1f : 1f, 1f, 1f);
                _renderer.enabled = true;
            }
        }
    }
}
