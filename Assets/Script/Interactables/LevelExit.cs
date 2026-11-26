using UnityEngine;

// Sortie de niveau : le joueur entre dans la zone, on charge la scène suivante (avec le fondu de SceneFlow).
[RequireComponent(typeof(Collider))]
public class LevelExit : MonoBehaviour
{
    public string nextScene = "Level2_Mansion";
    public bool locked = false; // TODO S2-M6 : verrouillée tant que PetShop n'est pas vaincu

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); // voir Checkpoint
    }

    void OnTriggerEnter(Collider other)
    {
        if (locked || other.GetComponentInParent<PlayerMovement>() == null) return;
        if (GameManager.Instance == null) return;

        // SceneFlow ignore un deuxième chargement pendant le fondu : pas besoin de verrou ici
        GameManager.Instance.LoadScene(nextScene);
    }
}
