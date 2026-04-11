using UnityEngine;

/// <summary>
/// Mapea los 4 slots de ataque del Action Map a sus AttackData correspondientes.
/// Créalo como asset: Click derecho > Create > Combat/AttackSlotConfig
/// Puedes tener varios (uno por personaje, modo, estilo de lucha...) y cambiarlos en runtime.
/// </summary>
[CreateAssetMenu(fileName = "NewAttackSlotConfig", menuName = "Combat/AttackSlotConfig")]
public class AttackSlotConfig : ScriptableObject
{
    [Header("Nombre de esta configuración")]
    [Tooltip("Ej: 'Estilo Agresivo', 'Defensor', 'Tyler Durden'...")]
    public string configName = "Config Default";

    [TextArea(1, 3)]
    public string description;

    [Header("Slots de ataque")]
    [Tooltip("Slot 1 del Action Map (ej: click izquierdo, botón cuadrado...)")]
    public AttackData slot1;

    [Tooltip("Slot 2 del Action Map")]
    public AttackData slot2;

    [Tooltip("Slot 3 del Action Map")]
    public AttackData slot3;

    [Tooltip("Slot 4 del Action Map")]
    public AttackData slot4;

    /// <summary>
    /// Devuelve el AttackData del slot indicado (1-4). Devuelve null si el slot está vacío.
    /// </summary>
    public AttackData GetAttack(int slotIndex)
    {
        return slotIndex switch
        {
            1 => slot1,
            2 => slot2,
            3 => slot3,
            4 => slot4,
            _ => null
        };
    }
}
