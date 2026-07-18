using System;

/// <summary>
/// Resolves non-secret runtime endpoints without storing environment-specific
/// values in scenes, prefabs, or tracked source files.
///
/// Development defaults remain local. CI, a standalone build, or the Unity
/// Editor can override them with environment variables or command-line args.
/// </summary>
public static class RuntimeEnvironmentConfig
{
    public const string DefaultApiBaseUrl = "http://localhost:8080/api";

    public static string EnvironmentName =>
        ReadOverride("MYDEFENSE_ENV", "env") ?? "local";

    public static string ApiBaseUrl => NormalizeBaseUrl(
        ReadOverride("MYDEFENSE_API_BASE_URL", "apiBaseUrl") ?? DefaultApiBaseUrl);

    public static bool HasApiBaseUrlOverride =>
        !string.IsNullOrWhiteSpace(ReadOverride("MYDEFENSE_API_BASE_URL", "apiBaseUrl"));

    public static string PhotonFusionAppId =>
        ReadOverride("MYDEFENSE_PHOTON_APP_ID", "photonAppId");

    private static string ReadOverride(string environmentVariable, string commandLineName)
    {
        string commandLineValue = ReadCommandLine(commandLineName);
        if (!string.IsNullOrWhiteSpace(commandLineValue))
            return commandLineValue.Trim();

        string environmentValue = Environment.GetEnvironmentVariable(environmentVariable);
        return string.IsNullOrWhiteSpace(environmentValue)
            ? null
            : environmentValue.Trim();
    }

    private static string ReadCommandLine(string name)
    {
        string prefix = "-" + name + "=";
        foreach (string argument in Environment.GetCommandLineArgs())
        {
            if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return argument.Substring(prefix.Length);
        }

        return null;
    }

    private static string NormalizeBaseUrl(string value)
    {
        string normalized = value.Trim();
        while (normalized.EndsWith("/", StringComparison.Ordinal))
            normalized = normalized.Substring(0, normalized.Length - 1);
        return normalized;
    }
}
