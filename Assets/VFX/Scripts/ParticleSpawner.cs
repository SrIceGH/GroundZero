using UnityEngine;

public class ParticleSpawner : MonoBehaviour
{
    [Header("Particle FX")]
    [Tooltip("Prefab del Particle System a spawnear")]
    [SerializeField] private GameObject particlePrefab;

    [Tooltip("Usar la normal de la superficie para orientar el efecto")]
    [SerializeField] private bool alignToSurfaceNormal = true;

    [Header("Posición")]
    [Tooltip("True = spawnea en el punto de contacto | False = spawnea en la posición de este GameObject")]
    [SerializeField] private bool spawnAtContactPoint = false;

    [Header("Filtro por Tag (opcional)")]
    [Tooltip("Si está vacío, reacciona a cualquier colisión")]
    [SerializeField] private string targetTag = "";

    [Header("Modo")]
    [Tooltip("True = OnTriggerEnter  |  False = OnCollisionEnter")]
    [SerializeField] private bool useTrigger = false;

    [Header("Cooldown")]
    [Tooltip("Tiempo mínimo entre spawns (segundos). 0 = sin límite")]
    [SerializeField] private float cooldown = 0.2f;

    private float _lastSpawnTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
        if (!useTrigger) return;
        if (!PassesTagFilter(other.gameObject)) return;

        Vector3 position = spawnAtContactPoint
            ? other.ClosestPoint(transform.position)
            : transform.position;

        SpawnEffect(position, Vector3.up);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;
        if (!PassesTagFilter(collision.gameObject)) return;

        ContactPoint contact = collision.contacts[0];
        Vector3 position = spawnAtContactPoint ? contact.point : transform.position;
        Vector3 normal = spawnAtContactPoint ? contact.normal : Vector3.up;

        SpawnEffect(position, normal);
    }

    private void SpawnEffect(Vector3 position, Vector3 normal)
    {
        if (particlePrefab == null)
        {
            Debug.LogWarning("[ParticleSpawner] No hay Particle Prefab asignado.", this);
            return;
        }

        if (Time.time - _lastSpawnTime < cooldown) return;
        _lastSpawnTime = Time.time;

        Quaternion rotation = alignToSurfaceNormal
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        GameObject instance = Instantiate(particlePrefab, position, rotation);

        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(instance, lifetime);
        }
        else
        {
            Destroy(instance, 5f);
            Debug.LogWarning("[ParticleSpawner] El prefab no tiene ParticleSystem en la raíz.", instance);
        }
    }

    private bool PassesTagFilter(GameObject go)
    {
        if (string.IsNullOrEmpty(targetTag)) return true;
        return go.CompareTag(targetTag);
    }
}