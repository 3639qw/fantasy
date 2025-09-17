using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections;
using System.Text;

public class AxeByLastMotion : MonoBehaviour
{
    [Header("Interact")]
    public KeyCode interactKey = KeyCode.Space;
    public float interactRange = 2.0f;
    public string treeTag = "Tree";
    public float cooldown = 0.4f;

    [Header("Last Motion Tracking (clip-name based)")]
    [Tooltip("최근 방향을 추적할 때, 이 토큰을 이름에 포함하는 클립(Idle/Walk/Run 등)만 신뢰합니다.")]
    public string[] motionNameTokens = new[] { "idle", "Walk" };

    [Header("Axe Clips (code-driven play)")]
    public AnimationClip axeLeft;
    public AnimationClip axeRight;
    public AnimationClip axeUp;
    public AnimationClip axeDown;

    [Range(0f, 1f)] public float hitNormalizedTime = 0.35f; // 클립 진행 중 히트 타이밍(%)

    [Header("Lock While Swinging (optional)")]
    [Tooltip("도끼 휘두르는 동안 비활성화할 컴포넌트들(이동/입력 스크립트 등)")]
    public Behaviour[] disableWhileSwinging;

    [Header("Inventory Gate (optional)")]
    public bool requireAxeSelected = false; // 필요하면 true
    public Sprite axeSprite;                // 인벤토리의 도끼 아이콘(스프라이트가 아니라 아이템ID로 바꿔도 OK)

    enum Dir { Left, Right, Up, Down }
    Dir _lastDir = Dir.Down; // 기본값

    Animator _anim;
    PlayableGraph _graph;
    AnimationClipPlayable _axePlayable;
    bool _isSwinging;
    float _cool;

    ChoppableTree _pendingTree; // 히트 타이밍까지 들고 있을 대상
    void Awake()
    {
        _anim = GetComponent<Animator>();
        if (!_anim) Debug.LogWarning("[AxeByLastMotion] Animator가 필요합니다.");
    }

    void OnDisable()
    {
        StopGraph();
    }

    void Update()
    {
        // 1) 최근 이동/대기 클립으로부터 방향 추적 (스윙 중일 땐 고정)
        if (!_isSwinging) UpdateLastDirectionFromAnimator();

        // 2) 입력 처리
        if (_cool > 0f) _cool -= Time.deltaTime;
        if (Input.GetKeyDown(interactKey) && !_isSwinging && _cool <= 0f)
        {
            TryInteractTree();
        }
    }

    // === 방향 추적 핵심 ===
    void UpdateLastDirectionFromAnimator()
    {
        if (_anim == null) return;

        var infos = _anim.GetCurrentAnimatorClipInfo(0);
        if (infos == null || infos.Length == 0) return;

        // 가장 가중치 높은 클립 하나만 본다
        var best = infos[0];
        string clipName = best.clip ? best.clip.name : "";

        if (string.IsNullOrEmpty(clipName)) return;

        // 지정한 토큰(Idle/Walk/Run/Move 등)을 이름에 포함하는 클립만 신뢰
        string lower = clipName.ToLower();
        bool looksLikeMotion = false;
        for (int i = 0; i < motionNameTokens.Length; i++)
        {
            if (lower.Contains(motionNameTokens[i]))
            {
                looksLikeMotion = true;
                break;
            }
        }
        if (!looksLikeMotion) return;

        // 이름 규칙에서 방향 파싱 (예: idle_LEFT, Walk_L, RunRight, Move_Down 등 유연 지원)
        _lastDir = ParseDirFromName(lower, _lastDir);
    }

    static Dir ParseDirFromName(string name, Dir fallback)
    {
        // 왼쪽
        if (name.Contains("_LEFT") || name.EndsWith("_L") || name.EndsWith("_left"))
            return Dir.Left;
        // 오른쪽
        if (name.Contains("_RIGHT") || name.EndsWith("_R") || name.EndsWith("_right"))
            return Dir.Right;
        // 위
        if (name.Contains("_UP") || name.EndsWith("_U") || name.EndsWith("_up"))
            return Dir.Up;
        // 아래
        if (name.Contains("_DOWN") || name.EndsWith("_D") || name.EndsWith("_down"))
            return Dir.Down;

        return fallback; // 못 찾으면 이전 방향 유지
    }

