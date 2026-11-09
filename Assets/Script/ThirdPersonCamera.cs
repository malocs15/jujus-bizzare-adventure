using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;                 
    public float distance = 6f;              
    public float heightOffset = 1.5f;        
    public float mouseSensitivity = 3f;
    public float minPitch = -10f;            
    public float maxPitch = 60f;             
    public float minDistance = 2f;
    public float maxDistance = 12f;
    public float zoomSpeed = 4f;
    public float followSmooth = 12f;         

    private float yaw;
    private float pitch = 20f;
    private Vector3 smoothedTarget;
    private InputAction lookAction;

    private const float LegacyMouseScale = 0.1f;

    void Start()
    {
        lookAction = InputSystem.actions.FindAction("Player/Look", true);
        lookAction.Enable();

        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        if (target != null)
        {
            smoothedTarget = target.position;
            yaw = target.eulerAngles.y;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
            && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // Delta souris en pixels ; x0.1 pour garder la même sensibilité que l'ancien axe "Mouse X/Y"
            Vector2 look = lookAction.ReadValue<Vector2>() * LegacyMouseScale;
            yaw += look.x * mouseSensitivity;
            pitch -= look.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // Molette : la valeur brute varie selon l'OS, on ne garde que le sens (1 cran = 0.1 comme avant)
            float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            if (scroll != 0f)
                distance -= Mathf.Sign(scroll) * LegacyMouseScale * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        Vector3 desired = target.position + Vector3.up * heightOffset;
        smoothedTarget = Vector3.Lerp(smoothedTarget, desired, followSmooth * Time.deltaTime);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = smoothedTarget - rot * Vector3.forward * distance;
        transform.rotation = rot;
    }
}