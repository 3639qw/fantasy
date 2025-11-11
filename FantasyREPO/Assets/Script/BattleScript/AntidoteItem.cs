using UnityEngine;

public class AntidoteItem : MonoBehaviour
{
    // 이 아이템을 플레이어가 사용했을 때
    public void OnUseItem(ItemData itemToUse, GameObject player)
    {
        if (itemToUse == null || player == null) return;

        // 1. 플레이어의 StatusCondition 스크립트를 가져옵니다.
        StatusCondition playerStatus = player.GetComponent<StatusCondition>();
        if (playerStatus == null)
        {
            Debug.LogError("아이템을 사용하려 했으나 플레이어에 StatusCondition 스크립트가 없습니다.");
            return;
        }

        // 2. [핵심] ItemData의 'curesStatusEffect' 값을 확인합니다.
        switch (itemToUse.curesStatusEffect)
        {
            case CuresStatusEffect.None:
                // 아무것도 안 함 (혹은 다른 효과, 예: 체력 회복 처리)
                // if (itemToUse.healAmount > 0) { ... }
                break;

            case CuresStatusEffect.Poison:
                playerStatus.CurePoison();
                break;

            case CuresStatusEffect.Bleeding:
                playerStatus.CureBleeding();
                break;

            case CuresStatusEffect.Slow:
                playerStatus.CureSlow();
                break;

            case CuresStatusEffect.All:
                playerStatus.CureAll();
                break;
        }

    }
}