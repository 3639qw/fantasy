using UnityEngine;
using System.Collections;

public class StatusCondition : MonoBehaviour
{
    // --- 필요한 컴포넌트 ---
    // 이 스크립트가 영향을 줄 플레이어의 다른 스크립트들
    private PlayerMove playerMovement; // 플레이어 이동 스크립트 (가정)
    private PlayerHealthController playerHealth;     // 플레이어 체력 스크립트 (가정)

    // <<< 1. 파티클 시스템 변수 추가 >>>
    [Header("상태 이상 파티클 (VFX)")]
    public ParticleSystem bleedingVFX;
    public ParticleSystem poisonVFX;
    public ParticleSystem slowVFX;

    // --- 코루틴 참조 변수 ---
    // 상태 이상이 중첩되지 않고 '갱신'되도록 관리하기 위함
    private Coroutine slowCoroutine;
    private Coroutine bleedingCoroutine;
    private Coroutine poisonCoroutine;

    // 플레이어의 원래 이동 속도를 저장하기 위한 변수
    private float originalMoveSpeed = -1f;

    void Start()
    {
        // 플레이어에 붙어있는 스크립트들을 자동으로 찾아옵니다.
        // [중요] 'PlayerMovement'와 'PlayerHealth'는 실제 사용 중인 스크립트 이름으로 바꿔야 합니다.
        playerMovement = GetComponent<PlayerMove>();
        playerHealth = GetComponent<PlayerHealthController>();

        if (playerMovement == null)
        {
            Debug.LogWarning("PlayerMovement 스크립트를 찾을 수 없습니다.");
        }
        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealth 스크립트를 찾을 수 없습니다.");
        }

        if (bleedingVFX) bleedingVFX.Stop();
        if (poisonVFX) poisonVFX.Stop();
        if (slowVFX) slowVFX.Stop();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
            ApplySlow(5f, 0.5f);
        if (Input.GetKeyDown(KeyCode.B))
            ApplyBleeding(5f, 5f, 1f);
        if (Input.GetKeyDown(KeyCode.P))
            ApplyPoison(5f, 1f);
        if (Input.GetKeyDown(KeyCode.U))
            CureAll();
        
    }

    // --- 1. 슬로우(Slow) 상태 이상 ---
    // (예: 3초간 50% 감속 -> duration = 3f, intensity = 0.5f)
    public void ApplySlow(float duration, float intensity)
    {
        if (playerMovement == null) return; // 이동 스크립트가 없으면 실행 중지

        // 이미 슬로우 코루틴이 실행 중이면, 중지하고 새로 시작 (지속시간 갱신)
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }

        // 아직 원래 속도를 저장하지 않았다면(첫 슬로우라면) 현재 속도를 저장
        if (originalMoveSpeed < 0f)
        {
            originalMoveSpeed = playerMovement._moveSpeed; // 'moveSpeed'는 실제 변수명으로 변경 필요
        }

        slowCoroutine = StartCoroutine(SlowStatus(duration, intensity));
        if (slowVFX) slowVFX.Play();
    }

    private IEnumerator SlowStatus(float duration, float intensity)
    {
        // 감속 적용 (예: 10 * (1 - 0.5) = 5)
        playerMovement._moveSpeed = originalMoveSpeed * (1f - intensity);
        Debug.Log("감속: " + intensity);

        // 지정된 시간(duration)만큼 대기
        yield return new WaitForSeconds(duration);
        
        // 시간이 지나면 원래 속도로 복구
        playerMovement._moveSpeed = originalMoveSpeed;
        originalMoveSpeed = -1f; // 플래그 리셋
        if (slowVFX) slowVFX.Stop();
        slowCoroutine = null; // 코루틴 완료
    }


    // --- 2. 출혈(Bleeding) 상태 이상 ---
    // (예: 5초 동안 1초마다 2의 데미지 -> duration = 5f, damagePerTick = 2f, tickInterval = 1f)
    public void ApplyBleeding(float duration, float damagePerTick, float tickInterval)
    {
        if (playerHealth == null) return; // 체력 스크립트가 없으면 실행 중지

        if (bleedingCoroutine != null) return;

        bleedingCoroutine = StartCoroutine(BleedingStatus(duration, damagePerTick, tickInterval));
    }

    private IEnumerator BleedingStatus(float duration, float damagePerTick, float tickInterval)
    {
        float timer = 0f;
        while (timer < duration)
        {
            // 다음 틱까지 대기
            yield return new WaitForSeconds(tickInterval);

            // 데미지 적용 (PlayerHealth 스크립트에 TakeDamage 함수가 있다고 가정)
            playerHealth.TakeDamage(damagePerTick);
            Debug.Log("출혈 데미지: " + damagePerTick); // 테스트용

            if (bleedingVFX)
                bleedingVFX.Play();
            
            timer += tickInterval;
        }
        bleedingCoroutine = null; // 코루틴 완료
    }

    // --- 3. 중독(Poison) 상태 이상 ---
    // (출혈과 동일한 구조, 하지만 별개의 코루틴으로 관리하여 중첩 가능)
    public void ApplyPoison(float damagePerTick, float tickInterval)
    {
        if (playerHealth == null) return;
        if (poisonCoroutine != null) return; 
        
        poisonCoroutine = StartCoroutine(PoisonStatus(damagePerTick, tickInterval));

    }

    private IEnumerator PoisonStatus(float damagePerTick, float tickInterval)
    {
        while (true)
        {
            // 1. 데미지 틱 간격만큼 대기
            yield return new WaitForSeconds(tickInterval);

            // 2. 데미지 적용
            playerHealth.TakeDamage(damagePerTick);

            if (poisonVFX)
            {
                poisonVFX.Play();
            }
        }
    }
    
    /// <summary>중독을 해제합니다.</summary>
    public void CurePoison()
    {
        if (poisonCoroutine != null)
        {
            StopCoroutine(poisonCoroutine);
            poisonCoroutine = null;
            Debug.Log("중독 해제됨");

            if (poisonVFX) poisonVFX.Stop();
        }
    }

    /// <summary>출혈을 즉시 중지시킵니다.</summary>
    public void CureBleeding()
    {
        if (bleedingCoroutine != null)
        {
            StopCoroutine(bleedingCoroutine);
            bleedingCoroutine = null;
            Debug.Log("출혈 멈춤");

            if (bleedingVFX) bleedingVFX.Stop();
        }
    }

    /// <summary>슬로우를 즉시 해제하고 속도를 복구합니다.</summary>
    public void CureSlow()
    {
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
            // 속도 즉시 복구
            if (originalMoveSpeed > 0f)
            {
                playerMovement._moveSpeed = originalMoveSpeed;
                originalMoveSpeed = -1f;
            }
            slowCoroutine = null;
            Debug.Log("슬로우 해제됨");

            if (slowVFX) slowVFX.Stop();
        }
    }

    /// <summary>모든 상태 이상을 해제합니다.</summary>
    public void CureAll()
    {
        Debug.Log("모든 상태 이상 해제 시도...");
        CurePoison();
        CureBleeding();
        CureSlow();
    }
}