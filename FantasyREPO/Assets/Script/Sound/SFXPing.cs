// SFXPing.cs
using UnityEngine;
public class SFXPing : MonoBehaviour
{
    public AudioClip testClip;  // 아무 SFX 하나 드래그
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
            SoundManager.Instance?.PlaySFX(testClip);
    }
}
