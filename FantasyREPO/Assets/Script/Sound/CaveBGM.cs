using UnityEngine;

public class CaveBGM : MonoBehaviour
{
    void Start()
    {
        SoundManage.instance.PlayBGM("CaveTheme");
    }
}