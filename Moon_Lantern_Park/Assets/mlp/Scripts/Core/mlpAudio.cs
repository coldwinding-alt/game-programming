// 游戏音效和背景音乐管理器
// 负责播放音效、切换背景音乐、控制音量大小。所有声音播放都通过这个类来调用。

using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 游戏音效和背景音乐管理器（单例）：负责播放音效、切换背景音乐、控制音量大小。所有声音播放都通过这个类来调用。
    /// </summary>
    public sealed class mlpAudio : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource musicSource;
        private AudioSource sfxSource;

        public bool MusicEnabled { get; private set; } = true;
        public bool SfxEnabled { get; private set; } = true;

        public static mlpAudio Instance { get; private set; }

        /// <summary>
        /// 创建或返回共享的音频管理器单例实例。
        /// </summary>
        /// <param name="parent">音频对象挂载的父级 Transform。</param>
        /// <returns>如果实例已存在则返回现有实例，否则创建新实例。</returns>
        public static mlpAudio Create(Transform parent)
        {
            if (Instance != null)
            {
                if (parent != null && Instance.transform.parent != parent)
                {
                    Instance.transform.SetParent(parent, false);
                }

                return Instance;
            }

            var go = new GameObject("mlpAudio");
            go.transform.SetParent(parent, false);
            Instance = go.AddComponent<mlpAudio>();
            Instance.musicSource = go.AddComponent<AudioSource>();
            Instance.musicSource.loop = true;
            Instance.musicSource.volume = 0.5f;
            Instance.sfxSource = go.AddComponent<AudioSource>();
            return Instance;
        }

        /// <summary>
        /// 当 Unity 销毁此对象时，清除共享实例引用。
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 开始循环播放背景音乐。如果音乐已关闭或同一首曲目正在播放，则不做任何操作。
        /// </summary>
        /// <param name="key">音乐文件的资源名称。</param>
        public void PlayMusic(string key)
        {
            if (!MusicEnabled)
            {
                return;
            }

            var clip = Load(key);
            if (clip == null)
            {
                return;
            }

            if (musicSource.clip == clip && musicSource.isPlaying)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.Play();
        }

        /// <summary>
        /// 停止当前正在播放的背景音乐。
        /// </summary>
        public void StopMusic()
        {
            musicSource.Stop();
        }

        /// <summary>
        /// 播放一次短音效。如果音效已关闭，则不做任何操作。
        /// </summary>
        /// <param name="key">音效文件的资源名称。</param>
        /// <param name="volume">音量，范围从 0（静音）到 1（最大音量）。</param>
        public void Play(string key, float volume = 1f)
        {
            if (!SfxEnabled)
            {
                return;
            }

            var clip = Load(key);
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        /// <summary>
        /// 切换背景音乐的开关状态。
        /// </summary>
        public void ToggleMusic()
        {
            MusicEnabled = !MusicEnabled;
            musicSource.mute = !MusicEnabled;
        }

        /// <summary>
        /// 切换音效的开关状态。
        /// </summary>
        public void ToggleSfx()
        {
            SfxEnabled = !SfxEnabled;
        }

        /// <summary>
        /// 加载音频资源。从 Resources 中按名称加载 AudioClip，加载后会缓存以避免重复加载。
        /// </summary>
        /// <param name="key">音频文件的资源名称。</param>
        /// <returns>加载到的 AudioClip，加载失败时可能为 null。</returns>
        private AudioClip Load(string key)
        {
            if (!clips.TryGetValue(key, out var clip))
            {
                clip = Resources.Load<AudioClip>($"mlp/Sound/{key}");
                clips[key] = clip;
            }

            return clip;
        }
    }
}
