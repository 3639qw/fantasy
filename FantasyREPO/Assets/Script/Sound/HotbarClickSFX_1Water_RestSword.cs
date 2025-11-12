// HotbarClickSFX_Slot1Water_Strict.cs
using UnityEngine;

public class HotbarClickSFX_Slot1Water_Strict : MonoBehaviour
{
    [Header("현재 선택된 슬롯(1~9) + 그 슬롯의 ItemData (외부에서 갱신)")]
    public int currentSlot = 1;
    public ItemData currentItem;  // 퀵슬롯 매니저가 선택 아이템을 넘겨주세요

    [Header("물뿌리개 판단 키워드(아이템의 ID/Name/Type에 포함되면 물로 판단)")]
    public string[] wateringKeywords = { "water", "watering", "wateringcan" };

    [Header("중복 방지(홀드/연타 보호)")]
    public float minInterval = 0.08f;
    float _last;

    [Header("테스트용: 숫자키로 슬롯 전환 허용")]
    public bool allowNumberKeysToSwitch = false;

    void Update()
    {
        if (allowNumberKeysToSwitch)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) currentSlot = 1;
            if (Input.GetKeyDown(KeyCode.Alpha2)) currentSlot = 2;
            if (Input.GetKeyDown(KeyCode.Alpha3)) currentSlot = 3;
            if (Input.GetKeyDown(KeyCode.Alpha4)) currentSlot = 4;
            if (Input.GetKeyDown(KeyCode.Alpha5)) currentSlot = 5;
            if (Input.GetKeyDown(KeyCode.Alpha6)) currentSlot = 6;
            if (Input.GetKeyDown(KeyCode.Alpha7)) currentSlot = 7;
            if (Input.GetKeyDown(KeyCode.Alpha8)) currentSlot = 8;
            if (Input.GetKeyDown(KeyCode.Alpha9)) currentSlot = 9;
        }

        if (!Input.GetMouseButtonDown(0)) return;
        if (Time.time - _last < minInterval) return;
        _last = Time.time;

        // 1번 슬롯 + 실제 아이템이 '물뿌리개'일 때만 WateringCan
        if (currentSlot == 1 && IsWateringCan(currentItem))
        {
            SoundManager.Instance?.PlayToolSFX(ToolType.WateringCan);
        }
        else
        {
            // 나머지는 전부 검 소리
            SoundManager.Instance?.PlayToolSFX(ToolType.Sword);
            // 필요하면 공격 전용 추가타:
            // SoundManager.Instance?.PlayAttackSFX();
        }
    }

    bool IsWateringCan(ItemData item)
    {
        if (!item) return false;
        string id = (item.itemID ?? "").ToLowerInvariant();
        string name = (item.itemName ?? "").ToLowerInvariant();
        string type = (item.itemType ?? "").ToLowerInvariant();

        foreach (var key in wateringKeywords)
        {
            var k = key.ToLowerInvariant();
            if (id.Contains(k) || name.Contains(k) || type.Contains(k))
                return true;
        }
        return false;
    }

    // 퀵슬롯 매니저에서 호출할 편의 메서드(선택)
    public void SetCurrent(int slot1Based, ItemData item)
    {
        currentSlot = Mathf.Clamp(slot1Based, 1, 9);
        currentItem = item;
    }
}
