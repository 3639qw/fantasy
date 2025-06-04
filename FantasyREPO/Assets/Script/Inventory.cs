using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Inventory System v3
/// • Quick Slots 10 (keys 1–0) + Bag 50
/// • AddItem(icon, amt) → Quick stack ▶ Quick empty ▶ Bag stack ▶ Bag empty
/// • If quick slots are full, items go to bag automatically
/// • Drag & Drop, empty-slot transparency, selection alpha effect kept
/// </summary>
public class Inventory : MonoBehaviour
{
    /* ────────── Singleton ────────── */
    public static Inventory Instance { get; private set; }

    /* ────────── Slot Structure ────────── */
    [System.Serializable]
    public class ItemSlot
    {
        public Image icon;
        public TMP_Text countLabel;
        [HideInInspector] public int count;
    }

    /* ────────── Arrays ────────── */
    [Header("퀵 슬롯 10 (UI 순서대로 1~0)")]
    public ItemSlot[] quickSlots = new ItemSlot[10];

    [Header("가방 50")]
    public ItemSlot[] bagSlots = new ItemSlot[50];

    [Header("빈 슬롯 플레이스홀더")]
    [SerializeField] private Sprite emptySprite;
    public Sprite EmptySprite => emptySprite;

    [Header("Bag UI 패널")][SerializeField] private GameObject bagPanel;

    /* current selected quick slot (1‒10). 'states' kept for legacy scripts */
    [SerializeField, Range(1, 10)] private int current = 1;
    public int states => current;

    /* ────────── Awake ────────── */
    void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(gameObject); return; }

        AutoAttachDragScripts();   // add Drag / Drop scripts to slots
        NormalizeCounts();         // if icon exists but count 0 → count = 1
        InitSlotVisuals();         // refresh all slots (alpha / label)
    }

    /* ────────── Init helpers ────────── */
    void InitSlotVisuals()
    {
        foreach (var s in quickSlots) RefreshSlot(s);
        foreach (var s in bagSlots) RefreshSlot(s);
    }

    void NormalizeCounts()
    {
        FixArray(quickSlots);
        FixArray(bagSlots);
    }
    void FixArray(ItemSlot[] arr)
    {
        foreach (var s in arr)
            if (s.count == 0 && s.icon && s.icon.sprite && s.icon.sprite != emptySprite)
                s.count = 1;
    }

    /* ────────── Input ────────── */
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && bagPanel)
            bagPanel.SetActive(!bagPanel.activeSelf);

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectQuick(1);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectQuick(2);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectQuick(3);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectQuick(4);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectQuick(5);
        else if (Input.GetKeyDown(KeyCode.Alpha6)) SelectQuick(6);
        else if (Input.GetKeyDown(KeyCode.Alpha7)) SelectQuick(7);
        else if (Input.GetKeyDown(KeyCode.Alpha8)) SelectQuick(8);
        else if (Input.GetKeyDown(KeyCode.Alpha9)) SelectQuick(9);
        else if (Input.GetKeyDown(KeyCode.Alpha0)) SelectQuick(10);
    }

    void SelectQuick(int idx)
    {
        current = Mathf.Clamp(idx, 1, quickSlots.Length);
        for (int i = 0; i < quickSlots.Length; i++)
        {
            ItemSlot s = quickSlots[i];
            if (s.count > 0)
                SetAlpha(s.icon, i == current - 1);   // selected 1f, else 0.6f
            else
                s.icon.color = new Color(1, 1, 1, 0); // empty remains transparent
        }
    }

    /* ────────── Add / Stack ────────── */
    public void AddItem(Sprite icon, int amount = 1)
    {
        if (TryStackOrFill(quickSlots, icon, amount)) return;
        if (TryStackOrFill(bagSlots, icon, amount)) return;
        Debug.LogWarning("Inventory full (quick+bag)");
    }

    bool TryStackOrFill(ItemSlot[] arr, Sprite icon, int amount)
    {
        // 1) stack same icon
        foreach (var s in arr)
            if (s.count > 0 && s.icon.sprite == icon)
            { s.count += amount; RefreshSlot(s); return true; }

        // 2) find empty
        foreach (var s in arr)
            if (s.count == 0)
            { s.icon.sprite = icon; s.count = amount; RefreshSlot(s); return true; }
        return false;
    }

    /* ────────── Consume ────────── */
    public bool ConsumeSelectedItem(int amount = 1)
    {
        ItemSlot slot = quickSlots[current - 1];
        if (slot.count < amount) return false;
        slot.count -= amount;
        RefreshSlot(slot);
        return true;
    }

    /* ────────── Slot Visuals ────────── */
    public void RefreshSlot(ItemSlot s)
    {
        s.countLabel.text = s.count > 1 ? $"×{s.count}" : "";
        if (s.count == 0)
        {
            s.icon.sprite = emptySprite;
            s.icon.color = new Color(1, 1, 1, 0);
        }
        else s.icon.color = Color.white;
    }

    void SetAlpha(Image img, bool selected)
    {
        if (!img) return;
        Color c = img.color; c.a = selected ? 1f : 0.6f; img.color = c;
    }

    /* ────────── Drag & Drop Helpers ────────── */
    public ItemSlot FindSlotByIcon(Image icon)
    {
        foreach (var q in quickSlots) if (q.icon == icon) return q;
        foreach (var b in bagSlots) if (b.icon == icon) return b;
        return null;
    }

    /* ────────── Auto Attach Drag Scripts ────────── */
    void AutoAttachDragScripts()
    {
        Attach(quickSlots);
        Attach(bagSlots);
    }
    void Attach(ItemSlot[] arr)
    {
        foreach (var s in arr)
        {
            if (!s.icon) continue;
            GameObject go = s.icon.gameObject;
            if (!go.TryGetComponent(out CanvasGroup cg)) cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            if (!go.GetComponent<SlotDrag>()) go.AddComponent<SlotDrag>();
            if (!go.GetComponent<SlotDropTarget>()) go.AddComponent<SlotDropTarget>();
        }
    }

    /* ────────── Helpers ────────── */
    public Sprite GetSelectedSprite() => quickSlots[current - 1].icon.sprite;
    public bool IsSelectedEmpty() => quickSlots[current - 1].count == 0;
}
