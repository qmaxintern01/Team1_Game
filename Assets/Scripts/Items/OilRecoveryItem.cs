using UnityEngine;

namespace Team1
{
    // フィールドに配置する回復アイテム。プレイヤーが接触するとオイルを回復して消える
    [RequireComponent(typeof(Collider2D))]
    public class OilRecoveryItem : MonoBehaviour
    {
        [SerializeField] private int _recoveryAmount = 25;

        // 敵撃破時のドロップなど、生成後に回復量を上書きしたい場合に使う
        public void SetRecoveryAmount(int amount)
        {
            _recoveryAmount = amount;
        }

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (!other.TryGetComponent(out PlayerOil playerOil))
            {
                Debug.LogWarning($"OilRecoveryItem: PlayerOil component not found on {other.name}.", this);
                return;
            }

            playerOil.AddOil(_recoveryAmount);
            AudioManager.Instance?.PlayItemPickupSe();
            Destroy(gameObject);
        }
    }
}
