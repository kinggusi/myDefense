using System;
using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Shared placeholder visual for the eight Mutation families.</summary>
    public sealed class MutationAuraView : MonoBehaviour
    {
        private Transform _aura;
        private float _phase;

        public void Apply(string mutationType)
        {
            string normalized = string.IsNullOrWhiteSpace(mutationType)
                ? "NONE"
                : mutationType.Trim().ToUpperInvariant();
            if (_aura == null)
            {
                GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                aura.name = "MutationAura";
                aura.transform.SetParent(transform, false);
                aura.transform.localPosition = new Vector3(0f, -0.42f, 0f);
                aura.transform.localScale = new Vector3(0.72f, 0.025f, 0.72f);
                Collider collider = aura.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                _aura = aura.transform;
            }
            Renderer renderer = _aura.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = ResolveColor(normalized);
            _aura.gameObject.SetActive(normalized != "NONE");
        }

        private void Update()
        {
            if (_aura == null || !_aura.gameObject.activeSelf)
                return;
            _phase += Time.deltaTime * 2.5f;
            float pulse = 0.68f + Mathf.Sin(_phase) * 0.06f;
            _aura.localScale = new Vector3(pulse, 0.025f, pulse);
            _aura.Rotate(Vector3.up, 45f * Time.deltaTime, Space.Self);
        }

        public static Color ResolveColor(string mutationType)
        {
            return mutationType switch
            {
                "GIANT" => new Color(1f, 0.35f, 0.15f),
                "BERSERK" => new Color(0.75f, 0.05f, 0.05f),
                "SWIFT" => new Color(0.1f, 0.85f, 1f),
                "TOXIC" => new Color(0.25f, 1f, 0.2f),
                "GREEDY" => new Color(1f, 0.8f, 0.05f),
                "OBESE" => new Color(0.8f, 0.25f, 0.95f),
                "FROZEN" => new Color(0.35f, 0.6f, 1f),
                "BLANK" => new Color(0.55f, 0.55f, 0.55f),
                _ => Color.clear
            };
        }
    }
}
