using System;
using System.Collections.Generic;
using MyDefense.Battle.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyDefense.Battle.Runtime
{
    /// <summary>
    /// Owns the reversible presentation-only switch for a Player 1 solo Daily run.
    /// It never mutates Scene assets and restores every captured object state when
    /// regular Battle mode resumes.
    /// </summary>
    public sealed class DailyBattleSoloPresentationController : MonoBehaviour
    {
        public static readonly IReadOnlyList<string> PlayerTwoOnlyObjectNames = Array.AsReadOnly(new[]
        {
            "EnemyGridParent",
            "Player2Lane_WaypointGroup",
            "EnemyWaypointGroup",
            "P2MonsterCountText",
            "P2_Green"
        });

        private readonly Dictionary<GameObject, bool> _capturedStates = new();
        private bool _soloEnabled;

        public bool SoloEnabled => _soloEnabled;

        public bool SetSoloPlayerOneMode(bool enabled, out string error)
        {
            error = null;
            if (enabled == _soloEnabled)
                return true;
            if (!enabled)
            {
                RestoreCapturedStates();
                FindFirstObjectByType<FusionKidnapBoardView>()?.SetSoloPlayerOneMode(false);
                _soloEnabled = false;
                return true;
            }

            var missing = new List<string>();
            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                error = "Daily solo presentation requires a loaded Scene owner.";
                return false;
            }
            foreach (string objectName in PlayerTwoOnlyObjectNames)
            {
                GameObject target = FindSceneObjectByExactName(scene, objectName, out bool duplicate);
                if (duplicate)
                {
                    missing.Add(objectName + " (duplicate)");
                    continue;
                }
                if (target == null)
                {
                    missing.Add(objectName);
                    continue;
                }
                if (!_capturedStates.ContainsKey(target))
                    _capturedStates.Add(target, target.activeSelf);
                target.SetActive(false);
            }
            if (missing.Count > 0)
            {
                RestoreCapturedStates();
                error = "Daily solo presentation is missing Player 2 objects: " + string.Join(", ", missing) + ".";
                return false;
            }

            FindFirstObjectByType<FusionKidnapBoardView>()?.SetSoloPlayerOneMode(true);
            _soloEnabled = true;
            return true;
        }

        private static GameObject FindSceneObjectByExactName(
            Scene scene,
            string objectName,
            out bool duplicate)
        {
            duplicate = false;
            GameObject match = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int index = 0; index < transforms.Length; index++)
                {
                    Transform candidate = transforms[index];
                    if (candidate == null || !string.Equals(candidate.name, objectName, StringComparison.Ordinal))
                        continue;
                    if (match != null)
                    {
                        duplicate = true;
                        return null;
                    }
                    match = candidate.gameObject;
                }
            }
            return match;
        }

        private void OnDisable()
        {
            if (_soloEnabled)
                SetSoloPlayerOneMode(false, out _);
        }

        private void RestoreCapturedStates()
        {
            foreach (KeyValuePair<GameObject, bool> pair in _capturedStates)
                if (pair.Key != null) pair.Key.SetActive(pair.Value);
            _capturedStates.Clear();
        }
    }
}
