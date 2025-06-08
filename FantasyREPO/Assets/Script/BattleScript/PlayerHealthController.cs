using UnityEngine;

public class PlayerHealthController : MonoBehaviour
{
    private GameManager _playerHP;
    private Animator _animator;
    private PlayerMove _playerMove;
    void Awake()
    {
        _playerHP = FindObjectOfType<GameManager>();
        _animator = GetComponent<Animator>();
        _playerMove = GetComponent<PlayerMove>();
    }

    public void TakeDamage(float amount)
    {
        GameManager.Instance.ConsumeSkill(1, amount);
        Debug.Log($"Player took {amount} damage");
        if (GameManager.Instance.HP <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        Debug.Log("Player has died.");
        _animator.SetTrigger("Die");
        _playerMove.isDie = true;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
