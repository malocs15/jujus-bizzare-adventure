using System.Collections.Generic;
using UnityEngine;

// À poser sur tout ce que la caméra peut verrouiller (ennemis, boss, mannequins).
// Chaque cible s'inscrit dans une liste statique : pas besoin de chercher dans la scène.
public class LockOnTarget : MonoBehaviour
{
    public static readonly List<LockOnTarget> All = new List<LockOnTarget>();

    [Header("Point visé (local)")]
    public Vector3 aimOffset = new Vector3(0f, 1.2f, 0f);

    private Health health;

    public Vector3 AimPoint => transform.TransformPoint(aimOffset);

    // Une cible morte ou désactivée ne peut plus être verrouillée
    public bool IsValid => isActiveAndEnabled && (health == null || health.IsAlive);

    void Awake()
    {
        health = GetComponentInParent<Health>();
    }

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(AimPoint, 0.2f);
    }
}
