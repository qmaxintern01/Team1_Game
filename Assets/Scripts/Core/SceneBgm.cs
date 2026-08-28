using UnityEngine;

namespace Team1
{
    // シーンに1つ置くだけで、そのシーン用のBGMを再生する。AudioManagerのBGMリストに登録した名前を指定する
    public class SceneBgm : MonoBehaviour
    {
        [SerializeField] private string _trackName;

        private void Start()
        {
            if (string.IsNullOrEmpty(_trackName))
            {
                return;
            }

            AudioManager.Instance?.PlayBgm(_trackName);
        }
    }
}
