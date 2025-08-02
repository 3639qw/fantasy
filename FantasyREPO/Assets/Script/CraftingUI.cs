// Assets/Scripts/CraftingUI.cs
using UnityEngine;
using UnityEngine.UI;

public class CraftingUI : MonoBehaviour
{
    /* ───── UI ───── */
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;   // 제작창 루트
    [SerializeField] private Button     craftBtn;    // 제작 버튼

    /* ───── 레시피 ───── */
    [Header("Ingredient / Product")]
    [SerializeField] private Sprite ingredientIcon;  // 재료 (예: 밀)
    [SerializeField] private int    ingredientNeed = 3;
    [SerializeField] private Sprite productIcon;     // 결과 (예: 빵)
    [SerializeField] private int    productGive  = 1;

    /* ───── 참조 ───── */
    [Header("Refs (선택)")]
    [SerializeField] private Inventory inventory;    // Inspector에 안 넣으면 Singleton / Find로 대체

    /* ─────────────── */

    private void Awake()
    {
        /* Inventory 참조 확보 (3-단 안전장치) */
        if (inventory == null)            inventory = Inventory.Instance;
        if (inventory == null)            inventory = FindObjectOfType<Inventory>();

        panelRoot.SetActive(false);
        craftBtn.onClick.AddListener(Craft);
    }

    private void Update()
    {
        /* 패널 토글 */
        if (Input.GetKeyDown(KeyCode.R))
            panelRoot.SetActive(!panelRoot.activeSelf);

        /* 재료 부족 시 버튼 비활성 */
        if (inventory != null)
            craftBtn.interactable = inventory.HasItem(ingredientIcon, ingredientNeed);
    }

    private void Craft()
    {
        if (inventory == null) return;
        if (!inventory.HasItem(ingredientIcon, ingredientNeed)) return;

        inventory.RemoveItem(ingredientIcon, ingredientNeed); // 재료 차감
        inventory.AddItem   (productIcon,   productGive );    // 결과 지급
        Debug.Log("제작 완료!");
    }
}
