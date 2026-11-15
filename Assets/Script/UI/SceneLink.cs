using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SceneLink : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] string sceneName;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Go);
    }

    void Go()
    {
        GameManager.Instance.LoadScene(sceneName);
    }
}