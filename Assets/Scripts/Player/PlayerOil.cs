using UnityEngine;

namespace Team1
{
    // 敵撃破時にEnemyBaseから加算される、プレイヤーのオイル(回復資源)を保持する
    public class PlayerOil : MonoBehaviour
    {
        [SerializeField] private int _maxOil = 100;
        [SerializeField] private int _currentOil;

        public int CurrentOil => _currentOil;
        public int MaxOil => _maxOil;

        private void Awake()
        {
            _currentOil = Mathf.Clamp(_currentOil, 0, _maxOil);
        }

        public void AddOil(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _currentOil = Mathf.Clamp(_currentOil + amount, 0, _maxOil);
        }
    }
}
