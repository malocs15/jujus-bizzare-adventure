using UnityEngine;

// Tout ce que le Stand peut attraper à distance (pierres, leviers). Section 5.2 du plan.
public interface IGrabbable
{
    // Vrai pour un objet que le Stand ramène et garde en main (pierre),
    // faux pour un objet qu'il actionne sur place (levier).
    bool CanBeCarried { get; }

    // holder : le point devant le joueur où l'objet doit se tenir
    void OnGrabbed(Transform holder);

    // velocity = Vector3.zero : l'objet est simplement lâché
    void OnThrown(Vector3 velocity);
}
