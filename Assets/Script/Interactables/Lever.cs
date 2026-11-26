using UnityEngine;
using UnityEngine.Events;

// Levier actionné à distance par le grab du Stand (utilisé dans les deux niveaux).
// Chaque grab le bascule ; les portes, ponts… s'abonnent à onToggled dans l'inspecteur.
public class Lever : MonoBehaviour, IGrabbable
{
    [Header("Poignée")]
    public Transform handle;            // pivot de la poignée, tourne autour de son axe X local
    public float offAngle = -40f;
    public float onAngle = 40f;
    public float rotateSpeed = 360f;    // degrés par seconde

    [Header("État")]
    public bool isOn = false;
    public bool singleUse = false;      // une fois activé, il reste bloqué

    [Header("Événements")]
    public UnityEvent<bool> onToggled;  // reçoit le nouvel état (true = activé)

    public bool CanBeCarried => false;

    void Start()
    {
        if (handle != null) handle.localRotation = Quaternion.Euler(TargetAngle, 0f, 0f);
    }

    public void OnGrabbed(Transform holder)
    {
        if (singleUse && isOn) return;
        isOn = !isOn;
        onToggled?.Invoke(isOn);
    }

    public void OnThrown(Vector3 velocity) { } // un levier ne se lance pas

    void Update()
    {
        if (handle == null) return;
        // La poignée fait partie du monde : elle suit WorldTime
        Quaternion target = Quaternion.Euler(TargetAngle, 0f, 0f);
        handle.localRotation = Quaternion.RotateTowards(handle.localRotation, target, rotateSpeed * WorldTime.DeltaTime);
    }

    private float TargetAngle => isOn ? onAngle : offAngle;
}
