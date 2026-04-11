using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// ─────────────────────────────────────────────
//  ENUMS Y TIPOS DE SOPORTE
// ─────────────────────────────────────────────

public enum PhaseType
{
    Impact,     // Un golpe real con hitbox
    Condition,  // Comprueba algo y salta a otra fase si se cumple
    Pause,      // Espera N segundos sin hacer nada
    Event,      // Llama a una función / animación / sonido
    ModeChange  // Activa o desactiva un PlayerMode
}

public enum AttackStyle
{
    Direct,     // Golpe recto, limpio
    Pendular    // Gancho, hayuco, movimiento en arco
}

public enum ConditionTrigger
{
    OnHit,          // Si este ataque golpea al enemigo
    OnBlocked,      // Si el enemigo bloquea este ataque
    OnReceiveHit,   // Si el jugador recibe un golpe durante esta fase
    OnMiss,         // Si el ataque no golpea nada
    Always          // Se cumple siempre (útil para saltos incondicionales)
}

public enum ModeChangeAction
{
    Enter,  // Activa el modo
    Exit    // Desactiva el modo
}

// ─────────────────────────────────────────────
//  HITBOX DE UN IMPACTO
// ─────────────────────────────────────────────

[Serializable]
public class AttackHitbox
{
    [Header("Rango horizontal (distancia desde el jugador)")]
    [Tooltip("Distancia mínima a la que llega el golpe (en unidades). 0 = pegado al cuerpo.")]
    public float rangeMin = 0f;

    [Tooltip("Distancia máxima a la que llega el golpe (en unidades). Referencia: Cerca~1u, Medio~2.5u, Largo~3.5u")]
    public float rangeMax = 1.5f;

    [Header("Rango vertical")]
    [Tooltip("Altura mínima del golpe desde el suelo (en unidades). Referencia: Bajo=0-1u")]
    public float heightMin = 0.5f;

    [Tooltip("Altura máxima del golpe desde el suelo (en unidades). Referencia: Alto=2-3u")]
    public float heightMax = 2f;

    [Header("Ancho del ataque")]
    [Tooltip("Ancho del área de golpe en unidades (eje lateral). Un golpe directo es estrecho, uno pendular es ancho.")]
    public float width = 0.5f;

    [Header("Visualización en editor")]
    [Tooltip("Color del Gizmo de esta hitbox en Scene View.")]
    public Color gizmoColor = new Color(1f, 0.2f, 0.2f, 0.35f);
}

// ─────────────────────────────────────────────
//  EFECTOS DE UN IMPACTO
// ─────────────────────────────────────────────

[Serializable]
public class AttackEffects
{
    [Tooltip("Empuja al enemigo hacia atrás X unidades al impactar.")]
    public float pushback = 0f;

    [Tooltip("Tiempo en segundos que el enemigo queda en stun tras recibir el golpe. 0 = sin stun.")]
    public float stunDuration = 0f;

    [Tooltip("Si es true, el atacante se lanza hacia adelante durante el golpe.")]
    public bool lunge = false;

    [Tooltip("Distancia del lunge hacia adelante, en unidades.")]
    public float lungeDistance = 0f;

    [Tooltip("Si es true, el ataque puede cancelarse con otro ataque antes de que acabe el endlag.")]
    public bool cancelable = false;

    [Tooltip("Frame (en segundos desde el inicio del impacto) a partir del cual se puede cancelar. Solo si 'cancelable' es true.")]
    public float cancelableAfter = 0f;

    [Tooltip("Etiquetas de efectos personalizados (ej: 'KNOCKDOWN', 'DIZZY', 'GRAB'). Tu código las leerá.")]
    public List<string> customEffectTags = new List<string>();
}

// ─────────────────────────────────────────────
//  FASE DE IMPACTO
// ─────────────────────────────────────────────

[Serializable]
public class ImpactPhaseData
{
    [Header("Hitbox")]
    public AttackHitbox hitbox;
    public AttackStyle attackStyle = AttackStyle.Direct;

    [Header("Daño")]
    [Tooltip("Daño de salud que inflige este impacto.")]
    public float damage = 10f;

    [Tooltip("Daño de conmoción (barra de stamina/aturdimiento). Rellena la barra de knockout.")]
    public float concussionDamage = 5f;

    [Header("Timing (en segundos)")]
    [Tooltip("Tiempo desde que se activa la fase hasta que la hitbox aparece.")]
    public float startup = 0.1f;

    [Tooltip("Cuánto tiempo está activa la hitbox.")]
    public float activeDuration = 0.1f;

    [Tooltip("Tiempo de recuperación tras el golpe hasta que puedes volver a atacar.")]
    public float endlag = 0.3f;

    [Header("Defensa durante el ataque")]
    [Tooltip("Reducción de daño recibido mientras realizas este ataque. 0 = sin protección, 1 = inmune.")]
    [Range(0f, 1f)]
    public float armorWhileAttacking = 0f;

