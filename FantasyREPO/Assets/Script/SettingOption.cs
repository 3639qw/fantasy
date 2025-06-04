using UnityEngine;

public class SettingOption : MonoBehaviour
{
    public GameObject G1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void ClosedOption()
    {
        G1.SetActive(false);
    }
}