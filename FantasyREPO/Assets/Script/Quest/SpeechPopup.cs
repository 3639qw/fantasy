// 추가: using UnityEngine; using TMPro; using UnityEngine.UI; 유지
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SpeechPopup : MonoBehaviour
{
    public RectTransform root;
    public TMP_Text label;
    public Image bubble;

    public Vector2 worldOffset = new Vector2(0f, 1.6f);
    public float floatUp = 0.5f;
    public float duration = 1.6f;
    public float popScale = 1.1f;

    CanvasGroup cg;
    Transform followTarget;
    Canvas canvas;                           // ✅ 캔버스 캐시

    void Awake()
    {
        if (!root) root = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        canvas = GetComponent<Canvas>();     // ✅ World Space Canvas
    }

    public void Play(Transform target, string text, float? dur = null, Vector2? offset = null)
    {
        followTarget = target;
        if (label) label.text = text;
        if (dur.HasValue) duration = dur.Value;
        if (offset.HasValue) worldOffset = offset.Value;

        // ✅ 매번 카메라/알파 리셋
        if (canvas && canvas.renderMode == RenderMode.WorldSpace)
            canvas.worldCamera = Camera.main;
        cg.alpha = 1f;

        StopAllCoroutines();
        StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        root.localScale = Vector3.one * popScale;

        Vector3 wStart = followTarget.position + (Vector3)worldOffset;
        Vector3 wEnd = wStart + new Vector3(0f, floatUp, 0f);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            root.position = Vector3.Lerp(wStart, wEnd, Mathf.SmoothStep(0, 1, k));

            // 페이드 인/아웃
            float a = (k < 0.2f) ? Mathf.InverseLerp(0f, 0.2f, k)
                     : (k > 0.8f) ? Mathf.InverseLerp(1f, 0.8f, k)
                     : 1f;
            cg.alpha = a;

            root.localScale = Vector3.Lerp(Vector3.one * popScale, Vector3.one, Mathf.SmoothStep(0, 1, k));
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
