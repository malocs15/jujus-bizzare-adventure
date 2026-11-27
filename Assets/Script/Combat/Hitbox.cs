using System.Collections.Generic;
using UnityEngine;

// Zone de frappe : une sphère devant son porteur. Chaque appel à Strike() applique
// des dégâts une fois à chaque IDamageable touché, avec particule et son d'impact,
// et renvoie les projectiles IDeflectable qui s'y trouvent (parade).
// Utilisée par le rush du Stand (StandCombat), réutilisable par les ennemis.
public class Hitbox : MonoBehaviour
{
    [Header("Zone")]
    public Vector3 localCenter = new Vector3(0f, 1.2f, 1.9f); // relatif au porteur : ici, là où frappe le Stand
    public float radius = 1f;
    public LayerMask layers = ~0;

    [Header("Impact")]
    public GameObject impactEffect;  // prefab de particules, détruit à la fin de son effet
    public AudioClip impactSound;
    [Range(0f, 1f)] public float impactVolume = 0.8f;

    // Celui qui frappe : on ne se blesse pas soi-même, et il est la source des dégâts
    [HideInInspector] public GameObject owner;

    private static readonly Collider[] buffer = new Collider[32];
    private readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();
    private readonly HashSet<IDeflectable> alreadyDeflected = new HashSet<IDeflectable>();

    public Vector3 Center => transform.TransformPoint(localCenter);

    // Retourne le nombre de cibles touchées (projectiles renvoyés compris)
    public int Strike(float amount)
    {
        Vector3 center = Center;
        // Collide : les projectiles ont un collider trigger, il faut aussi les trouver
        int count = Physics.OverlapSphereNonAlloc(center, radius, buffer, layers, QueryTriggerInteraction.Collide);
        alreadyHit.Clear();
        alreadyDeflected.Clear();
        int hits = 0;

        for (int i = 0; i < count; i++)
        {
            Collider col = buffer[i];
            if (owner != null && col.transform.IsChildOf(owner.transform)) continue;
            if (col.transform.IsChildOf(transform)) continue;

            // Parade : un projectile dans la zone est renvoyé (un objet à la fois IDeflectable
            // et IDamageable est donc renvoyé, jamais blessé)
            IDeflectable deflectable = col.GetComponentInParent<IDeflectable>();
            if (deflectable != null)
            {
                GameObject striker = owner != null ? owner : gameObject;
                // Déjà renvoyé par nous lors d'un coup précédent du rush : pas de second impact
                Projectile projectile = deflectable as Projectile;
                bool alreadyOurs = projectile != null && projectile.Owner == striker;

                if (!alreadyOurs && alreadyDeflected.Add(deflectable))
                {
                    deflectable.Deflect(transform.forward, striker);
                    PlayImpact(col.bounds.center);
                    hits++;
                }
                continue;
            }

            if (col.isTrigger) continue; // les autres triggers (zones, détecteurs) ne prennent pas de coups

            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive || !alreadyHit.Add(target)) continue;

            Vector3 point = col.ClosestPoint(center);
            Vector3 direction = point - (owner != null ? owner.transform.position : transform.position);
            direction.y = 0f;

            target.TakeDamage(new DamageInfo
            {
                amount = amount,
                point = point,
                direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward,
                source = owner != null ? owner : gameObject
            });
            PlayImpact(point);
            hits++;
        }
        return hits;
    }

    private void PlayImpact(Vector3 point)
    {
        if (impactEffect != null)
            Instantiate(impactEffect, point, Quaternion.LookRotation(-transform.forward));

        if (impactSound == null) return;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(impactSound, impactVolume);
        else
            AudioSource.PlayClipAtPoint(impactSound, point, impactVolume);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(Center, radius);
    }
}
