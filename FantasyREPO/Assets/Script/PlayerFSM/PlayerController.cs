using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Animator _animator;

    private PlayerState _currentState;

    [Header("State Reference")]
    public PlayerMoveState moveState;
    public PlayerAttackState attackState;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [HideInInspector] public Vector2 inputVector;
    [HideInInspector] public Vector2 lastDirection = Vector2.down;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();

        moveState = new PlayerMoveState(this, _rb, _animator);
        attackState = new PlayerAttackState(this, _animator);
    }

    private void Start()
    {
        SwitchState(moveState);
    }

    private void Update()
    {
        inputVector = InputManager.Movement;

        _currentState?.Update();

        if (Input.GetMouseButtonDown(0))
        {
            SwitchState(attackState);
        }
    }

    private void FixedUpdate()
    {
        _currentState?.FixedUpdate();
    }

    public void SwitchState(PlayerState newState)
    {
        if (_currentState == newState) return;

        _currentState?.Exit();
        _currentState = newState;
        _currentState?.Enter();
    }

    public void EndAttack()
    {
        SwitchState(moveState); // 또는 상태 전환 로직
    }
}
