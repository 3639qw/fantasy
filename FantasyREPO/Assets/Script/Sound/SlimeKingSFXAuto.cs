// SlimeKingSFXAuto.cs
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SlimeKingSFXAuto : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private SlimeKingSFX sfx;

    [Header("Animator State Names (Base Layer)")]
    [SerializeField] private string attackStateName = "SlimeKing_Attack";
    [SerializeField] private string skillStateName = "SlimeKing_Skill";

    [Header("Timings (match SlimeKing.cs)")]
    [Tooltip("공격 전 차지 시간")]
    [SerializeField] private float chargeTime = 0.25f;
    [Tooltip("점프 공격 지속 시간")]
    [SerializeField] private float jumpDuration = 1.84f;
    [Tooltip("스킬(벌 소환) 시전 시간")]
    [SerializeField] private float skillCastTime = 0.30f;

    [Header("Layer Index")]
    [SerializeField] private int layerIndex = 0; // 보통 0 (Base Layer)

    int _attackHash, _skillHash;
    int _lastStateHash;
    Coroutine _timeline;

    void Reset()
    {
        animator = GetComponent<Animator>();
        sfx = GetComponent<SlimeKingSFX>() ?? GetComponentInChildren<SlimeKingSFX>(true);
    }

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!sfx) sfx = GetComponent<SlimeKingSFX>() ?? GetComponentInChildren<SlimeKingSFX>(true);

        _attackHash = Animator.StringToHash(attackStateName);
        _skillHash = Animator.StringToHash(skillStateName);
        _lastStateHash = 0;
    }

    void Update()
    {
        if (!animator) return;

        var info = animator.GetCurrentAnimatorStateInfo(layerIndex);
        int currentHash = info.shortNameHash;

        // 상태 진입 감지
        if (currentHash != _lastStateHash)
        {
            OnStateChanged(_lastStateHash, currentHash);
            _lastStateHash = currentHash;
        }
    }

    void OnDisable()
    {
        if (_timeline != null)
        {
            StopCoroutine(_timeline);
            _timeline = null;
        }
    }

    void OnStateChanged(int prevHash, int currentHash)
    {
        // 이전 타임라인 중지
        if (_timeline != null)
        {
            StopCoroutine(_timeline);
            _timeline = null;
        }

        if (currentHash == _attackHash)
        {
            // 공격 타임라인: 차지 -> 이륙 -> 착지
            _timeline = StartCoroutine(CoAttackSfxTimeline());
        }
        else if (currentHash == _skillHash)
        {
            // 스킬 타임라인: 차지 후 벌 소환 SFX
            _timeline = StartCoroutine(CoSkillSfxTimeline());
        }
        // 다른 상태로 나가면 아무 것도 하지 않음
    }

    IEnumerator CoAttackSfxTimeline()
    {
        // 차지 시작
        sfx?.PlayJumpCharge();
        if (chargeTime > 0f) yield return new WaitForSeconds(chargeTime);

        // 이륙
        sfx?.PlayJumpLaunch();
        if (jumpDuration > 0f) yield return new WaitForSeconds(jumpDuration);

        // 착지/충돌
        sfx?.PlayJumpImpact();

        _timeline = null;
    }

    IEnumerator CoSkillSfxTimeline()
    {
        // 차지 구간 대기
        if (chargeTime > 0f) yield return new WaitForSeconds(chargeTime);

        // 벌 소환 SFX (실제 소환은 SlimeKing.cs가 하지만, 우리는 타이밍만 맞춰서 재생)
        sfx?.PlayBeeSummon();

        // 필요하면 시전 끝까지 대기 (순수 타이밍 유지용)
        if (skillCastTime > 0f) yield return new WaitForSeconds(skillCastTime);

        _timeline = null;
    }
}
