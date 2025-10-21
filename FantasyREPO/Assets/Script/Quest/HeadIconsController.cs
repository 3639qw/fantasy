using UnityEngine;
using UnityEngine.UI;

public class HeadIconsController : MonoBehaviour
{
    [Header("World-Space Icons")]
    public Image bubbleIcon;           // 말풍선
    public Image checkIcon;            // V표시

    [Header("Offsets (world units)")]
    public Vector2 bubbleOffset = new Vector2(-0.10f, 0.30f); // ← 왼쪽/위 오프셋
    public Vector2 checkOffset = new Vector2(0.12f, 0.28f); // ← 오른쪽/위 오프셋

    SpriteRenderer[] spriteRenderers;

    void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        // 안전: 캔버스 월드스페이스/소팅 보정
        if (bubbleIcon) SetupCanvas(bubbleIcon);
        if (checkIcon) SetupCanvas(checkIcon);
    }

    void SetupCanvas(Image img)
    {
        var canvas = img.GetComponentInParent<Canvas>();
        if (!canvas) return;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        if (string.IsNullOrEmpty(canvas.sortingLayerName)) canvas.sortingLayerName = "UI";
        canvas.sortingOrder = 200;
        var rt = canvas.GetComponent<RectTransform>();
        rt.localScale = Vector3.one * 0.01f;
        if (rt.sizeDelta == Vector2.zero) rt.sizeDelta = new Vector2(96, 96);
    }

    void LateUpdate()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) return;

        // 스프라이트 상단 좌표 계산
        var bounds = new Bounds(transform.position, Vector3.zero);
        foreach (var r in spriteRenderers) bounds.Encapsulate(r.bounds);
        var head = new Vector3(bounds.center.x, bounds.max.y, transform.position.z);

        // 각 아이콘을 개별 오프셋으로 배치
        if (bubbleIcon)
            bubbleIcon.transform.position = head + (Vector3)bubbleOffset;

        if (checkIcon)
            checkIcon.transform.position = head + (Vector3)checkOffset;
    }

    // 외부(예: NPCQuestGiver)에서 상태별 토글 호출용
    public void ShowBubble(bool show)
    {
        if (!bubbleIcon) return;
        bubbleIcon.enabled = show && bubbleIcon.sprite != null;
    }
    public void ShowCheck(bool show)
    {
        if (!checkIcon) return;
        checkIcon.enabled = show && checkIcon.sprite != null;
    }
}
