using UnityEngine;

public class StageBGM : MonoBehaviour
{
    void Start()
    {
        SoundManage.instance.PlayBGM("ForestTheme");
    }
}