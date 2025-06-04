using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 커서에 따라다니는 "유령 아이콘". Overlay / Screen‑Space‑Camera / World 모두 지원.
/// </summary>
[RequireComponent(typeof(Image))]
public class DragGhost : MonoBehaviour
{
    public static DragGhost Instance;

    Image img;
    RectTransform rt;
    Canvas canvas;

    void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(gameObject); return; }

        img = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        img.raycastTarget = false;   // 드래그 투과
        Hide();
        transform.SetAsLastSibling(); // Canvas 최상단
    }

    /* --------- API --------- */
    public void Show(Sprite s)
    {
        Debug.Log($"Ghost.Show sprite={(s != null ? s.name : "NULL")}");
        if (s == null) return;
        img.sprite = s;
        img.SetNativeSize();          // 0×0 방지
        img.color = Color.white;    // 알파 1 보장
        img.enabled = true;
    }

    public void Hide() => img.enabled = false;

    /* --------- 커서 추적 --------- */
    void LateUpdate()
    {
        if (!img.enabled) return;

        // Overlay 모드면 스크린 좌표 = UI 좌표이므로 바로 사용
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rt.position = Input.mousePosition;
        }
        else // Screen‑Space‑Camera 또는 World
        {
            Camera cam = canvas.worldCamera ?? Camera.main;
            if (cam == null) { rt.position = Input.mousePosition; return; }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                cam,
                out Vector2 pos);
            rt.anchoredPosition = pos;
        }
    }
}