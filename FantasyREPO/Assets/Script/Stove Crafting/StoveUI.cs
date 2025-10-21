// Assets/Scripts/Crafting/StoveUI.cs
using UnityEngine;
using UnityEngine.UI;

public class StoveUI : MonoBehaviour
{
    [Header("UI Roots")]
    [SerializeField] private GameObject panelRoot;

    [Header("Buttons")]
    [SerializeField] private Button startBtn;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private Button clearBtn;

    [Header("Slots")]
    [SerializeField] private CraftingSlotDropTarget ingredientSlotA;
    [SerializeField] private CraftingSlotDropTarget ingredientSlotB;
    [SerializeField] private Image resultPreview;      // 미리보기(완성 예상 아이콘)
    [SerializeField] private Sprite emptySprite;

    [Header("Progress UI (Image Type = Filled)")]
    [SerializeField] private Image progressFill;       // Fill 이미지 (fillAmount 0~1)
    [SerializeField] private TMPro.TMP_Text timeLabel; // 남은 시간 표기(옵션)

    [Header("Recipes")]
    [SerializeField] private CookRecipeSO[] recipes;

    [Header("Refs")]
    [SerializeField] private Inventory inventory;      // 비워두면 자동

    // ────────────────────────────────────────────────
    // 내부 상태
    private CookRecipeSO match;
    private bool swapped;
    private bool cooking;
    private float cookTime;    // 총 시간(초)
    private float cookTimer;   // 진행 시간(초)

    // 열려 있는 StoveUI 전역 관리 (중복 방지)
    public static StoveUI CurrentlyOpen { get; private set; }

    void Awake()
    {
        if (!inventory) inventory = Inventory.Instance ?? FindObjectOfType<Inventory>();

        if (panelRoot) panelRoot.SetActive(false);

        if (startBtn) startBtn.onClick.AddListener(StartCook);
        if (cancelBtn) cancelBtn.onClick.AddListener(CancelCook);
        if (clearBtn) clearBtn.onClick.AddListener(ClearAll);

        // 진행바 기본값/타입 보정
        EnsureProgressImageIsFilled();
        SetPreviewEmpty();
        UpdateProgressUI(0f, 0f);
    }

    void OnDestroy()
    {
        if (startBtn) startBtn.onClick.RemoveListener(StartCook);
        if (cancelBtn) cancelBtn.onClick.RemoveListener(CancelCook);
        if (clearBtn) clearBtn.onClick.RemoveListener(ClearAll);

        if (CurrentlyOpen == this) CurrentlyOpen = null;
    }

    void Update()
    {
        if (!panelRoot || !panelRoot.activeSelf) return;

        // 요리 중
        if (cooking)
        {
            cookTimer += Time.deltaTime;
            float t = Mathf.Clamp01(cookTimer / Mathf.Max(0.01f, cookTime));
            UpdateProgressUI(t, Mathf.Max(0f, cookTime - cookTimer));

            if (cookTimer >= cookTime)
            {
                CompleteCook();
            }

            if (startBtn) startBtn.interactable = false;
            if (clearBtn) clearBtn.interactable = false;
            if (cancelBtn) cancelBtn.interactable = true;
            return;
        }

        // 요리 전: 매칭 시도 / 버튼 상태 갱신
        TryMatch();
        if (startBtn) startBtn.interactable = CanStart();
        if (clearBtn) clearBtn.interactable = true;
        if (cancelBtn) cancelBtn.interactable = false;

        if (Input.GetKeyDown(KeyCode.Escape)) ClosePanel();
    }

    private bool CanStart()
    {
        return inventory && match && resultPreview && resultPreview.sprite != emptySprite;
    }

    private void TryMatch()
    {
        match = null; swapped = false;

        if (!ingredientSlotA || !ingredientSlotB || !resultPreview || recipes == null || recipes.Length == 0)
        {
            SetPreviewEmpty();
            return;
        }

        var aSpr = ingredientSlotA.IsEmpty() ? null : ingredientSlotA.CurrentSprite;
        var bSpr = ingredientSlotB.IsEmpty() ? null : ingredientSlotB.CurrentSprite;
        var aCnt = ingredientSlotA.CurrentCount;
        var bCnt = ingredientSlotB.CurrentCount;

        if (aSpr == null && bSpr == null) { SetPreviewEmpty(); return; }

        CookRecipeSO best = null; bool bestSwap = false;
        int bestScore = -1, bestTie = -1;

        foreach (var r in recipes)
        {
            if (!r) continue;
            if (r.IsMatch(aSpr, aCnt, bSpr, bCnt, out var sw))
            {
                int cA = Mathf.Max(0, r.countA);
                int cB = Mathf.Max(0, r.countB);
                int score = cA + cB;
                int tie = Mathf.Max(cA, cB);

                if (score > bestScore || (score == bestScore && tie > bestTie))
                {
                    best = r; bestSwap = sw; bestScore = score; bestTie = tie;
                }
            }
        }

        match = best; swapped = bestSwap;

        if (match && match.output)
        {
            resultPreview.sprite = match.output;
            resultPreview.color = Color.white;
        }
        else
        {
            SetPreviewEmpty();
        }
    }

