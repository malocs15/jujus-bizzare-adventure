using UnityEngine;

// Projectile déplacé par script (pas de Rigidbody dynamique) pour obéir à WorldTime :
// il se fige pendant l'arrêt du temps (section 5.5). Utilisé par la tourelle de test,
// puis par les glaçons du faucon et les couteaux de Bio.
// Parade : si le rush du Stand le touche, il est renvoyé vers celui qui l'a tiré.
public class Projectile : MonoBehaviour, IDeflectable
{
    [Header("Vol")]
    public float speed = 14f;
    public float radius = 0.2f;          // rayon utilisé pour détecter les collisions
    public float lifetime = 6f;

    [Header("Dégâts")]
    public float damage = 20f;

    [Header("Parade")]
    // 0 : simple réflexion sur la direction du coup ; 1 : droit sur celui qui l'avait tiré.
    // Entre les deux, on mélange les deux directions.
    [Range(0f, 1f)] public float aimAssist = 1f;
    public float deflectSpeedFactor = 1.5f;       // un projectile renvoyé va plus vite

    // Celui qui a tiré ou renvoyé en dernier : le projectile ne le touche pas
    public GameObject Owner { get; private set; }
    public bool WasDeflected { get; private set; }

    private Vector3 velocity;
    private float age;

    public void Launch(Vector3 direction, GameObject owner)
    {
        velocity = direction.normalized * speed;
        Owner = owner;
        FaceVelocity();
    }

    // hitDirection : direction du coup qui renvoie le projectile (l'avant du Stand)
    public void Deflect(Vector3 hitDirection, GameObject newOwner)
    {
        if (newOwner == Owner) return; // déjà renvoyé par ce porteur : on ne le renvoie pas deux fois

        // Le projectile repart vers celui qui l'avait lancé (ou renvoyé) juste avant
        GameObject previousOwner = Owner;

        // 1. Réflexion : on inverse la composante de la vitesse le long de la direction du coup
        //    (v' = v - 2 (v·n) n). Un projectile reçu de face repart vers où il venait.
        Vector3 reflected = Vector3.Reflect(velocity, hitDirection.normalized).normalized;

        // 2. Aide à la visée : on corrige la direction vers le tireur précédent
        Vector3 direction = reflected;
        if (previousOwner != null && aimAssist > 0f)
        {
            Vector3 toShooter = (AimPointOf(previousOwner) - transform.position).normalized;
            direction = Vector3.Slerp(reflected, toShooter, aimAssist);
        }

        velocity = direction * speed * deflectSpeedFactor;
        FaceVelocity();
        Owner = newOwner;
        WasDeflected = true;
        age = 0f; // nouveau trajet : nouvelle durée de vie
    }

    void Update()
    {
        float dt = WorldTime.DeltaTime;
        if (dt <= 0f) return; // arrêt du temps : figé

        age += dt;
        if (age > lifetime)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 step = velocity * dt;
        RaycastHit hit;
        if (FindHit(step, out hit))
        {
            transform.position += step.normalized * hit.distance;
            // distance 0 : déjà en contact au départ, Unity ne donne pas de point fiable
            OnHit(hit, hit.distance > 0f ? hit.point : transform.position);
        }
        else
        {
            transform.position += step;
        }
    }

    // On balaie le trajet de cette image avec une sphère, en ignorant le tireur et les triggers
    private bool FindHit(Vector3 step, out RaycastHit best)
    {
        best = new RaycastHit();
        float length = step.magnitude;
        if (length < 0.0001f) return false;

        float bestDistance = float.MaxValue;
        foreach (RaycastHit h in Physics.SphereCastAll(transform.position, radius, step / length, length, ~0, QueryTriggerInteraction.Ignore))
        {
            if (Owner != null && h.transform.IsChildOf(Owner.transform)) continue;
            if (h.transform.IsChildOf(transform)) continue;
            if (h.distance < bestDistance)
            {
                bestDistance = h.distance;
                best = h;
            }
        }
        return bestDistance < float.MaxValue;
    }

    private void OnHit(RaycastHit hit, Vector3 point)
    {
        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        if (target != null && target.IsAlive)
        {
            target.TakeDamage(new DamageInfo
            {
                amount = damage,
                point = point,
                direction = velocity.normalized,
                source = Owner != null ? Owner : gameObject
            });
        }
        Destroy(gameObject);
    }

    private void FaceVelocity()
    {
        if (velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(velocity);
    }

    private static Vector3 AimPointOf(GameObject target)
    {
        Collider c = target.GetComponentInChildren<Collider>();
        return c != null ? c.bounds.center : target.transform.position;
    }
}
