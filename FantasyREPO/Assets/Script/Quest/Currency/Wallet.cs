using System;
using UnityEngine;

public class Wallet : MonoBehaviour
{
    public static Wallet Instance { get; private set; }
    public event Action<int> OnGoldChanged;

    [SerializeField] private int startGold = 0;
    private const string PlayerPrefsKey = "PLAYER_GOLD";
    public int Gold { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Gold = PlayerPrefs.GetInt(PlayerPrefsKey, startGold);
        OnGoldChanged?.Invoke(Gold);
    }

    public bool CanSpend(int amount) => amount >= 0 && Gold >= amount;

    public bool TrySpend(int amount)
    {
        if (!CanSpend(amount)) return false;
        Gold -= amount;
        PlayerPrefs.SetInt(PlayerPrefsKey, Gold);
        OnGoldChanged?.Invoke(Gold);
        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        PlayerPrefs.SetInt(PlayerPrefsKey, Gold);
        OnGoldChanged?.Invoke(Gold);
    }

#if UNITY_EDITOR
    // 테스트용 치트: G키로 100골드 추가
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) Add(100);
    }
#endif
}
