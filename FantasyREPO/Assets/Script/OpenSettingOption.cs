using UnityEngine;

public class OpenSettingOption : MonoBehaviour
{
    public GameObject G1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void ClosedOption()
    {
        G1.SetActive(!G1.activeSelf);
    }
}