using UnityEngine;

/// <summary>
/// Small persistent singleton that carries player choices (e.g. selected hero)
/// across scene loads, from Hero Select into the match scene.
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public HeroDefinitionSO SelectedHero { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
