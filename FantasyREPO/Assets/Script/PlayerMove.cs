using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _runSpeed = 8f;

    private Vector2 vector;
    private Rigidbody2D _rb;
    private Animator _animator;

    private const string _horizontal = "Horizontal";
    private const string _vertical = "Vertical";
    private const string _lastHorizontal = "LastHorizontal";
    private const string _lastVertical = "LastVertical";

    private Vector2 _lastDirection = Vector2.down;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        vector.Set(InputManager.Movement.x, InputManager.Movement.y);

        float currentSpeed = (Input.GetKey(KeyCode.LeftShift) && GameManager.Instance.ST > 0)
            ? _runSpeed
            : _moveSpeed;

        _rb.linearVelocity = vector * currentSpeed;

        _animator.SetFloat(_horizontal, vector.x);
        _animator.SetFloat(_vertical, vector.y);

        if (vector != Vector2.zero)
        {
            _animator.SetFloat(_lastHorizontal, vector.x);
            _animator.SetFloat(_lastVertical, vector.y);
            _lastDirection = vector.normalized;
        }
    }

    public Vector2 LastDirection => _lastDirection;
}
