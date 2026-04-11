using UnityEngine;

/// <summary>
/// Componente que vive en el GameObject de hitbox instanciado por AttackExecutor.
/// Detecta colisiones con enemigos y notifica al AttackExecutor.
///
/// No necesitas tocar este archivo. Se configura automáticamente desde SpawnHitbox().
/// </summary>
public class HitboxTrigger : MonoBehaviour
{
    private AttackExecutor _executor;
    private AttackPhase    _phase;
    private ImpactPhaseData _data;

    /// <summary>
    /// Inicializa el trigger con el contexto del ataque.
    /// Llamado por AttackExecutor justo después de instanciar el GameObject.
    /// </summary>
    public void Initialize(AttackExecutor executor, AttackPhase phase, ImpactPhaseData data)
    {
        _executor = executor;
        _phase    = phase;
        _data     = data;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignorar colisión con el propio jugador o sus hijos
        if (other.transform.IsChildOf(_executor.transform)) return;

        _executor.OnHitboxContact(other, _phase, _data);
    }
}
