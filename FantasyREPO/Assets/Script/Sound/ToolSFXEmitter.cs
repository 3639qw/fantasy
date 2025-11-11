// ToolSFXEmitter.cs  (플레이어나 툴 애니메이션 오브젝트에 붙여서 사용)
using UnityEngine;

public class ToolSFXEmitter : MonoBehaviour
{
    [Header("현재 이 오브젝트가 재생할 도구 타입")]
    public ToolType tool = ToolType.Pickaxe;

    [Header("같은 프레임/키홀드 중 중복 방지 쿨다운(초)")]
    public float minInterval = 0.08f;

    private float _lastTime;

    /// <summary>도구가 '실제로 사용된 순간'에 호출</summary>
    public void PlayOnce(float intensity01 = 1f)
    {
        if (Time.time - _lastTime < minInterval) return;
        _lastTime = Time.time;
        SoundManager.Instance?.PlayToolSFX(tool, intensity01);
    }
}
