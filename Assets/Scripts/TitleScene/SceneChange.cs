using Team1;
using UnityEngine;

public class SceneChange : MonoBehaviour
{
    private void Awake()
    {
        // ゲームオーバー等でTime.timeScaleが0のまま遷移してきても、タイトル操作が止まらないようにする
        Time.timeScale = 1f;
    }

    public void EnterGame()
    {
        SceneTransitionManager.LoadScene("GameScene");
    }
}
