using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackMessenger : MonoBehaviour
{
    public static FeedbackMessenger instance;

    [Header("Message")]
    public Text messageText;
    public float messageDuration = 2.2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip messageSound;

    private Coroutine activeRoutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        HideMessage();
    }

    public void ShowMessage(string message)
    {
        if (messageText == null)
        {
            return;
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }
        activeRoutine = StartCoroutine(ShowMessageRoutine(message));
    }

    public void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private IEnumerator ShowMessageRoutine(string message)
    {
        messageText.text = message;
        PlayClip(messageSound);
        yield return new WaitForSeconds(messageDuration);
        HideMessage();
        activeRoutine = null;
    }

    private void HideMessage()
    {
        if (messageText != null)
        {
            messageText.text = "";
        }
    }
}
