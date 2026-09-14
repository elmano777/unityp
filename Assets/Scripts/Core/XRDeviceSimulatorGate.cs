using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

/// <summary>
/// Activates the XR Device Simulator only when no real headset is running, so desktop testing without a
/// headset still works, while a Quest over Link (or a Quest build) is never overridden by simulated devices.
/// (The simulator removes every other HMD input device while active, which would freeze real head tracking.)
/// Keep the simulator GameObject INACTIVE in the scene and reference it here.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class XRDeviceSimulatorGate : MonoBehaviour
{
    [Tooltip("The (inactive) XR Device Simulator object in this scene.")]
    [SerializeField] private GameObject simulator;
    [Tooltip("Only ever enable the simulator inside the Unity Editor (never in player builds).")]
    [SerializeField] private bool editorOnly = true;

    private void Awake()
    {
        if (simulator == null) return;

        bool allowed = Application.isEditor || !editorOnly;
        bool headsetRunning = IsHeadsetRunning();
        simulator.SetActive(allowed && !headsetRunning);

        Debug.Log(headsetRunning
            ? "XRDeviceSimulatorGate: XR headset running; XR Device Simulator stays disabled."
            : allowed
                ? "XRDeviceSimulatorGate: no XR headset running; enabling XR Device Simulator."
                : "XRDeviceSimulatorGate: no XR headset running; simulator not allowed in this build.");
    }

    /// <summary>True if an XR loader initialized and its display subsystem is running.</summary>
    public static bool IsHeadsetRunning()
    {
        XRGeneralSettings settings = XRGeneralSettings.Instance;
        if (settings == null || settings.Manager == null || settings.Manager.activeLoader == null) return false;

        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        foreach (XRDisplaySubsystem display in displays)
        {
            if (display.running) return true;
        }
        return false;
    }
}
