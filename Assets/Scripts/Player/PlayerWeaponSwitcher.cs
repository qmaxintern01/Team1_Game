using UnityEngine;
using UnityEngine.InputSystem;

namespace Team1
{
    // 武器切替の枠組み。現状はナイフのみ攻撃処理を実装済みで、銃2種は選択できるだけの状態
    public class PlayerWeaponSwitcher : MonoBehaviour
    {

        private static readonly WeaponType[] Weapons =
        {
            WeaponType.Knife,
            WeaponType.AssaultRifle,
            WeaponType.GrenadeLauncher,
        };

        // 数字キー(123)/マウスホイールでの武器切替は既存のInput Actionsアセットに未定義のため、専用のInputActionをコードで生成する
        private InputAction _select1Action;
        private InputAction _select2Action;
        private InputAction _select3Action;
        private InputAction _scrollAction;

        private int _currentIndex;

        public WeaponType CurrentWeapon => Weapons[_currentIndex];

        // UI表示用に現在の武器種名を日本語で返す
        public string GetCurrentWeaponName()
        {
            switch (CurrentWeapon)
            {
                case WeaponType.Knife:
                    return "ナイフ";
                case WeaponType.AssaultRifle:
                    return "アサルトライフル";
                case WeaponType.GrenadeLauncher:
                    return "グレネードランチャー";
                default:
                    return CurrentWeapon.ToString();
            }
        }

        private void OnEnable()
        {
            _select1Action = new InputAction("SelectWeapon1", binding: "<Keyboard>/1");
            _select2Action = new InputAction("SelectWeapon2", binding: "<Keyboard>/2");
            _select3Action = new InputAction("SelectWeapon3", binding: "<Keyboard>/3");
            _scrollAction = new InputAction("WeaponScroll", InputActionType.Value, binding: "<Mouse>/scroll/y");

            _select1Action.performed += HandleSelect1;
            _select2Action.performed += HandleSelect2;
            _select3Action.performed += HandleSelect3;
            _scrollAction.performed += HandleScroll;

            _select1Action.Enable();
            _select2Action.Enable();
            _select3Action.Enable();
            _scrollAction.Enable();
        }

        private void OnDisable()
        {
            _select1Action.performed -= HandleSelect1;
            _select2Action.performed -= HandleSelect2;
            _select3Action.performed -= HandleSelect3;
            _scrollAction.performed -= HandleScroll;

            _select1Action.Dispose();
            _select2Action.Dispose();
            _select3Action.Dispose();
            _scrollAction.Dispose();
        }

        private void HandleSelect1(InputAction.CallbackContext context) => SelectWeapon(0);
        private void HandleSelect2(InputAction.CallbackContext context) => SelectWeapon(1);
        private void HandleSelect3(InputAction.CallbackContext context) => SelectWeapon(2);

        private void HandleScroll(InputAction.CallbackContext context)
        {
            float scroll = context.ReadValue<float>();
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            int direction = scroll > 0f ? 1 : -1;
            SelectWeapon((_currentIndex + direction + Weapons.Length) % Weapons.Length);
        }

        private void SelectWeapon(int index)
        {
            _currentIndex = index;
            Debug.Log($"武器切替: {CurrentWeapon}");
        }
    }
}
