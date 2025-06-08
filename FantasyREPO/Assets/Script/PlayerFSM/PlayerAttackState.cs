using UnityEngine;

public class PlayerAttackState : PlayerState
{
    private Animator _animator;

    public PlayerAttackState(PlayerController player, Animator animator) : base(player)
    {
        _animator = animator;
    }

    public override void Enter()
    {
        Vector2 attackDir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - player.transform.position).normalized;

        _animator.SetFloat("AttackX", attackDir.x);
        _animator.SetFloat("AttackY", attackDir.y);
        _animator.SetTrigger("Attack");
    }

    public override void Update()
    {
        // 공격 중에는 움직이지 않음
    }
}
