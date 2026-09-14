using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives the Hero Select screen: hook each hero button's OnClick to SelectHero(index).
/// Plays the chosen hero's entry voice line, waits for it to finish, then loads the match scene.
/// </summary>
public class HeroSelectManager : MonoBehaviour
{
    [SerializeField] private HeroDefinitionSO[] availableHeroes;
    [SerializeField] private AudioSource entryAudioSource;
    [SerializeField] private string nextSceneName = "SampleScene";

    /// <summary>
    /// Hook this up to a hero button's OnClick event, passing that hero's index
    /// in availableHeroes.
    /// </summary>
    public void SelectHero(int index)
    {
        if (availableHeroes == null || index < 0 || index >= availableHeroes.Length)
        {
            Debug.LogError($"HeroSelectManager: invalid hero index {index}.");
            return;
        }

        HeroDefinitionSO hero = availableHeroes[index];
        if (hero == null)
        {
            Debug.LogError($"HeroSelectManager: availableHeroes[{index}] is not assigned.");
            return;
        }

        if (GameSession.Instance != null)
        {
            GameSession.Instance.SelectedHero = hero;
        }
        else
        {
            Debug.LogWarning("HeroSelectManager: no GameSession in the scene; selected hero will not persist.");
        }

        StartCoroutine(PlayEntryLineThenLoadScene(hero));
    }

    private IEnumerator PlayEntryLineThenLoadScene(HeroDefinitionSO hero)
    {
        float waitTime = 0f;

        if (hero.entryClip != null && entryAudioSource != null)
        {
            entryAudioSource.PlayOneShot(hero.entryClip);
            waitTime = hero.entryClip.length;
        }

        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
