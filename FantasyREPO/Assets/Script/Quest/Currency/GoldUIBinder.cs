using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class GoldUIBinder : MonoBehaviour
{
    public enum DimMode { ExternalManual, AutoSizeToText }

    [Header("Refs")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private Image dimPanel; // 수동 모드에서도 할당만 되어 있으면 OK

    [Header("Mode")]
    [SerializeField] private DimMode dimMode = DimMode.ExternalManual; // 기본: 수동 DIM

    [Header("Text")]
    [SerializeField] private bool setYellowText = true;

    [Header("Auto-Size Options (DimMode = AutoSizeToText 일 때만 사용)")]
    [SerializeField] private Vector2 padding = new Vector2(16f, 8f);
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.55f;
    [SerializeField] private Color dimColor = Color.black;
    [SerializeField] private bool forceDimBehindText = true;

    private void Awake()
    {
        if (goldText == null) goldText = GetComponentInChildren<TMP_Text>(true);
        if (dimMode == DimMode.AutoSizeToText) ApplyDimStyle();
    }

    private void OnEnable() { TryHookAndRefresh(); }
    private void Start() { TryHookAndRefresh(); }

    private void OnDisable()
    {
        if (Wallet.Instance != null)
            Wallet.Instance.OnGoldChanged -= Refresh;
    }

    private void TryHookAndRefresh()
    {
        if (Wallet.Instance != null)
        {
            Wallet.Instance.OnGoldChanged -= Refresh;
            Wallet.Instance.OnGoldChanged += Refresh;
            Refresh(Wallet.Instance.Gold);
        }
        else
        {
            goldText.text = "0 G";
            if (dimMode == DimMode.AutoSizeToText) ResizeDimToText();
        }

        if (setYellowText)
            goldText.color = new Color32(255, 217, 59, 255);
    }

    private void Refresh(int value)
    {
        goldText.text = $"{value:n0} G";
        if (dimMode == DimMode.AutoSizeToText) ResizeDimToText();
    }

    // ===== Auto DIM =====
    private void ApplyDimStyle()
    {
        if (dimPanel == null) return;
        var c = dimPanel.color;
        dimPanel.color = new Color(dimColor.r, dimColor.g, dimColor.b, dimAlpha);
        dimPanel.raycastTarget = false;
        ResizeDimToText();
    }

    private void ResizeDimToText()
    {
        if (dimPanel == null || goldText == null) return;

        goldText.ForceMeshUpdate();
        Vector2 size = goldText.GetRenderedValues(false);
        if (size.x < 1f || size.y < 1f)
            size = goldText.GetPreferredValues(goldText.text);

        var rt = dimPanel.rectTransform;
        rt.sizeDelta = size + padding * 2f;

        if (forceDimBehindText &&
            dimPanel.transform.GetSiblingIndex() > goldText.transform.GetSiblingIndex())
        {
            dimPanel.transform.SetSiblingIndex(0); // 같은 부모에서 뒤로
        }
    }
}
