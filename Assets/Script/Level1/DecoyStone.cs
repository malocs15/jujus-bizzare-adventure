using UnityEngine;

// Pierre-leurre : une pierre lancée au grab fait du bruit à chaque impact (sol, mur, ennemi) via NoiseSystem.
// Le traqueur sous le sable (Geb) se dirige vers ce bruit : on le détourne pendant que le joueur passe.
// Une pierre simplement lâchée (coup reçu, Stand rappelé) ne fait pas de bruit, pour ne pas attirer Geb sur le joueur.
[RequireComponent(typeof(GrabbableRock))]
public class DecoyStone : MonoBehaviour
{
    [Header("Bruit à l'impact")]
    public float loudSpeed = 10f;   // vitesse d'impact (m/s) qui donne le bruit maximal (1)
    public float minSpeed = 2f;     // en dessous (petit rebond, pierre qui roule), aucun bruit

    private GrabbableRock rock;

    void Awake()
    {
        rock = GetComponent<GrabbableRock>();
    }

    void OnEnable()
    {
        rock.Impacted += OnImpacted;
    }

    void OnDisable()
    {
        rock.Impacted -= OnImpacted;
    }

    // Premier impact d'un lancer (environ 17 m/s) : bruit maximal ; les rebonds suivants sont plus faibles
    private void OnImpacted(Vector3 point, float speed)
    {
        if (!rock.WasThrown || speed < minSpeed) return;
        NoiseSystem.Emit(point, Mathf.Clamp01(speed / loudSpeed));
    }
}
