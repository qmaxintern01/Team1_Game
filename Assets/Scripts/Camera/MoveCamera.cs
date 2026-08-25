using UnityEngine;

namespace Team1
{
    public class MoveCamera : MonoBehaviour
    {
        [SerializeField] private GameObject _player;

        private void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player");

            // エラー確認
            Debug.Assert(_player != null, $"{nameof(_player)} is not assigned.", this);
        }

        private void LateUpdate()
        {
            transform.position = new Vector3(_player.transform.position.x, _player.transform.position.y, transform.position.z);
        }
    }
}
