using UnityEngine;

public class PopupManager : MonoBehaviour
{

    public GameObject loginPanel;
    public GameObject registerPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
    }

    /// <summary>
    /// 회원가입 버튼 클릭시 해당 dialog active
    /// </summary>
    public void registerButtonClick()
    {
        loginPanel.SetActive(!loginPanel.activeSelf);
        registerPanel.SetActive(!registerPanel.activeSelf);
    }
    
}
