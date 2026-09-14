using UnityEngine;

/// <summary>
/// Small persistent singleton that carries player choices (e.g. selected hero)
/// across scene loads, from Hero Select into the match scene.
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public HeroDefinitionSO SelectedHero { get; set; }

    /// <summary>
    /// True when the selected hero's entry voice line already played before the match scene loaded
    /// (during the story intro), so the match scene shouldn't play it again. Cleared when a hero is
    /// confirmed and consumed by <see cref="HeroEntryAnnouncer"/>.
    /// </summary>
    public bool EntryLinePlayed { get; set; }

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
