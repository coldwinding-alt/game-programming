// 游戏音效和背景音乐管理器
// 负责播放音效、切换背景音乐、控制音量大小。所有声音播放都通过这个类来调用。

using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    public sealed class mlpAudio : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource musicSource;
        private AudioSource sfxSource;

        public bool MusicEnabled { get; private set; } = true;
        public bool SfxEnabled { get; private set; } = true;

        public static mlpAudio Instance { get; private set; }

        /// <summary>
        /// Create or return the single shared audio manager instance.
        /// </summary>
        /// <param name="parent">The transform that this audio object will be attached to.</param>
        /// <returns>The existing instance if one already exists, otherwise creates a new one.</returns>
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
        /// Clear the shared instance reference when this object is destroyed by Unity.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Start playing a background music track in a loop.
        /// Does nothing if music is disabled or if the same track is already playing.
        /// </summary>
        /// <param name="key">The resource name of the music file.</param>
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
        /// Stop the currently playing background music.
        /// </summary>
        public void StopMusic()
        {
            musicSource.Stop();
        }

        /// <summary>
        /// Play a short sound effect once. Does nothing if sound effects are disabled.
        /// </summary>
        /// <param name="key">The resource name of the sound file.</param>
        /// <param name="volume">Volume from 0 (silent) to 1 (full loud).</param>
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
        /// Turn background music on if it is off, or off if it is on.
        /// </summary>
        public void ToggleMusic()
        {
            MusicEnabled = !MusicEnabled;
            musicSource.mute = !MusicEnabled;
        }

        /// <summary>
        /// Turn sound effects on if they are off, or off if they are on.
        /// </summary>
        public void ToggleSfx()
        {
            SfxEnabled = !SfxEnabled;
        }

        /// <summary>
        /// Executes Load for the mlpAudio workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="key">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
