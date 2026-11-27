using UnityEngine;

// Checkpoint : quand le joueur passe dedans, le GameManager retient son identifiant.
// La réapparition au dernier checkpoint (et la sauvegarde) viendra avec S1-J7.
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    public string checkpointId = "Level1_Start"; // unique dans tout le jeu

    [Header("Repère visuel (facultatif)")]
    public Renderer flag;                         // change de couleur une fois activé
    public Color activeColor = new Color(0.3f, 1f, 0.4f);

    public bool Activated { get; private set; }

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        // Layer « Ignore Raycast » : le test de sol du joueur ne doit pas prendre le trigger pour du sol
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    void OnTriggerEnter(Collider other)
    {
        if (Activated || other.GetComponentInParent<PlayerMovement>() == null) return;

        Activated = true;
        if (GameManager.Instance != null) GameManager.Instance.SetCheckpoint(checkpointId);
        if (flag != null) flag.material.color = activeColor;
    }
}
