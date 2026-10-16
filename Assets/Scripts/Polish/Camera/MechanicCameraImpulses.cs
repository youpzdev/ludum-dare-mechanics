using System;
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
            throw new InvalidOperationException($"MechanicCameraImpulses '{name}' requires presentation and cameraShake: presentation={presentation}, cameraShake={cameraShake}.");
        }
        if (!ValidStrength(bridgeCompleted) || !ValidStrength(bridgeDestroyed) ||
            !ValidStrength(hookAttached) || !ValidStrength(hookStrongPull) ||
            !ValidStrength(batteryDocked) || !ValidStrength(doorOpened))
        {
            throw new InvalidOperationException($"MechanicCameraImpulses '{name}' strengths must be finite in [0,1]: bridgeCompleted={bridgeCompleted}, bridgeDestroyed={bridgeDestroyed}, hookAttached={hookAttached}, hookStrongPull={hookStrongPull}, batteryDocked={batteryDocked}, doorOpened={doorOpened}.");
        }
        subscribedPresentation = presentation;
        subscribedPresentation.Cue += OnCue;
    }

    private void OnCue(MechanicCue cue, Vector3 point)
    {
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
