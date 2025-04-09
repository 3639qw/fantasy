using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPopupLogin : MonoBehaviour
{
    public TMP_InputField idTxt;
    public TMP_InputField pwTxt;
    public Button[] arrBtns;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.arrBtns[0].onClick.AddListener(() =>
        {
            this.LogInBtn();
        });

        this.arrBtns[1].onClick.AddListener(() =>
        {
            this.ClothBtn();
        });
    }

    public void ClothBtn()
    {
        this.gameObject.SetActive(false);
    }

    public void LogInBtn()
    {
        string id = this.idTxt.text;
        string pw = this.pwTxt.text;

        Debug.LogFormat("ID:{0}\nPW:{1}", id, pw);
    }
}
