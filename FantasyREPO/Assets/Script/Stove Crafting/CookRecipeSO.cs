// Assets/Scripts/Crafting/CookRecipeSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "CookRecipe", menuName = "Game/Crafting/Cook Recipe")]
public class CookRecipeSO : ScriptableObject
{
    [Header("Inputs (재료)")]
    public Sprite inputA;
    public int countA = 1;

    public Sprite inputB;     // 단일 재료면 null
    public int countB = 0;    // 단일 재료면 0

    [Header("Output (결과)")]
    public Sprite output;
    public int outputCount = 1;

    [Header("Options")]
    [Tooltip("체크 시 A+B 순서가 중요합니다.")]
    public bool orderMatters = false;

    [Tooltip("요리 완료까지 걸리는 시간(초)")]
    public float timeSeconds = 5f;

    /// <summary>
    /// a/b: 슬롯 스프라이트, ca/cb: 수량 / swapped: 순서 무시 매칭에서 뒤바뀌었는지
    /// </summary>
    public bool IsMatch(Sprite a, int ca, Sprite b, int cb, out bool swapped)
    {
        swapped = false;

        // 단일 재료
        if (inputB == null || countB <= 0)
        {
            bool left = (a == inputA && ca >= countA) && (b == null || cb <= 0);
            bool right = (b == inputA && cb >= countA) && (a == null || ca <= 0);
            if (right) swapped = true;
            return left || right;
        }

        // 양쪽 재료
        if (orderMatters)
            return (a == inputA && ca >= countA && b == inputB && cb >= countB);

        bool normal = (a == inputA && ca >= countA && b == inputB && cb >= countB);
        if (normal) return true;

        bool swappedMatch = (a == inputB && ca >= countB && b == inputA && cb >= countA);
        if (swappedMatch) swapped = true;
        return swappedMatch;
    }
}
