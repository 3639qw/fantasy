using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [Header("인벤토리 아이템")]
    public Image hand1; // 1번째 기구
    public Image hand2; // 2번째 기구
    public Image hand3; // 3번째 기구
    public int states; // 최초 장착기구 (1,2,3)

    private void Awake()
    {
        SetHandAlpha(states);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            states = 1;
            SetHandAlpha(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            states = 2;
            SetHandAlpha(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            states = 3;
            SetHandAlpha(3);
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
}
