using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    static public GameManager instance = null;
    
    [Header("플레이어 오브젝트")]
    [SerializeField] protected internal GameObject player;
    
    
    [Header("사용자 수치 설정")]
    [SerializeField] protected internal float maxHP = 100f; // 체력 최대치
    [SerializeField] protected internal float maxMP = 50f; // 마법능력 최대치
    [SerializeField] protected internal float maxST = 100f; // 힘 최대치
    [SerializeField] protected internal float recoverHP = 10f; // 체력 회복량
    [SerializeField] protected internal float recoverMP = 10f; // 마법능력 회복량
    [SerializeField] protected internal float recoverST = 10f; // 힘 회복량
    [SerializeField] protected internal float HP = 100f; // 현재 체력
    [SerializeField] protected internal float MP = 50f; // 현재 마법능력
    [SerializeField] protected internal float ST = 100f; // 현재 힘

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                return null;
            }
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RecoverSkill(3,10); // 1초에 10씩 힘을 회복함
        Debug.Log(ST);
        
    }
    
    /// <summary>
    /// 수치유형별로 입력받아 1초당 입력된만큼 수치를 증가시킴
    /// </summary>
    /// <param name="type">증가할 수치(1: 체력, 2: 마법능력, 3: 힘)</param>
    /// <param name="amount">1초당 증가할 양</param>
    /// <returns></returns>
    protected internal void RecoverSkill(int type, float amount)
    {
        if (type == 1)
        {
            // 체력
            if (HP < maxHP)
            {
                HP += amount * Time.deltaTime;
                if (HP > maxHP)
                    HP = maxHP;
            }
        }
        else if (type == 2)
        {
            // 마법능력
            if (MP < maxMP)
            {
                MP += amount * Time.deltaTime;
                if (MP > maxMP)
                    MP = maxMP;
            }
        }
        else if (type == 3)
        {
            // 힘
            if (ST < maxST)
            {
                ST += amount * Time.deltaTime;
                if (ST > maxST)
                    ST = maxST;
            }
        }
    }


    /// <summary>
    /// 수치유형별로 입력된만큼 정률적으로 감소시킴
    /// </summary>
    /// <param name="type">감소할 수치(1: 체력, 2: 마법능력, 3: 힘)</param>
    /// <param name="amount">감소할 양</param>
    protected internal void ConsumeSkill(int type, float amount)
    {
        if (type == 1)
        {
            // 체력
            if (HP > 0 && HP >= amount)
                HP -= amount;
        }else if (type == 2)
        {
            // 마법능력
            if (MP > 0 && MP >= amount)
                MP -= amount;
        }else if (type == 3)
        {
            // 힘
            if (ST > 0 && ST >= amount)
                ST -= amount;
        }
    }
    
    
}
