using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    [SerializeField] private GameObject _player;

    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player");

        // エラー確認
        Debug.Assert(_player != null, $"{nameof(_player)} is not assigned.", this);
    }

    // Update is called once per frame
    private void LateUpdate()
    {
        this.transform.position = new Vector3(_player.transform.position.x, _player.transform.position.y, this.transform.position.z);
    }
}
