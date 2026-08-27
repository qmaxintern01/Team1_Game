using UnityEngine;

namespace Team1
{
    /// <summary>
    /// ミニマップ用カメラをプレイヤーのXY座標に追従させる。Z座標は自身の初期値を維持する。
    /// </summary>
    public class MinimapFollow : MonoBehaviour
    {
        [SerializeField] private GameObject _player;

        private float _fixedZ;

        private void Awake()
        {
            _fixedZ = transform.position.z;

            if (_player == null)
            {
                _player = GameObject.FindGameObjectWithTag("Player");
            }

            Debug.Assert(_player != null, $"{nameof(_player)} is not assigned.", this);
        }

        private void LateUpdate()
        {
            if (_player == null)
            {
                return;
            }

            transform.position = new Vector3(_player.transform.position.x, _player.transform.position.y, _fixedZ);
        }
    }
}
