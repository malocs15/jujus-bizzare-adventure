using UnityEngine;

// Tout ce que le Stand peut renvoyer (glaçons, couteaux). Section 5.2 du plan.
public interface IDeflectable
{
    // hitDirection : direction du coup qui renvoie l'objet (sert d'axe de réflexion) ;
    // c'est l'implémentation qui choisit la direction finale.
    // newOwner : celui qui renvoie, devient le propriétaire de l'objet.
    void Deflect(Vector3 hitDirection, GameObject newOwner);
}
