using UnityEngine;
using UnityEngine.InputSystem;

// Grab : clic droit, le Stand attrape à distance l'objet IGrabbable visé au centre de l'écran.
// Une pierre est ramenée devant le joueur puis lancée au clic gauche (ou lâchée au clic droit) ;
// un levier est actionné sur place.
[RequireComponent(typeof(StandController), typeof(PlayerMovement))]
public class StandGrab : MonoBehaviour
{
    [Header("Visée")]
    public float grabRange = 12f;        // distance max entre le joueur et l'objet
    // Écart max au centre de l'écran, en coordonnées viewport (0 à 1 sur chaque axe :
    // un peu plus large horizontalement sur un écran 16:9)
    [Range(0.01f, 0.5f)] public float aimTolerance = 0.12f;
    public LayerMask grabLayers = ~0;

    [Header("Objet porté")]
    public Vector3 holdOffset = new Vector3(0f, 1.8f, 1.6f); // point de maintien, relatif au joueur (lu au lancement)
    public float throwSpeed = 16f;
    public float throwLift = 3f;         // un peu de hauteur pour une belle parabole
    public float actionRecovery = 0.3f;  // temps en Grab après un lancer ou un levier

    // held est une interface : on passe par Object pour que == null détecte un objet détruit
    public bool IsHolding => held != null && (held as Object) != null;

    private StandController stand;
    private PlayerMovement player;
    private Transform holdPoint;
    private InputAction grabAction;
    private InputAction attackAction;
    private IGrabbable held;
    private float recoveryTimer;

    void Awake()
    {
        stand = GetComponent<StandController>();
        player = GetComponent<PlayerMovement>();

        holdPoint = new GameObject("GrabHoldPoint").transform;
        holdPoint.SetParent(transform, false);
        holdPoint.localPosition = holdOffset;
    }

    void Start()
    {
        grabAction = InputSystem.actions.FindAction("Player/Grab", true);
        attackAction = InputSystem.actions.FindAction("Player/Attack", true);
        grabAction.Enable();
        attackAction.Enable();
    }

    // Composant désactivé : on lâche l'objet et on rend la main au joueur
    void OnDisable()
    {
        Drop();
        recoveryTimer = 0f;
        if (player != null && player.State == PlayerState.Grab)
            player.ChangeState(PlayerState.Locomotion);
    }

    void Update()
    {
        if (held != null) // référence brute : TickHolding gère aussi le cas d'un objet détruit
            TickHolding();
        else if (recoveryTimer > 0f)
            TickRecovery();
        else if (grabAction.WasPressedThisFrame())
            TryGrab();
    }

    public bool TryGrab()
    {
        if (player.State != PlayerState.Locomotion || !stand.IsSummoned) return false;

        IGrabbable target = FindTarget();
        if (target == null) return false;

        target.OnGrabbed(holdPoint);
        player.ChangeState(PlayerState.Grab);

        if (target.CanBeCarried)
            held = target;            // la pierre vole vers holdPoint toute seule
        else
            recoveryTimer = actionRecovery; // levier : actionné, on rend la main
        return true;
    }

    private void TickHolding()
    {
        // L'objet a pu être détruit pendant qu'on le tenait (pierre-leurre, chute…)
        if ((held as Object) == null)
        {
            held = null;
            if (player.State == PlayerState.Grab) player.ChangeState(PlayerState.Locomotion);
            return;
        }

        // Touché ou Stand renvoyé : on lâche l'objet
        if (player.State != PlayerState.Grab || !stand.IsSummoned)
        {
            Drop();
            return;
        }

        if (attackAction.WasPressedThisFrame())
            Throw();
        else if (grabAction.WasPressedThisFrame())
            Drop();
    }

    private void TickRecovery()
    {
        recoveryTimer -= Time.deltaTime;
        if (recoveryTimer <= 0f && player.State == PlayerState.Grab)
            player.ChangeState(PlayerState.Locomotion);
    }

    private void Throw()
    {
        Vector3 forward = Camera.main != null ? Camera.main.transform.forward : transform.forward;

        // On peut lancer avant que la pierre soit arrivée : elle part alors d'où elle est.
        // Le joueur se tourne vers la direction du lancer
        Vector3 flat = new Vector3(forward.x, 0f, forward.z);
        if (flat.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(flat);

        held.OnThrown(forward * throwSpeed + Vector3.up * throwLift);
        stand.PlayAnimation("Throw");
        held = null;
        recoveryTimer = actionRecovery;
    }

    public void Drop()
    {
        if (held == null) return;
        if ((held as Object) != null) held.OnThrown(Vector3.zero);
        held = null;
        if (player != null && player.State == PlayerState.Grab)
            player.ChangeState(PlayerState.Locomotion);
    }

    // Aide à la visée : parmi les objets attrapables à portée, on prend celui qui est le plus
    // proche du centre de l'écran (dans la tolérance), à condition que rien ne le cache.
    private IGrabbable FindTarget()
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        IGrabbable best = null;
        float bestScore = aimTolerance;
        Vector2 center = new Vector2(0.5f, 0.5f);

        foreach (Collider col in Physics.OverlapSphere(transform.position, grabRange, grabLayers, QueryTriggerInteraction.Ignore))
        {
            IGrabbable g = col.GetComponentInParent<IGrabbable>();
            if (g == null) continue;

            Vector3 point = col.bounds.center;
            Vector3 vp = cam.WorldToViewportPoint(point);
            if (vp.z <= 0f) continue; // derrière la caméra

            float score = Vector2.Distance(new Vector2(vp.x, vp.y), center);
            if (score < bestScore && IsVisible(cam.transform.position, point, col))
            {
                bestScore = score;
                best = g;
            }
        }
        return best;
    }

    // Vrai si le premier obstacle entre la caméra et le point (hors joueur) est l'objet lui-même
    private bool IsVisible(Vector3 from, Vector3 to, Collider target)
    {
        Vector3 dir = to - from;
        RaycastHit[] hits = Physics.RaycastAll(from, dir.normalized, dir.magnitude, grabLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; // le joueur est souvent entre les deux
            return hit.collider == target;
        }
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.TransformPoint(holdOffset), 0.2f);
    }
}
