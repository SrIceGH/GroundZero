using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Componente que se añade al jugador.
/// Lee un AttackData y ejecuta sus fases en orden,
/// gestionando hitboxes, timings, interrupciones, modos y eventos.
///
/// Las hitboxes se instancian como GameObjects hijos del jugador,
/// centradas en su posición y orientadas según su forward.
/// Se destruyen automáticamente al acabar la fase activa.
/// </summary>
[RequireComponent(typeof(PlayerModeController))]
public class AttackExecutor : MonoBehaviour
{
    [Header("Configuración de slots")]
    [Tooltip("El set de ataques activo. Puedes cambiarlo en runtime para cambiar de estilo de lucha.")]
    public AttackSlotConfig activeSlotConfig;

    [Header("Referencias")]
    [Tooltip("Layer de los enemigos para detectar colisiones de hitbox.")]
    public LayerMask enemyLayer;

    // ── Estado interno ──────────────────────────────────────────────
    private bool _isAttacking = false;
    private bool _interrupted = false;
    private Coroutine _attackCoroutine;
    private PlayerModeController _modeController;
    private Animator _animator;

    // Hitbox instanciada actualmente (solo una a la vez por fase)
    private GameObject _activeHitboxGO;

    // Objetos ya golpeados en la fase activa (evita golpear dos veces)
    private readonly HashSet<Collider> _hitThisPhase = new HashSet<Collider>();

    // ── Eventos públicos ────────────────────────────────────────────
    public event System.Action<AttackData> OnAttackStarted;
    public event System.Action<AttackData> OnAttackFinished;
    public event System.Action<AttackPhase, GameObject, float> OnHitLanded;
    public event System.Action OnAttackInterrupted;

    // ───────────────────────────────────────────────────────────────
    private void Awake()
    {
        _modeController = GetComponent<PlayerModeController>();
        _animator = GetComponentInChildren<Animator>();
    }

    // ───────────────────────────────────────────────────────────────
    //  API PÚBLICA
    // ───────────────────────────────────────────────────────────────

    /// <summary>Ejecuta el ataque del slot indicado (1-4).</summary>
    public void TriggerAttack(int slotIndex)
    {
        if (activeSlotConfig == null) return;

        AttackData attack = activeSlotConfig.GetAttack(slotIndex);
        if (attack == null) return;

        if (_isAttacking && !CanCancelCurrentAttack()) return;

        if (_attackCoroutine != null)
            StopCoroutine(_attackCoroutine);

        DestroyActiveHitbox();
        _attackCoroutine = StartCoroutine(ExecuteAttack(attack));
    }

    /// <summary>Interrumpe el ataque actual. Llamar desde el sistema de daño.</summary>
    public void InterruptAttack()
    {
        if (!_isAttacking) return;
        _interrupted = true;
    }

    public bool IsAttacking() => _isAttacking;

    // ───────────────────────────────────────────────────────────────
    //  EJECUCIÓN DEL ATAQUE
    // ───────────────────────────────────────────────────────────────

    private IEnumerator ExecuteAttack(AttackData attack)
    {
        _isAttacking = true;
        _interrupted = false;
        OnAttackStarted?.Invoke(attack);

        int phaseIndex = 0;

        while (phaseIndex < attack.phases.Count)
        {
            AttackPhase phase = attack.phases[phaseIndex];
            int nextPhase = phaseIndex + 1;

            switch (phase.phaseType)
            {
                case PhaseType.Impact:
                    yield return StartCoroutine(ExecuteImpact(phase, attack));
                    break;

                case PhaseType.Condition:
                    int jump = -1;
                    yield return StartCoroutine(ExecuteCondition(phase, result => jump = result));
                    if (jump >= 0) nextPhase = jump;
                    break;

                case PhaseType.Pause:
                    yield return StartCoroutine(ExecutePause(phase));
                    break;

                case PhaseType.Event:
                    ExecuteEvent(phase);
                    break;

                case PhaseType.ModeChange:
                    ExecuteModeChange(phase);
                    break;
            }

            if (_interrupted)
            {
                if (CanPhaseBeInterrupted(phase, attack))
                {
                    DestroyActiveHitbox();
                    OnAttackInterrupted?.Invoke();
                    break;
                }
                else
                {
                    _interrupted = false;
                }
            }

            phaseIndex = nextPhase;
        }

        DestroyActiveHitbox();
        _isAttacking = false;
        OnAttackFinished?.Invoke(attack);
    }

    // ───────────────────────────────────────────────────────────────
    //  FASE: IMPACTO
    // ───────────────────────────────────────────────────────────────

