using UnityEngine;

public class BossBGM : MonoBehaviour
{
    void Start()
    {
        SoundManage.instance.PlayBGM("BossTheme");
    }
}