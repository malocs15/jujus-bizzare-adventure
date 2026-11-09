using System.Collections;
using UnityEngine;

public class StandController : MonoBehaviour
{
    [Header("Activation & Touche")]
    public KeyCode toggleKey = KeyCode.E;
    public bool isStandActive = false; // Désactivé par défaut, s'active avec E
    public AudioClip zaWarudoSound;

    [Header("Positionnement devant le joueur (JJBA Anime style)")]
    public float frontOffset = 1.35f;      // Distance devant Jotaro
    public float hoverOffset = 0.25f;      // Légère lévitation au-dessus du sol
    public float followSpeed = 14f;        // Vitesse de suivi des pas
    public float rotationSpeed = 16f;      // Vitesse d'alignement du regard
    public float hoverAmplitude = 0.08f;   // Amplitude de l'ondulation de vol
    public float hoverFrequency = 2.4f;    // Fréquence de flottement

    [Header("Aura du Stand")]
    public Color standAuraColor = new Color(0.75f, 0.25f, 1f, 1f); // Violet signature Star Platinum

    private GameObject standInstance;
    private AudioSource audioSource;
    private Light auraLight;
    private Coroutine transitionCoroutine;

    void Start()
    {
        // 1. Initialiser l'AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // Charger le son Za Warudo
        if (zaWarudoSound == null)
        {
            zaWarudoSound = Resources.Load<AudioClip>("star-platinum-za-warudo");
        }
#if UNITY_EDITOR
        if (zaWarudoSound == null)
        {
            zaWarudoSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/star-platinum-za-warudo.mp3");
        }
#endif

        // 2. Initialiser ou lier le modèle 3D de Star Platinum
        SetupStandModel();

        // 3. Masquer au démarrage (inactif jusqu'à l'appui sur E)
        if (!isStandActive && standInstance != null)
        {
            standInstance.SetActive(false);
        }
    }

    void Update()
    {
        // Invocation / Retrait du Stand avec la touche E
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleStand();
        }
    }

    void LateUpdate()
    {
        // Quand Star Platinum est actif, il se positionne et flotte devant Jotaro
        if (isStandActive && standInstance != null && standInstance.activeSelf)
        {
            UpdateStandPosition();
        }
    }

    public void ToggleStand()
    {
        isStandActive = !isStandActive;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        if (isStandActive)
        {
            // Son d'invocation mythique
            if (zaWarudoSound != null && audioSource != null)
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

    private void UpdateStandPosition()
    {
        // Ondulation naturelle de lévitation
        float hover = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;

        // Position cible : devant le joueur (JJBA style)
        Vector3 targetPos = transform.position + 
                            transform.forward * frontOffset + 
                            Vector3.up * (hoverOffset + hover);

        // Suivi fluide de la position même en marchant / courant
        standInstance.transform.position = Vector3.Lerp(standInstance.transform.position, targetPos, followSpeed * Time.deltaTime);

        // Alignement avec l'orientation de Jotaro (regarde dans la même direction que lui)
        standInstance.transform.rotation = Quaternion.Slerp(standInstance.transform.rotation, transform.rotation, rotationSpeed * Time.deltaTime);
    }

    private IEnumerator SummonRoutine()
    {
        standInstance.SetActive(true);
        standInstance.transform.localScale = Vector3.zero;

        // Émerge depuis Jotaro vers l'avant
        Vector3 startPos = transform.position + Vector3.up * hoverOffset;
        standInstance.transform.position = startPos;

        if (auraLight != null) auraLight.intensity = 8f;

        float elapsed = 0f;
        float duration = 0.22f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.SmoothStep(0f, 1f, t);
            standInstance.transform.localScale = Vector3.one * scale;

            if (auraLight != null)
            {
                auraLight.intensity = Mathf.Lerp(8f, 2.5f, t);
            }
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
            float scale = Mathf.SmoothStep(1f, 0f, t);
            standInstance.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        standInstance.transform.localScale = Vector3.zero;
        standInstance.SetActive(false);
    }

    private void SetupStandModel()
    {
        // 1. Vérifier si un objet "Star Platinum" existe déjà dans la scène
        GameObject existing = GameObject.Find("Star Platinum");
        if (existing != null)
        {
            standInstance = existing;
        }
        else
        {
            standInstance = new GameObject("Star Platinum");

            // Instancier le modèle 3D
            GameObject modelPrefab = null;
#if UNITY_EDITOR
            modelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/StarPlatinum.obj");
#endif
            if (modelPrefab == null)
            {
                modelPrefab = Resources.Load<GameObject>("StarPlatinum");
            }

            if (modelPrefab != null)
            {
                Instantiate(modelPrefab, standInstance.transform);
            }
        }

        // 2. Charger et appliquer le matériau Star Platinum avec texture
        Material spMat = null;
#if UNITY_EDITOR
        spMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/StarPlatinum.mat");
#endif
        if (spMat == null)
        {
            spMat = Resources.Load<Material>("StarPlatinum");
        }

        if (spMat == null)
        {
            spMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Texture2D tex = null;
#if UNITY_EDITOR
            tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/StarPlatinum.png");
#endif
            if (tex != null) spMat.mainTexture = tex;
            if (spMat.HasProperty("_Cull")) spMat.SetFloat("_Cull", 1f); // Render Face: Back pour faces non inversées
        }

        // Forcer l'application du matériau texturé sur tous les sous-meshes
        foreach (var rend in standInstance.GetComponentsInChildren<Renderer>())
        {
            rend.material = spMat;
        }

        // Supprimer les colliders pour ne pas bloquer les déplacements de Jotaro
        foreach (var col in standInstance.GetComponentsInChildren<Collider>())
        {
            Destroy(col);
        }

        // 3. Aura violette
        GameObject auraObj = new GameObject("StandAura");
        auraObj.transform.SetParent(standInstance.transform, false);
        auraObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        auraLight = auraObj.AddComponent<Light>();
        auraLight.type = LightType.Point;
        auraLight.color = standAuraColor;
        auraLight.range = 3.5f;
        auraLight.intensity = 2.5f;
    }
}
