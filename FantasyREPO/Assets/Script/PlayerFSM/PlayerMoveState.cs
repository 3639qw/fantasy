using UnityEngine;

public class PlayerMoveState : PlayerState
{
    private Rigidbody2D _rb;
    private Animator _animator;

    public PlayerMoveState(PlayerController player, Rigidbody2D rb, Animator animator) : base(player)
    {
        _rb = rb;
        _animator = animator;
    }

    public override void Update()
    {
        Vector2 input = player.inputVector;

        _animator.SetFloat("Horizontal", input.x);
        _animator.SetFloat("Vertical", input.y);

        if (input != Vector2.zero)
        {
            player.lastDirection = input.normalized;
            _animator.SetFloat("LastHorizontal", input.x);
            _animator.SetFloat("LastVertical", input.y);
        }
    }

    public override void FixedUpdate()
    {
        Vector2 move = player.inputVector * player.moveSpeed;
        _rb.linearVelocity = move;
    }

    public override void Exit()
    {
        _rb.linearVelocity = Vector2.zero;
    }
}
