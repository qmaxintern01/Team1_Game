using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Team1
{
    /// <summary>
    /// BGM/SEを一元管理するシングルトン。インスペクターの各スロットに音声ファイルをドラッグ&ドロップするだけで
    /// 再生できるようにする(コード変更不要)。DontDestroyOnLoadでシーンをまたいで生存する。
    /// SEの各カテゴリは配列にしているため、複数の音を登録するとランダムに再生され、単調さを避けられる。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Serializable]
        private class NamedClip
        {
            public string Name;
            public AudioClip Clip;
            public bool Loop = true;
        }

        // SEもBGMと同様にクリップごとにループの有無を選べるようにする。
        // Loopがtrueのクリップは専用のAudioSourceで再生し続け、falseのものは従来通りPlayOneShotで重ね鳴きする
        [Serializable]
        private class SeClip
        {
            public AudioClip Clip;
            public bool Loop;
        }

        public static AudioManager Instance { get; private set; }

        [Header("BGM (名前を付けて登録し、PlayBgm(名前)で再生)")]
        [SerializeField] private List<NamedClip> _bgmTracks = new List<NamedClip>();
        [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.6f;
        [SerializeField] private float _bgmFadeDuration = 0.5f;

        [Header("SE - 敵の攻撃(WeakEnemy)")]
        [SerializeField] private List<SeClip> _weakEnemyAttackClips = new List<SeClip>();

        [Header("SE - 敵の攻撃(MidBoss 近接)")]
        [SerializeField] private List<SeClip> _midBossMeleeAttackClips = new List<SeClip>();

        [Header("SE - 敵の攻撃(MidBoss 溜め攻撃)")]
        [SerializeField] private List<SeClip> _midBossChargeAttackClips = new List<SeClip>();

        [Header("SE - 敵の攻撃(BigBoss 近接)")]
        [SerializeField] private List<SeClip> _bigBossMeleeAttackClips = new List<SeClip>();

        [Header("SE - 敵の攻撃(BigBoss 範囲薙ぎ払い)")]
        [SerializeField] private List<SeClip> _bigBossSweepAttackClips = new List<SeClip>();

        [Header("SE - 敵の攻撃(BigBoss ジャンプスタンプ)")]
        [SerializeField] private List<SeClip> _bigBossStompAttackClips = new List<SeClip>();

        [Header("SE - プレイヤーの攻撃(ナイフ)")]
        [FormerlySerializedAs("_playerAttackClips")]
        [SerializeField] private List<SeClip> _knifeAttackClips = new List<SeClip>();

        [Header("SE - プレイヤーの攻撃(銃)")]
        [SerializeField] private List<SeClip> _gunAttackClips = new List<SeClip>();

        [Header("SE - 武器切替(ナイフ)")]
        [SerializeField] private List<SeClip> _switchToKnifeClips = new List<SeClip>();

        [Header("SE - 武器切替(銃)")]
        [SerializeField] private List<SeClip> _switchToGunClips = new List<SeClip>();

        [Header("SE - ヒット(被弾)")]
        [SerializeField] private List<SeClip> _hitClips = new List<SeClip>();

        [Header("SE - アイテム取得")]
        [SerializeField] private List<SeClip> _itemPickupClips = new List<SeClip>();

        [Header("SE - 敵撃破(WeakEnemy)")]
        [SerializeField] private List<SeClip> _weakEnemyDefeatedClips = new List<SeClip>();

        [Header("SE - 敵撃破(MidBoss)")]
        [SerializeField] private List<SeClip> _midBossDefeatedClips = new List<SeClip>();

        [Header("SE - 敵撃破(BigBoss)")]
        [SerializeField] private List<SeClip> _bigBossDefeatedClips = new List<SeClip>();

        [Header("SE - プレイヤーの歩行")]
        [SerializeField] private List<SeClip> _playerFootstepClips = new List<SeClip>();
        // 歩行SEはUpdateから毎フレーム呼ばれても連打にならないよう、最小間隔を空けて間引く(ループ再生時は無視される)
        [SerializeField] private float _footstepInterval = 0.35f;

        [Header("SE共通音量")]
        [SerializeField, Range(0f, 1f)] private float _seVolume = 1f;

        private AudioSource _bgmSource;
        private AudioSource _seSource;
        private AudioSource _weakEnemyAttackLoopSource;
        private AudioSource _midBossMeleeAttackLoopSource;
        private AudioSource _midBossChargeAttackLoopSource;
        private AudioSource _bigBossMeleeAttackLoopSource;
        private AudioSource _bigBossSweepAttackLoopSource;
        private AudioSource _bigBossStompAttackLoopSource;
        private AudioSource _knifeAttackLoopSource;
        private AudioSource _gunAttackLoopSource;
        private AudioSource _switchToKnifeLoopSource;
        private AudioSource _switchToGunLoopSource;
        private AudioSource _hitLoopSource;
        private AudioSource _itemPickupLoopSource;
        private AudioSource _weakEnemyDefeatedLoopSource;
        private AudioSource _midBossDefeatedLoopSource;
        private AudioSource _bigBossDefeatedLoopSource;
        private AudioSource _playerFootstepLoopSource;
        private Coroutine _bgmFadeRoutine;
        private string _currentBgmName;
        private float _lastFootstepTime = float.NegativeInfinity;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.volume = _bgmVolume;

            _seSource = gameObject.AddComponent<AudioSource>();
            _seSource.loop = false;
            _seSource.playOnAwake = false;
        }

        // インスペクターでBGMに登録した名前を指定して再生する。既に同じ曲を再生中なら何もしない
        public void PlayBgm(string trackName)
        {
            if (_currentBgmName == trackName && _bgmSource.isPlaying)
            {
                return;
            }

            NamedClip track = FindTrack(trackName);
            if (track == null || track.Clip == null)
            {
                Debug.LogWarning($"BGM '{trackName}' が見つかりません。AudioManagerのBGMリストに登録してください。", this);
                return;
            }

            _currentBgmName = trackName;

            if (_bgmFadeRoutine != null)
            {
                StopCoroutine(_bgmFadeRoutine);
            }

            _bgmFadeRoutine = StartCoroutine(CrossFadeBgmRoutine(track.Clip, track.Loop));
        }

        public void StopBgm()
        {
            _currentBgmName = null;

            if (_bgmFadeRoutine != null)
            {
                StopCoroutine(_bgmFadeRoutine);
            }

            _bgmFadeRoutine = StartCoroutine(FadeOutAndStopRoutine());
        }

        private IEnumerator CrossFadeBgmRoutine(AudioClip nextClip, bool loop)
        {
            yield return FadeVolume(_bgmSource, _bgmSource.volume, 0f, _bgmFadeDuration * 0.5f);

            _bgmSource.clip = nextClip;
            _bgmSource.loop = loop;
            _bgmSource.Play();

            yield return FadeVolume(_bgmSource, 0f, _bgmVolume, _bgmFadeDuration * 0.5f);
            _bgmFadeRoutine = null;
        }

        private IEnumerator FadeOutAndStopRoutine()
        {
            yield return FadeVolume(_bgmSource, _bgmSource.volume, 0f, _bgmFadeDuration);
            _bgmSource.Stop();
            _bgmFadeRoutine = null;
        }

        // シーン遷移中(Time.timeScale=0)でもフェードが完了するようunscaledDeltaTimeを使う
        private static IEnumerator FadeVolume(AudioSource source, float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                source.volume = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            source.volume = to;
        }

        public void PlayWeakEnemyAttackSe() => PlaySe(_weakEnemyAttackClips, ref _weakEnemyAttackLoopSource);
        public void StopWeakEnemyAttackSe() => StopLoopSe(_weakEnemyAttackLoopSource);

        public void PlayMidBossMeleeAttackSe() => PlaySe(_midBossMeleeAttackClips, ref _midBossMeleeAttackLoopSource);
        public void StopMidBossMeleeAttackSe() => StopLoopSe(_midBossMeleeAttackLoopSource);

        public void PlayMidBossChargeAttackSe() => PlaySe(_midBossChargeAttackClips, ref _midBossChargeAttackLoopSource);
        public void StopMidBossChargeAttackSe() => StopLoopSe(_midBossChargeAttackLoopSource);

        public void PlayBigBossMeleeAttackSe() => PlaySe(_bigBossMeleeAttackClips, ref _bigBossMeleeAttackLoopSource);
        public void StopBigBossMeleeAttackSe() => StopLoopSe(_bigBossMeleeAttackLoopSource);

        public void PlayBigBossSweepAttackSe() => PlaySe(_bigBossSweepAttackClips, ref _bigBossSweepAttackLoopSource);
        public void StopBigBossSweepAttackSe() => StopLoopSe(_bigBossSweepAttackLoopSource);

        public void PlayBigBossStompAttackSe() => PlaySe(_bigBossStompAttackClips, ref _bigBossStompAttackLoopSource);
        public void StopBigBossStompAttackSe() => StopLoopSe(_bigBossStompAttackLoopSource);

        public void PlayKnifeAttackSe() => PlaySe(_knifeAttackClips, ref _knifeAttackLoopSource);
        public void StopKnifeAttackSe() => StopLoopSe(_knifeAttackLoopSource);

        public void PlayGunAttackSe() => PlaySe(_gunAttackClips, ref _gunAttackLoopSource);
        public void StopGunAttackSe() => StopLoopSe(_gunAttackLoopSource);

        public void PlaySwitchToKnifeSe() => PlaySe(_switchToKnifeClips, ref _switchToKnifeLoopSource);
        public void StopSwitchToKnifeSe() => StopLoopSe(_switchToKnifeLoopSource);

        public void PlaySwitchToGunSe() => PlaySe(_switchToGunClips, ref _switchToGunLoopSource);
        public void StopSwitchToGunSe() => StopLoopSe(_switchToGunLoopSource);

        public void PlayHitSe() => PlaySe(_hitClips, ref _hitLoopSource);
        public void StopHitSe() => StopLoopSe(_hitLoopSource);

        public void PlayItemPickupSe() => PlaySe(_itemPickupClips, ref _itemPickupLoopSource);
        public void StopItemPickupSe() => StopLoopSe(_itemPickupLoopSource);

        public void PlayWeakEnemyDefeatedSe() => PlaySe(_weakEnemyDefeatedClips, ref _weakEnemyDefeatedLoopSource);
        public void StopWeakEnemyDefeatedSe() => StopLoopSe(_weakEnemyDefeatedLoopSource);

        public void PlayMidBossDefeatedSe() => PlaySe(_midBossDefeatedClips, ref _midBossDefeatedLoopSource);
        public void StopMidBossDefeatedSe() => StopLoopSe(_midBossDefeatedLoopSource);

        public void PlayBigBossDefeatedSe() => PlaySe(_bigBossDefeatedClips, ref _bigBossDefeatedLoopSource);
        public void StopBigBossDefeatedSe() => StopLoopSe(_bigBossDefeatedLoopSource);

        public void PlayPlayerFootstepSe()
        {
            if (Time.time - _lastFootstepTime < _footstepInterval)
            {
                return;
            }

            _lastFootstepTime = Time.time;
            PlaySe(_playerFootstepClips, ref _playerFootstepLoopSource);
        }

        public void StopPlayerFootstepSe() => StopLoopSe(_playerFootstepLoopSource);

        // Loopがtrueのクリップは専用AudioSourceで再生し続け(既に同じクリップを再生中なら何もしない)、
        // falseのクリップは共有の_seSourceでPlayOneShotする(複数の音が重なっても途切れない)
        private void PlaySe(List<SeClip> clips, ref AudioSource loopSource)
        {
            if (clips == null || clips.Count == 0)
            {
                return;
            }

            SeClip entry = clips[UnityEngine.Random.Range(0, clips.Count)];
            if (entry?.Clip == null)
            {
                return;
            }

            if (!entry.Loop)
            {
                _seSource.PlayOneShot(entry.Clip, _seVolume);
                return;
            }

            if (loopSource == null)
            {
                loopSource = gameObject.AddComponent<AudioSource>();
                loopSource.playOnAwake = false;
                loopSource.loop = true;
            }

            loopSource.volume = _seVolume;

            if (loopSource.clip != entry.Clip || !loopSource.isPlaying)
            {
                loopSource.clip = entry.Clip;
                loopSource.Play();
            }
        }

        private static void StopLoopSe(AudioSource loopSource)
        {
            if (loopSource != null && loopSource.isPlaying)
            {
                loopSource.Stop();
            }
        }

        private NamedClip FindTrack(string trackName)
        {
            foreach (NamedClip entry in _bgmTracks)
            {
                if (entry.Name == trackName)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
