using UnityEngine;

// Sables mouvants : le joueur dedans est très ralenti, et s'il y reste trop longtemps
// il s'enfonce et perd de la vie. Il peut en sortir en marchant ou en sautant.
// Comme ShadeZone, on teste la position du joueur à chaque image plutôt que OnTriggerExit.
[RequireComponent(typeof(BoxCollider))]
public class Quicksand : MonoBehaviour
{
    [Range(0.05f, 1f)] public float speedFactor = 0.35f; // vitesse du joueur dans le sable
    public float safeTime = 1.5f;                         // temps avant de commencer à s'enfoncer
    public float damagePerSecond = 10f;

    private BoxCollider area;
    private PlayerMovement player;
    private Health playerHealth;
    private bool inside;
    private float insideTimer;
    private float tickTimer;

    void Awake()
    {
        area = GetComponent<BoxCollider>();
        area.isTrigger = true;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); // voir ShadeZone
    }

    void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        player = FindFirstObjectByType<PlayerMovement>();
        playerHealth = player != null ? player.GetComponent<Health>() : null;
    }

    // Zone désactivée : on rend sa vitesse au joueur
    void OnDisable()
    {
        SetInside(false);
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer(); // joueur réapparu (checkpoint) : on le cherche à nouveau
            if (player == null) return;
        }

        // Pieds du joueur légèrement au-dessus du sol. La zone fait 2 m de haut :
        // un joueur qui saute (1,5 m) reste dedans, on ne traverse pas en sautillant.
        Vector3 feet = player.transform.position + Vector3.up * 0.1f;
        bool nowInside = (area.ClosestPoint(feet) - feet).sqrMagnitude < 0.0001f;
        SetInside(nowInside);

        if (!inside)
        {
            insideTimer = 0f;
            return;
        }

        // Piège du monde : WorldTime (section 5.5). Pendant l'arrêt du temps, le joueur ne s'enfonce plus.
        insideTimer += WorldTime.DeltaTime;
        tickTimer = Mathf.Max(0f, tickTimer - WorldTime.DeltaTime);
        if (insideTimer >= safeTime && tickTimer <= 0f && playerHealth != null)
        {
            tickTimer = 1f; // une fois par seconde
            playerHealth.TakeDamage(new DamageInfo
            {
                amount = damagePerSecond,
                point = feet,
                direction = Vector3.zero, // dégât d'environnement : pas de recul
                source = gameObject
            });
        }
    }

    private void SetInside(bool value)
    {
        if (value == inside) return;
        inside = value;
        if (player != null) player.SpeedMultiplier = value ? speedFactor : 1f;
    }
}
