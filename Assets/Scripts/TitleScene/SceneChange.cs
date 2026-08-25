using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    public void EnterGame()
    {
        SceneManager.LoadScene("GameScene");
    }
}
