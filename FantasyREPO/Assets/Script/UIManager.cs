using System;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    static public UIManager instance;

    private GameManager gm;


    [Header("사용자 수치 슬라이더")]
    [SerializeField]private Slider gaugeHP; // 체력 표시할 이미지 UI
    [SerializeField]private Slider gaugeMP; // 마법능력 표시할 이미지 UI
    [SerializeField]private Slider gaugeST; // 힘 표시할 이미지 UI
    

    public static UIManager Instance
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
        gm = GameManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        gaugeHP.value = gm.HP / gm.maxHP;
        gaugeMP.value = gm.MP / gm.maxMP;
        gaugeST.value = gm.ST / gm.maxST;

    }


    


}
    
    
