using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// États du Stand (section 4.6 du plan)
public enum StandState { Hidden, Summoning, Following, Attacking, Dismissing }

public class StandController : MonoBehaviour
{
    [Header("Références (à assigner dans l'inspecteur)")]
    [SerializeField] private GameObject standPrefab;   // Assets/Prefabs/StarPlatinum.prefab
    [SerializeField] private AudioClip zaWarudoSound;  // Assets/Audio/star-platinum-za-warudo.mp3

    [Header("Activation")]
    [FormerlySerializedAs("isStandActive")]
    public bool startSummoned = false; // sinon le Stand apparaît avec l'action "Stand" (E)

    [Header("Positionnement devant le joueur (JJBA Anime style)")]
    public float frontOffset = 1.35f;      // Distance devant le joueur
    public float hoverOffset = 0.25f;      // Légère lévitation au-dessus du sol
    public float followSpeed = 14f;        // Vitesse de suivi des pas
    public float rotationSpeed = 16f;      // Vitesse d'alignement du regard
    public float hoverAmplitude = 0.08f;   // Amplitude de l'ondulation de vol
    public float hoverFrequency = 2.4f;    // Fréquence de flottement

    [Header("Transitions")]
    public float summonDuration = 0.22f;
    public float dismissDuration = 0.18f;

    [Header("Attaque (s'ajoute aux clips de StandAnimator)")]
    public float attackLunge = 0.4f;      // le Stand avance pendant le rush
    public float jabAmplitude = 0.25f;    // va-et-vient des coups
    public float jabFrequency = 12f;

    [Header("FSM (lecture seule)")]
    [SerializeField] private StandState currentState; // copie de fsm.Current, pour l'inspecteur

    public StandState State => fsm.Current;
    // Vrai quand le Stand est sorti et peut agir (rush, grab…)
    public bool IsSummoned => fsm.Current == StandState.Following || fsm.Current == StandState.Attacking;
    public Transform StandTransform => standInstance != null ? standInstance.transform : null;

    private StateMachine<StandState> fsm;
    private GameObject standInstance;
    private Animator standAnimator;     // Animator du modèle du Stand (StandAnimator.controller)
    private AudioSource audioSource;
    private Light auraLight;
    private PlayerMovement playerMovement;
    private InputAction standAction;
    private float stateTimer;
    private float dismissStartScale;

    void Awake()
    {
        fsm = new StateMachine<StandState>();
        fsm.Add(StandState.Hidden, enter: EnterHidden, tick: TickHidden);
        fsm.Add(StandState.Summoning, enter: EnterSummoning, tick: TickSummoning);
        fsm.Add(StandState.Following, tick: TickToggleOff);
        fsm.Add(StandState.Attacking, tick: TickToggleOff);
        fsm.Add(StandState.Dismissing, enter: EnterDismissing, tick: TickDismissing);
    }

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        standAction = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Stand") : null;
        if (standAction == null)
            Debug.LogWarning("StandController : action 'Player/Stand' introuvable dans les Input Actions du projet.", this);
        else
            standAction.Enable();

        if (standPrefab == null)
        {
            Debug.LogError("StandController : aucun Stand Prefab assigné dans l'inspecteur.", this);
            enabled = false;
            return;
        }

        // Le Stand n'est pas enfant du joueur : il le suit avec du retard dans LateUpdate
        standInstance = Instantiate(standPrefab);
        standInstance.name = standPrefab.name;
        auraLight = standInstance.GetComponentInChildren<Light>();
        standAnimator = standInstance.GetComponentInChildren<Animator>();

