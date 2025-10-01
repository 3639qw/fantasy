// Assets/Scripts/Crafting/RecipeSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "Recipe", menuName = "Game/Crafting/Recipe")]
public class RecipeSO : ScriptableObject
{
    [Header("Inputs (재료)")]
    public Sprite inputA;
    public int countA = 1;

    public Sprite inputB;      // 단일 재료 레시피면 null
    public int countB = 0;  // 단일 재료 레시피면 0

    [Header("Output (결과)")]
    public Sprite output;
    public int outputCount = 1;

    [Header("옵션")]
    [Tooltip("체크 시 A+B 순서가 중요합니다.")]
    public bool orderMatters = false;

    /// <summary>
    /// a/b: 슬롯 스프라이트, ca/cb: 슬롯 수량
    /// swapped: 순서 무시 매칭에서 A/B가 뒤바뀌어 매칭되었는지
    /// </summary>
    public bool IsMatch(Sprite a, int ca, Sprite b, int cb, out bool swapped)
    {
        swapped = false;

        // --- 단일 재료 레시피 ---
        if (inputB == null || countB <= 0)
        {
            // 한쪽 슬롯이 inputA를 충분한 개수로 가지고 있고, 다른 쪽은 비어있거나 0개
            bool left = (a == inputA && ca >= countA) && (b == null || cb <= 0);
            bool right = (b == inputA && cb >= countA) && (a == null || ca <= 0);
            if (right) swapped = true;
            return left || right;
        }

        // --- 양쪽 재료 레시피 ---
        if (orderMatters)
        {
            // A칸에 A가 countA 이상, B칸에 B가 countB 이상
            return (a == inputA && ca >= countA && b == inputB && cb >= countB);
        }
        else
        {
            // 순서 무시: (A,B) 또는 (B,A)
            bool normal = (a == inputA && ca >= countA && b == inputB && cb >= countB);
            if (normal) return true;

            bool swappedMatch = (a == inputB && ca >= countB && b == inputA && cb >= countA);
            if (swappedMatch) swapped = true;
            return swappedMatch;
        }
    }
}
