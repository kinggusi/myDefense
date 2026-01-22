using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class HttpManager : MonoBehaviour
{
    public static HttpManager Instance;
    
    // 서버 주소 (자신의 환경에 맞게)
    private const string BASE_URL = "http://localhost:8080/api";

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 1. [상점] 가챠 요청
    public void PostGacha(string username, int count, Action<string> onSuccess)
    {
        string url = $"{BASE_URL}/shop/gacha?username={username}&count={count}";
        StartCoroutine(SendPostRequest(url, onSuccess));
    }

    // 2. [인벤] 왹져 강화 요청
    public void PostUpgrade(string username, int alienId, Action<string> onSuccess)
    {
        string url = $"{BASE_URL}/aliens/{alienId}/upgrade?username={username}";
        StartCoroutine(SendPostRequest(url, onSuccess));
    }

    IEnumerator SendPostRequest(string url, Action<string> callback)
    {
        // POST는 보내는 데이터가 없어도 body가 필요하긴 함 (여기선 빈값)
        using (UnityWebRequest req = UnityWebRequest.Post(url, "", "application/json"))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[Http Success] {req.downloadHandler.text}");
                callback?.Invoke(req.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"[Http Error] {req.error} : {req.downloadHandler.text}");
            }
        }
    }
}