    void TryInteractTree()
    {
        // (선택) 인벤토리 게이트
        if (requireAxeSelected && !IsAxeSelected()) return;

        // 가까운 나무 하나 찾기(각도/시선 필요 없이 "근처" 우선)
        var cols = Physics2D.OverlapCircleAll(transform.position, interactRange);
        Collider2D nearest = null; float bestSqr = float.MaxValue;

        foreach (var c in cols)
        {
            if (!c || (treeTag.Length > 0 && !c.CompareTag(treeTag))) continue;
            float sq = (c.transform.position - transform.position).sqrMagnitude;
            if (sq < bestSqr) { bestSqr = sq; nearest = c; }
        }
        if (!nearest) return;

        _pendingTree = nearest.GetComponentInParent<ChoppableTree>();
        if (!_pendingTree) return;

        // 최근 방향에 맞는 도끼 클립 선택
        var clip = PickAxeClip(_lastDir);
        if (clip == null)
        {
            Debug.LogWarning("[AxeByLastMotion] 해당 방향의 도끼 클립이 비어 있음");
            return;
        }

        // 이동/입력 잠금 & 원샷 재생
        SetDisabledWhileSwinging(true);
        PlayOneShot(clip);
    }

    AnimationClip PickAxeClip(Dir d)
    {
        switch (d)
        {
            case Dir.Left: return axeLeft;
            case Dir.Right: return axeRight;
            case Dir.Up: return axeUp;
            case Dir.Down: return axeDown;
        }
        return axeDown;
    }

    // 원샷 재생: 끝날 때까지 기다렸다가 자르기
    void PlayOneShot(AnimationClip clip)
    {
        if (_graph.IsValid()) _graph.Destroy();

        _graph = PlayableGraph.Create("AxeOneShot_LastMotion");
        var output = AnimationPlayableOutput.Create(_graph, "AxeOutput", _anim);

        _axePlayable = AnimationClipPlayable.Create(_graph, clip);
        _axePlayable.SetApplyFootIK(false);

        // 재생 종료 판정이 확실하도록 길이/랩모드 지정
        _axePlayable.SetDuration(clip.length);
        _axePlayable.SetSpeed(1.0); // 필요시 속도 조절
        _axePlayable.SetTime(0.0);
        _axePlayable.SetDone(false);

        output.SetSourcePlayable(_axePlayable);

        _isSwinging = true;
        _cool = cooldown;

        SetDisabledWhileSwinging(true);
        _graph.Play();

        StartCoroutine(WaitAxeEndThenChop());
    }

    System.Collections.IEnumerator WaitAxeEndThenChop()
    {
        // 클립이 끝날 때까지 대기 (루프/블렌딩 영향 없이 Playable의 시간 기준으로)
        while (_graph.IsValid() && _axePlayable.IsValid())
        {
            // 방법 1: IsDone() 사용 (권장)
            if (_axePlayable.IsDone()) break;

            // 방법 2: 시간 비교 (엔진 버전 따라 안전)
            if (_axePlayable.GetTime() >= _axePlayable.GetDuration()) break;

            yield return null;
        }

        // ★ 여기서 "끝난 다음" 잘라준다
        if (_pendingTree != null)
        {
            _pendingTree.ChopOnce();
            _pendingTree = null;
        }

        StopGraph();                 // 그래프 정리
        _isSwinging = false;
        SetDisabledWhileSwinging(false);
    }


    IEnumerator SwingRoutine(float clipLen)
    {
        // 히트 타이밍
        float hitTime = Mathf.Clamp01(hitNormalizedTime) * clipLen;
        yield return new WaitForSeconds(hitTime);

        if (_pendingTree) { _pendingTree.ChopOnce(); _pendingTree = null; }

        // 종료까지 대기
        float remain = Mathf.Max(0f, clipLen - hitTime);
        yield return new WaitForSeconds(remain);

        StopGraph();
        _isSwinging = false;
        SetDisabledWhileSwinging(false);
    }

    void StopGraph()
    {
        if (_graph.IsValid())
        {
            _graph.Stop();
            _graph.Destroy();
        }
    }

    void SetDisabledWhileSwinging(bool off)
    {
        if (disableWhileSwinging == null) return;
        foreach (var b in disableWhileSwinging)
            if (b) b.enabled = !off;
    }

    bool IsAxeSelected()
    {
        var inv = Inventory.Instance;
        if (inv == null) return true; // 인벤토리 시스템 없으면 일단 허용
        var sel = inv.GetSelectedSprite();
        if (axeSprite && sel) return sel == axeSprite;
        return !inv.IsSelectedEmpty();
    }
}
