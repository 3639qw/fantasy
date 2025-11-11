using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneStart : MonoBehaviour
{

    public void GameStart(){
        SceneManager.LoadScene("Overworld_MSM");
    }
    public void Quit(){
        Application.Quit();
    }
    
}
