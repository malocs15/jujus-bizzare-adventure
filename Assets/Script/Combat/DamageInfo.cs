using UnityEngine;

public struct DamageInfo
{
    public float amount;
    public Vector3 point;
    public Vector3 direction;   // Vector3.zero = dégât d'environnement (chaleur…) : pas de recul sur le joueur
    public GameObject source;
}