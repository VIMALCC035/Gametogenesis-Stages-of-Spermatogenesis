using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraFadeController : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Black Screen Hold")]
    [SerializeField] private float blackHoldDuration = 0f;

    private Coroutine fadeCoroutine;

    /// <summary>
    /// Fades the screen to black, holds for the configured duration,
    /// then fades back to transparent.
    /// </summary>
    public void FadeInAndOut()
    {
        StartFadeSequence(fadeDuration, blackHoldDuration);
    }

    /// <summary>
    /// Fades the screen to black, holds for the specified duration,
    /// then fades back to transparent.
    /// </summary>
    public void FadeInAndOut(float duration)
    {
        StartFadeSequence(duration, blackHoldDuration);
    }

    /// <summary>
    /// Fades the screen to black, holds for the specified duration,
    /// then fades back to transparent.
    /// </summary>
    public void FadeInAndOut(float fadeTime, float holdDuration)
    {
        StartFadeSequence(fadeTime, holdDuration);
    }

    private void StartFadeSequence(float fadeTime, float holdDuration)
    {
        if (fadeImage == null)
        {
            Debug.LogWarning(
                $"{nameof(CameraFadeController)}: Fade Image is not assigned.",
                this
            );

            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeSequenceRoutine(
            Mathf.Max(0f, fadeTime),
            Mathf.Max(0f, holdDuration)
        ));
    }

    private IEnumerator FadeSequenceRoutine(float fadeTime, float holdDuration)
    {
        // Fade Out: Transparent -> Black
        yield return FadeRoutine(1f, fadeTime);

        // Hold on black
        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        // Fade In: Black -> Transparent
        yield return FadeRoutine(0f, fadeTime);

        fadeCoroutine = null;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        Color color = fadeImage.color;
        float startAlpha = color.a;

        if (duration <= 0f)
        {
            color.a = targetAlpha;
            fadeImage.color = color;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            color.a = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                progress
            );

            fadeImage.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        fadeImage.color = color;
    }
}