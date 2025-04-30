using UnityEngine;

public class PopupManager : MonoBehaviour
{
    
    
    public GameObject popupPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        //ClosePopup();
    }
    public void OpenPopup()
    {
        popupPanel.SetActive(true);
    }

    // Update is called once per frame
    public void ClosePopup()
    {
        popupPanel.SetActive(false);
    }
}