    [Tooltip("Desplazamiento del jugador hacia adelante al ejecutar este ataque, en unidades.")]
    public float forwardMovement = 0f;

    [Header("Interrupción")]
    [Tooltip("Si el jugador puede ser interrumpido por un golpe recibido durante este impacto.")]
    public bool canBeInterrupted = true;

    [Tooltip("Segundos desde el inicio del startup en que el ataque pasa a ser interrumpible. 0 = siempre.")]
    public float interruptibleAfter = 0f;

    [Header("Efectos")]
    public AttackEffects effects;
}

// ─────────────────────────────────────────────
//  FASE DE CONDICIÓN
// ─────────────────────────────────────────────

[Serializable]
public class ConditionPhaseData
{
    [Tooltip("Qué tiene que ocurrir para que se cumpla la condición.")]
    public ConditionTrigger trigger = ConditionTrigger.OnReceiveHit;

    [Tooltip("Índice de la fase a la que saltar si la condición se cumple.")]
    public int jumpToPhaseIndex = 0;

    [Tooltip("Tiempo máximo que esta fase espera a que se cumpla la condición (en segundos). 0 = no expira.")]
    public float timeout = 0f;

    [Tooltip("Índice de la fase a la que ir si se agota el tiempo sin cumplirse la condición. -1 = terminar el ataque.")]
    public int timeoutPhaseIndex = -1;
}

// ─────────────────────────────────────────────
//  FASE DE PAUSA
// ─────────────────────────────────────────────

[Serializable]
public class PausePhaseData
{
    [Tooltip("Tiempo de espera en segundos antes de pasar a la siguiente fase.")]
    public float duration = 0.1f;

    [Tooltip("Si el jugador puede ser interrumpido durante esta pausa.")]
    public bool canBeInterrupted = true;
}

// ─────────────────────────────────────────────
//  FASE DE EVENTO
// ─────────────────────────────────────────────

[Serializable]
public class EventPhaseData
{
    [Tooltip("Nombre del trigger de animación a activar. Déjalo vacío si no quieres afectar al Animator.")]
    public string animationTrigger = "";

    [Tooltip("Nombre del AudioClip a reproducir (busca en tu AudioManager). Déjalo vacío si no hay sonido.")]
    public string soundName = "";

    [Tooltip("Evento Unity configurable desde el inspector. Puedes arrastrar cualquier función pública aquí.")]
    public UnityEvent onExecute;

    [Tooltip("Tag personalizado para que tu código ejecute lógica propia al llegar a esta fase.")]
    public string customEventTag = "";
}

// ─────────────────────────────────────────────
//  FASE DE CAMBIO DE MODO
// ─────────────────────────────────────────────

[Serializable]
public class ModeChangePhaseData
{
    [Tooltip("El modo a activar o desactivar.")]
    public PlayerMode targetMode;

    [Tooltip("Entrar o salir del modo.")]
    public ModeChangeAction action = ModeChangeAction.Enter;
}

// ─────────────────────────────────────────────
//  FASE GENÉRICA (contenedor de todas las anteriores)
// ─────────────────────────────────────────────

[Serializable]
public class AttackPhase
{
    [Tooltip("Tipo de esta fase.")]
    public PhaseType phaseType = PhaseType.Impact;

    [Tooltip("Nombre descriptivo para identificarla en el inspector. No afecta al juego.")]
    public string phaseName = "Nueva Fase";

    // Solo uno de estos se usa según phaseType:
    public ImpactPhaseData impact;
    public ConditionPhaseData condition;
    public PausePhaseData pause;
    public EventPhaseData @event;
    public ModeChangePhaseData modeChange;
}

// ─────────────────────────────────────────────
//  ATTACK DATA (ScriptableObject principal)
// ─────────────────────────────────────────────

/// <summary>
/// Define un ataque completo como una secuencia de fases.
/// Créalo como asset: Click derecho > Create > Combat/AttackData
/// </summary>
[CreateAssetMenu(fileName = "NewAttack", menuName = "Combat/AttackData")]
public class AttackData : ScriptableObject
{
    [Header("Identificación")]
    public string attackName = "Nuevo Ataque";

    [TextArea(2, 5)]
    public string description;

    [Header("Propiedades globales del ataque")]
    [Tooltip("Si el ataque completo puede ser interrumpido (override global). Las fases individuales pueden refinarlo.")]
    public bool globallyInterruptible = true;

    [Tooltip("Si es true, este ataque puede ser cancelado por otro ataque en cualquier momento (override de fase).")]
    public bool globalCancelable = false;

    [Header("Secuencia de fases")]
    [Tooltip("Lista ordenada de fases que componen este ataque. Se ejecutan en orden salvo que una condición salte.")]
    public List<AttackPhase> phases = new List<AttackPhase>();
}
