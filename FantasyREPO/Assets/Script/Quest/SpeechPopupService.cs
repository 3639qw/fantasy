
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpeechPopupService : MonoBehaviour
{
    public static SpeechPopupService I { get; private set; }

    [SerializeField] private SpeechPopup popupPrefab;
    [SerializeField] private int warmCount = 3;

    readonly List<SpeechPopup> pool = new();

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        // DontDestroyOnLoad(gameObject);   // 원하면 사용

        StartCoroutine(InitAfterCamera());  // ✅ 카메라 준비 후 초기화
        SceneManager.activeSceneChanged += (_, __) => StartCoroutine(SetWorldCameraForAll());
    }

    IEnumerator InitAfterCamera()
    {
        yield return new WaitUntil(() => Camera.main != null);
        for (int i = 0; i < warmCount; i++) pool.Add(Create());
        yield return SetWorldCameraForAll();
    }

    IEnumerator SetWorldCameraForAll()
    {
        yield return new WaitUntil(() => Camera.main != null);
        foreach (var sp in pool)
        {
            if (sp && sp.TryGetComponent(out Canvas c) && c.renderMode == RenderMode.WorldSpace)
                c.worldCamera = Camera.main;
        }
    }

    SpeechPopup Create()
    {
        var go = Instantiate(popupPrefab.gameObject);
        go.transform.SetParent(transform, true);  // 부모 스케일 영향 방지
        go.transform.localScale = Vector3.one;
        go.SetActive(false);
        return go.GetComponent<SpeechPopup>();
    }

    SpeechPopup Rent()
    {
        for (int i = pool.Count - 1; i >= 0; i--) if (pool[i] == null) pool.RemoveAt(i);
        foreach (var sp in pool) if (sp && !sp.gameObject.activeSelf) return sp;
        var created = Create(); pool.Add(created); return created;
    }

    public void Show(Transform target, string text, float duration = 1.6f, Vector2? offset = null)
    {
        if (!popupPrefab || !target) return;
        var sp = Rent();
        sp.gameObject.SetActive(true);
        sp.Play(target, text, duration, offset);
    }

    internal void HideAllActive()
    {
        throw new NotImplementedException();
    }
}
