using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;

    private Vector2 vector;

    private Rigidbody2D _rb;
    private Animator _animator;

    private const string _horizontal = "Horizontal";
    private const string _vertical = "Vertical";
    private const string _lastHorizontal = "LastHorizontal";
    private const string _lastVertical = "LastVertical";

    void Start()
    {

    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        vector.Set(InputManager.Movement.x, InputManager.Movement.y);
        _rb.linearVelocity = vector * _moveSpeed;

        _animator.SetFloat(_horizontal, vector.x);
        _animator.SetFloat(_vertical, vector.y);

        if (vector != Vector2.zero) {
            _animator.SetFloat(_lastHorizontal, vector.x);
            _animator.SetFloat(_lastVertical, vector.y);
        }
    }

}
