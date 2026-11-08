using UnityEngine;

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

    void Start()
    {
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
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
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            distance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        Vector3 desired = target.position + Vector3.up * heightOffset;
        smoothedTarget = Vector3.Lerp(smoothedTarget, desired, followSmooth * Time.deltaTime);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = smoothedTarget - rot * Vector3.forward * distance;
        transform.rotation = rot;
    }
}