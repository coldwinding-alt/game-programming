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

            Instance = FindAnyObjectByType<rimrushAudio>(FindObjectsInactive.Include);
            if (Instance != null)
            {
                if (parent != null && Instance.transform.parent != parent)
                {
                    Instance.transform.SetParent(parent, false);
                }

                Instance.EnsureSources();
                return Instance;
            }

            var go = new GameObject("rimrushAudio");
            go.transform.SetParent(parent, false);
            Instance = go.AddComponent<rimrushAudio>();
            Instance.EnsureSources();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureSources();
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

        private void EnsureSources()
        {
            if (musicSource == null || sfxSource == null)
            {
                var sources = GetComponents<AudioSource>();
                if (musicSource == null && sources.Length > 0)
                {
                    musicSource = sources[0];
                }

                if (sfxSource == null && sources.Length > 1)
                {
                    sfxSource = sources[1];
                }
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }

            musicSource.loop = true;
            musicSource.volume = 0.5f;
            musicSource.playOnAwake = false;

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
        }

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
