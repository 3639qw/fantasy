using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DragGhost : MonoBehaviour
{
    public static DragGhost Instance;

    Image img;
    RectTransform rt;
    Canvas canvas;

    Image srcIcon;
    Color srcPrevColor;

    /* ───────────── 초기화 ───────────── */
    void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(gameObject); return; }

        img = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        img.raycastTarget = false;
        transform.SetAsLastSibling();
        Hide();
    }

    /* ───────────── 드래그 시작 ───────────── */
    public void Show(Image source)
    {
        if (!source || !source.sprite) return;

        // 1) 원본 투명화
        srcIcon = source;
        srcPrevColor = source.color;
        srcIcon.color = new Color(srcPrevColor.r, srcPrevColor.g, srcPrevColor.b, 0f);

        // 2) 유령 아이콘 설정
        img.sprite = source.sprite;
        img.SetNativeSize();
        if (rt.rect.width < 10f)       // 0×0 방지 (아이콘이 작을 때)
            rt.sizeDelta = new Vector2(64, 64);

        img.color = Color.white;
        img.enabled = true;
    }

    /* ───────────── 드래그 종료 ───────────── */
    public void Hide()
    {
        if (srcIcon)
        {
            // 이미 emptySprite 로 바뀐 슬롯이라면 계속 투명 유지
            Sprite empty = Inventory.Instance.EmptySprite;
            bool isEmptySprite = (srcIcon.sprite == empty);
            srcIcon.color = isEmptySprite ? new Color(1, 1, 1, 0) : srcPrevColor;
        }
        srcIcon = null;
        img.enabled = false;
    }

    /* ───────────── 커서 추적 ───────────── */
    void LateUpdate()
    {
        if (!img.enabled) return;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            rt.position = Input.mousePosition;
        else
        {
            Camera cam = canvas.worldCamera ?? Camera.main;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, Input.mousePosition, cam, out Vector2 pos);
            rt.anchoredPosition = pos;
        }
    }
}
