using System;
using UnityEngine;

// Jauge de chaleur du désert : elle monte au soleil, redescend à l'ombre (ShadeZone),
// et quand elle est pleine, le joueur perd de la vie jusqu'à ce qu'il trouve de l'ombre.
[RequireComponent(typeof(Health))]
public class HeatGauge : MonoBehaviour
{
    [Header("Chaleur (0 à 1)")]
    public float riseInSun = 0.02f;       // pleine en 50 s de soleil
    public float fallInShade = 0.25f;     // vide en 4 s d'ombre

    [Header("Dégâts quand la jauge est pleine")]
    public float damagePerSecond = 5f;    // quand la jauge est pleine
    public float damageTick = 1f;         // les dégâts tombent une fois par seconde

    public float Heat { get; private set; }
    public bool InShade { get; private set; }

    // Pour l'affichage de la jauge dans le HUD (0 à 1)
    public event Action<float> HeatChanged;

    private Health health;
    private float tickTimer;

    void Awake()
    {
        health = GetComponent<Health>();
    }

    void Update()
    {
        if (!health.IsAlive) return;

        // On teste le torse du joueur (1 m au-dessus des pieds)
        InShade = ShadeZone.IsInShade(transform.position + Vector3.up);

        // Joueur : Time.deltaTime (la chaleur ne s'arrête pas avec le temps du monde)
        float before = Heat;
        float rate = InShade ? -fallInShade : riseInSun;
        Heat = Mathf.Clamp01(Heat + rate * Time.deltaTime);
        if (!Mathf.Approximately(before, Heat)) HeatChanged?.Invoke(Heat);

        // Le minuteur tourne toujours : longer le bord d'une ombre (dedans, dehors, dedans…)
        // ne doit pas faire tomber les dégâts plus d'une fois par seconde
        tickTimer = Mathf.Max(0f, tickTimer - Time.deltaTime);
        if (Heat >= 1f && tickTimer <= 0f)
        {
            tickTimer = damageTick;
            health.TakeDamage(new DamageInfo
            {
                amount = damagePerSecond * damageTick,
                point = transform.position,
                direction = Vector3.zero,   // direction nulle = dégât d'environnement, pas de recul (PlayerDamage)
                source = gameObject
            });
        }
    }
}
