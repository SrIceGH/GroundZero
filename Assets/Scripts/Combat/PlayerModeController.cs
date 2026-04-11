using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona los modos activos del jugador (Invulnerable, Armado, Imparable...).
/// Se añade al jugador junto a AttackExecutor.
/// </summary>
public class PlayerModeController : MonoBehaviour
{
    // Modo activo actual. Solo puede haber uno a la vez (puedes cambiar esto a una pila si lo necesitas).
    public PlayerMode CurrentMode { get; private set; }

    private Coroutine _modeCoroutine;

    // Eventos para que otros sistemas reaccionen
    public event System.Action<PlayerMode> OnModeEntered;
    public event System.Action<PlayerMode> OnModeExited;

    // ──────────────────────────────────────────────────────────────
    /// <summary>Activa un modo. Si ya hay uno activo, lo reemplaza.</summary>
    public void EnterMode(PlayerMode mode)
    {
        if (mode == null) return;

        // Salir del modo anterior si existe
        if (CurrentMode != null)
            ExitCurrentMode();

        CurrentMode = mode;
        OnModeEntered?.Invoke(mode);

        Debug.Log($"[ModeController] Entrando en modo: {mode.modeName}");

        if (!mode.indefinite)
            _modeCoroutine = StartCoroutine(ModeTimer(mode));
    }

    /// <summary>Desactiva un modo específico si está activo.</summary>
    public void ExitMode(PlayerMode mode)
    {
        if (CurrentMode != mode) return;
        ExitCurrentMode();
    }

    /// <summary>Fuerza la salida del modo actual, sea cual sea.</summary>
    public void ForceExitCurrentMode()
    {
        if (CurrentMode == null) return;
        ExitCurrentMode();
    }

    // ──────────────────────────────────────────────────────────────
    //  Propiedades de conveniencia (para consultar desde otros sistemas)
    // ──────────────────────────────────────────────────────────────

    public bool IsInvulnerable    => CurrentMode != null && CurrentMode.isInvulnerable;
    public bool IsUnpushable      => CurrentMode != null && CurrentMode.isUnpushable;
    public bool IsLockedDown      => CurrentMode != null && CurrentMode.locksPlayer;
    public bool CannotBeInterrupted => CurrentMode != null && CurrentMode.cannotBeInterrupted;
    public float SpeedMultiplier  => CurrentMode != null ? CurrentMode.speedMultiplier : 1f;
    public float DamageMultiplier => CurrentMode != null ? CurrentMode.damageMultiplier : 1f;

    /// <summary>Comprueba si el modo activo tiene un tag personalizado concreto.</summary>
    public bool HasTag(string tag) => CurrentMode != null && CurrentMode.customTag == tag;

    // ──────────────────────────────────────────────────────────────
    private void ExitCurrentMode()
    {
        if (_modeCoroutine != null)
        {
            StopCoroutine(_modeCoroutine);
            _modeCoroutine = null;
        }

        PlayerMode exiting = CurrentMode;
        CurrentMode = null;
        OnModeExited?.Invoke(exiting);

        Debug.Log($"[ModeController] Saliendo del modo: {exiting.modeName}");
    }

    private IEnumerator ModeTimer(PlayerMode mode)
    {
        yield return new WaitForSeconds(mode.duration);
        if (CurrentMode == mode)
            ExitCurrentMode();
    }
}
