using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Vitesses & Saut")]
    public float walkSpeed = 2.5f;     
    public float runSpeed = 5.5f;    
    public float acceleration = 10f;  
    public float turnSpeed = 720f;     
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

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Support direct AZERTY (Z/Q/S/D) en complément du QWERTY (W/A/S/D) et des flèches
        if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.UpArrow)) v = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v = -1f;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow)) h = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h = 1f;

        Vector3 input = Vector3.ClampMagnitude(new Vector3(h, 0f, v), 1f);
        bool running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

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

            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
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