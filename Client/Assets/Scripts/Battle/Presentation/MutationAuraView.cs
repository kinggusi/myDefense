using System;
using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    /// <summary>
    /// Procedural Battle-owned presentation for the eight Mutation families.
    /// The hierarchy is built once and profiles are toggled without allocating
    /// materials or colliders when Mutation state changes.
    /// </summary>
    public sealed class MutationAuraView : MonoBehaviour
    {
        private const string AuraRootName = "MutationAura";
        private const string ProfilePrefix = "Profile_";

        private Transform _auraRoot;
        private MutationVisualProfile _activeProfile;
        private string _activeMutationType = "NONE";
        private float _elapsed;

        public string ActiveMutationType => _activeMutationType;

        public void Apply(string mutationType)
        {
            string normalized = Normalize(mutationType);
            EnsureHierarchy();

            bool supported = TryGetProfile(normalized, out MutationVisualProfile profile);
            _activeMutationType = supported ? normalized : "NONE";
            _activeProfile = supported ? profile : default;
            _elapsed = 0f;

            for (int index = 0; index < Profiles.Length; index++)
            {
                Transform group = _auraRoot.Find(ProfilePrefix + Profiles[index].MutationType);
                if (group != null)
                    group.gameObject.SetActive(supported && Profiles[index].MutationType == normalized);
            }

            _auraRoot.localPosition = new Vector3(0f, -0.42f, 0f);
            _auraRoot.localRotation = Quaternion.identity;
            _auraRoot.localScale = Vector3.one;
            _auraRoot.gameObject.SetActive(supported);
        }

        private void Update()
        {
            if (_auraRoot == null || !_auraRoot.gameObject.activeSelf || _activeProfile.MutationType == null)
                return;

            _elapsed += Time.deltaTime;
            float pulse = _activeProfile.PulseAmplitude <= 0f
                ? 0f
                : Mathf.Sin(_elapsed * _activeProfile.PulseSpeed) * _activeProfile.PulseAmplitude;
            float bob = _activeProfile.BobAmplitude <= 0f
                ? 0f
                : Mathf.Sin(_elapsed * _activeProfile.BobSpeed) * _activeProfile.BobAmplitude;
            _auraRoot.localPosition = new Vector3(0f, -0.42f + bob, 0f);
            _auraRoot.localScale = Vector3.one * (1f + pulse);
            _auraRoot.localRotation = Quaternion.Euler(
                _activeProfile.TiltDegrees,
                _elapsed * _activeProfile.RotationSpeed,
                0f);
        }

        private void EnsureHierarchy()
        {
            if (_auraRoot == null)
            {
                Transform existing = transform.Find(AuraRootName);
                if (existing != null)
                {
                    _auraRoot = existing;
                    DisableAndRemoveCollider(existing.gameObject);
                    Renderer legacyRenderer = existing.GetComponent<Renderer>();
                    if (legacyRenderer != null)
                        legacyRenderer.enabled = false;
                }
                else
                {
                    GameObject root = new GameObject(AuraRootName);
                    root.transform.SetParent(transform, false);
                    _auraRoot = root.transform;
                }
            }

            for (int index = 0; index < Profiles.Length; index++)
            {
                MutationVisualProfile profile = Profiles[index];
                string groupName = ProfilePrefix + profile.MutationType;
                if (_auraRoot.Find(groupName) != null)
                    continue;

                GameObject group = new GameObject(groupName);
                group.transform.SetParent(_auraRoot, false);
                BuildProfileGeometry(group.transform, profile);
                group.SetActive(false);
            }
        }

        private static void BuildProfileGeometry(Transform group, MutationVisualProfile profile)
        {
            switch (profile.MutationType)
            {
                case "GIANT":
                    CreateElement(group, "GiantHalo", PrimitiveType.Cylinder,
                        Vector3.zero, new Vector3(0.92f, 0.025f, 0.92f), Vector3.zero, profile.PrimaryColor);
                    CreateElement(group, "GiantCore", PrimitiveType.Sphere,
                        new Vector3(0f, 0.08f, 0f), new Vector3(0.28f, 0.08f, 0.28f), Vector3.zero, profile.SecondaryColor);
                    break;
                case "BERSERK":
                    for (int index = 0; index < 4; index++)
                    {
                        float angle = 45f + index * 90f;
                        Vector3 position = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 0.42f);
                        CreateElement(group, "BerserkSpike" + index, PrimitiveType.Cube,
                            position, new Vector3(0.09f, 0.035f, 0.42f), new Vector3(0f, angle, 0f),
                            index % 2 == 0 ? profile.PrimaryColor : profile.SecondaryColor);
                    }
                    break;
                case "SWIFT":
                    for (int index = 0; index < 3; index++)
                    {
                        CreateElement(group, "SwiftWing" + index, PrimitiveType.Cube,
                            new Vector3(-0.32f + index * 0.32f, 0.02f * index, 0f),
                            new Vector3(0.26f, 0.018f, 0.62f - index * 0.12f),
                            new Vector3(0f, -24f, 0f),
                            index == 1 ? profile.SecondaryColor : profile.PrimaryColor);
                    }
                    break;
                case "TOXIC":
                    CreateElement(group, "ToxicPool", PrimitiveType.Cylinder,
                        Vector3.zero, new Vector3(0.64f, 0.018f, 0.64f), Vector3.zero, profile.PrimaryColor);
                    for (int index = 0; index < 3; index++)
                    {
                        CreateElement(group, "ToxicBubble" + index, PrimitiveType.Sphere,
                            new Vector3(-0.3f + index * 0.3f, 0.08f + index * 0.025f, index % 2 == 0 ? 0.16f : -0.12f),
                            Vector3.one * (0.09f + index * 0.025f), Vector3.zero, profile.SecondaryColor);
                    }
                    break;
                case "GREEDY":
                    for (int index = 0; index < 3; index++)
                    {
                        float angle = index * 120f;
                        Vector3 position = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.12f, 0.42f);
                        CreateElement(group, "GreedyCoin" + index, PrimitiveType.Cylinder,
                            position, new Vector3(0.16f, 0.025f, 0.16f), new Vector3(90f, angle, 0f),
                            index == 0 ? profile.SecondaryColor : profile.PrimaryColor);
                    }
                    break;
                case "OBESE":
                    CreateElement(group, "ObeseShell", PrimitiveType.Sphere,
                        new Vector3(0f, 0.05f, 0f), new Vector3(0.72f, 0.16f, 0.72f), Vector3.zero, profile.PrimaryColor);
                    CreateElement(group, "ObeseBelt", PrimitiveType.Cylinder,
                        Vector3.zero, new Vector3(0.82f, 0.025f, 0.82f), Vector3.zero, profile.SecondaryColor);
                    break;
                case "FROZEN":
                    for (int index = 0; index < 3; index++)
                    {
                        float angle = index * 120f;
                        Vector3 position = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.08f, 0.34f);
                        CreateElement(group, "FrozenCrystal" + index, PrimitiveType.Cube,
                            position, new Vector3(0.16f, 0.22f + index * 0.04f, 0.16f),
                            new Vector3(35f, angle, 45f), index == 1 ? profile.SecondaryColor : profile.PrimaryColor);
                    }
                    break;
                case "BLANK":
                    CreateElement(group, "BlankPlate", PrimitiveType.Cylinder,
                        Vector3.zero, new Vector3(0.58f, 0.012f, 0.58f), Vector3.zero, profile.PrimaryColor);
                    CreateElement(group, "BlankBar", PrimitiveType.Cube,
                        new Vector3(0f, 0.04f, 0f), new Vector3(0.42f, 0.025f, 0.08f), Vector3.zero, profile.SecondaryColor);
                    break;
            }
        }

        private static void CreateElement(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEulerAngles,
            Color color)
        {
            GameObject element = GameObject.CreatePrimitive(primitiveType);
            element.name = name;
            element.transform.SetParent(parent, false);
            element.transform.localPosition = localPosition;
            element.transform.localScale = localScale;
            element.transform.localEulerAngles = localEulerAngles;
            DisableAndRemoveCollider(element);

            Renderer renderer = element.GetComponent<Renderer>();
            if (renderer == null)
                return;
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetColor("_Color", color);
            properties.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(properties);
        }

        private static void DisableAndRemoveCollider(GameObject target)
        {
            Collider collider = target == null ? null : target.GetComponent<Collider>();
            if (collider == null)
                return;
            collider.enabled = false;
            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }

        public static string ResolveMarker(string mutationType)
        {
            return TryGetProfile(Normalize(mutationType), out MutationVisualProfile profile)
                ? profile.Marker
                : string.Empty;
        }

        public static Color ResolveColor(string mutationType)
        {
            return TryGetProfile(Normalize(mutationType), out MutationVisualProfile profile)
                ? profile.PrimaryColor
                : Color.clear;
        }

        private static string Normalize(string mutationType)
            => string.IsNullOrWhiteSpace(mutationType) ? "NONE" : mutationType.Trim().ToUpperInvariant();

        private static bool TryGetProfile(string mutationType, out MutationVisualProfile profile)
        {
            for (int index = 0; index < Profiles.Length; index++)
            {
                if (!string.Equals(Profiles[index].MutationType, mutationType, StringComparison.Ordinal))
                    continue;
                profile = Profiles[index];
                return true;
            }
            profile = default;
            return false;
        }

        private readonly struct MutationVisualProfile
        {
            public readonly string MutationType;
            public readonly string Marker;
            public readonly Color PrimaryColor;
            public readonly Color SecondaryColor;
            public readonly float PulseAmplitude;
            public readonly float PulseSpeed;
            public readonly float RotationSpeed;
            public readonly float BobAmplitude;
            public readonly float BobSpeed;
            public readonly float TiltDegrees;

            public MutationVisualProfile(
                string mutationType,
                string marker,
                Color primaryColor,
                Color secondaryColor,
                float pulseAmplitude,
                float pulseSpeed,
                float rotationSpeed,
                float bobAmplitude,
                float bobSpeed,
                float tiltDegrees = 0f)
            {
                MutationType = mutationType;
                Marker = marker;
                PrimaryColor = primaryColor;
                SecondaryColor = secondaryColor;
                PulseAmplitude = pulseAmplitude;
                PulseSpeed = pulseSpeed;
                RotationSpeed = rotationSpeed;
                BobAmplitude = bobAmplitude;
                BobSpeed = bobSpeed;
                TiltDegrees = tiltDegrees;
            }
        }

        private static readonly MutationVisualProfile[] Profiles =
        {
            new("GIANT", "GIA", new Color(1f, 0.35f, 0.15f), new Color(1f, 0.72f, 0.18f), 0.10f, 2.2f, 22f, 0.01f, 1.2f),
            new("BERSERK", "BER", new Color(0.75f, 0.05f, 0.05f), new Color(1f, 0.28f, 0.12f), 0.07f, 8.5f, 135f, 0.02f, 7f, 4f),
            new("SWIFT", "SWI", new Color(0.1f, 0.85f, 1f), new Color(0.65f, 1f, 1f), 0.025f, 10f, 260f, 0.035f, 9f, -8f),
            new("TOXIC", "TOX", new Color(0.25f, 1f, 0.2f), new Color(0.75f, 1f, 0.12f), 0.065f, 3.1f, 34f, 0.075f, 2.4f, 2f),
            new("GREEDY", "GRE", new Color(1f, 0.8f, 0.05f), new Color(1f, 1f, 0.45f), 0.04f, 5f, 175f, 0.04f, 4f, 7f),
            new("OBESE", "OBE", new Color(0.8f, 0.25f, 0.95f), new Color(1f, 0.55f, 0.9f), 0.13f, 1.7f, -28f, 0.018f, 1.4f),
            new("FROZEN", "FRO", new Color(0.35f, 0.6f, 1f), new Color(0.75f, 0.95f, 1f), 0.018f, 1.2f, 12f, 0.008f, 1f, 12f),
            new("BLANK", "BLK", new Color(0.55f, 0.55f, 0.55f), new Color(0.25f, 0.25f, 0.25f), 0f, 0f, 0f, 0f, 0f)
        };
    }
}
