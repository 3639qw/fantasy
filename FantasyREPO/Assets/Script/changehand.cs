using UnityEngine;

public class ChangeHand : MonoBehaviour
{
    [Header("�� ��������Ʈ��")]
    public SpriteRenderer hand1;
    public SpriteRenderer hand2;
    public SpriteRenderer hand3;
    public int states;

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

    private void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        Color c = renderer.color;
        c.a = alpha;
        renderer.color = c;
    }
}