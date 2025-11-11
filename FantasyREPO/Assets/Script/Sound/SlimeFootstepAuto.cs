// SlimeFootstepAuto.cs  (SlimeKing 오브젝트에 붙이기)
using UnityEngine;

public class SlimeFootstepAuto : MonoBehaviour
{
    public SlimeKingSFX sfx;         // SlimeKingSFX 드래그
    public float interval = 0.35f;   // 너무 자주면 ↑
    public float deadzone = 0.02f;   // 정지 판정(프레임당 이동거리)

    Vector3 _lastPos;
    float _last;

    void OnEnable() => _lastPos = transform.position;

    void Update()
    {
        var cur = transform.position;
        float dist = (cur - _lastPos).magnitude;
        _lastPos = cur;

        if (dist < deadzone) return;             // 안 움직이면 무음
        if (Time.time - _last < interval) return;

        if (sfx && sfx.footstepClips != null && sfx.footstepClips.Length > 0)
        {
            var clip = sfx.footstepClips[Random.Range(0, sfx.footstepClips.Length)];
            SoundManager.Instance?.PlaySFX(clip, sfx.footstepVol, sfx.footstepPitchJitter);
            _last = Time.time;
        }
    }
}
