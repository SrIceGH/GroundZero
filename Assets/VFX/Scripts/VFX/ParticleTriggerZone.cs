using System.Collections;
using UnityEngine;

/// <summary>
/// Adjunta este script al GameObject que tiene el Collider (marcado como Trigger).
/// Asegúrate de que el Player tiene la tag "Player" y un Rigidbody o CharacterController.
/// El Particle System puede estar en el mismo GameObject o asignarse manualmente.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ParticleTriggerZone : MonoBehaviour
{
    public enum ParticleProperty
    {
        // ── Main ──────────────────────────────────────────────────────────
        [InspectorName("Main/Start Lifetime")]      StartLifetime,
        [InspectorName("Main/Start Speed")]         StartSpeed,
        [InspectorName("Main/Start Size")]          StartSize,
        [InspectorName("Main/Start Size X")]        StartSizeX,
        [InspectorName("Main/Start Size Y")]        StartSizeY,
        [InspectorName("Main/Start Size Z")]        StartSizeZ,
        [InspectorName("Main/Start Rotation")]      StartRotation,
        [InspectorName("Main/Gravity Modifier")]    GravityModifier,
        [InspectorName("Main/Simulation Speed")]    SimulationSpeed,
        [InspectorName("Main/Max Particles")]       MaxParticles,

        // ── Emission ──────────────────────────────────────────────────────
        [InspectorName("Emission/Rate Over Time")]      EmissionRateOverTime,
        [InspectorName("Emission/Rate Over Distance")]  EmissionRateOverDistance,

        // ── Velocity Over Lifetime ────────────────────────────────────────
        [InspectorName("Velocity Over Lifetime/Linear X")]       VelocityLinearX,
        [InspectorName("Velocity Over Lifetime/Linear Y")]       VelocityLinearY,
        [InspectorName("Velocity Over Lifetime/Linear Z")]       VelocityLinearZ,
        [InspectorName("Velocity Over Lifetime/Orbital X")]      VelocityOrbitalX,
        [InspectorName("Velocity Over Lifetime/Orbital Y")]      VelocityOrbitalY,
        [InspectorName("Velocity Over Lifetime/Orbital Z")]      VelocityOrbitalZ,
        [InspectorName("Velocity Over Lifetime/Speed Modifier")] VelocitySpeedModifier,

        // ── Limit Velocity Over Lifetime ──────────────────────────────────
        [InspectorName("Limit Velocity Over Lifetime/Dampen")]   LimitVelocityDampen,

        // ── Force Over Lifetime ───────────────────────────────────────────
        [InspectorName("Force Over Lifetime/X")]    ForceX,
        [InspectorName("Force Over Lifetime/Y")]    ForceY,
        [InspectorName("Force Over Lifetime/Z")]    ForceZ,

        // ── Noise ─────────────────────────────────────────────────────────
        [InspectorName("Noise/Strength")]           NoiseStrength,
        [InspectorName("Noise/Frequency")]          NoiseFrequency,
        [InspectorName("Noise/Scroll Speed")]       NoiseScrollSpeedX,
        [InspectorName("Noise/Octaves")]            NoiseOctaves,
        [InspectorName("Noise/Octave Multiplier")]  NoiseOctaveMultiplier,
        [InspectorName("Noise/Octave Scale")]       NoiseOctaveScale,
        [InspectorName("Noise/Position Amount")]    NoisePosAmplitude,
        [InspectorName("Noise/Size Amount")]        NoiseSizeAmount,
        [InspectorName("Noise/Rotation Amount")]    NoiseRotationAmount,

        // ── Collision ─────────────────────────────────────────────────────
        [InspectorName("Collision/Dampen")]         CollisionDampen,
        [InspectorName("Collision/Bounce")]         CollisionBounce,
        [InspectorName("Collision/Lifetime Loss")]  CollisionLifetimeLoss,
        [InspectorName("Collision/Radius Scale")]   CollisionRadiusScale,

        // ── Trails ────────────────────────────────────────────────────────
        [InspectorName("Trails/Ratio")]             TrailRatio,
        [InspectorName("Trails/Lifetime")]          TrailLifetime,
        [InspectorName("Trails/Min Vertex Distance")] TrailMinVertexDistance,
        [InspectorName("Trails/Width")]             TrailWidth,

        // ── Renderer ──────────────────────────────────────────────────────
        [InspectorName("Renderer/Min Particle Size")]   RendererMinParticleSize,
        [InspectorName("Renderer/Max Particle Size")]   RendererMaxParticleSize,
        [InspectorName("Renderer/Normal Direction")]    RendererNormalDirection,
        [InspectorName("Renderer/Pivot X")]             RendererPivotX,
        [InspectorName("Renderer/Pivot Y")]             RendererPivotY,
        [InspectorName("Renderer/Pivot Z")]             RendererPivotZ,
    }

    [Header("Referencias")]
    [Tooltip("El Particle System a modificar. Si se deja vacío, se busca en este GameObject.")]
    public ParticleSystem targetParticleSystem;

    [Header("Configuración")]
    [Tooltip("Propiedad del Particle System que se va a interpolar.")]
    public ParticleProperty propertyToChange = ParticleProperty.StartLifetime;

    [Tooltip("Valor cuando el Player ESTÁ FUERA de la zona.")]
    public float valueWhenOutside = 0f;

    [Tooltip("Valor cuando el Player ESTÁ DENTRO de la zona.")]
    public float valueWhenInside = 5f;

    [Tooltip("Tiempo en segundos que tarda la interpolación.")]
    public float transitionDuration = 1.5f;

    [Tooltip("Tag del objeto que activa el trigger.")]
    public string playerTag = "Player";

    // ── Privados ──────────────────────────────────────────────────────────
    private ParticleSystem.MainModule                       _main;
    private ParticleSystem.EmissionModule                   _emission;
    private ParticleSystem.VelocityOverLifetimeModule       _velocity;
    private ParticleSystem.LimitVelocityOverLifetimeModule  _limitVel;
    private ParticleSystem.ForceOverLifetimeModule          _force;
    private ParticleSystem.NoiseModule                      _noise;
    private ParticleSystem.CollisionModule                  _collision;
    private ParticleSystem.TrailModule                      _trails;
    private ParticleSystemRenderer                          _renderer;
    private Coroutine _activeCoroutine;
    private float _currentValue;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        // Aseguramos que el Collider es Trigger
        GetComponent<Collider>().isTrigger = true;

        // Buscamos el PS si no se asignó
        if (targetParticleSystem == null)
            targetParticleSystem = GetComponent<ParticleSystem>();

        if (targetParticleSystem == null)
        {
            Debug.LogError($"[ParticleTriggerZone] No se encontró ningún Particle System en '{gameObject.name}'.");
            enabled = false;
            return;
        }

        _main      = targetParticleSystem.main;
        _emission  = targetParticleSystem.emission;
        _velocity  = targetParticleSystem.velocityOverLifetime;
        _limitVel  = targetParticleSystem.limitVelocityOverLifetime;
        _force     = targetParticleSystem.forceOverLifetime;
        _noise     = targetParticleSystem.noise;
        _collision = targetParticleSystem.collision;
        _trails    = targetParticleSystem.trails;
        _renderer  = targetParticleSystem.GetComponent<ParticleSystemRenderer>();

        // Estado inicial = fuera de la zona
        _currentValue = valueWhenOutside;
        ApplyValue(_currentValue);
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        StartTransition(valueWhenInside);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        StartTransition(valueWhenOutside);
    }

    // ─────────────────────────────────────────────────────────────────────
    void StartTransition(float targetValue)
    {
        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        _activeCoroutine = StartCoroutine(LerpProperty(_currentValue, targetValue, transitionDuration));
    }

    IEnumerator LerpProperty(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _currentValue = Mathf.Lerp(from, to, elapsed / duration);
            ApplyValue(_currentValue);
            yield return null;
        }

        _currentValue = to;
        ApplyValue(_currentValue);
        _activeCoroutine = null;
    }

    // ─────────────────────────────────────────────────────────────────────
    void ApplyValue(float value)
    {
        switch (propertyToChange)
        {
            // ── Main ──────────────────────────────────────────────────────
            case ParticleProperty.StartLifetime:
                _main.startLifetime = value; break;
            case ParticleProperty.StartSpeed:
                _main.startSpeed = value; break;
            case ParticleProperty.StartSize:
                _main.startSize = value; break;
            case ParticleProperty.StartSizeX:
                _main.startSizeX = value; break;
            case ParticleProperty.StartSizeY:
                _main.startSizeY = value; break;
            case ParticleProperty.StartSizeZ:
                _main.startSizeZ = value; break;
            case ParticleProperty.StartRotation:
                _main.startRotation = value * Mathf.Deg2Rad; break;
            case ParticleProperty.GravityModifier:
                _main.gravityModifier = value; break;
            case ParticleProperty.SimulationSpeed:
                _main.simulationSpeed = value; break;
            case ParticleProperty.MaxParticles:
                _main.maxParticles = Mathf.RoundToInt(value); break;

            // ── Emission ──────────────────────────────────────────────────
            case ParticleProperty.EmissionRateOverTime:
                _emission.rateOverTime = value; break;
            case ParticleProperty.EmissionRateOverDistance:
                _emission.rateOverDistance = value; break;

            // ── Velocity over Lifetime ────────────────────────────────────
            case ParticleProperty.VelocityLinearX:
                _velocity.x = value; break;
            case ParticleProperty.VelocityLinearY:
                _velocity.y = value; break;
            case ParticleProperty.VelocityLinearZ:
                _velocity.z = value; break;
            case ParticleProperty.VelocityOrbitalX:
                _velocity.orbitalX = value; break;
            case ParticleProperty.VelocityOrbitalY:
                _velocity.orbitalY = value; break;
            case ParticleProperty.VelocityOrbitalZ:
                _velocity.orbitalZ = value; break;
            case ParticleProperty.VelocitySpeedModifier:
                _velocity.speedModifier = value; break;

            // ── Limit Velocity over Lifetime ──────────────────────────────
            case ParticleProperty.LimitVelocityDampen:
                _limitVel.dampen = value; break;

            // ── Force over Lifetime ───────────────────────────────────────
            case ParticleProperty.ForceX:
                _force.x = value; break;
            case ParticleProperty.ForceY:
                _force.y = value; break;
            case ParticleProperty.ForceZ:
                _force.z = value; break;

            // ── Noise ─────────────────────────────────────────────────────
            case ParticleProperty.NoiseStrength:
                _noise.strength = value; break;
            case ParticleProperty.NoiseFrequency:
                _noise.frequency = value; break;
            case ParticleProperty.NoiseScrollSpeedX:
                _noise.scrollSpeed = value; break;   // scrollSpeed es un MinMaxCurve; asignar float usa modo constante
            case ParticleProperty.NoiseOctaves:
                _noise.octaveCount = Mathf.Clamp(Mathf.RoundToInt(value), 1, 8); break;
            case ParticleProperty.NoiseOctaveMultiplier:
                _noise.octaveMultiplier = value; break;
            case ParticleProperty.NoiseOctaveScale:
                _noise.octaveScale = value; break;
            case ParticleProperty.NoisePosAmplitude:
                _noise.positionAmount = value; break;
            case ParticleProperty.NoiseSizeAmount:
                _noise.sizeAmount = value; break;
            case ParticleProperty.NoiseRotationAmount:
                _noise.rotationAmount = value; break;

            // ── Collision ─────────────────────────────────────────────────
            case ParticleProperty.CollisionDampen:
                _collision.dampen = value; break;
            case ParticleProperty.CollisionBounce:
                _collision.bounce = value; break;
            case ParticleProperty.CollisionLifetimeLoss:
                _collision.lifetimeLoss = value; break;
            case ParticleProperty.CollisionRadiusScale:
                _collision.radiusScale = value; break;

            // ── Trails ────────────────────────────────────────────────────
            case ParticleProperty.TrailRatio:
                _trails.ratio = value; break;
            case ParticleProperty.TrailLifetime:
                _trails.lifetime = value; break;
            case ParticleProperty.TrailMinVertexDistance:
                _trails.minVertexDistance = value; break;
            case ParticleProperty.TrailWidth:
                _trails.widthOverTrail = value; break;

            // ── Renderer ──────────────────────────────────────────────────
            case ParticleProperty.RendererMinParticleSize:
                if (_renderer) _renderer.minParticleSize = value; break;
            case ParticleProperty.RendererMaxParticleSize:
                if (_renderer) _renderer.maxParticleSize = value; break;
            case ParticleProperty.RendererNormalDirection:
                if (_renderer) _renderer.normalDirection = value; break;
            case ParticleProperty.RendererPivotX:
                if (_renderer) _renderer.pivot = new Vector3(value, _renderer.pivot.y, _renderer.pivot.z); break;
            case ParticleProperty.RendererPivotY:
                if (_renderer) _renderer.pivot = new Vector3(_renderer.pivot.x, value, _renderer.pivot.z); break;
            case ParticleProperty.RendererPivotZ:
                if (_renderer) _renderer.pivot = new Vector3(_renderer.pivot.x, _renderer.pivot.y, value); break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);

        if (col is BoxCollider box)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
        }
    }
#endif
}