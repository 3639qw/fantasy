using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FootstepAutoByVelocity2D : MonoBehaviour
{
    [Tooltip("달리기 여부 입력(없으면 항상 걷기로 처리)")]
    public bool isRunning;
    [Tooltip("Shift 등 외부에서 값 갱신하고 싶다면 이 필드를 공개로 두고 스크립트에서 토글하세요.")]
    public KeyCode runKey = KeyCode.LeftShift;
    public bool useRunKey = false;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!rb) rb = GetComponentInParent<Rigidbody2D>();
    }

    private void Update()
    {
        if (useRunKey) isRunning = Input.GetKey(runKey);

        float speed = rb ? rb.linearVelocity.magnitude : 0f;
        SoundManager.Instance?.TryPlayFootstepSimple(speed, isRunning);
    }
}
