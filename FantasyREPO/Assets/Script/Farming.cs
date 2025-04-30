using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Farming : MonoBehaviour
{
    static public Farming instance = null;

    private GameManager gm;
    private  Inventory inv;


    [Header("농지 룰타일")] 
    [SerializeField] private Tilemap farmLand; // 농지의 타일맵

    [SerializeField] private TileBase grassTile; // 농지화 할수 있는 잔디 타일
    [SerializeField] private TileBase tilledTile; // 1차 농지
    [SerializeField] private TileBase farmTile; // 2차 농지
    [SerializeField] private TileBase wetfarmTile; // 급수를 실시한 농지
    

    public static Farming Instance
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
        inv = Inventory.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        
        // 개간
        if (Input.GetKeyDown(KeyCode.Space))
        {
            BuildFarm(5);
        }
        
    }


    /// <summary>
    /// 잔디를 개간한다
    /// </summary>
    /// <param name="reqST">개간을 수행함으로 감소할 힘</param>
    void BuildFarm(float reqST)
    {
        Vector3 targetWorldPos = gm.player.transform.position;
        Vector3Int tilePos = farmLand.WorldToCell(targetWorldPos);
        TileBase currentTile = farmLand.GetTile(tilePos);

        if (gm.ST >= reqST)
        {
            // 현재 남은 힘이 파라미터로 받은 힘보다 많은 경우 개간 돌입

            if (currentTile == tilledTile)
            {
                farmLand.SetTile(tilePos,farmTile);
                gm.ConsumeSkill(3,reqST);
            }else if (currentTile == farmTile)
            {
                farmLand.SetTile(tilePos,wetfarmTile);
                gm.ConsumeSkill(3,reqST);
            }
            
            // if (currentTile == grassTile)
            // {
            //     // 잔디를 1차 농지화
            //     if (inv.states == 2)
            //     {
            //         farmLand.SetTile(tilePos, tilledTile); // 1차 농지화
            //         gm.ConsumeSkill(3,reqST); // 힘을 감소
            //         Debug.Log("1차농지 성공!!");
            //     }
            //     
            // }else if (currentTile == tilledTile)
            // {
            //     // 1차 농지를 2차 농지화
            //     if (inv.states == 2)
            //     {
            //         Vector3Int centerPos = tilePos;
            //         if (Is3x3Tilled(centerPos))
            //         {
            //             Replace3x3WithFarm(tilePos,farmTile);
            //             gm.ConsumeSkill(3,reqST);
            //             Debug.Log("2차농지 성공!!");
            //         }
            //     }
            // }
            // else if(currentTile == farmTile) 
            // {
            //     // 2차 농지에 급수
            //     if (inv.states == 1)
            //     {
            //         Vector3Int centerPos = tilePos;
            //         Replace3x3WithFarm(centerPos,wetfarmTile);
            //         gm.ConsumeSkill(3,reqST);
            //         Debug.Log("2차농지 급수성공!!");   
            //     }
            // }
        }
        else
        {
            Debug.Log("힘이 부족해서 개간을 실패했습니다");   
        }
    }
    
    
    
    bool Is3x3Tilled(Vector3Int center)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int checkPos = center + new Vector3Int(x, y, 0);
                if (farmLand.GetTile(checkPos) != tilledTile)
                    return false;
            }
        }
        return true;
    }
    
    
    /// <summary>
    /// 2차 농지화
    /// </summary>
    /// <param name="center"></param>
    /// <param name="tile"></param>
    void Replace3x3WithFarm(Vector3Int center, TileBase tile)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int changePos = center + new Vector3Int(x, y, 0);
                farmLand.SetTile(changePos, tile);
            }
        }
    }
}
