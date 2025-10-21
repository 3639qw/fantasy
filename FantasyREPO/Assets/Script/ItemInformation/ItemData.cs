// Assets/Scripts/Inventory/ItemData.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New ItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("필수 정보")]
    [Tooltip("절대로 중복되지 않는 고유 ID (예: IronOre_001)")]
    public string itemID;

    [Tooltip("게임 내 표시 이름")]
    public string itemName;

    [Tooltip("인벤/UI 아이콘")]
    public Sprite itemIcon;

    [Header("추가 정보 (선택)")]
    [TextArea] public string description;
    public float attackPower;
    [Tooltip("Tool / Weapon / Potion 등")] public string itemType;

    [Tooltip("슬롯당 최대 겹침 수")]
    [Min(1)] public int maxStack = 99;

    /* ===========================
     * 정적 레지스트리 (Sprite/ID -> ItemData)
     * =========================== */
    private static Dictionary<Sprite, ItemData> s_bySprite;
    private static Dictionary<string, ItemData> s_byId;
    private static bool s_initialized;

    /// <summary>아이콘으로 ItemData 찾기 (없으면 null)</summary>
    public static ItemData FindBySprite(Sprite icon)
    {
        EnsureIndex();
        if (!icon) return null;
        return s_bySprite.TryGetValue(icon, out var data) ? data : null;
    }

    /// <summary>ID로 ItemData 찾기 (없으면 null)</summary>
    public static ItemData FindById(string id)
    {
        EnsureIndex();
        if (string.IsNullOrEmpty(id)) return null;
        return s_byId.TryGetValue(id, out var data) ? data : null;
    }

    /// <summary>Resources 내 모든 ItemData를 스캔해 인덱스 작성</summary>
    private static void EnsureIndex()
    {
        if (s_initialized) return;

        s_bySprite = new Dictionary<Sprite, ItemData>();
        s_byId = new Dictionary<string, ItemData>();

        // ⚠️ 모든 ItemData 에셋을 Resources 폴더(하위 아무 위치) 안에 두세요.
        var all = Resources.LoadAll<ItemData>("");
        foreach (var it in all)
        {
            if (it == null) continue;

            if (!string.IsNullOrEmpty(it.itemID))
            {
                if (!s_byId.ContainsKey(it.itemID))
                    s_byId.Add(it.itemID, it);
                else
                    Debug.LogWarning($"[ItemData] 중복 itemID 발견: {it.itemID} ({it.name})");
            }

            if (it.itemIcon && !s_bySprite.ContainsKey(it.itemIcon))
                s_bySprite.Add(it.itemIcon, it);
        }

        s_initialized = true;
        Debug.Log($"[ItemData] Indexed {all.Length} items (Resources).");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxStack < 1) maxStack = 1;
    }
#endif
}
