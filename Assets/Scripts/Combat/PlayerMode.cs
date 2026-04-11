using UnityEngine;

/// <summary>
/// Define un "modo" especial en el que puede entrar el jugador durante un ataque.
/// Ejemplos: "Armado", "Invulnerable", "Imparable", "Contraataque activo"...
/// Créalo como asset: Click derecho > Create > Combat/PlayerMode
/// </summary>
[CreateAssetMenu(fileName = "NewPlayerMode", menuName = "Combat/PlayerMode")]
public class PlayerMode : ScriptableObject
{
    [Header("Identificación")]
    [Tooltip("Nombre del modo. Úsalo en tu código para identificarlo.")]
    public string modeName;

    [TextArea(2, 4)]
    public string description;

    [Header("Duración")]
    [Tooltip("Si es true, el modo dura indefinidamente hasta que algo lo cancele.")]
    public bool indefinite = false;

    [Tooltip("Duración en segundos. Solo se usa si 'indefinite' es false.")]
    public float duration = 1f;

    [Header("Propiedades de combate")]
    [Tooltip("El jugador no recibe daño mientras este modo esté activo.")]
    public bool isInvulnerable = false;

    [Tooltip("El jugador no puede ser interrumpido por golpes recibidos.")]
    public bool cannotBeInterrupted = false;

    [Tooltip("El jugador no puede ser movido por empujones (pushback) externos.")]
    public bool isUnpushable = false;

    [Tooltip("El jugador no puede realizar acciones (ataques, movimiento) durante este modo.")]
    public bool locksPlayer = false;

    [Header("Modificadores opcionales")]
    [Tooltip("Multiplicador de daño que inflige el jugador durante este modo. 1 = normal.")]
    public float damageMultiplier = 1f;

    [Tooltip("Multiplicador de velocidad de movimiento durante este modo. 1 = normal.")]
    public float speedMultiplier = 1f;

    [Header("Tag personalizado")]
    [Tooltip("String libre para que tu código lo lea y ejecute lógica propia. Ej: 'COUNTER_STANCE', 'RAGE', 'GUARD_BREAK'")]
    public string customTag = "";
}
