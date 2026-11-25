using MyDefense.Battle.Combat;
using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Follows an existing network projectile; no collisions or damage.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class BattlePaintedProjectileView : MonoBehaviour
    {
        private BattleProjectileNetworkState _state;
        private BattleUnitArtView _source;
        private BattleUnitArtShot _shot;
        private BattleUnitVisual _kind;
        private ulong _projectileId;
        private SpriteRenderer _paint;
        private Sprite _sprite;
        private Vector3 _launchOffset;
        private float _initialDistance;
        private Renderer[] _legacy;
        private bool[] _legacyEnabled;
        private bool _released;
        private bool _initialized;

        public void Configure(BattleProjectileNetworkState state, BattleUnitArtView source)
            => Configure(state, source, source.CaptureShot(state.RuntimeProjectileId));

        public void Configure(BattleProjectileNetworkState state, BattleUnitArtView source, BattleUnitArtShot shot)
        {
            PrepareArtwork(source, shot, state.RuntimeProjectileId);
            _state = state;
        }

        private void PrepareArtwork(BattleUnitArtView source, BattleUnitArtShot shot, ulong projectileId)
        {
            ReleaseArtwork();
            _released = false;
            _initialized = false;
            _source = source; _shot = shot; _kind = shot.Visual; _projectileId = projectileId;
            _legacy = GetComponentsInChildren<Renderer>(true);
            _legacyEnabled = new bool[_legacy.Length];
            for (int i = 0; i < _legacy.Length; i++)
                _legacyEnabled[i] = _legacy[i].enabled;
            _paint = new GameObject("Painted network projectile").AddComponent<SpriteRenderer>();
            _paint.enabled = false; _paint.sortingOrder = 5;
        }

        private void LateUpdate()
        {
            if (_state == null || _paint == null || _state.Object == null || !_state.Object.IsValid) return;
            if (!_state.TryGetLocalPresentationPosition(out Vector3 current)) return;
            Vector3 target = _state.ResolvePresentationTargetPosition();
            Transform lockedTarget=null;
            if(_state.Runner!=null&&_state.TargetNetworkId.IsValid
                &&_state.Runner.TryFindObject(_state.TargetNetworkId,out var targetObject)&&targetObject!=null&&targetObject.IsValid)
                lockedTarget=targetObject.transform;
            DrawArtwork(current,target,lockedTarget);
        }

        private void DrawArtwork(Vector3 current,Vector3 target,Transform lockedTarget)
        {
            if (!_initialized)
            {
                if (!InitializeArtwork(target)) return;
            }
            float remaining = Mathf.Clamp01(Vector3.Distance(current, target) / _initialDistance);
            float progress = 1f - remaining;
            Vector3 visualTarget=BattleMonsterArtView.ResolveHitPoint(lockedTarget,target);
            // Only the sprite trajectory is adjusted. Simulation origin, locked
            // target, travel time, collisions and accepted hit remain untouched.
            Vector3 position = current + _launchOffset * remaining+(visualTarget-target)*progress;
            if (_kind >= BattleUnitVisual.Puppeteer1)
                position += Vector3.up * (Mathf.Sin(progress * Mathf.PI) * (_kind == BattleUnitVisual.Puppeteer2 ? .38f : .16f));
            Vector3 heading = visualTarget - position;
            Camera camera = Camera.main;
            Quaternion rotation = camera != null ? camera.transform.rotation : Quaternion.Euler(60f, 0f, 0f);
            float size = _kind == BattleUnitVisual.Mecharon ? .52f : .74f;
            if (_kind >= BattleUnitVisual.Puppeteer1)
            {
                rotation = Quaternion.Euler(25f, 0f, 0f);
                _paint.transform.localScale = new Vector3(heading.x < 0f ? -1f : 1f, 1f, 1f);
            }
            else
            {
                float angle = camera != null ? Mathf.Atan2(Vector3.Dot(heading, camera.transform.up),
                    Vector3.Dot(heading, camera.transform.right)) * Mathf.Rad2Deg : 0f;
                rotation = Quaternion.AngleAxis(angle, rotation * Vector3.forward) * rotation;
                _paint.transform.localScale = Vector3.one * size;
            }
            _paint.transform.SetPositionAndRotation(position, rotation);
            if (_shot.Matches(_source)) _source.UpdateFlight(_projectileId, position);
        }

        private bool InitializeArtwork(Vector3 target)
        {
            // A move, merge or deletion cannot change this projectile's launch
            // metadata. Source is optional and only supplies matching animation.
            if (_shot.Matches(_source)) _source.ObserveShot(_projectileId, target);
            _sprite = _shot.CreateProjectileSprite();
            if (_sprite == null) return false;
            Vector3 emission = _shot.ResolveEmissionPoint(target);
            _initialDistance = Mathf.Max(.001f, Vector3.Distance(_shot.SimulationOrigin, target));
            _launchOffset = emission - _shot.SimulationOrigin;
            _paint.sprite = _sprite; _paint.enabled = true; _initialized = true;
            for (int i = 0; i < _legacy.Length; i++) if (_legacy[i] != null) _legacy[i].enabled = false;
            BattleArtImpact.Play(_shot.Catalog.Effects(_kind), _kind == BattleUnitVisual.Mecharon ? 0
                : _kind == BattleUnitVisual.Diad ? 0 : 3, emission, .42f, .14f,
                _kind == BattleUnitVisual.Mecharon ? target - emission : Vector3.zero);
            return true;
        }

        public void ReleaseArtwork()
        {
            if (_released) return;
            _released = true;
            if (_shot.Matches(_source)) _source.EndFlight(_projectileId);
            if (_legacy != null)
                for (int i = 0; i < _legacy.Length; i++)
                    if (_legacy[i] != null) _legacy[i].enabled = _legacyEnabled[i];
            BattleSpriteGeometry.Release(_paint != null ? _paint.gameObject : null);
            BattleSpriteGeometry.Release(_sprite);
            _paint = null; _sprite = null; _legacy = null;
        }
        private void OnDestroy() => ReleaseArtwork();
        private void OnDisable() => ReleaseArtwork();
    }

    /// <summary>Short-lived, owned artwork emitted only from a real presentation event.</summary>
    public sealed class BattleArtImpact : MonoBehaviour
    {
        private Sprite _sprite;
        private SpriteRenderer _renderer;
        private float _started;
        private float _life;
        private float _size;

        public static void Play(BattleEffectArt atlas, int index, Vector3 position, float size, float life, Vector3 direction = default)
        {
            Sprite sprite = atlas?.Create(index);
            if (sprite == null) return;
            var root = new GameObject("Accepted Battle artwork");
            var impact = root.AddComponent<BattleArtImpact>();
            impact._sprite = sprite; impact._started = Time.time; impact._life = Mathf.Max(.01f, life); impact._size = size;
            impact._renderer = root.AddComponent<SpriteRenderer>();
            impact._renderer.sprite = sprite; impact._renderer.sortingOrder = 6;
            Camera camera = Camera.main;
            Quaternion rotation = camera != null ? camera.transform.rotation : Quaternion.Euler(60f, 0f, 0f);
            if (camera != null && direction.sqrMagnitude > .0001f)
                rotation = Quaternion.AngleAxis(Mathf.Atan2(Vector3.Dot(direction, camera.transform.up),
                    Vector3.Dot(direction, camera.transform.right)) * Mathf.Rad2Deg, camera.transform.forward) * rotation;
            root.transform.SetPositionAndRotation(position, rotation);
            root.transform.localScale = Vector3.one * size;
        }

        private void Update()
        {
            float phase = (Time.time - _started) / _life;
            if (phase >= 1f) { Destroy(gameObject); return; }
            transform.localScale = Vector3.one * (_size * Mathf.Lerp(.70f, 1.15f, Mathf.Sqrt(phase)));
            _renderer.color = new Color(1f, 1f, 1f, 1f - phase);
        }
        public void ReleaseArtwork() { BattleSpriteGeometry.Release(_sprite); _sprite = null; }
        private void OnDestroy() => ReleaseArtwork();
    }
}
