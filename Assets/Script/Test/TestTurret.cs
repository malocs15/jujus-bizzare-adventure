using UnityEngine;

// Tourelle de test pour Sandbox : vise le joueur et tire un Projectile à intervalle régulier.
// Sert à tester la parade : elle doit être détruite par son propre projectile renvoyé.
[RequireComponent(typeof(Health))]
public class TestTurret : MonoBehaviour
{
    [Header("Tir")]
    public Projectile projectilePrefab;
    public Transform muzzle;             // point de sortie du projectile
    public float fireInterval = 2.5f;
    public float range = 20f;
    public float turnSpeed = 180f;       // degrés par seconde

    private Health health;
    private Transform player;
    private float fireTimer;

    void Awake()
    {
        health = GetComponent<Health>();
        fireTimer = fireInterval;
    }

    void Start()
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null) player = pm.transform;
    }

    void Update()
    {
        // Objet du monde : WorldTime (la tourelle se fige pendant l'arrêt du temps)
        float dt = WorldTime.DeltaTime;
        if (dt <= 0f || player == null || !health.IsAlive) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.magnitude > range) return;

        // Pivote vers le joueur
        if (toPlayer.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(toPlayer), turnSpeed * dt);

        fireTimer -= dt;
        if (fireTimer <= 0f)
        {
            fireTimer = fireInterval;
            Fire();
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null || muzzle == null) return;
        Vector3 aim = player.position + Vector3.up * 1.2f; // vise le torse
        Projectile p = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
        p.Launch(aim - muzzle.position, gameObject);
    }
}
