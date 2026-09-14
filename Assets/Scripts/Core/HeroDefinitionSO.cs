using UnityEngine;

/// <summary>
/// Defines a selectable hero: display name, portrait card art, 3D preview model, and the voice line
/// played when the player picks this hero.
/// Create new hero instances via Assets > Create > Deadlock > Hero Definition.
/// </summary>
[CreateAssetMenu(fileName = "NewHero", menuName = "Deadlock/Hero Definition", order = 0)]
public class HeroDefinitionSO : ScriptableObject
{
    [Tooltip("Display name shown on the Hero Select card.")]
    public string heroName;

    [Tooltip("Portrait art shown on the Hero Select card.")]
    public Sprite portrait;

    [Tooltip("Voice line played when the player selects this hero.")]
    public AudioClip entryClip;

    [Tooltip("3D model shown on the hero-select pedestal.")]
    public GameObject previewModel;
}
