using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int MaxLevel = 5;

    [Header("Player state (visible for debugging)")]
    [SerializeField] int playerLevel = 1;
    [SerializeField] string lastCheckpointId = "";
    SceneFlow sceneFlow;

    public int PlayerLevel => playerLevel;
    public string LastCheckpointId => lastCheckpointId;

    public event Action<int> LevelUp;
    public event Action PlayerDied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("GameManager");
        Instance = go.AddComponent<GameManager>();
        DontDestroyOnLoad(go);
        Instance.sceneFlow = go.AddComponent<SceneFlow>();
        SceneManager.sceneLoaded += Instance.OnFirstSceneLoaded;
    }

    void OnFirstSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnFirstSceneLoaded;
        playerLevel = DefaultLevelFor(scene.name);
    }

    static int DefaultLevelFor(string sceneName)
    {
        switch (sceneName)
        {
            case "Level1_Desert": return 2;
            case "Level2_Mansion": return 4;
            default: return 1;
        }
    }

    [ContextMenu("Grant level")]
    public void GrantLevel()
    {
        if (playerLevel >= MaxLevel) return;
        playerLevel++;
        LevelUp?.Invoke(playerLevel);
    }

    public void SetCheckpoint(string id)
    {
        lastCheckpointId = id;
    }

    public void NotifyPlayerDied()
    {
        PlayerDied?.Invoke();
    }

    public void LoadScene(string sceneName)
    {
        sceneFlow.Load(sceneName);
    }

    [ContextMenu("Reload current scene")]
    void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }
}