// Game sound effects and background music manager
// Responsible for playing sound effects, switching background music, and controlling volume. All sound playback is called through this class.

using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Game sound effects and background music manager (single case): Responsible for playing sound effects, switching background music, and controlling volume. All sound playback is called through this class.
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
        /// Creates or returns a shared audio manager singleton instance.
        /// </summary>
        /// <param name="parent">The parent Transform to which the audio object is mounted. </param>
        /// <returns>Returns the existing instance if the instance already exists, otherwise creates a new instance. </returns>
        public static mlpAudio Create(Transform parent)
        {
            // 1. The singleton already exists: update the parent and return the existing instance

            if (Instance != null)
            {
                if (parent != null && Instance.transform.parent != parent)
                {
                    Instance.transform.SetParent(parent, false);
                }

                return Instance;
            }

            // 2. Create a new GameObject and mount it to the specified parent

            var go = new GameObject("mlpAudio");
            go.transform.SetParent(parent, false);
            // 3. Add the mlpAudio component and two AudioSources (music + sound effects)
            Instance = go.AddComponent<mlpAudio>();
            Instance.musicSource = go.AddComponent<AudioSource>();
            Instance.musicSource.loop = true;
            Instance.musicSource.volume = 0.5f;
            Instance.sfxSource = go.AddComponent<AudioSource>();
            return Instance;
        }

        /// <summary>
        /// When Unity destroys this object, the shared instance reference is cleared.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Start looping background music. If the music is off or the same track is playing, nothing is done.
        /// </summary>
        /// <param name="key">The resource name of the music file. </param>
        public void PlayMusic(string key)
        {
            // 1. Skip when the music switch is turned off

            if (!MusicEnabled)
            {
                return;
            }

            // 2. Load audio resources

            var clip = Load(key);
            if (clip == null)
            {
                return;
            }

            // 3. The same track will not be played repeatedly when it is being played.
            if (musicSource.clip == clip && musicSource.isPlaying)
            {
                return;
            }

            // 4. Set the audio clip and start playing
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
        /// Play a short sound effect once. If sound effects are off, do nothing.
        /// </summary>
        /// <param name="key">The resource name of the sound effect file. </param>
        /// <param name="volume">Volume, ranging from 0 (silent) to 1 (maximum volume). </param>
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
        /// Toggle background music on and off.
        /// </summary>
        public void ToggleMusic()
        {
            MusicEnabled = !MusicEnabled;
            musicSource.mute = !MusicEnabled;
        }

        /// <summary>
        /// Toggle the sound effect on and off.
        /// </summary>
        public void ToggleSfx()
        {
            SfxEnabled = !SfxEnabled;
        }

        /// <summary>
        /// Load audio resources. Loads the AudioClip by name from Resources and caches it to avoid repeated loading.
        /// </summary>
        /// <param name="key">The resource name of the audio file. </param>
        /// <returns>The AudioClip loaded to, may be null if loading fails. </returns>
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
