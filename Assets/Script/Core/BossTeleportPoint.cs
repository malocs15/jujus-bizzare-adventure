using UnityEngine;

// Repère à poser dans chaque niveau, juste avant le boss : la god key F4 y téléporte le joueur.
public class BossTeleportPoint : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }
}
