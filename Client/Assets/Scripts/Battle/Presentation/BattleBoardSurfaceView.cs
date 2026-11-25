using System.Collections.Generic;
using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Replaces only the visible tile tops, retaining every original transform/collider.</summary>
    public sealed class BattleBoardSurfaceView : MonoBehaviour
    {
        private readonly List<(MeshRenderer renderer, bool enabled)> _originals = new();
        private readonly List<GameObject> _surfaces = new();
        private readonly List<Mesh> _meshes = new();
        private Material _material;
        public int SurfaceCount => _surfaces.Count;

        public bool Configure(Transform player1, Transform player2, BattleUnitArtCatalog catalog)
        {
            ReleaseSurfaces();
            if (catalog == null || catalog.BoardTexture == null || catalog.BoardShader == null)
            {
                Debug.LogWarning("[BattleArt] Painted board artwork is unavailable; import the art catalog.");
                return false;
            }
            _material = new Material(catalog.BoardShader) { mainTexture = catalog.BoardTexture };
            AddBoard(player1, catalog.BoardTexture);
            AddBoard(player2, catalog.BoardTexture);
            return _surfaces.Count > 0;
        }

        private void AddBoard(Transform grid, Texture2D texture)
        {
            if (grid == null) return;
            for (int row = 0; row < 4; row++)
            for (int column = 0; column < 6; column++)
            {
                Transform tile = grid.Find($"Tile_{column}_{row}");
                if (tile == null || !tile.TryGetComponent(out MeshFilter filter)
                    || filter.sharedMesh == null || !tile.TryGetComponent(out MeshRenderer original)) continue;
                Bounds bounds = filter.sharedMesh.bounds;
                Vector3 scale = tile.lossyScale;
                float halfX = .53f / Mathf.Max(.001f, Mathf.Abs(scale.x));
                float halfZ = .53f / Mathf.Max(.001f, Mathf.Abs(scale.z));
                float y = bounds.max.y + .005f / Mathf.Max(.001f, Mathf.Abs(scale.y));
                float u = ((row + column) & 1) == 0 ? 0f : .5f;
                float insetX = 2f / texture.width, insetY = 2f / texture.height;
                var mesh = new Mesh { name = "Battle painted tile top" };
                mesh.vertices = new[] { new Vector3(-halfX, y, -halfZ), new Vector3(-halfX, y, halfZ),
                    new Vector3(halfX, y, halfZ), new Vector3(halfX, y, -halfZ) };
                mesh.uv = new[] { new Vector2(u + insetX, .5f + insetY), new Vector2(u + insetX, 1f - insetY),
                    new Vector2(u + .5f - insetX, 1f - insetY), new Vector2(u + .5f - insetX, .5f + insetY) };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var surface = new GameObject("Painted tile top", typeof(MeshFilter), typeof(MeshRenderer));
                surface.transform.SetParent(tile, false);
                surface.GetComponent<MeshFilter>().sharedMesh = mesh;
                surface.GetComponent<MeshRenderer>().sharedMaterial = _material;
                _originals.Add((original, original.enabled)); original.enabled = false;
                _surfaces.Add(surface); _meshes.Add(mesh);
            }
        }

        public void ReleaseSurfaces()
        {
            foreach (var original in _originals)
                if (original.renderer != null) original.renderer.enabled = original.enabled;
            foreach (GameObject surface in _surfaces) BattleSpriteGeometry.Release(surface);
            foreach (Mesh mesh in _meshes) BattleSpriteGeometry.Release(mesh);
            BattleSpriteGeometry.Release(_material);
            _originals.Clear(); _surfaces.Clear(); _meshes.Clear(); _material = null;
        }
        private void OnDestroy() => ReleaseSurfaces();
    }
}
