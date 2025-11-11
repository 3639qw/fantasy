using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField inputField_ID;
    public TMP_InputField inputField_PW;

    private string apiUrl = "http://localhost:5106/api/login";

    public void LoginButtonClick()
    {
        StartCoroutine(LoginRequest(inputField_ID.text, inputField_PW.text));
    }

    private IEnumerator LoginRequest(string userId, string password)
    {
        LoginRequest loginRequest = new LoginRequest
        {
            ID = userId,
            Password = password
        };

        string json = JsonUtility.ToJson(loginRequest);

        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("로그인 성공 응답: " + www.downloadHandler.text);

                LoginResponse response = JsonUtility.FromJson<LoginResponse>(www.downloadHandler.text);

                if (!string.IsNullOrEmpty(response.token))
                {
                    PlayerPrefs.SetString("authToken", response.token);
                    PlayerPrefs.Save();

                    Debug.Log($"✅ JWT 저장 완료: {response.token}");
                    Debug.Log($"🔍 PlayerPrefs에서 읽은 토큰: {PlayerPrefs.GetString("authToken")}");

                    // 씬 이동
                    SceneManager.LoadScene("Main1");
                }
                else
                {
                    Debug.LogError("❌ 서버 응답에 Token이 없습니다!");
                }
            }
            else
            {
                Debug.LogError($"❌ 로그인 실패 ({www.responseCode}): {www.downloadHandler.text}");
            }
        }
    }
}

[System.Serializable]
public class LoginRequest
{
    public string ID;
    public string Password;
}

[System.Serializable]
public class LoginResponse
{
    public int userUniqueID;
    public string nickname;
    public string token;
}