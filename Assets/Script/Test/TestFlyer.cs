using UnityEngine;

// Cible de test pour Sandbox : tourne en cercle dans les airs en montant et descendant.
// Sert à tester le lock-on sur une cible en vol (comme le futur faucon de glace).
public class TestFlyer : MonoBehaviour
{
    [Header("Trajectoire")]
    public float radius = 6f;
    public float height = 5f;
    public float heightVariation = 2f;
    public float angularSpeed = 40f; // degrés par seconde

    private Vector3 center;
    private float angle;

    void Start()
    {
        center = transform.position;
    }

    void Update()
    {
        // Objet du monde : il obéit à WorldTime (arrêt du temps)
        angle += angularSpeed * WorldTime.DeltaTime;
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad) * radius, height + Mathf.Sin(rad * 2f) * heightVariation, Mathf.Sin(rad) * radius);
        transform.position = center + offset;
        transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad)));
    }
}
