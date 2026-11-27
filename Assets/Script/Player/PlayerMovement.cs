using UnityEngine;
using UnityEngine.InputSystem;

// États du joueur (section 4.6 du plan)
public enum PlayerState { Locomotion, Jump, Rush, Grab, Hit, Dead }

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Vitesses & Saut")]
    // Vitesses calées sur la vitesse naturelle des clips Mixamo (averageSpeed x humanScale)
    // pour éviter que les pieds glissent. Si tu les changes, change aussi les seuils
    // du Blend Tree "Locomotion" dans JotaroController (1.95 / 5.0).
    public float walkSpeed = 1.95f;
    public float runSpeed = 5f;
    public float acceleration = 10f;
    public float turnSpeed = 540f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;

    [Header("États")]
    public float actionSpeedFactor = 0.5f; // vitesse de marche réduite pendant Rush et Grab
    public float hitStunDuration = 0.4f;   // durée pendant laquelle le joueur ne contrôle plus rien après un coup

    [Header("Animator")]
    public string speedParameter = "Speed";
    public string jumpParameter = "Jump";
    public string groundedParameter = "Grounded";
    public string freeFallParameter = "FreeFall";

    [Header("FSM (lecture seule)")]
    [SerializeField] private PlayerState currentState; // copie de fsm.Current, pour l'inspecteur

    public float CurrentSpeed { get; private set; }
    public bool IsGrounded { get; private set; }
    public PlayerState State => fsm.Current;

    private StateMachine<PlayerState> fsm;
    private CharacterController controller;
    private Animator animator;
    private float verticalVelocity;
    private Vector3 horizontalVelocity;
    private float stateTimer;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        // Construit dans Awake pour que les autres scripts puissent changer l'état dès leur Start
        fsm = new StateMachine<PlayerState>();
        fsm.Add(PlayerState.Locomotion, tick: TickLocomotion);
        fsm.Add(PlayerState.Jump, tick: TickJump);
        fsm.Add(PlayerState.Rush, tick: TickAction);
        fsm.Add(PlayerState.Grab, tick: TickAction);
        fsm.Add(PlayerState.Hit, enter: StopHorizontal, tick: TickHit);
        fsm.Add(PlayerState.Dead, enter: StopHorizontal, tick: TickDead);
        ChangeState(PlayerState.Locomotion);
    }

    void Start()
    {
        // Actions "Project-wide" (Assets/InputSystem_Actions.inputactions).
        // Les touches clavier sont positionnelles : WASD en QWERTY = ZQSD en AZERTY, sans code spécifique.
        moveAction = InputSystem.actions.FindAction("Player/Move", true);
        jumpAction = InputSystem.actions.FindAction("Player/Jump", true);
        sprintAction = InputSystem.actions.FindAction("Player/Sprint", true);
        moveAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
    }

    // Point d'entrée unique pour changer d'état, utilisé aussi par StandCombat, Health…
    public void ChangeState(PlayerState next)
    {
        stateTimer = 0f;
        fsm.ChangeState(next);
        currentState = next;
    }

    void Update()
    {
        UpdateGrounded();
        stateTimer += Time.deltaTime;
        fsm.Tick();
        ApplyGravityAndMove();
        UpdateAnimator();
    }

    // ---------- États ----------

    private void TickLocomotion()
    {
        Steer(sprintAction.IsPressed() ? runSpeed : walkSpeed);

        if (!IsGrounded)
        {
            ChangeState(PlayerState.Jump); // tombe d'un rebord sans sauter
        }
        else if (jumpAction.WasPressedThisFrame())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (animator != null) animator.SetBool(jumpParameter, true);
            ChangeState(PlayerState.Jump);
        }
    }

    private void TickJump()
    {
        Steer(sprintAction.IsPressed() ? runSpeed : walkSpeed);

        // On ne considère l'atterrissage qu'en descente, sinon le sol du départ compterait
        if (IsGrounded && verticalVelocity <= 0f)
            ChangeState(PlayerState.Locomotion);
    }

    // Rush et Grab : le joueur marche lentement sans se retourner, pour que le Stand
    // reste face à sa cible. C'est le script du Stand qui ramène le joueur en Locomotion.
    private void TickAction()
    {
        Steer(walkSpeed * actionSpeedFactor, false);
    }

    private void TickHit()
    {
        Steer(0f);
        if (stateTimer >= hitStunDuration)
            ChangeState(IsGrounded ? PlayerState.Locomotion : PlayerState.Jump);
    }

    private void TickDead()
    {
        Steer(0f);
    }

    private void StopHorizontal()
    {
        horizontalVelocity = Vector3.zero;
    }

    // ---------- Déplacement commun à tous les états ----------

    // Accélère vers la direction demandée, relative à la caméra. maxSpeed = 0 : on freine sur place.
    // turn = false : le joueur se déplace sans changer d'orientation.
    private void Steer(float maxSpeed, bool turn = true)
    {
        Vector3 moveDir = Vector3.zero;
        if (maxSpeed > 0f)
        {
            Vector2 move = moveAction.ReadValue<Vector2>();
            Vector3 input = Vector3.ClampMagnitude(new Vector3(move.x, 0f, move.y), 1f);

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (Camera.main != null)
            {
                forward = Camera.main.transform.forward;
                right = Camera.main.transform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
            }
            moveDir = forward * input.z + right * input.x;
        }

        bool moving = moveDir.sqrMagnitude > 0.001f;
        Vector3 targetVelocity = moving ? moveDir.normalized * maxSpeed : Vector3.zero;
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

        if (moving && turn)
        {
            Quaternion target = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }

    // Détection solide du sol (CharacterController + Raycast de secours)
    private void UpdateGrounded()
    {
        IsGrounded = controller.isGrounded;
        if (!IsGrounded && verticalVelocity <= 0.1f)
            IsGrounded = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.35f);
    }

    private void ApplyGravityAndMove()
    {
        if (IsGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f; // colle au sol sans accumuler de vitesse
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        verticalVelocity = Mathf.Max(verticalVelocity, -30f);

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // Filet de sécurité en cas de chute hors de la map (vide)
        if (transform.position.y < -20f)
        {
            Vector3 pos = transform.position;
            pos.y = 0.5f;
            transform.position = pos;
            verticalVelocity = 0f;
        }

        Vector3 flat = controller.velocity;
        flat.y = 0f;
        CurrentSpeed = flat.magnitude;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        bool inAir = fsm.Current == PlayerState.Jump;
        animator.SetBool(groundedParameter, !inAir);
        if (!inAir)
        {
            animator.SetBool(freeFallParameter, false);
            animator.SetBool(jumpParameter, false);
        }
        else if (verticalVelocity < 0f)
        {
            animator.SetBool(freeFallParameter, true);
        }
        animator.SetFloat(speedParameter, CurrentSpeed, 0.1f, Time.deltaTime);
    }

    private void OnLand() { } // appelé par un Animation Event du clip de saut
}
