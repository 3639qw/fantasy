using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;

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

    public static GameManager Instance => instance;

    [Header("시간 및 낮밤 설정")]
    public TMP_Text TimeText;
    public Light2D globalLight;
    public Light2D moonLight;
    public Color dayColor = Color.white;
    public Color nightColor = new Color(0.05f, 0.1f, 0.2f);
    public float transitionDuration = 1.0f;

    private float realTime = 1f;
    private float time;
    private int gameTime = 1080; // 오전 6시 시작
    public static bool isNight = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
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
        RecoverSkill(3, 10);

        if (Input.GetKey(KeyCode.LeftShift) && PlayerIsMoving())
        {
            ConsumeSkill(3, 20 * Time.deltaTime);
        }

        if (isNight)
        {
            time = 0;
            gameTime = 360;
            isNight = false;
            TimeCheck();
        }
        else
        {
            TimeCheck();
        }

        UpdateLightingTransition();

        Debug.Log($"ST: {ST}");
    }

    private bool PlayerIsMoving()
    {
        if (player == null) return false;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        return rb != null && rb.linearVelocity.sqrMagnitude > 0.01f;
    }

    protected internal void RecoverSkill(int type, float amount)
    {
        float delta = amount * Time.deltaTime;

        switch (type)
        {
            case 1: HP = Mathf.Min(HP + delta, maxHP); break;
            case 2: MP = Mathf.Min(MP + delta, maxMP); break;
            case 3: ST = Mathf.Min(ST + delta, maxST); break;
        }
    }

    protected internal void ConsumeSkill(int type, float amount)
    {
        switch (type)
        {
            case 1: if (HP >= amount) HP -= amount; break;
            case 2: if (MP >= amount) MP -= amount; break;
            case 3: if (ST >= amount) ST -= amount; break;
        }
    }

    public void TimeCheck()
    {
        time += Time.deltaTime;

        if (time >= realTime)
        {
            gameTime += 10;
            if (gameTime >= 1440) gameTime = 0;
            time = 0;
            TimeTextChange();
        }
    }

    public void TimeTextChange(bool isDay = false)
    {
        string prefix = (gameTime >= 720 && gameTime < 1440) ? "오후  " : "오전  ";

        int hour24 = gameTime / 60;
        int hour12 = hour24 % 12 == 0 ? 12 : hour24 % 12;
        string hour = hour12.ToString("D2");
        string minute = (gameTime % 60).ToString("D2");

        if (isDay)
            TimeText.text = "오전  06 : 00";
        else
            TimeText.text = prefix + hour + " : " + minute;
    }

    private void UpdateLightingTransition()
    {
        float currentHour = gameTime / 60.0f;
        float lerpFactor;

        if (currentHour >= 6f && currentHour < 7f)
        {
            lerpFactor = Mathf.Clamp01((currentHour - 6f) / transitionDuration);
            globalLight.color = Color.Lerp(nightColor, dayColor, lerpFactor);
            globalLight.intensity = Mathf.Lerp(0.4f, 1.0f, lerpFactor);
            if (moonLight != null) moonLight.intensity = Mathf.Lerp(0.6f, 0.0f, lerpFactor);
        }
        else if (currentHour >= 17f && currentHour < 18f)
        {
            lerpFactor = Mathf.Clamp01((currentHour - 17f) / transitionDuration);
            globalLight.color = Color.Lerp(dayColor, nightColor, lerpFactor);
            globalLight.intensity = Mathf.Lerp(1.0f, 0.4f, lerpFactor);
            if (moonLight != null) moonLight.intensity = Mathf.Lerp(0.0f, 0.6f, lerpFactor);
        }
        else if (currentHour >= 7f && currentHour < 17f)
        {
            globalLight.color = dayColor;
            globalLight.intensity = 1.0f;
            if (moonLight != null) moonLight.intensity = 0.0f;
        }
        else
        {
            globalLight.color = nightColor;
            globalLight.intensity = 0.4f;
            if (moonLight != null) moonLight.intensity = 0.6f;
        }
    }

    public bool IsMorning() => gameTime >= 420 && gameTime < 430;
    public bool IsNight() => gameTime >= 1080 || gameTime < 360;
}
