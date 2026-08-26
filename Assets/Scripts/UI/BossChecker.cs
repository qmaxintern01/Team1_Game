using UnityEngine;
using UnityEngine.SceneManagement;

namespace Team1
{
    public class BossChecker : MonoBehaviour
    {
        [SerializeField] private BigBoss _bigBoss;

        private void Awake()
        {
            _bigBoss = GameObject.FindObjectOfType<BigBoss>();

            // エラー検出
            Debug.Assert(_bigBoss != null, $"{nameof(_bigBoss)} is not assigned.", this);
        }

        // Update is called once per frame
        private void Update()
        {
            if( _bigBoss == null)
            {
                // BigBossがシーン上に存在しない場合、ボス戦が終了したとみなし、ゲームクリア処理を行う
                Debug.Log("BigBoss is defeated. Game Clear!");
                //SceneManager.LoadScene("GameClearScene");
                return;
            }
        }
    }
}
