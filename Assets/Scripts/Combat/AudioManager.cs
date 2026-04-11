using UnityEngine;

/// <summary>
/// AudioManager mínimo para que el sistema de combate compile.
/// Reemplaza esto con tu propio AudioManager si ya tienes uno.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Reproduce un sonido por nombre en una posición del mundo.
    /// Implementa aquí tu lógica de AudioSource / AudioMixer.
    /// </summary>
    public void PlaySound(string soundName, Vector3 position)
    {
        // TODO: busca el clip por nombre en tu banco de sonidos y reprodúcelo.
        Debug.Log($"[AudioManager] PlaySound: {soundName} en {position}");
    }
}