    private IEnumerator ExecuteImpact(AttackPhase phase, AttackData attack)
    {
        ImpactPhaseData data = phase.impact;
        _hitThisPhase.Clear();

        // ── Startup ──────────────────────────────────────
        float elapsed = 0f;
        while (elapsed < data.startup)
        {
            if (data.forwardMovement > 0f)
                transform.position += transform.forward * (data.forwardMovement / data.startup) * Time.deltaTime;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ── Fase activa: instanciar hitbox hijo ───────────
        // El GameObject se crea centrado en el jugador y orientado con su forward.
        // Se mueve con el jugador automáticamente al ser hijo suyo.
        _activeHitboxGO = SpawnHitbox(data);
        _activeHitboxGO.GetComponent<HitboxTrigger>().Initialize(this, phase, data);

        elapsed = 0f;
        while (elapsed < data.activeDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        DestroyActiveHitbox();

        // ── Endlag ───────────────────────────────────────
        elapsed = 0f;
        while (elapsed < data.endlag)
        {
            if (_interrupted)
            {
                bool canInterrupt = attack.globallyInterruptible &&
                                    data.canBeInterrupted &&
                                    elapsed >= data.interruptibleAfter;
                if (canInterrupt) yield break;
                else _interrupted = false;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // ───────────────────────────────────────────────────────────────
    //  SPAWN Y GESTIÓN DE HITBOX
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un GameObject hijo del jugador con un BoxCollider trigger.
    ///
    /// Geometría (espacio local del jugador):
    ///   Eje Z (forward): desde rangeMin hasta rangeMax
    ///   Eje Y (arriba):  desde heightMin hasta heightMax
    ///   Eje X (lateral): centrado, ancho = width
    ///
    /// Al ser hijo del jugador, se mueve y rota con él automáticamente.
    /// </summary>
    private GameObject SpawnHitbox(ImpactPhaseData data)
    {
        AttackHitbox hb = data.hitbox;

        float depth  = hb.rangeMax - hb.rangeMin;
        float height = hb.heightMax - hb.heightMin;

        Vector3 localOffset = new Vector3(
            0f,
            hb.heightMin + height * 0.5f,
            hb.rangeMin  + depth  * 0.5f
        );

        GameObject go = new GameObject($"Hitbox_{data.attackStyle}");
        go.transform.SetParent(transform);
        go.transform.localPosition = localOffset;
        go.transform.localRotation = Quaternion.identity;

        BoxCollider col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size      = new Vector3(hb.width, height, depth);
        col.center    = Vector3.zero;

        go.layer = gameObject.layer;
        go.AddComponent<HitboxTrigger>();
        return go;
    }

    private void DestroyActiveHitbox()
    {
        if (_activeHitboxGO != null)
        {
            Destroy(_activeHitboxGO);
            _activeHitboxGO = null;
        }
    }

    /// <summary>
    /// Llamado por HitboxTrigger cuando detecta un enemigo.
    /// Aplica daño y efectos si ese collider no fue golpeado ya en esta fase.
    /// </summary>
    public void OnHitboxContact(Collider other, AttackPhase phase, ImpactPhaseData data)
    {
        if (!enemyLayer.Contains(other.gameObject.layer)) return;
        if (_hitThisPhase.Contains(other)) return;
        _hitThisPhase.Add(other);

        ApplyHitEffects(other.gameObject, phase, data);
    }

    private void ApplyHitEffects(GameObject target, AttackPhase phase, ImpactPhaseData data)
    {
        float finalDamage = data.damage;
        if (_modeController.CurrentMode != null)
            finalDamage *= _modeController.CurrentMode.damageMultiplier;

        target.GetComponent<IDamageable>()?.TakeDamage(finalDamage, data.concussionDamage);

        if (data.effects.pushback > 0f)
            target.GetComponent<IPushable>()?.ApplyPushback(transform.forward, data.effects.pushback);

        if (data.effects.stunDuration > 0f)
            target.GetComponent<IStunnable>()?.ApplyStun(data.effects.stunDuration);

        foreach (string tag in data.effects.customEffectTags)
            target.GetComponent<ICustomEffectReceiver>()?.ReceiveEffect(tag, gameObject);

        OnHitLanded?.Invoke(phase, target, finalDamage);
    }

    // ───────────────────────────────────────────────────────────────
    //  FASE: CONDICIÓN
    // ───────────────────────────────────────────────────────────────

    private IEnumerator ExecuteCondition(AttackPhase phase, System.Action<int> jumpTo)
    {
        ConditionPhaseData data = phase.condition;
        float elapsed = 0f;

        bool hitLanded = false;
        System.Action<AttackPhase, GameObject, float> onHit = (p, go, dmg) => hitLanded = true;
        OnHitLanded += onHit;

        while (true)
        {
            elapsed += Time.deltaTime;
            bool conditionMet = false;

            switch (data.trigger)
            {
                case ConditionTrigger.OnReceiveHit:
                    conditionMet = _receivedHitThisFrame;
                    _receivedHitThisFrame = false;
                    break;
                case ConditionTrigger.OnHit:
                    conditionMet = hitLanded;
                    break;
                case ConditionTrigger.Always:
                    conditionMet = true;
                    break;
                case ConditionTrigger.OnBlocked:
                    conditionMet = _blockedThisFrame;
                    _blockedThisFrame = false;
                    break;
                case ConditionTrigger.OnMiss:
                    conditionMet = _missedThisFrame;
                    _missedThisFrame = false;
                    break;
            }

            if (conditionMet)
            {
                OnHitLanded -= onHit;
                jumpTo?.Invoke(data.jumpToPhaseIndex);
                yield break;
            }

            if (data.timeout > 0f && elapsed >= data.timeout)
            {
                OnHitLanded -= onHit;
                jumpTo?.Invoke(data.timeoutPhaseIndex);
                yield break;
            }

            yield return null;
        }
    }

    private bool _receivedHitThisFrame = false;
    private bool _blockedThisFrame     = false;
    private bool _missedThisFrame      = false;

    public void NotifyHitReceived() => _receivedHitThisFrame = true;
    public void NotifyBlocked()     => _blockedThisFrame     = true;
    public void NotifyMiss()        => _missedThisFrame      = true;

    // ───────────────────────────────────────────────────────────────
    //  FASE: PAUSA
    // ───────────────────────────────────────────────────────────────

    private IEnumerator ExecutePause(AttackPhase phase)
    {
        PausePhaseData data = phase.pause;
        float elapsed = 0f;
        while (elapsed < data.duration)
        {
            if (_interrupted && data.canBeInterrupted) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // ───────────────────────────────────────────────────────────────
    //  FASE: EVENTO
    // ───────────────────────────────────────────────────────────────

    private void ExecuteEvent(AttackPhase phase)
    {
        EventPhaseData data = phase.@event;

        if (_animator != null && !string.IsNullOrEmpty(data.animationTrigger))
            _animator.SetTrigger(data.animationTrigger);

        if (!string.IsNullOrEmpty(data.soundName))
            AudioManager.Instance?.PlaySound(data.soundName, transform.position);

        data.onExecute?.Invoke();
    }

    // ───────────────────────────────────────────────────────────────
    //  FASE: CAMBIO DE MODO
    // ───────────────────────────────────────────────────────────────

    private void ExecuteModeChange(AttackPhase phase)
    {
        ModeChangePhaseData data = phase.modeChange;
        if (data.targetMode == null) return;

        if (data.action == ModeChangeAction.Enter)
            _modeController.EnterMode(data.targetMode);
        else
            _modeController.ExitMode(data.targetMode);
    }

    // ───────────────────────────────────────────────────────────────
    //  HELPERS
    // ───────────────────────────────────────────────────────────────

    private bool CanCancelCurrentAttack() => false; // expande según tu lógica

    private bool CanPhaseBeInterrupted(AttackPhase phase, AttackData attack)
    {
        if (!attack.globallyInterruptible) return false;
        if (_modeController.CurrentMode != null && _modeController.CurrentMode.cannotBeInterrupted) return false;

        return phase.phaseType switch
        {
            PhaseType.Impact => phase.impact?.canBeInterrupted ?? true,
            PhaseType.Pause  => phase.pause?.canBeInterrupted  ?? true,
            _                => true
        };
    }

    // ───────────────────────────────────────────────────────────────
    //  GIZMOS
    // ───────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (activeSlotConfig == null) return;

        for (int s = 1; s <= 4; s++)
        {
            AttackData atk = activeSlotConfig.GetAttack(s);
            if (atk == null) continue;

            foreach (AttackPhase phase in atk.phases)
            {
                if (phase.phaseType != PhaseType.Impact || phase.impact == null) continue;

                AttackHitbox hb = phase.impact.hitbox;
                float depth  = hb.rangeMax - hb.rangeMin;
                float height = hb.heightMax - hb.heightMin;

                Vector3 localOffset = new Vector3(
                    0f,
                    hb.heightMin + height * 0.5f,
                    hb.rangeMin  + depth  * 0.5f
                );

                Vector3 worldCenter = transform.TransformPoint(localOffset);

                Gizmos.color = hb.gizmoColor;
                Matrix4x4 old = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, Vector3.one);
                Gizmos.DrawCube(Vector3.zero, new Vector3(hb.width, height, depth));
                Gizmos.matrix = old;
            }
        }
    }
}

// ─────────────────────────────────────────────────────────────────
//  EXTENSIÓN: LayerMask.Contains
// ─────────────────────────────────────────────────────────────────
public static class LayerMaskExtensions
{
    public static bool Contains(this LayerMask mask, int layer) =>
        (mask.value & (1 << layer)) != 0;
}
