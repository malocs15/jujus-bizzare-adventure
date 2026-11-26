using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class StandController : MonoBehaviour
{
    [Header("Références (à assigner dans l'inspecteur)")]
    [SerializeField] private GameObject standPrefab;   // Assets/Prefabs/StarPlatinum.prefab
    [SerializeField] private AudioClip zaWarudoSound;  // Assets/Audio/star-platinum-za-warudo.mp3

    [Header("Activation")]
    public bool isStandActive = false; // Désactivé par défaut, s'active avec l'action "Stand" (E)

    [Header("Positionnement devant le joueur (JJBA Anime style)")]
    public float frontOffset = 1.35f;      // Distance devant Jotaro
    public float hoverOffset = 0.25f;      // Légère lévitation au-dessus du sol
    public float followSpeed = 14f;        // Vitesse de suivi des pas
    public float rotationSpeed = 16f;      // Vitesse d'alignement du regard
    public float hoverAmplitude = 0.08f;   // Amplitude de l'ondulation de vol
    public float hoverFrequency = 2.4f;    // Fréquence de flottement

    private GameObject standInstance;
    private AudioSource audioSource;
    private Light auraLight;
    private Coroutine transitionCoroutine;
    private PlayerMovement playerMovement;
    private InputAction standAction;

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

        if (isStandActive)
            standInstance.transform.SetPositionAndRotation(GetTargetPosition(), GetTargetRotation());
        standInstance.SetActive(isStandActive);
    }

    void OnDestroy()
    {
        if (standInstance != null) Destroy(standInstance);
    }

    void Update()
    {
        if (standAction != null && standAction.WasPressedThisFrame())
            ToggleStand();
    }

    void LateUpdate()
    {
        // Quand Star Platinum est actif, il se positionne et flotte devant Jotaro
        if (isStandActive && standInstance != null && standInstance.activeSelf)
            UpdateStandPosition();
    }

    public void ToggleStand()
    {
        if (standInstance == null) return;

        isStandActive = !isStandActive;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        if (isStandActive)
        {
            if (zaWarudoSound != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(zaWarudoSound, 1f);
            }
            transitionCoroutine = StartCoroutine(SummonRoutine());
        }
        else
        {
            transitionCoroutine = StartCoroutine(DismissRoutine());
        }
    }

    private Vector3 GetTargetPosition()
    {
        // Ondulation naturelle de lévitation
        float hover = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;

        // Le Stand se projette légèrement plus en avant lors de la course
        float speedBonus = playerMovement != null ? playerMovement.CurrentSpeed * 0.08f : 0f;

        return transform.position +
               transform.forward * (frontOffset + speedBonus) +
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

    private IEnumerator SummonRoutine()
    {
        standInstance.SetActive(true);
        standInstance.transform.localScale = Vector3.zero;

        // Émerge depuis Jotaro vers l'avant
        standInstance.transform.position = transform.position + Vector3.up * hoverOffset;

        if (auraLight != null) auraLight.intensity = 8f;

        float elapsed = 0f;
        float duration = 0.22f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            standInstance.transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t);

            if (auraLight != null)
                auraLight.intensity = Mathf.Lerp(8f, 2.5f, t);
            yield return null;
        }

        standInstance.transform.localScale = Vector3.one;
        if (auraLight != null) auraLight.intensity = 2.5f;
    }

    private IEnumerator DismissRoutine()
    {
        float elapsed = 0f;
        float duration = 0.18f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            standInstance.transform.localScale = Vector3.one * Mathf.SmoothStep(1f, 0f, t);
            yield return null;
        }

        standInstance.transform.localScale = Vector3.zero;
        standInstance.SetActive(false);
    }
}
