using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;
    
    // 🔥 자신의 서버 주소로 수정 (localhost는 유니티 에디터 기준)
    private const string BaseUrl = "http://localhost:8080/api/game";

    void Awake() { Instance = this; DontDestroyOnLoad(gameObject); }

    // 일반적인 POST (소환 등)
    public void Post(string uri, WWWForm form, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(PostRequest(BaseUrl + uri, form, onSuccess, onError));
    }

    // JSON POST (머지 등 @RequestBody용)
    public void PostJson(string uri, string json, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(PostJsonRequest(BaseUrl + uri, json, onSuccess, onError));
    }

    IEnumerator PostRequest(string url, WWWForm form, Action<string> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) onError?.Invoke(www.error);
            else onSuccess?.Invoke(www.downloadHandler.text);
        }
    }

    IEnumerator PostJsonRequest(string url, string json, Action<string> onSuccess, Action<string> onError)
    {
        var www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success) onError?.Invoke(www.error);
        else onSuccess?.Invoke(www.downloadHandler.text);
    }
}