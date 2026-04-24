using System.Collections;
using UnityEngine;

public class SolarSystemTarget : MonoBehaviour
{
    [Tooltip("Short display name shown in the kid-friendly fact panel.")]
    public string displayName = "Planet";

    [TextArea(2, 4)]
    [Tooltip("A simple fact shown when this object is clicked.")]
    public string factText = "This space object is fun to explore.";

    [Tooltip("World-space offset used by the camera while this object is selected.")]
    public Vector3 cameraOffset = new Vector3(0f, 0.45f, -2.2f);

    [Tooltip("How much larger the object becomes during the click feedback pulse.")]
    public float pulseScaleMultiplier = 1.18f;

    [Tooltip("How long the click feedback pulse lasts.")]
    public float pulseDuration = 0.55f;

    private Vector3 originalScale;
    private Coroutine pulseRoutine;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void PlaySelectedFeedback()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
        }

        pulseRoutine = StartCoroutine(Pulse());
    }

    private IEnumerator Pulse()
    {
        float halfDuration = Mathf.Max(0.01f, pulseDuration * 0.5f);
        Vector3 largeScale = originalScale * pulseScaleMultiplier;

        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(originalScale, largeScale, elapsed / halfDuration);
            yield return null;
        }

        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(largeScale, originalScale, elapsed / halfDuration);
            yield return null;
        }

        transform.localScale = originalScale;
        pulseRoutine = null;
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
    }
}
