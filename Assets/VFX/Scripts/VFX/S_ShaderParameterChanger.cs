using System.Collections;
using UnityEngine;

public class SimpleShaderTriggerOpacity : MonoBehaviour
{
    public Renderer targetRenderer;
    public string parameterName = "_Opacity";

    public float minValue = 0f;
    public float maxValue = 1f;

    public float durationIn = 1f;
    public AnimationCurve curveIn = AnimationCurve.Linear(0, 0, 1, 1);

    public float durationOut = 1f;
    public AnimationCurve curveOut = AnimationCurve.Linear(0, 0, 1, 1);

    private MaterialPropertyBlock propBlock;
    private Coroutine currentAnim;

    void Start()
    {
        propBlock = new MaterialPropertyBlock();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentAnim != null) StopCoroutine(currentAnim);
            currentAnim = StartCoroutine(AnimateOpacity(minValue, maxValue, durationIn, curveIn));
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentAnim != null) StopCoroutine(currentAnim);
            currentAnim = StartCoroutine(AnimateOpacity(maxValue, minValue, durationOut, curveOut));
        }
    }

    IEnumerator AnimateOpacity(float from, float to, float duration, AnimationCurve curve)
    {
        float t = 0f;

        while (t < duration)
        {
            float normalizedTime = t / duration;
            float curveValue = curve.Evaluate(normalizedTime);
            float value = Mathf.Lerp(from, to, curveValue);

            targetRenderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat(parameterName, value);
            targetRenderer.SetPropertyBlock(propBlock);

            t += Time.deltaTime;
            yield return null;
        }

        // Clamp final
        targetRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(parameterName, to);
        targetRenderer.SetPropertyBlock(propBlock);
    }
}
