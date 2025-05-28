using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    static public GameManager instance = null;

    [Header("플레이어 오브젝트")]
    [SerializeField] protected internal GameObject player;

    [Header("사용자 수치 설정")]
    [SerializeField] protected internal float maxHP = 100f;
    [SerializeField] protected internal float maxMP = 50f;
    [SerializeField] protected internal float maxST = 100f;
    [SerializeField] protected internal float recoverHP = 10f;
    [SerializeField] protected internal float recoverMP = 10f;
    [SerializeField] protected internal float recoverST = 10f;
    [SerializeField] protected internal float HP = 100f;
    [SerializeField] protected internal float MP = 50f;
    [SerializeField] protected internal float ST = 100f;

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
                return null;
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        if (PlayerPositionManager.NextPlayerPosition != null)
        {
            transform.position = (Vector2)PlayerPositionManager.NextPlayerPosition;
            PlayerPositionManager.NextPlayerPosition = null;
        }
    }

    void Update()
    {
        RecoverSkill(3, 10); // ST 회복

        if (Input.GetKey(KeyCode.LeftShift) && PlayerIsMoving())
        {
            ConsumeSkill(3, 20 * Time.deltaTime); // ST 소모
        }

        // 디버깅용 출력
        Debug.Log($"ST: {ST}");
    }

    private bool PlayerIsMoving()
    {
        if (player == null) return false;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return false;

        return rb.linearVelocity.sqrMagnitude > 0.01f;
    }

    protected internal void RecoverSkill(int type, float amount)
    {
        if (type == 1 && HP < maxHP)
        {
            HP += amount * Time.deltaTime;
            HP = Mathf.Min(HP, maxHP);
        }
        else if (type == 2 && MP < maxMP)
        {
            MP += amount * Time.deltaTime;
            MP = Mathf.Min(MP, maxMP);
        }
        else if (type == 3 && ST < maxST)
        {
            ST += amount * Time.deltaTime;
            ST = Mathf.Min(ST, maxST);
        }
    }

    protected internal void ConsumeSkill(int type, float amount)
    {
        if (type == 1 && HP >= amount) HP -= amount;
        else if (type == 2 && MP >= amount) MP -= amount;
        else if (type == 3 && ST >= amount) ST -= amount;
    }
}