    private void StartCook()
    {
        if (!CanStart()) return;

        // 재료 소모 (시작 시)
        if (match.inputB == null || match.countB <= 0)
        {
            if (!swapped) ingredientSlotA.Consume(match.countA);
            else ingredientSlotB.Consume(match.countA);
        }
        else
        {
            if (!swapped)
            {
                ingredientSlotA.Consume(match.countA);
                ingredientSlotB.Consume(match.countB);
            }
            else
            {
                ingredientSlotA.Consume(match.countB);
                ingredientSlotB.Consume(match.countA);
            }
        }

        cooking = true;
        cookTime = Mathf.Max(0.1f, match.timeSeconds);
        cookTimer = 0f;
        UpdateProgressUI(0f, cookTime);

        Debug.Log($"[Stove] 요리 시작: {match.output?.name} ({cookTime:0.0}s)");
    }

    private void CompleteCook()
    {
        cooking = false;

        if (inventory && match && match.output)
        {
            // result sprite → ItemData 매핑 (ItemData.FindBySprite 구현되어 있어야 함)
            var outData = ItemData.FindBySprite(match.output);
            if (outData != null)
            {
                inventory.AddItem(outData, Mathf.Max(1, match.outputCount));   // ✅ ItemData 전달
            }
            else
            {
                Debug.LogWarning("[StoveUI] 결과 아이콘과 매칭되는 ItemData가 없습니다. Resources에 ItemData를 배치했는지 확인하세요.");
            }
        }

        Debug.Log($"[Stove] 요리 완료: {match.output?.name} x{match.outputCount}");

        SetPreviewEmpty();
        match = null;
        UpdateProgressUI(0f, 0f);
    }

    private void CancelCook()
    {
        if (!cooking) return;
        cooking = false;
        Debug.Log("[Stove] 요리 취소");
        TryMatch();
        UpdateProgressUI(0f, 0f);
    }

    public void ClearAll()
    {
        if (cooking) return;
        ingredientSlotA?.Clear();
        ingredientSlotB?.Clear();
        SetPreviewEmpty();
        match = null;
    }

    private void SetPreviewEmpty()
    {
        if (!resultPreview) return;
        resultPreview.sprite = emptySprite;
        var c = resultPreview.color; c.a = 0.6f; resultPreview.color = c;
    }

    private void UpdateProgressUI(float t01, float remain)
    {
        if (progressFill)
        {
            // Filled 타입/세팅 보정(실행 중 변경돼도 안전)
            if (progressFill.type != Image.Type.Filled)
                progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0; // Left
            progressFill.fillAmount = Mathf.Clamp01(t01);
        }

        if (timeLabel) timeLabel.text = (remain > 0f) ? $"{remain:0.0}s" : "";
    }

    private void EnsureProgressImageIsFilled()
    {
        if (!progressFill) return;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0; // Left
        progressFill.fillAmount = 0f;
        // 보이도록 알파 1로 보정(스프라이트 자체 알파가 0이면 보이지 않음)
        var c = progressFill.color; if (c.a < 1f) { c.a = 1f; progressFill.color = c; }
    }

    // ────────────────────────────────────────────────
    // UI 열기/닫기 (중복 방지 포함)
    public void TogglePanel()
    {
        if (!panelRoot) return;

        bool wantOpen = !panelRoot.activeSelf;
        if (wantOpen)
        {
            if (CurrentlyOpen && CurrentlyOpen != this)
                CurrentlyOpen.ClosePanel();

            panelRoot.SetActive(true);
            CurrentlyOpen = this;
        }
        else
        {
            ClosePanel();
        }
    }

    public void OpenPanel()
    {
        if (!panelRoot || panelRoot.activeSelf) return;

        if (CurrentlyOpen && CurrentlyOpen != this)
            CurrentlyOpen.ClosePanel();

        panelRoot.SetActive(true);
        CurrentlyOpen = this;
    }

    public void ClosePanel()
    {
        if (!panelRoot || !panelRoot.activeSelf) return;

        panelRoot.SetActive(false);
        if (CurrentlyOpen == this) CurrentlyOpen = null;
    }
}
