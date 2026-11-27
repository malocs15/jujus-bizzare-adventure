using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Touches de triche pour tester et pour la démo (actives seulement après F1) :
// F2 invincibilité, F3 niveau maximum, F4 aller au boss, F5 niveau suivant, F6 soin complet.
// Elle se crée toute seule au lancement et survit aux changements de scène.
public class GodKeys : MonoBehaviour
{
    public bool Active { get; private set; }
    public bool Invincible { get; private set; }

    private InputAction toggleAction, invincibleAction, maxLevelAction, bossAction, nextLevelAction, healAction;
    private string message;
    private float messageTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("GodKeys");
        DontDestroyOnLoad(go);
        go.AddComponent<GodKeys>();
    }

    void Awake()
    {
        // Carte "Debug" de Assets/InputSystem_Actions.inputactions
        toggleAction = Find("Debug/GodKeysToggle");
        invincibleAction = Find("Debug/Invincible");
        maxLevelAction = Find("Debug/MaxLevel");
        bossAction = Find("Debug/GoToBoss");
        nextLevelAction = Find("Debug/NextLevel");
        healAction = Find("Debug/FullHeal");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private static InputAction Find(string path)
    {
        InputAction a = InputSystem.actions.FindAction(path, true);
        a.Enable();
        return a;
    }

    // L'invincibilité suit le joueur d'une scène à l'autre
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayerDamage pd = FindFirstObjectByType<PlayerDamage>();
        if (pd != null) pd.GodMode = Invincible;
    }

    void Update()
    {
        if (toggleAction.WasPressedThisFrame())
        {
            Active = !Active;
            Show(Active ? "God keys ON (F2-F6)" : "God keys OFF");
        }
        // Temps réel : le message doit disparaître même si le jeu est en pause
        if (messageTimer > 0f) messageTimer -= Time.unscaledDeltaTime;
        if (!Active) return;

        if (invincibleAction.WasPressedThisFrame()) ToggleInvincible();
        if (maxLevelAction.WasPressedThisFrame()) MaxLevel();
        if (bossAction.WasPressedThisFrame()) GoToBoss();
        if (nextLevelAction.WasPressedThisFrame()) NextLevel();
        if (healAction.WasPressedThisFrame()) FullHeal();
    }

    private void ToggleInvincible()
    {
        Invincible = !Invincible;
        PlayerDamage pd = FindFirstObjectByType<PlayerDamage>();
        if (pd != null) pd.GodMode = Invincible;
        Show(Invincible ? "Invincible" : "Vulnérable");
    }

    private void MaxLevel()
    {
        if (GameManager.Instance == null)
        {
            Show("Pas de GameManager");
            return;
        }
        while (GameManager.Instance.PlayerLevel < GameManager.MaxLevel)
            GameManager.Instance.GrantLevel();
        Show("Niveau " + GameManager.Instance.PlayerLevel);
    }

    // Chaque niveau pose un BossTeleportPoint devant son boss
    private void GoToBoss()
    {
        BossTeleportPoint point = FindFirstObjectByType<BossTeleportPoint>();
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (point == null || player == null)
        {
            Show("Pas de boss dans cette scène");
            return;
        }
        if (player.State == PlayerState.Dead)
        {
            Show("Le joueur est mort");
            return;
        }
        // Le CharacterController doit être coupé pour qu'on puisse déplacer le joueur
        CharacterController cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
        cc.enabled = true;
        Show("Téléporté au boss");
    }

    // Ordre des niveaux du jeu (les Build Settings contiennent aussi des scènes de test)
    private static readonly string[] levelOrder = { "Intro", "Level1_Desert", "Level2_Mansion", "Ending" };

    private void NextLevel()
    {
        int index = System.Array.IndexOf(levelOrder, SceneManager.GetActiveScene().name);
        if (index < 0 || index + 1 >= levelOrder.Length || GameManager.Instance == null)
        {
            Show("Pas de niveau suivant");
            return;
        }
        GameManager.Instance.LoadScene(levelOrder[index + 1]);
    }

    private void FullHeal()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        Health health = player != null ? player.GetComponent<Health>() : null;
        if (health == null || !health.IsAlive)
        {
            Show("Pas de joueur vivant"); // Heal ne ressuscite pas
            return;
        }
        health.Heal(health.max);
        Show("Soin complet");
    }

    private void Show(string text)
    {
        message = text;
        messageTimer = 2f;
    }

    // Affichage minimal de débogage (pas de vrai HUD pour un outil de test)
    void OnGUI()
    {
        // L'invincibilité reste affichée même god keys éteintes, pour ne jamais l'oublier en démo
        if (Active || Invincible)
            GUI.Label(new Rect(10, 10, 300, 20), (Active ? "GOD KEYS" : "") + (Invincible ? " · INVINCIBLE" : ""));
        if (messageTimer > 0f) GUI.Label(new Rect(10, 30, 400, 20), message);

        // God keys actives : vie et énergie du Stand en chiffres, en attendant le HUD (S1-J5)
        if (Active)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player == null) return;
            Health health = player.GetComponent<Health>();
            StandCombat combat = player.GetComponent<StandCombat>();
            string stats = "";
            if (health != null) stats += "PV " + Mathf.CeilToInt(health.Current) + " / " + Mathf.CeilToInt(health.max);
            if (combat != null) stats += "   Énergie " + Mathf.RoundToInt(combat.Energy);
            GUI.Label(new Rect(10, 50, 400, 20), stats);
        }
    }
}
