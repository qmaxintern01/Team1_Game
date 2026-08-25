using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChange : MonoBehaviour
{
    SceneManager sceneManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //sceneManager = SceneManager.GetActiveScene();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void EnterGame()
    {
        SceneManager.LoadScene("GameScene");
    }
}
