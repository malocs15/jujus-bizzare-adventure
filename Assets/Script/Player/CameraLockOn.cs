using UnityEngine;
using UnityEngine.InputSystem;

// Lock-on : clic molette verrouille la cible la plus proche du centre de l'écran.
// ThirdPersonCamera lit Current pour cadrer la cible ; un deuxième clic déverrouille.
[RequireComponent(typeof(Camera))]
public class CameraLockOn : MonoBehaviour
{
    [Header("Sélection")]
    public float maxDistance = 25f;          // portée pour verrouiller
    public float breakDistance = 32f;        // au-delà, le verrou saute
    [Range(0f, 0.5f)] public float screenMargin = 0.05f; // la cible doit être visible à l'écran

    // Cible verrouillée, ou null. Le HUD pourra la lire pour afficher le marqueur.
    public LockOnTarget Current { get; private set; }

    private Camera cam;
    private ThirdPersonCamera rig; // donne le joueur suivi (target)
    private InputAction lockAction;

    void Awake()
    {
        cam = GetComponent<Camera>();
        rig = GetComponent<ThirdPersonCamera>();
    }

    void Start()
    {
        lockAction = InputSystem.actions.FindAction("Player/LockOn", true);
        lockAction.Enable();
    }

    void Update()
    {
        if (lockAction.WasPressedThisFrame())
        {
            if (Current != null) Current = null;
            else Current = FindBestTarget();
        }

        // Le verrou saute si la cible meurt, disparaît ou s'éloigne trop
        if (Current != null && (!Current.IsValid || DistanceToPlayer(Current) > breakDistance))
            Current = null;
    }

    private LockOnTarget FindBestTarget()
    {
        LockOnTarget best = null;
        float bestScore = float.MaxValue;
        Vector2 center = new Vector2(0.5f, 0.5f);

        foreach (LockOnTarget t in LockOnTarget.All)
        {
            if (!t.IsValid || DistanceToPlayer(t) > maxDistance) continue;

            Vector3 vp = cam.WorldToViewportPoint(t.AimPoint);
            bool onScreen = vp.z > 0f &&
                            vp.x > screenMargin && vp.x < 1f - screenMargin &&
                            vp.y > screenMargin && vp.y < 1f - screenMargin;
            if (!onScreen) continue;

            // Distance au centre de l'écran, en x comme en y : marche aussi pour une cible en vol
            float score = Vector2.Distance(new Vector2(vp.x, vp.y), center);
            if (score < bestScore)
            {
                bestScore = score;
                best = t;
            }
        }
        return best;
    }

    private float DistanceToPlayer(LockOnTarget t)
    {
        Vector3 from = rig != null && rig.target != null ? rig.target.position : transform.position;
        return Vector3.Distance(from, t.AimPoint);
    }
}
