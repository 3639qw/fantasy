// ExplodeOnPlayerHitSFX.cs
using UnityEngine;

public class ExplodeOnPlayerHitSFX : MonoBehaviour
{
    public MushroomExplosionSFX sfx;       // 같은 오브젝트에 있으면 자동 할당 가능
    public string playerTag = "PlayerCollider";
    public bool once = true;
    bool _done;

    void Reset() => sfx = GetComponent<MushroomExplosionSFX>();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (once && _done) return;
        if (!other.CompareTag(playerTag)) return;

        sfx?.PlayNow();     // 폭발 사운드 발사
        _done = true;
    }
}
