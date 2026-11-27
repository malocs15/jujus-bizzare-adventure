using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Combat du Stand, au clic gauche :
// - clic court : rush, le Stand enchaîne des coups rapides pendant rushDuration ;
// - clic maintenu : le Stand charge, et au relâchement d'une charge complète il donne
//   un seul coup très fort (coup chargé, pour les murs épais).
// Chaque attaque coûte de l'énergie, qui se recharge.
[RequireComponent(typeof(StandController), typeof(PlayerMovement))]
public class StandCombat : MonoBehaviour
{
    [Header("Rush")]
    public Hitbox hitbox;               // sphère de frappe devant le joueur, là où se tient le Stand
    [Min(0.05f)] public float rushDuration = 0.8f;
    public float hitInterval = 0.08f;   // un coup toutes les 0,08 s : ~10 coups par rush
    public float damagePerHit = 4f;

    [Header("Coup chargé")]
    public float chargeStartDelay = 0.25f; // un clic plus court reste un rush
    [Min(0.05f)] public float chargeTime = 1f; // durée pour une charge complète
    public float chargedDamage = 60f;
    public float chargedCost = 40f;
    public float punchWindup = 0.12f;      // petit temps avant l'impact
    [Min(0.05f)] public float punchDuration = 0.35f;

    [Header("Énergie du Stand")]
    public float maxEnergy = 100f;
    public float rushCost = 25f;
    public float regenPerSecond = 20f;
    public float regenDelay = 0.6f;     // pause avant que l'énergie remonte

    public float Energy { get; private set; }
    public bool IsAttacking => attackTimer > 0f;      // rush ou coup chargé en cours
    public bool IsCharging { get; private set; }
    public float ChargeProgress { get; private set; } // de 0 à 1, pour un futur affichage

    // (énergie actuelle, énergie max) : pour la barre d'énergie du HUD
    public event Action<float, float> EnergyChanged;

    private StandController stand;
    private PlayerMovement player;
    private InputAction attackAction;
    private float attackTimer;
    private float hitTimer;
    private float currentInterval;      // intervalle et dégâts de l'attaque en cours
    private float currentDamage;
    private bool singleHit;             // coup chargé : une seule frappe
    private bool buttonHeld;
    private float holdTime;             // depuis l'appui
    private float chargeTimer;          // depuis le début de la charge
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

    // Si le composant est désactivé en pleine attaque, on rend la main proprement
    void OnDisable()
    {
        if (IsCharging) CancelCharge();
        if (IsAttacking) EndStrikes();
        buttonHeld = false;
    }

    void Update()
    {
        if (IsAttacking)
            TickStrikes();
        else
            TickButton();

        Regenerate();
    }

    // ---------- Lecture du clic : on décide au relâchement ----------

    private void TickButton()
    {
        // On ne compte que les appuis faits en Locomotion : un clic qui lance une pierre,
        // ou fait pendant un saut ou un coup reçu, ne doit pas donner un rush au relâchement
        if (attackAction.WasPressedThisFrame() && player.State == PlayerState.Locomotion)
        {
            buttonHeld = true;
            holdTime = 0f;
        }
        if (!buttonHeld) return;

        holdTime += Time.deltaTime;

        if (IsCharging)
        {
            TickCharge();
            if (!buttonHeld) return; // charge annulée par un coup reçu
        }
        else if (holdTime >= chargeStartDelay)
        {
            TryStartCharge(); // si c'est impossible, le relâchement donnera un rush
        }

        if (attackAction.WasReleasedThisFrame() || !attackAction.IsPressed())
        {
            buttonHeld = false;
            if (IsCharging)
            {
                bool full = ChargeProgress >= 1f;
                CancelCharge();
                if (full) StartChargedPunch();
                else TryRush(); // relâché trop tôt : rush normal
            }
            else
            {
                TryRush();
            }
        }
    }

    // ---------- Rush ----------

