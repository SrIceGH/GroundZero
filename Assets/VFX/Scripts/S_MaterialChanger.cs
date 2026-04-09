using UnityEngine;
using UnityEngine.InputSystem;

public class S_MaterialChanger : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] Material targetMaterial;
    [SerializeField] string propertyName = "_Parameter";
    [SerializeField] float valueA = 0f;
    [SerializeField] float valueB = 1f;
    [SerializeField] float duration = 1f;
    [SerializeField] AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    bool toggled = false;
    Coroutine currentCoroutine;


    PlayerInput playerInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    void Update()
    {
        if (playerInput.actions["Change"].triggered)
        {
            toggled = !toggled;

            if (currentCoroutine != null)
                StopCoroutine(currentCoroutine);

            currentCoroutine = StartCoroutine(Transition(toggled ? valueB : valueA));
        }
    }

    System.Collections.IEnumerator Transition(float targetValue)
    {
        float startValue = targetMaterial.GetFloat(propertyName);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(elapsed / duration);
            targetMaterial.SetFloat(propertyName, Mathf.Lerp(startValue, targetValue, t));
            yield return null;
        }

        targetMaterial.SetFloat(propertyName, targetValue);
    }
}