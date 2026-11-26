using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Animator")]
    public string speedParameter = "Speed";
    public string jumpParameter = "Jump";
    public string groundedParameter = "Grounded";
    public string freeFallParameter = "FreeFall";

    public float CurrentSpeed { get; private set; }

    private CharacterController controller;
    private Animator animator;
    private float verticalVelocity;
    private Vector3 horizontalVelocity;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        // Actions "Project-wide" (Assets/InputSystem_Actions.inputactions).
        // Les touches clavier sont positionnelles : WASD en QWERTY = ZQSD en AZERTY, sans code spécifique.
        moveAction = InputSystem.actions.FindAction("Player/Move", true);
        jumpAction = InputSystem.actions.FindAction("Player/Jump", true);
        sprintAction = InputSystem.actions.FindAction("Player/Sprint", true);
        moveAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
    }

    void Update()
    {
        Vector2 move = moveAction.ReadValue<Vector2>();
        Vector3 input = Vector3.ClampMagnitude(new Vector3(move.x, 0f, move.y), 1f);
        bool running = sprintAction.IsPressed();

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
        Vector3 moveDir = forward * input.z + right * input.x;

        float targetSpeed = moveDir.sqrMagnitude > 0.001f
            ? (running ? runSpeed : walkSpeed)
            : 0f;
        Vector3 targetVelocity = moveDir.normalized * targetSpeed;
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        // Détection solide du sol (CharacterController + Raycast de secours)
        bool isGrounded = controller.isGrounded;
        if (!isGrounded && verticalVelocity <= 0.1f)
        {
            isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.35f);
        }

        // Saut et gestion de la gravité
        if (isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (animator != null)
            {
                animator.SetBool(groundedParameter, true);
                animator.SetBool(freeFallParameter, false);
                animator.SetBool(jumpParameter, false);
            }

            if (jumpAction.WasPressedThisFrame())
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null)
                {
                    animator.SetBool(jumpParameter, true);
                    animator.SetBool(groundedParameter, false);
                }
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
            if (animator != null)
            {
                animator.SetBool(groundedParameter, false);
                if (verticalVelocity < 0f)
                    animator.SetBool(freeFallParameter, true);
            }
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

        if (animator != null)
            animator.SetFloat(speedParameter, CurrentSpeed, 0.1f, Time.deltaTime);
    }

    private void OnLand() { }
}
