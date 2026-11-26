using UnityEngine;

// Orbe de soin : le joueur la touche, elle lui rend de la vie puis disparaît.
// Si le joueur a déjà toute sa vie, elle reste là pour plus tard.
[RequireComponent(typeof(Collider))]
public class HealOrb : MonoBehaviour
{
    public float healAmount = 30f;

    [Header("Animation")]
    public float bobHeight = 0.2f;     // l'orbe monte et descend doucement
    public float bobSpeed = 2f;
    public float spinSpeed = 90f;      // degrés par seconde

    private Vector3 startPosition;
    private float time;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        // Layer « Ignore Raycast » : sinon le test de sol du joueur le ferait tenir sur l'orbe en plein saut
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        startPosition = transform.position;
    }

    void Update()
    {
        // Objet du monde : WorldTime (l'orbe se fige pendant l'arrêt du temps)
        time += WorldTime.DeltaTime;
        transform.position = startPosition + Vector3.up * Mathf.Sin(time * bobSpeed) * bobHeight;
        transform.Rotate(0f, spinSpeed * WorldTime.DeltaTime, 0f);
    }

    // Stay plutôt que Enter : un joueur déjà dans l'orbe qui se fait toucher est soigné tout de suite
    void OnTriggerStay(Collider other)
    {
        // Seul le joueur ramasse les orbes (les ennemis ont aussi un Health)
        if (other.GetComponentInParent<PlayerMovement>() == null) return;

        Health health = other.GetComponentInParent<Health>();
        if (health == null || !health.IsAlive || health.Current >= health.max) return;

        health.Heal(healAmount);
        Destroy(gameObject);
    }
}
