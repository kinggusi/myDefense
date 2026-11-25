#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    public static class BattleDevelopmentPhotonApplication
    {
        private const string Argument = "-battleArtPhotonAppId=";

        public static bool TryReadOverride(string[] arguments, out string appId, out bool supplied)
        {
            appId = null; supplied = false;
            if (arguments == null) return false;
            foreach (string argument in arguments)
            {
                if (argument == null || !argument.StartsWith(Argument, StringComparison.OrdinalIgnoreCase)) continue;
                // Reject duplicates rather than choosing an ambiguous application.
                if (supplied) { appId = null; return false; }
                supplied = true;
                if (!Guid.TryParse(argument.Substring(Argument.Length), out Guid parsed) || parsed == Guid.Empty) return false;
                appId = parsed.ToString("D");
            }
            return supplied && appId != null;
        }

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyBeforeFirstScene()
        {
            if (!TryReadOverride(Environment.GetCommandLineArgs(), out string appId, out bool supplied))
            {
                if (supplied) Debug.LogError("[BattleArt] Invalid development Photon application override was ignored.");
                return;
            }
            // Player memory only. This entire method is absent in the Editor
            // and production, and never opens or writes PhotonAppSettings.asset.
            Fusion.Photon.Realtime.PhotonAppSettings settings = Fusion.Photon.Realtime.PhotonAppSettings.Global;
            if (settings == null || settings.AppSettings == null)
            {
                Debug.LogError("[BattleArt] Development Photon application settings are unavailable.");
                return;
            }
            settings.AppSettings.AppIdFusion = appId;
            Debug.Log("[BattleArt] Applied explicit development Photon application override.");
        }
#endif
    }
}
#endif
