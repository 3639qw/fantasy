using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

[System.Serializable]
public class SceneData
{
    public string SceneName;
    public float PositionX;
    public float PositionY;
}

public class SceneSync : MonoBehaviour
{
    private string baseUrl = "http://localhost:5106/api/scene";

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    // 씬 로드될때 플레이어 위치 불러오기
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(LoadSceneWithTokenCheck());
    }
    
    // 씬 언로드될때 플레이어 현재 위치 저장
    private void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"💾 Scene about to unload: {scene.name}, saving position...");
        StartCoroutine(SaveScene());
    }

    
    private IEnumerator LoadSceneWithTokenCheck()
    {
        float timeout = 2f;
        float elapsed = 0f;
        string token = PlayerPrefs.GetString("authToken", null);

        while (string.IsNullOrEmpty(token) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
            token = PlayerPrefs.GetString("authToken", null);
        }

        if (!string.IsNullOrEmpty(token))
        {
            StartCoroutine(LoadScene());
        }
        else
        {
            Debug.LogWarning("❌ JWT token not found after waiting. Login first!");
        }
    }

    public IEnumerator SaveScene()
    {
        string token = PlayerPrefs.GetString("authToken", null);
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("❌ JWT token not found. Login first!");
            yield break;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("❌ Player object not found!");
            yield break;
        }

        Vector2 pos = player.transform.position;
        SceneData data = new SceneData
        {
            SceneName = SceneManager.GetActiveScene().name,
            PositionX = pos.x,
            PositionY = pos.y
        };
        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest req = new UnityWebRequest(baseUrl + "/save", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + token);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Scene saved to server: {data.SceneName} ({data.PositionX}, {data.PositionY})");
            }
            else
            {
                Debug.LogError("❌ Save failed: " + req.error);
                Debug.LogError("서버 응답: " + req.downloadHandler.text);
            }
        }
    }

    public IEnumerator LoadScene()
    {
        string token = PlayerPrefs.GetString("authToken", null);
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("❌ JWT token not found. Login first!");
            yield break;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        using (UnityWebRequest req = UnityWebRequest.Get(baseUrl + "/load/" + sceneName))
        {
            req.SetRequestHeader("Authorization", "Bearer " + token);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                Debug.Log("📦 서버 응답(JSON): " + json);

                try
                {
                    // ✅ JsonUtility는 PascalCase에 맞춰서 SceneData 사용
                    SceneData data = JsonUtility.FromJson<SceneData>(json);

                    // 서버가 camelCase로 보내면 수동 변환
                    if (data.PositionX == 0 && data.PositionY == 0)
                    {
                        // 수동 파싱 (간단)
                        var dict = JsonToDictionary(json);
                        data.SceneName = dict.ContainsKey("sceneName") ? dict["sceneName"] : data.SceneName;
                        data.PositionX = dict.ContainsKey("positionX") ? float.Parse(dict["positionX"]) : data.PositionX;
                        data.PositionY = dict.ContainsKey("positionY") ? float.Parse(dict["positionY"]) : data.PositionY;
                    }

                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        player.transform.position = new Vector2(data.PositionX, data.PositionY);
                        Debug.Log($"✅ Loaded scene {data.SceneName} pos=({data.PositionX}, {data.PositionY})");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("❌ JSON 파싱 중 오류: " + ex.Message);
                    Debug.LogError("응답 내용: " + json);
                }
            }
            else
            {
                Debug.LogWarning($"⚠ No scene data found for {sceneName}. 서버 응답: {req.downloadHandler.text}");
            }
        }
    }

    // JSON => Dict Parser (camelcase 대응)
    private Dictionary<string, string> JsonToDictionary(string json)
    {
        var dict = new Dictionary<string, string>();
        json = json.Trim().TrimStart('{').TrimEnd('}');
        string[] entries = json.Split(',');
        foreach (var entry in entries)
        {
            string[] kv = entry.Split(':');
            if (kv.Length != 2) continue;
            string key = kv[0].Trim().Trim('"');
            string value = kv[1].Trim().Trim('"');
            dict[key] = value;
        }
        return dict;
    }

    void OnApplicationQuit()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            StartCoroutine(SaveScene());
        }
    }
    
}
