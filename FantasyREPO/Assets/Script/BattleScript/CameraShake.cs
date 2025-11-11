using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // 1. 싱글톤(Singleton) 인스턴스
    // (어디서든 CameraShake.Instance.Shake()로 호출할 수 있게 함)
    public static CameraShake Instance { get; private set; }

    private Vector3 originalLocalPos; // 흔들기 전 원래 카메라 위치
    private Coroutine _shakeCoroutine; // 실행 중인 쉐이크 코루틴

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 카메라의 원래 '로컬' 위치 저장
        // (카메라가 다른 오브젝트(예: Player)의 자식이 아닐 경우)
        originalLocalPos = transform.localPosition;
    }

    /// <summary>
    /// 카메라를 흔드는 메인 함수
    /// </summary>
    /// <param name="duration">흔들리는 시간 (초)</param>
    /// <param name="magnitude">흔들리는 강도</param>
    public void Shake(float duration, float magnitude)
    {
        // (선택사항) 만약 이미 흔들리고 있다면, 이전 것은 멈추고 새로 시작
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            transform.localPosition = originalLocalPos; // 즉시 원위치
        }
        
        // 새로운 쉐이크 코루틴 시작
        _shakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float timer = 0f;

        // 흔들기 시작 전, 현재 카메라의 로컬 위치를 다시 한 번 저장
        // (카메라가 플레이어를 따라다니는 경우를 대비)
        originalLocalPos = transform.localPosition; 

        while (timer < duration)
        {
            // 매 프레임 랜덤한 오프셋 생성
            // (magnitude가 0.5라면 -0.5 ~ +0.5 사이)
            float x = (Random.value * 2f - 1f) * magnitude;
            float y = (Random.value * 2f - 1f) * magnitude;
            
            // z축은 건드리지 않음 (2D 게임)
            transform.localPosition = new Vector3(originalLocalPos.x + x, originalLocalPos.y + y, originalLocalPos.z);

            timer += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        // 시간이 다 되면 정확히 원래 위치로 복구
        transform.localPosition = originalLocalPos;
        _shakeCoroutine = null; // 코루틴 완료
    }
}