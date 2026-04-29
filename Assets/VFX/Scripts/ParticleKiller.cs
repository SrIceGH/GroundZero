using System.Collections;
using UnityEngine;

public class ParticleKiller : MonoBehaviour
{
    [Header("Referencia")]
    [SerializeField] private ParticleSystem targetPS;

    [Header("Filtro")]
    [Tooltip("Tag del objeto que activa el apagado.")]
    [SerializeField] private string targetTag = "Player";

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag(targetTag)) return;

        _triggered = true;
        targetPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        StartCoroutine(WaitAndDestroy());
    }

    private IEnumerator WaitAndDestroy()
    {
        while (targetPS.particleCount > 0)
            yield return null;

        Destroy(targetPS.gameObject);
    }
}