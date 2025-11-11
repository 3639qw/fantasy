using UnityEngine;

public class FootstepByMotion2D : MonoBehaviour
{
    [Tooltip("달리기 여부(외부에서 갱신해도 됨)")]
    public bool isRunning;
    [Tooltip("정지 판정 데드존(프레임당 이동거리)")]
    public float moveDeadzone = 0.02f; // 0.02~0.04 권장

    private Vector3 _lastPos;

    private void OnEnable()
    {
        _lastPos = transform.position;
    }

    private void Update()
    {
        Vector3 cur = transform.position;
        float dist = (cur - _lastPos).magnitude;
        _lastPos = cur;

        // 정지로 간주
        if (dist < moveDeadzone) return;

        // 초당 속도 (프레임당 이동거리 / dt)
        float speed = dist / Mathf.Max(Time.deltaTime, 0.0001f);
        SoundManager.Instance?.TryPlayFootstepSimple(speed, isRunning);
    }
}
