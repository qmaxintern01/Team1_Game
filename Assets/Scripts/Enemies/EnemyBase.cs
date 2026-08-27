using System.Collections;
using Team1.Result;
using UnityEngine;

namespace Team1
{
    [RequireComponent(typeof(Health))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("ステータス")]
        [SerializeField] protected int _maxHp = 70;
        [SerializeField] protected int _oilRecoveryAmount = 10;

        [Header("ドロップアイテム")]
        [SerializeField] protected OilRecoveryItem _dropItemPrefab;

        [Header("移動・索敵")]
        [SerializeField] protected float _detectionRange = 8f;
        [SerializeField] protected float _attackRange = 1.5f;
        [SerializeField] protected float _attackCooldown = 1.5f;
        [SerializeField] protected float _wallCollisionRadius = 0.45f;
        [SerializeField] protected LayerMask _wallLayer;

        // EnemyPatrolが追跡開始距離として参照する。索敵距離の情報源をここに一本化する
        public float DetectionRange => _detectionRange;

        [Header("攻撃判定")]
        [SerializeField] protected AttackHitbox _attackHitbox;
        [SerializeField] protected float _hitActiveDuration = 0.15f;

        [Header("演出")]
        [SerializeField] protected AttackRangeIndicator _attackRangeIndicator;
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected Color _damageFlashColor = Color.red;
        [SerializeField] protected float _damageFlashDuration = 0.1f;

        public int OilRecoveryAmount => _oilRecoveryAmount;

        protected GameObject _player;
        protected Health _health;
        private IFacingDirection _facingDirection;
        private Color _defaultSpriteColor;
        private Coroutine _damageFlashRoutine;

        private bool _lastHitWasKnife;
        private bool _lastHitWasBackstab;

        // 予備動作(テレグラフ)や着地演出の最中は、移動・次の攻撃判定を止めるためのフラグ
        protected bool _isBusy;

        // テレグラフ演出やジャンプ攻撃など、EnemyBase側が直接transformを動かしている間はEnemyPatrolの移動を止めるために公開する
        public bool IsBusy => _isBusy;

        private float _attackTimer;

        protected virtual void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _health = GetComponent<Health>();
            _facingDirection = GetComponent<IFacingDirection>();

