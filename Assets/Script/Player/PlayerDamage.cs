using UnityEngine;

// Réaction du joueur aux dégâts reçus par son Health :
// recul + état Hit, invulnérabilité brève (le joueur clignote), puis état Dead à la mort.
[RequireComponent(typeof(Health), typeof(PlayerMovement))]
public class PlayerDamage : MonoBehaviour
{
    [Header("Coup reçu")]
    public float knockbackSpeed = 6f;          // vitesse du recul, qui retombe pendant l'état Hit
    public float invulnerabilityDuration = 1f; // pendant ce temps, les coups suivants sont ignorés
    public float blinkInterval = 0.1f;

    private Health health;
    private PlayerMovement movement;
    private Renderer[] renderers;
    private float invulnerableTimer;
    private float blinkTimer;
    private bool visible = true;

    void Awake()
    {
        health = GetComponent<Health>();
        movement = GetComponent<PlayerMovement>();
        renderers = GetComponentsInChildren<Renderer>();

        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    void OnDestroy()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    private void OnDamaged(DamageInfo info)
    {
        if (!health.IsAlive) return; // c'est OnDied qui s'en occupe

        // Recul horizontal, dans la direction du coup
        Vector3 push = info.direction;
        push.y = 0f;
        if (push.sqrMagnitude < 0.0001f) push = -transform.forward;
        movement.ApplyHit(push.normalized * knockbackSpeed);

        invulnerableTimer = invulnerabilityDuration;
        health.Invulnerable = true;
    }

    private void OnDied()
    {
        movement.ChangeState(PlayerState.Dead);

        // Le Stand disparaît avec son porteur
        StandController stand = GetComponent<StandController>();
        if (stand != null && (stand.IsSummoned || stand.State == StandState.Summoning))
            stand.ChangeState(StandState.Dismissing);

        EndInvulnerability();
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyPlayerDied(); // l'écran game over écoute cet événement
    }

    // Composant désactivé : on ne laisse pas le joueur invulnérable ou invisible
    void OnDisable()
    {
        EndInvulnerability();
    }

    void Update()
    {
        if (invulnerableTimer <= 0f) return;

        // Joueur : Time.deltaTime (il n'est pas figé par l'arrêt du temps)
        invulnerableTimer -= Time.deltaTime;
        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            blinkTimer = blinkInterval;
            SetVisible(!visible);
        }

        if (invulnerableTimer <= 0f) EndInvulnerability();
    }

    private void EndInvulnerability()
    {
        invulnerableTimer = 0f;
        health.Invulnerable = false;
        SetVisible(true);
    }

    private void SetVisible(bool value)
    {
        visible = value;
        foreach (Renderer r in renderers) r.enabled = value;
    }
}
