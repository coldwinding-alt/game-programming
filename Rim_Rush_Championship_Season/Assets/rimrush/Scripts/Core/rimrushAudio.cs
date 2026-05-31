// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushAudio 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

using System.Collections.Generic;
using UnityEngine;

namespace rimrush
{
    public sealed class rimrushAudio : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource musicSource;
        private AudioSource sfxSource;

        public bool MusicEnabled { get; private set; } = true;
        public bool SfxEnabled { get; private set; } = true;

        public static rimrushAudio Instance { get; private set; }

        /// <summary>
        /// Executes Create for the rimrushAudio workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static rimrushAudio Create(Transform parent)
        {
            if (Instance != null)
            {
                if (parent != null && Instance.transform.parent != parent)
                {
                    Instance.transform.SetParent(parent, false);
                }

                return Instance;
            }

            var go = new GameObject("rimrushAudio");
            go.transform.SetParent(parent, false);
            Instance = go.AddComponent<rimrushAudio>();
            Instance.musicSource = go.AddComponent<AudioSource>();
            Instance.musicSource.loop = true;
            Instance.musicSource.volume = 0.5f;
            Instance.sfxSource = go.AddComponent<AudioSource>();
            return Instance;
        }

        /// <summary>
        /// Executes On Destroy for the rimrushAudio workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Executes Play Music for the rimrushAudio workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="key">Input value used by this step of the workflow.</param>
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
        /// Executes Stop Music for the rimrushAudio workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void StopMusic()
        {
            musicSource.Stop();
        }

        /// <summary>
        /// Executes Play for the rimrushAudio workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="key">Input value used by this step of the workflow.</param>
        /// <param name="volume">Input value used by this step of the workflow.</param>
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
        /// Executes Toggle Music for the rimrushAudio workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void ToggleMusic()
        {
            MusicEnabled = !MusicEnabled;
            musicSource.mute = !MusicEnabled;
        }

        /// <summary>
        /// Executes Toggle Sfx for the rimrushAudio workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void ToggleSfx()
        {
            SfxEnabled = !SfxEnabled;
        }

        /// <summary>
        /// Executes Load for the rimrushAudio workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="key">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private AudioClip Load(string key)
        {
            if (!clips.TryGetValue(key, out var clip))
            {
                clip = Resources.Load<AudioClip>($"rimrush/Sound/{key}");
                clips[key] = clip;
            }

            return clip;
        }
    }
}
