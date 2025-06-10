using UnityEngine;

public class PlayerHealthController : MonoBehaviour
{
    private GameManager _playerHP;
    private Animator _animator;
    private PlayerMove _playerMove;
    private int AttackCool;
    void Awake()
    {
        _playerHP = FindObjectOfType<GameManager>();
        _animator = GetComponent<Animator>();
        _playerMove = GetComponent<PlayerMove>();
    }

    public void TakeDamage(float amount)
    {
        Debug.Log($"AttackCool {AttackCool}");
        if (AttackCool <= 0)
        {
            _playerMove.isDamaged = true;
            GameManager.Instance.ConsumeSkill(1, amount);
            Debug.Log($"Player took {amount} damage");
            _animator.SetTrigger("Attacked");
            AttackCool = 30;
            if (GameManager.Instance.HP <= 0)
            {
                Die();
            }
        }
    }
    private void Die()
    {
        Debug.Log("Player has died.");
        _animator.SetTrigger("Die");
        _playerMove.isDie = true;
    }

    // public void endDamage()
    // {
    //     _animator.ResetTrigger("Attacked");
    // }

    // Update is called once per frame
    void Update()
    {
        if (AttackCool >= 0)
        {
            AttackCool -= 1;
        }
        else if (AttackCool <= 0)
        {
            _playerMove.isDamaged = false;
        }
    }
}
