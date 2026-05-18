using System.Collections.Generic;
using UnityEngine;

namespace BasketballLegends2020
{
    public sealed class BLAudio : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource musicSource;
        private AudioSource sfxSource;

        public bool MusicEnabled { get; private set; } = true;
        public bool SfxEnabled { get; private set; } = true;

        public static BLAudio Instance { get; private set; }

        public static BLAudio Create(Transform parent)
        {
            if (Instance != null)
            {
                if (parent != null && Instance.transform.parent != parent)
                {
                    Instance.transform.SetParent(parent, false);
                }

                return Instance;
            }

            var go = new GameObject("BLAudio");
            go.transform.SetParent(parent, false);
            Instance = go.AddComponent<BLAudio>();
            Instance.musicSource = go.AddComponent<AudioSource>();
            Instance.musicSource.loop = true;
            Instance.musicSource.volume = 0.5f;
            Instance.sfxSource = go.AddComponent<AudioSource>();
            return Instance;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

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

        public void StopMusic()
        {
            musicSource.Stop();
        }

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

        public void ToggleMusic()
        {
            MusicEnabled = !MusicEnabled;
            musicSource.mute = !MusicEnabled;
        }

        public void ToggleSfx()
        {
            SfxEnabled = !SfxEnabled;
        }

        private AudioClip Load(string key)
        {
            if (!clips.TryGetValue(key, out var clip))
            {
                clip = Resources.Load<AudioClip>($"BL2020/Sound/{key}");
                clips[key] = clip;
            }

            return clip;
        }
    }
}
