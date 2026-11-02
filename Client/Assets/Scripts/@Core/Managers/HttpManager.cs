using System;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Legacy callers share the same authenticated transport.</summary>
public class HttpManager : MonoBehaviour
{
    public static HttpManager Instance;
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public void PostGacha(string username, int count, Action<string> onSuccess)
        => NetworkManager.Instance.PostJson("/shop/gacha?username=" + UnityWebRequest.EscapeURL(username) + "&count=" + count, "{}", onSuccess, LogFailure);
    public void PostUpgrade(string username, int alienId, Action<string> onSuccess)
        => NetworkManager.Instance.PostJson("/aliens/" + alienId + "/upgrade?username=" + UnityWebRequest.EscapeURL(username), "{}", onSuccess, LogFailure);
    private static void LogFailure(string ignored) { Debug.LogWarning("요청을 완료하지 못했습니다. 로그인과 연결 상태를 확인해 주세요."); }
}
