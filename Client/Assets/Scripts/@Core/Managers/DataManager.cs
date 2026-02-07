using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    public int Diamond;   // 상점용 다이아
    public int UserGold;  // 강화용 골드

    // 내 왹져 목록 (Key: AlienId)
    public Dictionary<int, UserAlienData> MyInventory = new Dictionary<int, UserAlienData>();

    // UI 갱신용 이벤트
    public event System.Action OnInventoryUpdated;
    public event System.Action OnCurrencyUpdated;
    void Awake() { Instance = this; DontDestroyOnLoad(gameObject); }

    // [가챠용] 리스트 통째로 갱신
    public void UpdateInventoryList(string json)
    {
        var list = JsonHelper.FromJsonList<UserAlienData>(json);
        foreach (var data in list)
        {
            UpdateLocalData(data);
        }
        OnInventoryUpdated?.Invoke(); // UI 갱신
    }

    // [강화용] 하나만 갱신
    public void UpdateSingleAlien(string json)
    {
        var data = JsonUtility.FromJson<UserAlienData>(json);
        UpdateLocalData(data);
        OnInventoryUpdated?.Invoke(); // UI 갱신
    }

    // 내부 갱신 로직
    private void UpdateLocalData(UserAlienData data)
    {
        if (MyInventory.ContainsKey(data.alienId))
            MyInventory[data.alienId] = data;
        else
            MyInventory.Add(data.alienId, data);
    }

    // 데이터 가져오기 (없으면 깡통 리턴)
    public UserAlienData GetUserAlien(int alienId)
    {
        if (MyInventory.ContainsKey(alienId)) return MyInventory[alienId];
        return new UserAlienData { alienId = alienId, level = 0, pieces = 0 };
    }

    public void UpdateUserInfo(int diamond, int gold)
    {
        this.Diamond = diamond;
        this.UserGold = gold;
        OnCurrencyUpdated?.Invoke(); // UI 갱신
    }
}