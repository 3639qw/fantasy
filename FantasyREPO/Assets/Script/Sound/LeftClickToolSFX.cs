// LeftClickToolSFXHotbarOverride_Except1.cs
using UnityEngine;

public class LeftClickToolSFXHotbarOverride_Except1 : MonoBehaviour
{
    [Header("현재 선택된 슬롯 (1-based)")]
    public int currentSlot = 1;

    [Header("테스트용: 숫자키로 슬롯 전환 허용")]
    public bool allowNumberKeysToSwitch = true;

    [Header("중복 방지(홀드/연타 보호)")]
    public float minInterval = 0.08f;
    float _last;

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

        // 1번 슬롯: 아무 동작/사운드 없음 (다른 시스템에 맡김)
        if (currentSlot == 1) return;

        // 2~5번 슬롯만 swing(검) 사운드 재생
        if (currentSlot >= 2 && currentSlot <= 5)
        {
            SoundManager.Instance?.PlayToolSFX(ToolType.Sword);
            // 필요 시: SoundManager.Instance?.PlayAttackSFX();
        }
        // 6번 이상: 소리 없음
    }

    public void SetCurrentSlot(int slot1Based) => currentSlot = Mathf.Max(1, slot1Based);
}