        if (startSummoned)
        {
            standInstance.transform.SetPositionAndRotation(GetTargetPosition(), GetTargetRotation());
            ChangeState(StandState.Following);
        }
        else
        {
            ChangeState(StandState.Hidden);
        }
    }

    void OnDestroy()
    {
        if (standInstance != null) Destroy(standInstance);
    }

    void Update()
    {
        if (standInstance == null) return;
        stateTimer += Time.deltaTime;
        fsm.Tick();
    }

    void LateUpdate()
    {
        // Pendant l'apparition, le suivi et l'attaque, le Stand flotte devant le joueur
        StandState s = fsm.Current;
        if (standInstance != null &&
            (s == StandState.Summoning || s == StandState.Following || s == StandState.Attacking))
            UpdateStandPosition();
    }

    // ---------- API pour les autres scripts ----------

    public void ChangeState(StandState next)
    {
        stateTimer = 0f;
        fsm.ChangeState(next);
        currentState = next;
    }

    public void ToggleStand()
    {
        if (standInstance == null) return;
        StandState s = fsm.Current;
        if (s == StandState.Hidden || s == StandState.Dismissing)
            ChangeState(StandState.Summoning);
        else
            ChangeState(StandState.Dismissing);
    }

    // Le rush (S1-M2) passe le Stand en Attacking, puis le rend avec StopAttack.
    public bool StartAttack()
    {
        if (fsm.Current != StandState.Following) return false;
        ChangeState(StandState.Attacking);
        return true;
    }

    public void StopAttack()
    {
        if (fsm.Current == StandState.Attacking)
            ChangeState(StandState.Following);
    }

    // Animations du Stand, pilotées par ses attaques (StandCombat, StandGrab)
    public void SetAnimationBool(string parameter, bool value)
    {
        if (standAnimator != null && standAnimator.isActiveAndEnabled) standAnimator.SetBool(parameter, value);
    }

    public void PlayAnimation(string trigger)
    {
        if (standAnimator != null && standAnimator.isActiveAndEnabled) standAnimator.SetTrigger(trigger);
    }

    // Pendant la charge du coup chargé, l'aura s'intensifie (t de 0 à 1)
    public void SetChargeGlow(float t)
    {
        if (auraLight != null && IsSummoned)
            auraLight.intensity = Mathf.Lerp(2.5f, 8f, t);
    }

    // ---------- États ----------

    private bool TogglePressed()
    {
        // Un joueur mort ne peut plus invoquer son Stand
        if (playerMovement != null && playerMovement.State == PlayerState.Dead) return false;
        return standAction != null && standAction.WasPressedThisFrame();
    }

    private void EnterHidden()
    {
        standInstance.transform.localScale = Vector3.zero;
        standInstance.SetActive(false);
    }

    private void TickHidden()
    {
        if (TogglePressed()) ToggleStand();
    }

    private void EnterSummoning()
    {
        standInstance.SetActive(true);
        if (playerMovement != null) playerMovement.PlaySummonGesture(); // le joueur pointe devant lui

        // Émerge depuis le joueur vers l'avant (sauf si on interrompt un renvoi : il repart d'où il est)
        if (standInstance.transform.localScale.x <= 0.01f)
            standInstance.transform.position = transform.position + Vector3.up * hoverOffset;

        if (zaWarudoSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(zaWarudoSound, 1f);
        }
        if (auraLight != null) auraLight.intensity = 8f;
    }

    private void TickSummoning()
    {
        float t = Mathf.Clamp01(stateTimer / summonDuration);
        standInstance.transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t);
        if (auraLight != null) auraLight.intensity = Mathf.Lerp(8f, 2.5f, t);

        if (TogglePressed()) ToggleStand();
        else if (t >= 1f) ChangeState(StandState.Following);
    }

    private void TickToggleOff()
    {
        if (TogglePressed()) ToggleStand();
    }

    private void EnterDismissing()
    {
        dismissStartScale = standInstance.transform.localScale.x;
        if (auraLight != null) auraLight.intensity = 2.5f;
    }

    private void TickDismissing()
    {
        float t = Mathf.Clamp01(stateTimer / dismissDuration);
        standInstance.transform.localScale = Vector3.one * Mathf.SmoothStep(dismissStartScale, 0f, t);

        if (TogglePressed()) ToggleStand();
        else if (t >= 1f) ChangeState(StandState.Hidden);
    }

    // ---------- Positionnement ----------

    private Vector3 GetTargetPosition()
    {
        // Ondulation naturelle de lévitation
        float hover = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;

        // Le Stand se projette légèrement plus en avant lors de la course
        float speedBonus = playerMovement != null ? playerMovement.CurrentSpeed * 0.08f : 0f;

        // Pendant le rush, il avance et fait des va-et-vient rapides
        float jab = 0f;
        if (fsm.Current == StandState.Attacking)
            jab = attackLunge + Mathf.Abs(Mathf.Sin(stateTimer * jabFrequency)) * jabAmplitude;

        return transform.position +
               transform.forward * (frontOffset + speedBonus + jab) +
               Vector3.up * (hoverOffset + hover);
    }

    private Quaternion GetTargetRotation()
    {
        // Légère inclinaison vers l'avant (posture de vol offensive naturelle)
        return transform.rotation * Quaternion.Euler(6f, 0f, 0f);
    }

    private void UpdateStandPosition()
    {
        Transform t = standInstance.transform;
        t.position = Vector3.Lerp(t.position, GetTargetPosition(), followSpeed * Time.deltaTime);
        t.rotation = Quaternion.Slerp(t.rotation, GetTargetRotation(), rotationSpeed * Time.deltaTime);
    }
}
