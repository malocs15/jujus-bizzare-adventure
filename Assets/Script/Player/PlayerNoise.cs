using System;
using UnityEngine;

// Bruit des pas du joueur : sur un sol SandSurface, il émet un bruit dont l'intensité
// dépend de sa vitesse (immobile : rien, marche : faible, course : fort). Sur la roche : rien.
[RequireComponent(typeof(PlayerMovement))]
public class PlayerNoise : MonoBehaviour
{
    public enum NoiseLevel { Silent, Walking, Running }

    [Header("Émission")]
    public float emitInterval = 0.3f;       // un « pas » toutes les 0,3 s
    public float walkIntensity = 0.35f;
    public float runIntensity = 1f;
    public float landingIntensity = 0.8f;   // atterrir sur le sable après un saut ou une chute
    public float minAirTime = 0.3f;         // en dessous, ce n'est pas un atterrissage (petite bosse, pente)
    public float runThreshold = 3.5f;       // au-dessus de cette vitesse, on court (m/s)
    public float groundCheckDistance = 0.6f;

    public NoiseLevel Level { get; private set; }
    public bool OnSand { get; private set; }

    // Pour l'indicateur de bruit du HUD
    public event Action<NoiseLevel> LevelChanged;

    private PlayerMovement movement;
    private float emitTimer;
    private float airTime;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        OnSand = IsOnSand();

        // Mort ou en train d'encaisser un coup : le joueur ne « marche » pas, aucun bruit de pas
        bool controlled = movement.State != PlayerState.Dead && movement.State != PlayerState.Hit;

        NoiseLevel level = NoiseLevel.Silent;
        if (controlled && OnSand && movement.IsGrounded && movement.CurrentSpeed > 0.2f)
            level = movement.CurrentSpeed > runThreshold ? NoiseLevel.Running : NoiseLevel.Walking;
        SetLevel(level);

        // Atterrissage sur le sable : un bruit unique, seulement après un vrai temps en l'air
        // (en dévalant une dune, le joueur peut décoller du sol une fraction de seconde)
        if (!movement.IsGrounded)
        {
            airTime += Time.deltaTime;
        }
        else
        {
            if (airTime >= minAirTime && OnSand && controlled)
                NoiseSystem.Emit(transform.position, landingIntensity);
            airTime = 0f;
        }

        if (level == NoiseLevel.Silent)
        {
            emitTimer = 0f;
            return;
        }

        // Joueur : Time.deltaTime (il n'est pas figé par l'arrêt du temps)
        emitTimer -= Time.deltaTime;
        if (emitTimer <= 0f)
        {
            emitTimer = emitInterval;
            NoiseSystem.Emit(transform.position, level == NoiseLevel.Running ? runIntensity : walkIntensity);
        }
    }

    private bool IsOnSand()
    {
        RaycastHit hit;
        if (!Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out hit,
                             groundCheckDistance, ~0, QueryTriggerInteraction.Ignore))
            return false;
        return hit.collider.GetComponentInParent<SandSurface>() != null;
    }

    private void SetLevel(NoiseLevel level)
    {
        if (level == Level) return;
        Level = level;
        LevelChanged?.Invoke(level);
    }
}
