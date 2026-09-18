using System;
using System.IO;
using UnityEngine;

namespace MyDefense.Auth
{
    /// <summary>Explicit Editor-session selection only; never a production login provider.</summary>
    public static class DevelopmentLoginPolicy
    {
        public const string SelectionKey = "MyDefense.Auth.DevelopmentAccount";
        public static string SelectedAccount
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.SessionState.GetString(SelectionKey, "");
#else
                return "";
#endif
            }
        }
        public static bool IsRequested => !string.IsNullOrEmpty(SelectedAccount);
        public static bool IsKnownAccount(string username) => username == "jjangash" || username == "kingusi";
        public static bool IsAllowed(string endpoint, string account)
            => Evaluate(Application.isEditor, Environment.GetEnvironmentVariable("MYDEFENSE_ENV"), endpoint, account);
        public static bool Evaluate(bool isEditor, string environment, string endpoint, string account)
        {
            return isEditor && (environment == "local" || environment == "dev") && IsKnownAccount(account)
                && Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && uri.IsLoopback
                && (uri.Scheme == "http" || uri.Scheme == "https") && string.IsNullOrEmpty(uri.UserInfo)
                && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
        }
        public static string CredentialDirectory(string persistentRoot, string regularProfile, string developmentAccount)
        {
            if (string.IsNullOrEmpty(developmentAccount))
                return Path.Combine(persistentRoot, "Auth", SecureCredentialStore.ScopeName(regularProfile));
            if (!IsKnownAccount(developmentAccount)) throw new InvalidOperationException("Unknown development account.");
            // A distinct directory branch prevents any profile string from colliding with the existing guest scope.
            return Path.Combine(persistentRoot, "Auth", "Development", SecureCredentialStore.ScopeName(developmentAccount));
        }
        public static bool MatchesAccount(AuthUser user, string selectedAccount)
            => user != null && user.userId > 0 && !user.isGuest && IsKnownAccount(selectedAccount)
                && user.username == selectedAccount;
    }
}
