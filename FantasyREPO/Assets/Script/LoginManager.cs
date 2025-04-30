using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LoginManager : MonoBehaviour
{
    // 로그인 패널 Root 
    public GameObject LoginView;

    public TMP_InputField inputField_ID;
    public TMP_InputField inputField_PW;
    public Button Button_Login;

    //Test를 위해 임의로 사용자 변수를 추가
    private string user = "admin";
    private string password = "admin";

    /// <summary>
    /// 로그인 버튼 클릭시 실행
    /// </summary>
    public void LoginButtonClick()
    {
        Debug.Log(inputField_ID.text + ", " + inputField_PW.text);

        if (inputField_ID.text == user && inputField_PW.text == password)
        {
            Debug.Log("로그인 성공");
            // 로그인 성공시 씬 이동
            SceneManager.LoadScene("Main1");
        }
        else
        {
            Debug.Log("로그인 실패");
        }
    }
}