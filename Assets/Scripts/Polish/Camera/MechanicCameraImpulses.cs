using UnityEngine;

public class MechanicCameraImpulses : MonoBehaviour
{
    [SerializeField] private MechanicPresentation presentation;
    [SerializeField] private DemoCameraShake cameraShake;
    [SerializeField] [Range(0f, 1f)] private float bridgeCompleted = 0.18f;
    [SerializeField] [Range(0f, 1f)] private float bridgeDestroyed = 0.3f;
    [SerializeField] [Range(0f, 1f)] private float hookAttached = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float hookStrongPull = 0.35f;
    [SerializeField] [Range(0f, 1f)] private float batteryDocked = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float doorOpened = 0.2f;

    private MechanicPresentation subscribedPresentation;

    private void OnEnable()
    {
        if (presentation == null || cameraShake == null)
        {
            Debug.LogError($"Импульсы: presentation={presentation}, cameraShake={cameraShake}", this);
            enabled = false;
            return;
        }
        if (!ValidStrength(bridgeCompleted) || !ValidStrength(bridgeDestroyed) ||
            !ValidStrength(hookAttached) || !ValidStrength(hookStrongPull) ||
            !ValidStrength(batteryDocked) || !ValidStrength(doorOpened))
        {
            Debug.LogError($"Импульсы вне [0,1]: bridge={bridgeCompleted}/{bridgeDestroyed}, hook={hookAttached}/{hookStrongPull}, battery={batteryDocked}, door={doorOpened}", this);
            enabled = false;
            return;
        }
        subscribedPresentation = presentation;
        subscribedPresentation.Cue += OnCue;
    }

    private void OnCue(MechanicCue cue, Vector3 point)
    {
        if (!isActiveAndEnabled) return;
        if (cameraShake == null)
        {
            Debug.LogError("Импульсы: потерян cameraShake", this);
            enabled = false;
            return;
        }
        switch (cue)
        {
            case MechanicCue.Cancelled:
                cameraShake.Clear();
                break;
            case MechanicCue.BridgeCompleted:
                cameraShake.Impulse(bridgeCompleted);
                break;
            case MechanicCue.BridgeDestroyed:
                cameraShake.Impulse(bridgeDestroyed);
                break;
            case MechanicCue.HookAttached:
                cameraShake.Impulse(hookAttached);
                break;
            case MechanicCue.HookStrongPull:
                cameraShake.Impulse(hookStrongPull);
                break;
            case MechanicCue.BatteryDocked:
                cameraShake.Impulse(batteryDocked);
                break;
            case MechanicCue.DoorOpened:
                cameraShake.Impulse(doorOpened);
                break;
        }
    }

    private static bool ValidStrength(float value) { return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f; }

    private void OnDisable()
    {
        if (subscribedPresentation != null)
        {
            subscribedPresentation.Cue -= OnCue;
            subscribedPresentation = null;
        }
        if (cameraShake != null) cameraShake.Clear();
    }
}
