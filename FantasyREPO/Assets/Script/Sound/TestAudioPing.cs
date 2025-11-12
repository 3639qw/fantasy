// TestAudioPing.cs
using UnityEngine;

public class TestAudioPing : MonoBehaviour
{
    public AudioClip clip;
    AudioSource src;

    void Awake()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D
        src.volume = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("[TestAudioPing] P pressed. Try PlayOneShot");
            if (clip) src.PlayOneShot(clip, 1f);
            else Debug.LogWarning("[TestAudioPing] clip is null");
        }
    }
}
