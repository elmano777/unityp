using System.Collections;
using UnityEngine;

/// <summary>
/// Match scene: plays the selected hero's entry voice line once, shortly after the scene starts
/// (the delay keeps it clear of scene-load hitches and lets the screen fade in first).
/// Does nothing if there is no GameSession / selected hero (e.g. pressing Play directly in this scene).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class HeroEntryAnnouncer : MonoBehaviour
{
    [Tooltip("Seconds (unscaled) to wait after the scene starts before playing the entry line.")]
    [SerializeField] private float delay = 0.5f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private IEnumerator Start()
    {
        HeroDefinitionSO hero = GameSession.Instance != null ? GameSession.Instance.SelectedHero : null;
        if (hero == null || hero.entryClip == null)
        {
            yield break;
        }

        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        audioSource.clip = hero.entryClip;
        audioSource.Play();
        Debug.Log($"HeroEntryAnnouncer: playing entry line '{hero.entryClip.name}' for {hero.heroName}.");
    }
}
