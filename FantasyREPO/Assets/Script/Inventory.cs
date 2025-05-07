using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    static public Inventory instance = null;

    [Header("인벤토리 아이템")]
    public Image hand1; // 1번 슬롯 (양동이)
    public Image hand2; // 2번 슬롯 (삽)
    public Image hand3; // 3번 슬롯 (빈손)

    [Header("플레이어 손 오브젝트")]
    public GameObject water; // Hand/water
    public GameObject axe;   // Hand/axe

    protected internal int states = 1;

    public static Inventory Instance
    {
        get
        {
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;

        SetHandAlpha(states);
        ApplyHandItem(states);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            states = 1;
            SetHandAlpha(1);
            ApplyHandItem(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            states = 2;
            SetHandAlpha(2);
            ApplyHandItem(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            states = 3;
            SetHandAlpha(3);
            ApplyHandItem(3);
        }
    }

    private void SetHandAlpha(int handIndex)
    {
        SetAlpha(hand1, handIndex == 1 ? 1f : 60f / 255f);
        SetAlpha(hand2, handIndex == 2 ? 1f : 60f / 255f);
        SetAlpha(hand3, handIndex == 3 ? 1f : 60f / 255f);
    }

    private void SetAlpha(Image renderer, float alpha)
    {
        Color c = renderer.color;
        c.a = alpha;
        renderer.color = c;
    }

    private void ApplyHandItem(int index)
    {
        water.SetActive(false);
        axe.SetActive(false);

        if (index == 1)
        {
            Debug.Log("Water 선택됨 - SetActive(true)");
            water.SetActive(true);
        }
        else if (index == 2)
        {
            Debug.Log("Axe 선택됨 - SetActive(true)");
            axe.SetActive(true);
        }
    }

}
