using System;
using UnityEngine;

// Bruit du désert : tout ce qui fait du bruit appelle Emit, et tout ce qui écoute
// (Geb, le traqueur sous le sable) s'abonne à NoiseEmitted. Les deux côtés ne se connaissent pas.
// Les écouteurs s'abonnent dans OnEnable et se désabonnent dans OnDisable.
public static class NoiseSystem
{
    // (position du bruit, intensité de 0 à 1)
    public static event Action<Vector3, float> NoiseEmitted;

    public static void Emit(Vector3 position, float intensity)
    {
        NoiseEmitted?.Invoke(position, Mathf.Clamp01(intensity));
    }

    // Le rechargement du domaine est désactivé au lancement du Play mode (Enter Play Mode Options) :
    // sans ce reset, les abonnés d'une partie précédente resteraient accrochés à l'événement.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetListeners()
    {
        NoiseEmitted = null;
    }
}
