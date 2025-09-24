// Assets/Scripts/Crafting/AnvilInteraction.cs
using UnityEngine;

public class AnvilInteraction : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CraftingUI craftingUI; // 비워두면 자동 탐색
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange = false;

    private void Awake()
    {
        if (!craftingUI) craftingUI = FindObjectOfType<CraftingUI>();
        var col = GetComponent<Collider2D>();
        if (!col)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            (col as BoxCollider2D).isTrigger = true;
        }
        else col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            craftingUI?.ClosePanel(); // 범위 벗어나면 자동 닫기
        }
    }

    private void Update()
    {
        if (!playerInRange) return;

        // O 키로 열고/닫기
        if (Input.GetKeyDown(KeyCode.O))
            craftingUI?.TogglePanel();
    }
}
