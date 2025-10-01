using UnityEngine;
using UnityEngine.UI;

public class FillBar : MonoBehaviour
{
    [SerializeField] private Image fill;  // Fill 이미지 연결

    void Reset()
    {
        if (!fill)
            fill = transform.Find("Fill")?.GetComponent<Image>();
    }

    // t01 = 0~1 범위로 채워지는 정도
    public void SetFill(float t01)
    {
        if (fill)
        {
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = Mathf.Clamp01(t01);
        }
    }
}
