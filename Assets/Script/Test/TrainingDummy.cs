using UnityEngine;

// Mannequin d'entraînement pour Sandbox : clignote quand il est touché,
// bascule en arrière à sa mort puis disparaît. Sert à tester les attaques.
[RequireComponent(typeof(Health))]
public class TrainingDummy : MonoBehaviour
{
    [Header("Retour visuel")]
    public Renderer body;
    public Color hitColor = new Color(1f, 0.35f, 0.3f);
    public float flashDuration = 0.08f;
    public float fallDuration = 0.5f;
    public float destroyDelay = 2f;

    private Health health;
    private MaterialPropertyBlock block;
    private Color baseColor = Color.white;
    private float flashTimer;
    private float fallTimer = -1f;
    private float destroyTimer = -1f;
    private Quaternion uprightRotation;
    private Quaternion fallenRotation;

    void Awake()
    {
        health = GetComponent<Health>();
        if (body == null) body = GetComponentInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        if (body != null && body.sharedMaterial != null && body.sharedMaterial.HasProperty("_BaseColor"))
            baseColor = body.sharedMaterial.GetColor("_BaseColor");

        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    void OnDestroy()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    private void OnDamaged(DamageInfo info)
    {
        flashTimer = flashDuration;
        SetColor(hitColor);
    }

    private void OnDied()
    {
        Debug.Log(name + " : mort", this);
        uprightRotation = transform.rotation;
        fallenRotation = Quaternion.AngleAxis(-90f, transform.right) * uprightRotation;
        fallTimer = 0f;
        destroyTimer = destroyDelay; // compté en WorldTime : figé pendant l'arrêt du temps
    }

    void Update()
    {
        // Le mannequin fait partie du monde : il obéit à WorldTime (arrêt du temps)
        float dt = WorldTime.DeltaTime;

        if (flashTimer > 0f)
        {
            flashTimer -= dt;
            if (flashTimer <= 0f) SetColor(baseColor);
        }

        if (fallTimer >= 0f && fallTimer < fallDuration)
        {
            fallTimer += dt;
            float t = Mathf.SmoothStep(0f, 1f, fallTimer / fallDuration);
            transform.rotation = Quaternion.Slerp(uprightRotation, fallenRotation, t);
        }

        if (destroyTimer > 0f)
        {
            destroyTimer -= dt;
            if (destroyTimer <= 0f) Destroy(gameObject);
        }
    }

    private void SetColor(Color c)
    {
        if (body == null) return;
        body.GetPropertyBlock(block);
        block.SetColor("_BaseColor", c);
        body.SetPropertyBlock(block);
    }
}
