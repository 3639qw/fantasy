using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoader_SFX : MonoBehaviour
{
    [Header("이동할 씬 이름 (Build Settings에 등록되어 있어야 함)")]
    [SceneSelector] public string sceneName;

    [Header("플레이어 태그 / 키")]
    [TagSelector] public string playerTag = "Player";
    public KeyCode triggerKey = KeyCode.Space;

    [Header("사운드")]
    public AudioClip moveClip;          // 씬 이동 시 재생
    public AudioClip arriveClip;        // 새 씬 진입 시 재생(선택)
    [Range(0, 1)] public float moveVol = 1f;
    [Range(0, 1)] public float arriveVol = 1f;

    [Header("재생/로딩 옵션")]
    public bool delayLoadUntilMoveClipEnds = true; // 이동 SFX 끝나면 로드
    public float extraDelay = 0f;                  // 추가 지연
    public bool oneShotSurviveLoad = true;         // 이동 SFX를 DontDestroyOnLoad 원샷으로

    bool _playerIn;
    GameManager _gm;

    void Start()
    {
        _gm = GameManager.Instance;
        if (arriveClip) SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (!_playerIn) return;
        if (!Input.GetKeyDown(triggerKey)) return;

        if (!SceneExistsInBuildSettings(sceneName))
        {
            Debug.LogError($"씬 '{sceneName}' 이(가) Build Settings에 없습니다.");
            return;
        }

        StartCoroutine(CoLoadWithSFX());
    }

    IEnumerator CoLoadWithSFX()
    {
        // Overworld에서 저장 필요 시
        if (SceneManager.GetActiveScene().name == "Overworld")
        {
            var gc = GameObject.FindWithTag("GameController");
            gc?.GetComponent<TilemapSerializer>()?.SaveTilemapToJson();
        }

        // 이동 사운드 재생
        float wait = 0f;
        if (moveClip)
        {
            if (oneShotSurviveLoad)
                PlayOneShotDontDestroy(moveClip, moveVol);

            else
                PlayLocal(moveClip, moveVol);

            if (delayLoadUntilMoveClipEnds)
                wait = moveClip.length;
        }

        if (wait + extraDelay > 0f)
            yield return new WaitForSeconds(wait + Mathf.Max(0f, extraDelay));

        SceneManager.LoadScene(sceneName);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (arriveClip) PlayOneShotDontDestroy(arriveClip, arriveVol);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerIn = true;
        if (SceneManager.GetActiveScene().name == "Overworld")
            _gm.playerStartPosition = other.transform.position;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) _playerIn = false;
    }

    // ========== 오디오 유틸 ==========

    void PlayLocal(AudioClip clip, float vol)
    {
        var src = GetComponent<AudioSource>();
        if (!src) src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false; src.loop = false; src.spatialBlend = 0f;
        src.PlayOneShot(clip, vol);
    }

    void PlayOneShotDontDestroy(AudioClip clip, float vol)
    {
        var go = new GameObject("[SceneSFX OneShot]");
        DontDestroyOnLoad(go);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false; src.loop = false; src.spatialBlend = 0f;
        src.PlayOneShot(clip, vol);
        Object.Destroy(go, clip.length + 0.1f);
    }

    bool SceneExistsInBuildSettings(string name)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(name)) return false;
        return EditorBuildSettings.scenes.Any(s => s.enabled &&
            Path.GetFileNameWithoutExtension(s.path).Equals(name));
#else
        // 런타임에서는 직접 확인하기 어려우니 빈 문자열 체크만
        return !string.IsNullOrEmpty(name);
#endif
    }

    // ===== 에디터 드로어들(기존과 동일) =====
#if UNITY_EDITOR
    public class TagSelectorAttribute : PropertyAttribute { }
    [CustomPropertyDrawer(typeof(TagSelectorAttribute))]
    public class TagSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
                property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
            else
                EditorGUI.PropertyField(position, property, label);
        }
    }

    public class SceneSelectorAttribute : PropertyAttribute { }
    [CustomPropertyDrawer(typeof(SceneSelectorAttribute))]
    public class SceneSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            string[] sceneNames = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => Path.GetFileNameWithoutExtension(s.path))
                .ToArray();

            int index = Mathf.Max(0, System.Array.IndexOf(sceneNames, property.stringValue));
            index = EditorGUI.Popup(position, label.text, index, sceneNames);
            if (index >= 0 && index < sceneNames.Length)
                property.stringValue = sceneNames[index];
        }
    }
#endif
}