            if (_spriteRenderer == null)
            {
                // 見た目に使われていない(Enabled=false)SpriteRendererを誤って拾わないようにする
                foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer.enabled)
                    {
                        _spriteRenderer = renderer;
                        break;
                    }
                }
            }

            // エラー確認
            Debug.Assert(_player != null, $"{nameof(_player)} is not assigned.", this);
            Debug.Assert(_attackHitbox != null, $"{nameof(_attackHitbox)} is not assigned.", this);

            if (_spriteRenderer != null)
            {
                _defaultSpriteColor = _spriteRenderer.color;
            }

            if (_wallLayer.value == 0)
            {
                _wallLayer = LayerMask.GetMask("Wall");
            }
        }

        protected virtual void OnEnable()
        {
            _health.Initialize(_maxHp);
            _health.OnDied += HandleDied;
            _health.OnDamaged += HandleDamaged;
        }

        protected virtual void OnDisable()
        {
            _health.OnDied -= HandleDied;
            _health.OnDamaged -= HandleDamaged;
        }

        // 撃破実績(ナイフ撃破数・背面撃破数)の集計に使うため、被弾のたびに直近のダメージ発生源を記録しておく
        public void NotifyHitSource(bool isKnife, bool isBackstab)
        {
            _lastHitWasKnife = isKnife;
            _lastHitWasBackstab = isBackstab;
        }

        protected virtual void Update()
        {
            // ゲーム開始カウントダウン等でTime.timeScale=0の間は、
            // _attackTimerの初期値が0のままだと初回Updateで攻撃条件を満たしてしまうため明示的に止める
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (_player == null || _health.IsDead)
            {
                return;
            }



            _attackTimer -= Time.deltaTime;

            // 移動はEnemyPatrolが一元管理するため、ここでは攻撃可否の判定のみ行う
            float distance = Vector3.Distance(transform.position, _player.transform.position);
            if (distance <= _attackRange && !_isBusy && _attackTimer <= 0f)
            {
                PerformAttack();
                _attackTimer = _attackCooldown;
            }
        }

        private void LateUpdate()
        {
            // 表示順をY座標で揃える(プレイヤー側はMovePlayerで同じ計算式を使用)
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = YSortConfig.CalculateSortingOrder(transform.position.y);
            }
        }

        protected abstract void PerformAttack();

        // 予兆表示・攻撃判定の双方で同じ向きを使うための取得口。IsBusy中はEnemyPatrolが向き更新を止めるため、
        // 攻撃を通して(予兆〜着弾まで)一定の値になる。
        // スプライトが実際に表示している上下左右4方向(DiscreteFacingDirection)を使うことで、見た目とズレないようにする
        protected Vector2 GetFacingDirection()
        {
            return _facingDirection != null ? _facingDirection.DiscreteFacingDirection : Vector2.up;
        }

        // 攻撃判定用ヒットボックスを一定時間だけ有効化し、Collider2Dのトリガーでダメージ対象を検出する
        protected IEnumerator ActivateHitboxRoutine(int damage, float radius, Vector2 facingDirection)
        {
            if (_attackHitbox == null)
            {
                yield break;
            }

            _attackHitbox.Activate(damage, radius, facingDirection);
            yield return new WaitForSeconds(_hitActiveDuration);
            _attackHitbox.Deactivate();
        }

        // 危険範囲を予告表示してから、一定時間後にその範囲へダメージを与える
        // recoveryTime: 大技の後の隙(ダウン・硬直)。IsBusyがtrueの間はEnemyPatrolの追尾・旋回が停止するため、
        // ここを延ばすほどプレイヤーが横移動で背後に回り込む猶予が長くなる
        protected IEnumerator TelegraphAndDealDamage(int damage, float radius, float telegraphTime, float recoveryTime = 0f)
        {
            _isBusy = true;
            // 予兆表示と実際の判定がズレないよう、この攻撃を通して使う向きをここで一度だけ取得する
            Vector2 facingDirection = GetFacingDirection();
            if (_attackRangeIndicator != null)
            {
                _attackRangeIndicator.Show(radius, facingDirection);
            }

            yield return new WaitForSeconds(telegraphTime);

            if (_attackRangeIndicator != null)
            {
                _attackRangeIndicator.Hide();
            }

            yield return ActivateHitboxRoutine(damage, radius, facingDirection);

            if (recoveryTime > 0f)
            {
                yield return new WaitForSeconds(recoveryTime);
            }

            _isBusy = false;
        }

        private void HandleDamaged(int amount)
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            if (_damageFlashRoutine != null)
            {
                StopCoroutine(_damageFlashRoutine);
            }

            _damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
        }

        private IEnumerator DamageFlashRoutine()
        {
            _spriteRenderer.color = _damageFlashColor;
            yield return new WaitForSeconds(_damageFlashDuration);
            _spriteRenderer.color = _defaultSpriteColor;
            _damageFlashRoutine = null;
        }

        private void HandleDied()
        {
            NotifyKillToTracker();

            if (_oilRecoveryAmount > 0 && _dropItemPrefab != null)
            {
                OilRecoveryItem drop = Instantiate(_dropItemPrefab, transform.position, Quaternion.identity);
                drop.SetRecoveryAmount(_oilRecoveryAmount);
            }

            Destroy(gameObject);
        }

        // リザルト集計はWeakEnemy/MidBossの撃破のみを対象とする(BigBossはゲームクリア判定側で扱うため対象外)
        private void NotifyKillToTracker()
        {
            if (this is BigBoss || RunResultTracker.Instance == null)
            {
                return;
            }

            RunResultTracker.Instance.NotifyEnemyKilled(this is MidBoss, _lastHitWasKnife, _lastHitWasBackstab);
        }
    }
}