using UnityEngine;

/// <summary>
/// Implementa esta interfaz en el componente de salud de tus personajes.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage, float concussionDamage);
}

/// <summary>
/// Implementa esta interfaz en el componente de movimiento de tus personajes.
/// </summary>
public interface IPushable
{
    void ApplyPushback(Vector3 direction, float force);
}

/// <summary>
/// Implementa esta interfaz para recibir efectos de stun.
/// </summary>
public interface IStunnable
{
    void ApplyStun(float duration);
}

/// <summary>
/// Implementa esta interfaz para recibir efectos con tags personalizados
/// (KNOCKDOWN, DIZZY, GRAB, etc.).
/// </summary>
public interface ICustomEffectReceiver
{
    void ReceiveEffect(string effectTag, GameObject source);
}
