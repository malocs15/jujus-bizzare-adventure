using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Combat du Stand. Rush : clic gauche, le Stand enchaîne des coups rapides
// devant le joueur pendant rushDuration. Chaque rush coûte de l'énergie, qui se recharge.
[RequireComponent(typeof(StandController), typeof(PlayerMovement))]
public class StandCombat : MonoBehaviour
{
    [Header("Rush")]
    public Hitbox hitbox;               // sphère de frappe devant le joueur, là où se tient le Stand
    [Min(0.05f)] public float rushDuration = 0.8f;
    public float hitInterval = 0.08f;   // un coup toutes les 0,08 s : ~10 coups par rush
    public float damagePerHit = 4f;

    [Header("Énergie du Stand")]
    public float maxEnergy = 100f;
    public float rushCost = 25f;
    public float regenPerSecond = 20f;
    public float regenDelay = 0.6f;     // pause avant que l'énergie remonte

    public float Energy { get; private set; }
    public bool IsRushing => rushTimer > 0f;

    // (énergie actuelle, énergie max) : pour la barre d'énergie du HUD
    public event Action<float, float> EnergyChanged;

    private StandController stand;
    private PlayerMovement player;
    private InputAction attackAction;
    private float rushTimer;
    private float hitTimer;
    private float lastSpendTime = -100f;

    void Awake()
    {
        stand = GetComponent<StandController>();
        player = GetComponent<PlayerMovement>();
        Energy = maxEnergy;
    }

    void Start()
    {
        attackAction = InputSystem.actions.FindAction("Player/Attack", true);
        attackAction.Enable();

        if (hitbox == null)
            Debug.LogError("StandCombat : aucune Hitbox assignée dans l'inspecteur.", this);
        else
            hitbox.owner = gameObject;

        EnergyChanged?.Invoke(Energy, maxEnergy);
    }

    // Si le composant est désactivé en plein rush, on rend la main proprement
    void OnDisable()
    {
        if (IsRushing) EndRush();
    }

    void Update()
    {
        if (IsRushing)
            TickRush();
        else if (attackAction.WasPressedThisFrame())
            TryRush();

        Regenerate();
    }

    public bool TryRush()
    {
        if (hitbox == null || player.State != PlayerState.Locomotion) return false;
        if (Energy < rushCost) return false;
        if (!stand.StartAttack()) return false; // le Stand doit être sorti (Following)

        SpendEnergy(rushCost);
        player.ChangeState(PlayerState.Rush);
        FaceCamera();
        rushTimer = rushDuration;
        hitTimer = 0f; // premier coup immédiat
        return true;
    }

    private void TickRush()
    {
        // Le rush s'interrompt si le joueur est touché ou si le Stand est renvoyé
        if (player.State != PlayerState.Rush || stand.State != StandState.Attacking)
        {
            EndRush();
            return;
        }

        hitTimer -= Time.deltaTime;
        if (hitTimer <= 0f)
        {
            hitbox.Strike(damagePerHit);
            hitTimer += hitInterval;
        }

        rushTimer -= Time.deltaTime;
        if (rushTimer <= 0f) EndRush();
    }

    private void EndRush()
    {
        rushTimer = 0f;
        stand.StopAttack();
        if (player.State == PlayerState.Rush)
            player.ChangeState(PlayerState.Locomotion);
    }

    // Le joueur se tourne vers là où regarde la caméra, pour frapper ce qu'on vise
    private void FaceCamera()
    {
        if (Camera.main == null) return;
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(forward);
    }

    public void SpendEnergy(float amount)
    {
        Energy = Mathf.Max(0f, Energy - amount);
        lastSpendTime = Time.time;
        EnergyChanged?.Invoke(Energy, maxEnergy);
    }

    private void Regenerate()
    {
        if (IsRushing || Energy >= maxEnergy || Time.time - lastSpendTime < regenDelay) return;
        Energy = Mathf.Min(maxEnergy, Energy + regenPerSecond * Time.deltaTime);
        EnergyChanged?.Invoke(Energy, maxEnergy);
    }
}
