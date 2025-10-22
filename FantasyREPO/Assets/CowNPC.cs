using UnityEngine;

public class CowNPC : MonoBehaviour
{
    private Animator animator;
    private float timer;
    private float nextEatTime;

    void Start()
    {
        animator = GetComponent<Animator>();
        SetNextEatTime();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= nextEatTime)
        {
            EatGrass();
            SetNextEatTime();
        }
    }

    void EatGrass()
    {
        if (animator)
            animator.SetTrigger("Eat");
    }

    void SetNextEatTime()
    {
        // 다음 풀 뜯기까지 대기시간 (2~4초 사이 랜덤)
        nextEatTime = timer + Random.Range(2f, 4f);
    }
}