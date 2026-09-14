using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows story lines one at a time on a black screen (e.g. after confirming a hero).
/// Lives on a world-space canvas parented to the (XR) camera ~2 m in front of the eyes, with a sorting
/// order above the ScreenFader so the text draws on top of the black fade.
/// Each line fades in, holds (longer for longer lines) and fades out. Either controller trigger or
/// Space/Enter skips to the next line; a short cooldown keeps one press from skipping several lines,
/// and only fresh presses count, so the trigger press that clicked CONFIRMAR can't skip line 1.
/// Usage from another coroutine: <c>yield return introPlayer.Play(lines);</c>
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasGroup))]
public class IntroSequencePlayer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Text that shows the current story line. Faded through its own CanvasGroup.")]
    [SerializeField] private TMP_Text lineText;

    [Header("Timing (seconds, unscaled)")]
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private float fadeOutDuration = 0.6f;
    [Tooltip("Fade-out used when the player skips a line.")]
    [SerializeField] private float skipFadeOutDuration = 0.15f;
    [SerializeField] private float baseHold = 2.5f;
    [Tooltip("Extra hold per character, so longer lines stay up longer.")]
    [SerializeField] private float holdPerCharacter = 0.04f;
    [Tooltip("Extra hold for the final line.")]
    [SerializeField] private float lastLineExtraHold = 1f;
    [Tooltip("Short black pause before each line.")]
    [SerializeField] private float gapBetweenLines = 0.25f;

    [Header("Input")]
    [Tooltip("e.g. XRI Left Interaction/Activate (trigger).")]
    [SerializeField] private InputActionReference leftTriggerAction;
    [Tooltip("e.g. XRI Right Interaction/Activate (trigger).")]
    [SerializeField] private InputActionReference rightTriggerAction;
    [Tooltip("Ignore input for this long after the sequence starts.")]
    [SerializeField] private float initialInputDelay = 0.4f;
    [Tooltip("Ignore input for this long after each skip (one press = one line).")]
    [SerializeField] private float inputCooldown = 0.3f;

    [Header("Look")]
    [Tooltip("Phrases highlighted in the accent color wherever they appear (TMP rich text).")]
    [SerializeField] private string[] accentPhrases = { "The Hidden King" };
    [SerializeField] private Color accentColor = new Color(1f, 0.68f, 0.25f, 1f);
    [Tooltip("While the intro runs, the parent camera only renders these layers (the screen is black anyway). " +
             "Keeps nearby opaque objects such as controller models from cutting into the text.")]
    [SerializeField] private bool isolateCamera = true;
    [SerializeField] private LayerMask introCullingMask = 1 << 5; // UI layer

    private Canvas canvas;
    private CanvasGroup group;
    private CanvasGroup lineGroup;
    private float inputBlockedUntil;
    private bool skipRequested;

    public bool IsPlaying { get; private set; }

    /// <summary>Index of the line currently shown (-1 when idle). Useful for tests/analytics.</summary>
    public int CurrentLineIndex { get; private set; } = -1;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        group = GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        if (lineText != null)
        {
            lineGroup = lineText.GetComponent<CanvasGroup>();
            if (lineGroup == null) lineGroup = lineText.gameObject.AddComponent<CanvasGroup>();
            lineGroup.interactable = false;
            lineGroup.blocksRaycasts = false;
        }

        HideImmediate();
    }

    private void OnEnable()
    {
        EnableAction(leftTriggerAction);
        EnableAction(rightTriggerAction);
    }

    private static void EnableAction(InputActionReference reference)
    {
        if (reference != null && reference.action != null) reference.action.Enable();
    }

    private void HideImmediate()
    {
        group.alpha = 0f;
        canvas.enabled = false;
        if (lineGroup != null) lineGroup.alpha = 0f;
    }

    /// <summary>True if the array has at least one non-empty line.</summary>
    public static bool HasLines(string[] lines) => LastNonEmptyIndex(lines) >= 0;

    /// <summary>
    /// Advances to the next line (same as pressing a trigger). Respects the input cooldown.
    /// Returns true if the request was accepted.
    /// </summary>
    public bool Skip()
    {
        if (!IsPlaying || skipRequested || Time.unscaledTime < inputBlockedUntil) return false;
        skipRequested = true;
        inputBlockedUntil = Time.unscaledTime + inputCooldown;
        return true;
    }

    /// <summary>Plays the lines one after another. Yield on it from a coroutine.</summary>
    public IEnumerator Play(string[] lines)
    {
        int lastIndex = LastNonEmptyIndex(lines);
        if (lastIndex < 0 || lineText == null) yield break;

        Camera cam = isolateCamera ? GetComponentInParent<Camera>() : null;
        int previousMask = cam != null ? cam.cullingMask : 0;

        IsPlaying = true;
        skipRequested = false;
        inputBlockedUntil = Time.unscaledTime + initialInputDelay;
        lineGroup.alpha = 0f;
        group.alpha = 0f;
        canvas.enabled = true;
        if (cam != null) cam.cullingMask = introCullingMask;

        try
        {
            // Hint fades in gently with the overlay.
            yield return FadeGroup(group, 1f, 0.3f);

            for (int i = 0; i <= lastIndex; i++)
            {
                string line = lines[i] != null ? lines[i].Trim() : string.Empty;
                if (line.Length == 0) continue;

                CurrentLineIndex = i;
                lineText.text = ApplyAccents(line);
                lineGroup.alpha = 0f;
                skipRequested = false;

                yield return Wait(gapBetweenLines);

                float hold = baseHold + holdPerCharacter * line.Length + (i == lastIndex ? lastLineExtraHold : 0f);

                // Fade in -> hold. A skip request ends either phase immediately.
                float t = 0f;
                while (!skipRequested && t < fadeInDuration)
                {
                    t += Time.unscaledDeltaTime;
                    lineGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeInDuration));
                    yield return null;
                    PollInput();
                }

                t = 0f;
                while (!skipRequested && t < hold)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                    PollInput();
                }

                // Fade out; switches to the quick rate if the player skips mid-fade.
                while (lineGroup.alpha > 0f)
                {
                    float duration = skipRequested ? skipFadeOutDuration : fadeOutDuration;
                    lineGroup.alpha = Mathf.MoveTowards(lineGroup.alpha, 0f,
                        Time.unscaledDeltaTime / Mathf.Max(0.01f, duration));
                    yield return null;
                    PollInput();
                }
            }

            yield return FadeGroup(group, 0f, 0.3f);
        }
        finally
        {
            if (cam != null) cam.cullingMask = previousMask;
            IsPlaying = false;
            skipRequested = false;
            CurrentLineIndex = -1;
            HideImmediate();
        }
    }

    private void PollInput()
    {
        if (skipRequested || Time.unscaledTime < inputBlockedUntil) return;

        bool pressed = WasPressed(leftTriggerAction) || WasPressed(rightTriggerAction);
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.spaceKey.wasPressedThisFrame ||
             keyboard.enterKey.wasPressedThisFrame ||
             keyboard.numpadEnterKey.wasPressedThisFrame))
        {
            pressed = true;
        }

        if (pressed) Skip();
    }

    private static bool WasPressed(InputActionReference reference)
    {
        // WasPressedThisFrame only fires on a fresh press, so a trigger still held from the CONFIRMAR click is ignored.
        return reference != null && reference.action != null && reference.action.WasPressedThisFrame();
    }

    private static IEnumerator FadeGroup(CanvasGroup target, float to, float duration)
    {
        float from = target.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            target.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        target.alpha = to;
    }

    private IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null; // no input here: a press in the gap must not skip a line the player hasn't seen
        }
    }

    private string ApplyAccents(string line)
    {
        if (accentPhrases == null) return line;
        string hex = ColorUtility.ToHtmlStringRGB(accentColor);
        foreach (string phrase in accentPhrases)
        {
            if (string.IsNullOrEmpty(phrase)) continue;
            line = line.Replace(phrase, $"<color=#{hex}>{phrase}</color>");
        }
        return line;
    }

    private static int LastNonEmptyIndex(string[] lines)
    {
        if (lines == null) return -1;
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            if (!string.IsNullOrWhiteSpace(lines[i])) return i;
        }
        return -1;
    }
}
