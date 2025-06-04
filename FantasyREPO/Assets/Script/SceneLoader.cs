using System;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
#endif

public class SceneLoader : MonoBehaviour
{
    private GameManager gm;
    
    [Header("이동할 씬 이름 (Build Settings에 등록되어 있어야 함)")]
    [SceneSelector]
    public string sceneName;

    [TagSelector]
    public string playerTag = "Player";

    
    [Header("반응할 키 선택")]
    public KeyCode triggerKey = KeyCode.Space;  // KeyCode 타입으로 드롭다운 표시됨

    private bool isPlayerInContact = false;

    public void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            if (SceneManager.GetActiveScene().name == "Overworld")
            {
                GameObject.FindWithTag("GameController").GetComponent<TilemapSerializer>().SaveTilemapToJson();    
            }
            
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("씬 이름이 비어있습니다.");
        }
    }

    private void Start()
    {
        gm = GameManager.Instance;
    }

    private void Update()
    {
        if (isPlayerInContact && Input.GetKeyDown(triggerKey))
        {
            if (SceneExistsInBuildSettings(sceneName))
            {
                LoadScene();
            }
            else
            {
                Debug.LogError($"씬 '{sceneName}'은(는) Build Settings에 존재하지 않습니다.");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInContact = true;
            if (SceneManager.GetActiveScene().name == "Overworld")
            {
                gm.playerStartPosition = other.transform.position;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInContact = false;
            // gm.playerStartPosition = null;
        }
    }
    
    
    
    

    private bool SceneExistsInBuildSettings(string name)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(name)) return false;
        return EditorBuildSettings.scenes.Any(scene =>
            scene.enabled &&
            Path.GetFileNameWithoutExtension(scene.path).Equals(name));
#else
        return false;
#endif
    }

#if UNITY_EDITOR

    // 태그 선택 애트리뷰트 및 드로어
    public class TagSelectorAttribute : PropertyAttribute { }

    [CustomPropertyDrawer(typeof(TagSelectorAttribute))]
    public class TagSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }

    // 씬 선택 애트리뷰트 및 드로어
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
            {
                property.stringValue = sceneNames[index];
            }
        }
    }

#endif
}
