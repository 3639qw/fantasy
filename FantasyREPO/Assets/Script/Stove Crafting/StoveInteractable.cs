// Assets/Scripts/Crafting/StoveInteractable.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StoveInteractable : MonoBehaviour
{
    [SerializeField] private StoveUI stoveUI;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode openKey = KeyCode.P;   // 👉 P 키로 변경

    private bool playerInRange;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Awake()
    {
        if (!stoveUI)
        {
            stoveUI = GetComponentInChildren<StoveUI>(true);
            if (!stoveUI)
                Debug.LogWarning("[StoveInteractable] StoveUI 참조 없음");
        }
    }

    void Update()
    {
        if (!playerInRange || stoveUI == null) return;

        if (Input.GetKeyDown(openKey))
        {
            stoveUI.TogglePanel();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            stoveUI.ClosePanel();
        }
    }
}
