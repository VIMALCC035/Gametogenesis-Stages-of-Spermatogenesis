using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[ExecuteAlways]
public class UIBlurController : MonoBehaviour
{
    [Header("Target Image")]
    public Image blurImage;

    [Header("Blur Settings")]
    [Range(0, 30)]
    public float initialBlur = 10f; // 🔥 start blur value

    [Range(0, 30)]
    public float blurAmount = 10f;

    public float fadeSpeed = 50f;

    Material runtimeMat;
    Coroutine routine;

    void Awake()
    {
        SetupMaterial();

        // 🔥 Start with initial blur
        blurAmount = initialBlur;
        ApplyBlur();
    }

    void OnValidate()
    {
        SetupMaterial();
        ApplyBlur();
    }

    void SetupMaterial()
    {
        if (blurImage == null)
        {
            blurImage = GetComponent<Image>();
        }

        if (blurImage == null || blurImage.material == null)
            return;

        runtimeMat = new Material(blurImage.material);
        blurImage.material = runtimeMat;
    }

    void ApplyBlur()
    {
        if (runtimeMat != null)
        {
            runtimeMat.SetFloat("_BlurSize", blurAmount);
        }
    }

    // 🎯 Animate to any blur
    public void AnimateBlur(float target)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(BlurRoutine(target));
    }

    IEnumerator BlurRoutine(float target)
    {
        float current = runtimeMat.GetFloat("_BlurSize");


        while (!Mathf.Approximately(current, target))
        {
            current = Mathf.MoveTowards(current, target, fadeSpeed * Time.deltaTime);
            runtimeMat.SetFloat("_BlurSize", current);
            yield return null;
        }

        runtimeMat.SetFloat("_BlurSize", target);
        blurAmount = target;
    }

    // 🔥 CALL THIS → BLUR → CLEAR
    public void ClearBlur()
    {
        AnimateBlur(0f);
    }

    // 🔥 OPTIONAL → CLEAR → BLUR AGAIN
    public void SetBlur()
    {
        AnimateBlur(initialBlur);
    }

    public void SetBlurFromEvent(float value)
    {
        AnimateBlur(value);
    }

    void Update()
    {
        ApplyBlur();

       
        if (Application.isPlaying && Input.GetKeyDown(KeyCode.B))
        {
            ClearBlur();
        }
    }
}
