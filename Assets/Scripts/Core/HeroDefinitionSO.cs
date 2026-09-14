using UnityEngine;

/// <summary>
/// Defines a selectable hero: display name, optional portrait, and the voice line
/// played when the player picks this hero and enters the match.
/// Create new hero instances via Assets > Create > Deadlock > Hero Definition.
/// </summary>
[CreateAssetMenu(fileName = "NewHero", menuName = "Deadlock/Hero Definition", order = 0)]
public class HeroDefinitionSO : ScriptableObject
{
    [Tooltip("Display name shown on the Hero Select button.")]
    public string heroName;

    [Tooltip("Optional portrait image for the Hero Select UI. Can be left empty until art exists.")]
    public Sprite portrait;

    [Tooltip("Voice line played when the player selects this hero and enters the match.")]
    public AudioClip entryClip;
}
