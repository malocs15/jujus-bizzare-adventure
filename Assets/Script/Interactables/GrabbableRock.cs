using System;
using UnityEngine;

// Pierre que le Stand peut attraper, ramener et lancer.
// Pas de Rigidbody dynamique : la trajectoire est calculée ici avec WorldTime,
// pour que la pierre se fige pendant l'arrêt du temps (section 5.5 du plan).
[RequireComponent(typeof(Collider))]
public class GrabbableRock : MonoBehaviour, IGrabbable
{
    private enum RockState { Resting, Held, Flying }

    [Header("Attrapée")]
    public float pullSpeed = 14f;       // vitesse à laquelle elle vole vers le Stand

    [Header("Vol")]
    public float gravity = -20f;
    public float radius = 0.3f;         // rayon utilisé pour détecter les collisions en vol
    [Range(0f, 1f)] public float bounciness = 0.35f;
    public float restSpeed = 2f;        // en dessous, la pierre s'arrête au lieu de rebondir
    public float hitDamage = 15f;       // dégâts infligés à un IDamageable touché en vol
    public float killHeight = -50f;     // tombée hors de la carte : détruite

    // (point d'impact, vitesse) : utilisé par les pierres-leurres (S2-M3) pour faire du bruit
    public event Action<Vector3, float> Impacted;

    public bool CanBeCarried => true;

    // Vrai si la pierre a été lancée (et non simplement lâchée) : seules les pierres lancées font les leurres
    public bool WasThrown { get; private set; }

    private RockState state = RockState.Resting;
    private Collider col;
    private Transform holder;
    private Transform ignoredRoot;      // celui qui a lancé la pierre : elle ne le touche pas
    private const float Skin = 0.01f;   // petite marge pour ne pas rester collée à une surface
    private Vector3 velocity;

    void Awake()
    {
        col = GetComponent<Collider>();
    }

    public void OnGrabbed(Transform newHolder)
    {
        holder = newHolder;
        ignoredRoot = newHolder.parent != null ? newHolder.parent : newHolder; // le joueur qui tient le point
        col.enabled = false;            // évite qu'elle pousse le joueur pendant qu'il la porte
        state = RockState.Held;
        WasThrown = false;
    }

    public void OnThrown(Vector3 newVelocity)
    {
        // Si la pierre est dans un mur au moment du lâcher, on la ramène au-dessus du joueur
        if (holder != null && IsInsideSomething())
            transform.position = ignoredRoot.position + Vector3.up * 1.2f;

        holder = null;
        velocity = newVelocity;
        WasThrown = newVelocity.sqrMagnitude > 0.01f; // StandGrab.Drop lâche la pierre avec une vitesse nulle
        state = RockState.Flying;
    }

    void Update()
    {
        if (state == RockState.Held)
        {
            // Tenue par le Stand : elle suit le joueur, même pendant l'arrêt du temps (Time.deltaTime)
            if (holder != null)
                transform.position = Vector3.MoveTowards(transform.position, holder.position, pullSpeed * Time.deltaTime);
        }
        else if (state == RockState.Flying)
        {
            // En vol, c'est un objet du monde : WorldTime (vaut 0 pendant l'arrêt du temps)
            float dt = WorldTime.DeltaTime;
            if (dt > 0f) Fly(dt);
        }

        if (transform.position.y < killHeight) Destroy(gameObject);
    }

    // Vrai si la sphère de la pierre chevauche un collider (hors joueur)
    private bool IsInsideSomething()
    {
        foreach (Collider c in Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (ignoredRoot != null && c.transform.IsChildOf(ignoredRoot)) continue;
            return true;
        }
        return false;
    }

    private void Fly(float dt)
    {
        velocity += Vector3.up * gravity * dt;
        Vector3 step = velocity * dt;

        // On balaie le trajet de cette image avec une sphère pour ne pas traverser les murs
        RaycastHit hit;
        if (SphereCastIgnoringThrower(transform.position, step, out hit))
        {
            transform.position += step.normalized * Mathf.Max(0f, hit.distance - Skin);
            OnImpact(hit);
        }
        else
        {
            transform.position += step;
        }
    }

    private bool SphereCastIgnoringThrower(Vector3 origin, Vector3 step, out RaycastHit best)
    {
        best = new RaycastHit();
        float length = step.magnitude;
        if (length < 0.0001f) return false;

        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, step / length, length, ~0, QueryTriggerInteraction.Ignore);
        float bestDistance = float.MaxValue;
        foreach (RaycastHit h in hits)
        {
            if (ignoredRoot != null && h.transform.IsChildOf(ignoredRoot)) continue;
            if (h.distance <= 0f) continue; // déjà en contact au départ : Unity ne donne pas de normale fiable
            if (h.distance < bestDistance)
            {
                bestDistance = h.distance;
                best = h;
            }
        }
        return bestDistance < float.MaxValue;
    }

    private void OnImpact(RaycastHit hit)
    {
        float speed = velocity.magnitude;
        Impacted?.Invoke(hit.point, speed);

        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        if (target != null && target.IsAlive && speed > restSpeed)
        {
            target.TakeDamage(new DamageInfo
            {
                amount = hitDamage,
                point = hit.point,
                direction = velocity.normalized,
                source = gameObject
            });
        }

        // Rebond : on réfléchit la vitesse sur la surface et on l'amortit
        velocity = Vector3.Reflect(velocity, hit.normal) * bounciness;
        if (velocity.magnitude < restSpeed && hit.normal.y > 0.5f)
        {
            state = RockState.Resting;
            velocity = Vector3.zero;
            ignoredRoot = null;
            col.enabled = true;
        }
    }
}
