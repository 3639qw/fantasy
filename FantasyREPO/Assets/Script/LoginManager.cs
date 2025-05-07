using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    //�α��� ȭ�� Root 
    public GameObject LoginView;

    public InputField inputField_ID;
    public InputField inputField_PW;
    public Button Button_Login;

    //Test�� ���� ���Ƿ� ����� ������ �߰�����
    private string user = "admin";
    private string password = "admin";

    /// <summary>
    /// �α��� ��ư Ŭ���� ����
    /// </summary>
    public void LoginButtonClick()
    {
        if (inputField_ID.text == user && inputField_PW.text == password)
        {
            Debug.Log("�α��� ����");
            //�α��� ������ �α��� â ����
            SceneManager.LoadScene("Main1");
        }
        else
        {
            Debug.Log("�α��� ����");
        }
    }
}