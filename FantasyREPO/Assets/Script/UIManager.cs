using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    static public UIManager instance;

    private GameManager gm;

    [Header("Gauge names to auto-bind (exact object names)")]
    [SerializeField] private string hpSliderName = "HP_Slider";
    [SerializeField] private string mpSliderName = "MP_Slider";
    [SerializeField] private string stSliderName = "ST_Slider";

    [Header("사용자 수치 슬라이더")]
    [SerializeField]private Slider gaugeHP; // 체력 표시할 이미지 UI
    [SerializeField]private Slider gaugeMP; // 마법능력 표시할 이미지 UI
    [SerializeField]private Slider gaugeST; // 힘 표시할 이미지 UI
    
    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnSceneLoaded(Scene s, LoadSceneMode m) => AutoBindGauges();

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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gm = GameManager.Instance;
        AutoBindGauges();
    }
    
    private static bool IsSceneObject(Component c) => c && c.gameObject.scene.IsValid();

    private Slider FindSliderByName(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) return null;
        var sliders = Resources.FindObjectsOfTypeAll<Slider>(); // 활성/비활성 + DDOL 포함
        foreach (var s in sliders)
        {
            if (!IsSceneObject(s)) continue;
            if (string.Equals(s.name, target, StringComparison.OrdinalIgnoreCase))
                return s;
        }
        return null;
    }

    public void AutoBindGauges()
    {
        if (gaugeHP == null) gaugeHP = FindSliderByName(hpSliderName);
        if (gaugeMP == null) gaugeMP = FindSliderByName(mpSliderName);
        if (gaugeST == null) gaugeST = FindSliderByName(stSliderName);

        SetupSlider(gaugeHP);
        SetupSlider(gaugeMP);
        SetupSlider(gaugeST);

        Debug.Log($"[UIManager][AutoBind] HP={(gaugeHP ? gaugeHP.name : "null")}, MP={(gaugeMP ? gaugeMP.name : "null")}, ST={(gaugeST ? gaugeST.name : "null")}");
    }

    private void SetupSlider(Slider s)
    {
        if (s == null) return;
        s.minValue = 0f;
        s.maxValue = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        if (gm == null) gm = GameManager.Instance;
        if (gm == null) return;
        
        float hpMax = Mathf.Max(1f, gm.maxHP);
        float mpMax = Mathf.Max(1f, gm.maxMP);
        float stMax = Mathf.Max(1f, gm.maxST);

        if (gaugeHP) gaugeHP.value = Mathf.Clamp01(gm.HP / hpMax);
        if (gaugeMP) gaugeMP.value = Mathf.Clamp01(gm.MP / mpMax);
        if (gaugeST) gaugeST.value = Mathf.Clamp01(gm.ST / stMax);
    }


    


}
    
    
