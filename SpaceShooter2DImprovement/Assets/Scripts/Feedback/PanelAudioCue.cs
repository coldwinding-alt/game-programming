using UnityEngine;

public class PanelAudioCue : MonoBehaviour
{
    public AudioClip clip;
    public AudioSource audioSource;

    private void OnEnable()
    {
        if (clip == null)
        {
            return;
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, Vector3.zero, 0.85f);
        }
    }
}
