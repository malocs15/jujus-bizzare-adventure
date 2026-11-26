using System.Collections.Generic;
using UnityEngine;

// Zone d'ombre (sous un palmier, dans une ruine, derrière un pilier) : une BoxCollider en trigger.
// Tant que le joueur est dedans, sa jauge de chaleur (HeatGauge) redescend.
// On teste la position du joueur plutôt que OnTriggerEnter/Exit : un collider désactivé
// (le Stand qu'on rappelle, par exemple) ne déclenche jamais de OnTriggerExit.
[RequireComponent(typeof(BoxCollider))]
public class ShadeZone : MonoBehaviour
{
    // Toutes les zones d'ombre actives de la scène
    private static readonly List<ShadeZone> zones = new List<ShadeZone>();

    private BoxCollider area;

    void Awake()
    {
        area = GetComponent<BoxCollider>();
        area.isTrigger = true; // on doit pouvoir y entrer
        // Layer « Ignore Raycast » : sinon le test de sol du joueur (PlayerMovement)
        // prendrait le dessus de la zone pour du sol en retombant d'un saut
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    void OnEnable() { zones.Add(this); }
    void OnDisable() { zones.Remove(this); }

    // Le point est-il dans au moins une zone d'ombre ?
    public static bool IsInShade(Vector3 point)
    {
        foreach (ShadeZone zone in zones)
        {
            if (!zone.area.enabled) continue;
            // ClosestPoint renvoie le point lui-même quand il est à l'intérieur du collider
            if ((zone.area.ClosestPoint(point) - point).sqrMagnitude < 0.0001f)
                return true;
        }
        return false;
    }

    // Visible dans l'éditeur seulement : un pavé bleu transparent
    void OnDrawGizmos()
    {
        Collider c = GetComponent<Collider>();
        if (c == null) return;
        Gizmos.color = new Color(0.2f, 0.4f, 1f, 0.25f);
        Gizmos.DrawCube(c.bounds.center, c.bounds.size);
    }
}