    public bool TryRush()
    {
        if (!CanAttack(rushCost)) return false;
        if (!stand.StartAttack()) return false; // le Stand doit être sorti (Following)

        SpendEnergy(rushCost);
        player.ChangeState(PlayerState.Rush);
        FaceCamera();
        StartStrikes(rushDuration, hitInterval, damagePerHit, 0f, false); // premier coup immédiat
        stand.SetAnimationBool("Rush", true);
        return true;
    }

    // ---------- Coup chargé ----------

    private void TryStartCharge()
    {
        if (!CanAttack(chargedCost) || !stand.IsSummoned) return;
        // TODO S1-J2 : ne l'autoriser qu'une fois la compétence débloquée (niveau 5, section 4.1)
        IsCharging = true;
        ChargeProgress = 0f;
        chargeTimer = 0f;
        player.ChangeState(PlayerState.Rush); // marche lente pendant la charge
        stand.SetAnimationBool("Charging", true);
    }

    private void TickCharge()
    {
        // Touché pendant la charge : elle est perdue
        if (player.State != PlayerState.Rush || !stand.IsSummoned)
        {
            CancelCharge();
            buttonHeld = false;
            return;
        }

        FaceCamera(); // on vise pendant la charge
        chargeTimer += Time.deltaTime;
        ChargeProgress = Mathf.Clamp01(chargeTimer / chargeTime);
        stand.SetChargeGlow(ChargeProgress);
    }

    private void CancelCharge()
    {
        IsCharging = false;
        ChargeProgress = 0f;
        stand.SetChargeGlow(0f);
        stand.SetAnimationBool("Charging", false);
        if (player.State == PlayerState.Rush)
            player.ChangeState(PlayerState.Locomotion);
    }

    private void StartChargedPunch()
    {
        if (!stand.StartAttack()) return;
        SpendEnergy(chargedCost);
        player.ChangeState(PlayerState.Rush);
        FaceCamera();
        StartStrikes(punchDuration, 0f, chargedDamage, punchWindup, true); // un seul coup, après l'élan
        stand.PlayAnimation("HeavyPunch");
    }

    // ---------- Frappes communes au rush et au coup chargé ----------

    private bool CanAttack(float cost)
    {
        return hitbox != null && player.State == PlayerState.Locomotion && Energy >= cost;
    }

    private void StartStrikes(float duration, float interval, float damage, float firstHitDelay, bool onlyOneHit)
    {
        attackTimer = duration;
        currentInterval = interval;
        currentDamage = damage;
        hitTimer = firstHitDelay;
        singleHit = onlyOneHit;
    }

    private void TickStrikes()
    {
        // L'attaque s'interrompt si le joueur est touché ou si le Stand est renvoyé
        if (player.State != PlayerState.Rush || stand.State != StandState.Attacking)
        {
            EndStrikes();
            return;
        }

        hitTimer -= Time.deltaTime;
        if (hitTimer <= 0f)
        {
            hitbox.Strike(currentDamage);
            // Coup unique : plus aucune frappe jusqu'à la fin de l'attaque
            hitTimer = singleHit ? float.MaxValue : hitTimer + currentInterval;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f) EndStrikes();
    }

    private void EndStrikes()
    {
        attackTimer = 0f;
        stand.SetAnimationBool("Rush", false);
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

    // ---------- Énergie ----------

    public void SpendEnergy(float amount)
    {
        Energy = Mathf.Max(0f, Energy - amount);
        lastSpendTime = Time.time;
        EnergyChanged?.Invoke(Energy, maxEnergy);
    }

    private void Regenerate()
    {
        if (IsAttacking || IsCharging || Energy >= maxEnergy || Time.time - lastSpendTime < regenDelay) return;
        Energy = Mathf.Min(maxEnergy, Energy + regenPerSecond * Time.deltaTime);
        EnergyChanged?.Invoke(Energy, maxEnergy);
    }
}
