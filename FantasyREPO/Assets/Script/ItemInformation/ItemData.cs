using UnityEngine;

/// <summary>
/// 모든 아이템의 원본 데이터를 정의하는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(fileName = "New ItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("필수 정보")]
    [Tooltip("절대로 중복되어서는 안 되는 고유 ID입니다. (예: IronSword_001)")]
    public string itemID;

    [Tooltip("게임 내에 표시될 아이템의 이름입니다. (예: 철 검)")]
    public string itemName;

    [Tooltip("인벤토리나 UI에 표시될 아이콘 이미지입니다.")]
    public Sprite itemIcon;

    [Header("추가 정보 (선택)")]
    [TextArea]
    [Tooltip("아이템에 대한 설명입니다.")]
    public string description;
    [Tooltip("아이템의 공격력입니다.")]
    public float attackPower;

    [Tooltip("아이템의 종류를 구분하기 위한 태그입니다. (예: Tool, Weapon, Potion)")]
    public string itemType;

    [Tooltip("한 슬롯에 최대로 겹칠 수 있는 개수입니다.")]
    [Min(1)]
    public int maxStack = 99;
}