using UnityEngine;

public class DebugProximity : MonoBehaviour
{
    bool inRange;
    void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) { inRange = true; Debug.Log("ENTER angel"); } }
    void OnTriggerExit2D(Collider2D other) { if (other.CompareTag("Player")) { inRange = false; Debug.Log("EXIT angel"); } }
    void Update() { if (inRange && Input.GetKeyDown(KeyCode.F)) Debug.Log("F pressed near angel"); }
}
