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

    [Header("Lock-on")]
    public CameraLockOn lockOn;              // sur la même caméra, trouvé tout seul si vide
    public float lockTurnSpeed = 8f;         // vitesse à laquelle la caméra se tourne vers la cible
    public float lockPitchOffset = 12f;      // regarde un peu d'au-dessus pour voir le joueur et la cible
    public float lockMinPitch = -35f;        // vers le haut : pour suivre une cible en vol
    public float minHeightAbovePlayer = 0.3f; // la caméra ne passe jamais sous les pieds du joueur
    public float lockBlendSpeed = 4f;        // fondu entre caméra libre et caméra verrouillée (1/s)

    private float yaw;
    private float pitch = 20f;
    private Vector3 smoothedTarget;
    private float lockBlend;                 // 0 = caméra libre, 1 = caméra verrouillée
    private Vector3 lastAimPoint;            // gardé après le déverrouillage, le temps du fondu
    private InputAction lookAction;

    private const float LegacyMouseScale = 0.1f;

    void Start()
    {
        lookAction = InputSystem.actions.FindAction("Player/Look", true);
        lookAction.Enable();

        if (lockOn == null) lockOn = GetComponent<CameraLockOn>();

        // Sans cible réglée dans la scène, on suit l'objet qui porte PlayerMovement
        // (le tag « Player » est aussi sur la racine du prefab, qui, elle, ne bouge pas)
        if (target == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null) target = player.transform;
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

        LockOnTarget locked = lockOn != null ? lockOn.Current : null;

        if (locked != null)
        {
            // Lock-on : la souris ne tourne plus la caméra, elle se place derrière le joueur face à la cible
            lastAimPoint = locked.AimPoint;
            Vector3 toTarget = lastAimPoint - smoothedTarget;
            float flatDistance = new Vector2(toTarget.x, toTarget.z).magnitude;
            float targetPitch = -Mathf.Atan2(toTarget.y, flatDistance) * Mathf.Rad2Deg + lockPitchOffset;
            targetPitch = Mathf.Clamp(targetPitch, lockMinPitch, maxPitch);

            // Cible presque à la verticale du joueur : la direction horizontale n'a plus de sens
            if (flatDistance > 0.5f)
            {
                float targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                yaw = Mathf.LerpAngle(yaw, targetYaw, lockTurnSpeed * Time.deltaTime);
            }
            pitch = Mathf.Lerp(pitch, targetPitch, lockTurnSpeed * Time.deltaTime);
        }
        else if (Cursor.lockState == CursorLockMode.Locked)
        {
            // Delta souris en pixels ; x0.1 pour garder la même sensibilité que l'ancien axe "Mouse X/Y"
            Vector2 look = lookAction.ReadValue<Vector2>() * LegacyMouseScale;
            yaw += look.x * mouseSensitivity;

            // Juste après un lock-on vers le haut, le pitch revient en douceur dans ses bornes
            if (pitch < minPitch - 0.5f)
                pitch = Mathf.Lerp(pitch, minPitch, lockTurnSpeed * Time.deltaTime);
            else
                pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, minPitch, maxPitch);
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // Molette : la valeur brute varie selon l'OS, on ne garde que le sens (1 cran = 0.1 comme avant)
            float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            if (scroll != 0f)
                distance -= Mathf.Sign(scroll) * LegacyMouseScale * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        Vector3 desired = target.position + Vector3.up * heightOffset;
        smoothedTarget = Vector3.Lerp(smoothedTarget, desired, followSmooth * Time.deltaTime);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 position = smoothedTarget - rot * Vector3.forward * distance;

        // Quand on regarde une cible haute, la caméra descendrait sous le sol : on la garde au-dessus
        float minY = target.position.y + minHeightAbovePlayer;
        if (position.y < minY) position.y = minY;
        transform.position = position;

        // Verrouillé : on vise entre le joueur et la cible pour garder les deux à l'écran.
        // lockBlend fait un fondu entre les deux visées pour éviter un saut au (dé)verrouillage.
        lockBlend = Mathf.MoveTowards(lockBlend, locked != null ? 1f : 0f, lockBlendSpeed * Time.deltaTime);
        if (lockBlend > 0f)
        {
            Quaternion lookAtBoth = Quaternion.LookRotation(
                Vector3.Lerp(smoothedTarget, lastAimPoint, 0.5f) - position);
            transform.rotation = Quaternion.Slerp(rot, lookAtBoth, lockBlend);
        }
        else
        {
            transform.rotation = rot;
        }
    }
}