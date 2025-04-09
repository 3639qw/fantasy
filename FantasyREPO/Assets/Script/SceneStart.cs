using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneStart : MonoBehaviour
{

    public void GameStart(){
        SceneManager.LoadScene("Overworld");
    }
    public void Quit(){
        Application.Quit();
    }
    
}
