using Team1;
using UnityEngine;

public class SceneChange : MonoBehaviour
{
    public void EnterGame()
    {
        SceneTransitionManager.LoadScene("GameScene");
    }
}
