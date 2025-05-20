using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class RegisterManager : MonoBehaviour
{
    [Header("Register Form")]
    public TMP_InputField registerID; // id
    public TMP_InputField registerPW; // pw
    public TMP_InputField registerMail; // email
    public TMP_InputField registerName; // 성명
    public TMP_InputField registerNick; // 닉네임

    private string apiUrl = "http://localhost:5106/api/register";

    public void RegisterButtonClick()
    {
        string userId = registerID.text;
        string password = registerPW.text;
        string mail = registerMail.text;
        string name = registerName.text;
        string nickname = registerNick.text;

        RegisterRequest request = new RegisterRequest
        {
            ID = userId,
            Password = password,
            Email = mail,
            Name = name,
            Nickname = nickname
        };

        string json = JsonUtility.ToJson(request);

        Debug.Log("Sending Register JSON: " + json);

        StartCoroutine(SendRegisterRequest(json));
    }

    private IEnumerator SendRegisterRequest(string json)
    {
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Register successful : " + request.downloadHandler.text);
            SceneManager.LoadScene("Main");
        }
        else
        {
            Debug.LogError("Register failed: " + request.error);
            Debug.LogError("Server Response: " + request.downloadHandler.text);
        }
    }
}

[System.Serializable]
public class RegisterRequest
{
    public string ID;
    public string Password;
    public string Email;
    public string Name;
    public string Nickname;
}