using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MyDefense.Battle.Runtime;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Read-only presentation coordinates. Never changes paths, boards, or physics.</summary>
    public sealed class PlanetBattlefieldLayout
    {
        public readonly Vector3[] Player1Slots;
        public readonly Vector3[] Player2Slots;
        public readonly Vector3[][] Paths;

        public PlanetBattlefieldLayout(Vector3[] p1, Vector3[] p2, Vector3[] lane1, Vector3[] lane2, Vector3[] boss)
        {
            Player1Slots = Copy(p1); Player2Slots = Copy(p2);
            Paths = new[] { Copy(lane1), Copy(lane2), Copy(boss) };
            if (Player1Slots.Length != 24 || Player2Slots.Length != 24)
                throw new ArgumentException("Battlefield presentation requires the authored 24 slots on both boards.");
            foreach (Vector3[] path in Paths)
                if (path.Length < 2) throw new ArgumentException("Battlefield presentation requires all three authored paths.");
        }

        public static PlanetBattlefieldLayout FromScene(Scene scene)
        {
            Transform p1 = null, p2 = null, pathRoot = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "GridManager") p1 = root.transform;
                if (root.name == "EnemyGridParent") p2 = root.transform;
                if (root.GetComponent<PathManager>() != null) pathRoot = root.transform;
            }
            if (p1 == null || p2 == null || pathRoot == null)
                throw new InvalidOperationException("Battlefield layout is missing its authored boards or PathManager.");
            var lanes = new Vector3[3][];
            foreach (WaypointGroup group in pathRoot.GetComponentsInChildren<WaypointGroup>(true))
            {
                int index = (int)group.Lane;
                if (index < 0 || index > 2 || lanes[index] != null)
                    throw new InvalidOperationException("Ambiguous authored battlefield Lane.");
                lanes[index] = group.Waypoints.ConvertAll(point => point.position).ToArray();
            }
            return new PlanetBattlefieldLayout(Slots(p1), Slots(p2), lanes[0], lanes[1], lanes[2]);
        }

        private static Vector3[] Slots(Transform grid)
        {
            var result = new Vector3[24];
            for (int i = 0; i < result.Length; i++)
            {
                Transform tile = grid.Find($"Tile_{i % 6}_{3 - i / 6}");
                if (tile == null) throw new InvalidOperationException("Authored battlefield tile missing: " + grid.name + "/" + i);
                result[i] = tile.position;
            }
            return result;
        }
        private static Vector3[] Copy(Vector3[] values) => values == null ? Array.Empty<Vector3>() : (Vector3[])values.Clone();
    }

    /// <summary>Owned, passive floor meshes. All gameplay objects remain untouched.</summary>
    public sealed class PlanetBattlefieldView : IDisposable
    {
        private readonly GameObject _root;
        private readonly List<Mesh> _meshes = new();
        private readonly List<Material> _materials = new();
        public GameObject Root => _root;
        public int TileCount { get; private set; }
        public Bounds FocusBounds { get; private set; }

        public PlanetBattlefieldView(Transform parent, PlanetContentProfile profile, PlanetBattlefieldLayout layout)
        {
            if (!profile.ValidateBattlefieldTheme(out string error)) throw new InvalidOperationException(error);
            _root = new GameObject("Planet Battlefield Surfaces");
            _root.transform.SetParent(parent, false);
            try
            {
                Material terrain = CreateMaterial(profile.EnvironmentMaterial, profile.BattlefieldTerrain);
                Material atlas = CreateMaterial(profile.EnvironmentMaterial, profile.BattlefieldSurfaces);
                var ground = new MeshData(_root.transform);
                Bounds play = new(layout.Player1Slots[0], Vector3.zero);
                foreach (Vector3 point in layout.Player1Slots) play.Encapsulate(point);
                foreach (Vector3 point in layout.Player2Slots) play.Encapsulate(point);
                foreach (Vector3[] path in layout.Paths) foreach (Vector3 point in path) play.Encapsulate(point);
                Vector3 center = play.center; center.y = 0f;
                // Wide-screen quiet fill contains no source-rim structures.
                ground.Rect(center.x - 20f, center.x + 20f, center.z - 30f, center.z + 30f,
                    -.02f, new Rect(.35f, .35f, .30f, .30f));
                // Piecewise UVs bring the authored 10-12% rim next to the actual
                // play union. The quiet center absorbs resizing, not the camera.
                float left = play.min.x - .15f, right = play.max.x + .15f;
                float bottom = play.min.z - .40f, top = play.max.z + .40f;
                float[] xs = { left - 1.1f, left, right, right + 1.1f };
                float[] zs = { bottom - 1.1f, bottom, top, top + 1.1f };
                FocusBounds = new Bounds(new Vector3((left + right) * .5f, .85f, (bottom + top) * .5f),
                    new Vector3(right - left + 2.2f, 1.7f, top - bottom + 2.2f));
                float[] uv = { 0f, .12f, .88f, 1f };
                for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                    ground.Rect(xs[col], xs[col + 1], zs[row], zs[row + 1], .005f,
                        Rect.MinMaxRect(uv[col], uv[row], uv[col + 1], uv[row + 1]));
                AddMesh("Terrain and connected rim", ground, terrain);

                var surfaces = new MeshData(_root.transform);
                AddBoard(surfaces, layout.Player1Slots, profile);
                AddBoard(surfaces, layout.Player2Slots, profile);
                for (int lane = 0; lane < 3; lane++)
                    Ribbon(surfaces, layout.Paths[lane], lane != 2, .64f, .012f + lane * .002f,
                        Cell(profile, 0, 0));
                AddMesh("Planet tiles lanes and borders", surfaces, atlas);
            }
            catch { Dispose(); throw; }
        }

        private Material CreateMaterial(Material template, Texture texture)
        {
            var result = new Material(template) { mainTexture = texture };
            _materials.Add(result); return result;
        }

        private void AddBoard(MeshData mesh, Vector3[] slots, PlanetContentProfile profile)
        {
            Bounds bounds = new(slots[0], Vector3.zero);
            for (int i = 0; i < slots.Length; i++)
            {
                Vector3 point = slots[i]; bounds.Encapsulate(point);
                mesh.Rect(point.x - .53f, point.x + .53f, point.z - .53f, point.z + .53f,
                    point.y + .060f, Cell(profile, (i / 6 + i % 6) & 1, 1));
                TileCount++;
            }
            float l = bounds.min.x - .575f, r = bounds.max.x + .575f;
            float b = bounds.min.z - .575f, t = bounds.max.z + .575f;
            Ribbon(mesh, new[] { new Vector3(l, 0f, b), new Vector3(l, 0f, t), new Vector3(r, 0f, t), new Vector3(r, 0f, b) },
                true, .09f, .025f, Cell(profile, 1, 0));
        }

        public static Rect Cell(PlanetContentProfile profile, int col, int row)
        {
            Texture2D texture = profile.BattlefieldSurfaces;
            Vector2 split = profile.BattlefieldAtlasSplit;
            // Reserve a mip-safe border inside each independently painted cell.
            float insetX = 16f / texture.width, insetY = 16f / texture.height;
            return Rect.MinMaxRect((col == 0 ? 0f : split.x) + insetX, (row == 0 ? 0f : split.y) + insetY,
                (col == 0 ? split.x : 1f) - insetX, (row == 0 ? split.y : 1f) - insetY);
        }

        private static void Ribbon(MeshData mesh, Vector3[] points, bool loop, float width, float y, Rect uv)
        {
            var offsets = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 previous = points[i] - points[i == 0 ? loop ? points.Length - 1 : 0 : i - 1];
                Vector3 next = points[i == points.Length - 1 ? loop ? 0 : i : i + 1] - points[i];
                previous.y = next.y = 0f;
                if (previous.sqrMagnitude < .0001f) previous = next;
                if (next.sqrMagnitude < .0001f) next = previous;
                Vector3 n0 = Vector3.Cross(Vector3.up, previous.normalized), n1 = Vector3.Cross(Vector3.up, next.normalized);
                Vector3 miter = (n0 + n1).normalized;
                offsets[i] = miter * (width * .5f / Mathf.Max(.5f, Vector3.Dot(miter, n1)));
            }
            for (int i = 0; i < (loop ? points.Length : points.Length - 1); i++)
            {
                int next = (i + 1) % points.Length;
                float length = Vector2.Distance(new Vector2(points[i].x, points[i].z), new Vector2(points[next].x, points[next].z));
                int pieces = Mathf.Max(1, Mathf.CeilToInt(length));
                for (int part = 0; part < pieces; part++)
                {
                    float a = (float)part / pieces, b = (float)(part + 1) / pieces;
                    Vector3 p = Vector3.Lerp(points[i], points[next], a), q = Vector3.Lerp(points[i], points[next], b);
                    p.y = q.y = y;
                    Vector3 o0 = Vector3.Lerp(offsets[i], offsets[next], a), o1 = Vector3.Lerp(offsets[i], offsets[next], b);
                    // The painted straight strip has rails on its left/right:
                    // U crosses the road, V follows travel. This also faces up.
                    mesh.Quad(p - o0, q - o1, q + o1, p + o0, uv);
                }
            }
        }

        private void AddMesh(string name, MeshData data, Material material)
        {
            Mesh mesh = data.Create(name); _meshes.Add(mesh);
            var model = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            model.transform.SetParent(_root.transform, false);
            model.GetComponent<MeshFilter>().sharedMesh = mesh;
            model.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        public void Dispose()
        {
            if (_root != null) _root.SetActive(false);
            foreach (Mesh mesh in _meshes) BattleSpriteGeometry.Release(mesh);
            foreach (Material material in _materials) BattleSpriteGeometry.Release(material);
            _meshes.Clear(); _materials.Clear(); BattleSpriteGeometry.Release(_root);
        }

        private sealed class MeshData
        {
            private readonly Transform _parent;
            private readonly List<Vector3> _vertices = new();
            private readonly List<Vector2> _uv = new();
            private readonly List<int> _indices = new();
            public MeshData(Transform parent) => _parent = parent;
            public void Rect(float left, float right, float bottom, float top, float y, Rect uv)
                => Quad(new Vector3(left, y, bottom), new Vector3(left, y, top), new Vector3(right, y, top), new Vector3(right, y, bottom), uv);
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Rect uv)
            {
                int start = _vertices.Count;
                foreach (Vector3 p in new[] { a, b, c, d }) _vertices.Add(_parent.InverseTransformPoint(p));
                _uv.AddRange(new[] { new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMin, uv.yMax),
                    new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMax, uv.yMin) });
                _indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            public Mesh Create(string name)
            {
                var mesh = new Mesh { name = name }; mesh.SetVertices(_vertices); mesh.SetUVs(0, _uv); mesh.SetTriangles(_indices, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
            }
        }
    }
}
