using System;
using UnityEngine;

public class MechanicEventEffects : MonoBehaviour
{
    [SerializeField] private MechanicPresentation presentation;
    [SerializeField] private ParticleSystem successBurst;
    [SerializeField] private ParticleSystem errorBurst;
    [SerializeField] private Light eventLight;
    [SerializeField] [Min(0f)] private float flashIntensity = 2f;
    [SerializeField] [Min(0.01f)] private float flashSeconds = 0.2f;
    [SerializeField] [Min(1)] private int successCount = 14;
    [SerializeField] [Min(1)] private int errorCount = 8;

    private bool initialized;
    private bool subscribed;

    private float flashRemaining;
    private float originalIntensity;
    private bool originalEnabled;
    private Vector3 originalLightPosition;
    private Vector3 originalSuccessPosition;
    private Vector3 originalErrorPosition;

    private bool doorFeedbackActive;

    private void OnEnable()
    {
        Initialize();
        presentation.Cue += OnCue;
        subscribed = true;
    }

    private void Initialize()
    {
        if (initialized) return;
        if (presentation == null || successBurst == null || errorBurst == null || eventLight == null || successBurst == errorBurst)
        {
            throw new InvalidOperationException($"MechanicEventEffects '{name}' needs presentation, distinct successBurst/errorBurst and eventLight references.");
        }
        if (!float.IsFinite(flashIntensity) || flashIntensity < 0f || !float.IsFinite(flashSeconds) || flashSeconds <= 0f || successCount < 1 || errorCount < 1)
        {
            throw new InvalidOperationException($"MechanicEventEffects '{name}' invalid flashIntensity={flashIntensity}, flashSeconds={flashSeconds}, successCount={successCount}, errorCount={errorCount}.");
        }
        ValidateParticles(successBurst);
        ValidateParticles(errorBurst);
        ValidateVisual(eventLight.transform);
        if (eventLight.transform == successBurst.transform || eventLight.transform == errorBurst.transform)
        {
            throw new InvalidOperationException($"MechanicEventEffects '{name}' eventLight and bursts must use independent transforms.");
        }
        originalIntensity = eventLight.intensity;
        originalEnabled = eventLight.enabled;
        originalLightPosition = eventLight.transform.position;
        originalSuccessPosition = successBurst.transform.position;
        originalErrorPosition = errorBurst.transform.position;
        initialized = true;
        ClearEffects();
    }

    private void ValidateParticles(ParticleSystem particles)
    {
        ValidateVisual(particles.transform);
        ParticleSystem.MainModule main = particles.main;
        if (main.loop || main.playOnAwake || main.simulationSpace != ParticleSystemSimulationSpace.World || main.stopAction != ParticleSystemStopAction.None || particles.subEmitters.enabled)
        {
            throw new InvalidOperationException($"MechanicEventEffects '{name}' particles '{particles.name}' require loop=false, playOnAwake=false, world simulation and no stop action/subemitters: loop={main.loop}, playOnAwake={main.playOnAwake}, simulationSpace={main.simulationSpace}, stopAction={main.stopAction}, subEmitters={particles.subEmitters.enabled}.");
        }
    }

    private void ValidateVisual(Transform target)
    {
        if (target == transform || target.childCount != 0 || target.GetComponent<Rigidbody2D>() != null || target.GetComponent<Collider2D>() != null || target.GetComponent<Rigidbody>() != null || target.GetComponent<Collider>() != null)
        {
            throw new InvalidOperationException($"MechanicEventEffects '{name}' visual '{target.name}' must be a dedicated leaf without physics: children={target.childCount}.");
        }
    }

    private void OnCue(MechanicCue cue, Vector3 point)
    {
        if (cue == MechanicCue.Cancelled || cue == MechanicCue.HookReleased)
        {
            ClearEffects(cue == MechanicCue.Cancelled && presentation.PreserveDoorFeedback && doorFeedbackActive);
            return;
        }
        bool rejected = cue == MechanicCue.InkRejected || cue == MechanicCue.HookRejected || cue == MechanicCue.BatteryRejected;
        ParticleSystem particles = rejected ? errorBurst : successBurst;
        if (cue == MechanicCue.HookStrongPull) return;
        doorFeedbackActive = cue == MechanicCue.DoorOpened;
        if (doorFeedbackActive) successBurst.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.transform.position = point;
        particles.Emit(rejected ? errorCount : successCount);
        Vector3 lightPosition = point;
        lightPosition.z = originalLightPosition.z;
        eventLight.transform.position = lightPosition;
        eventLight.enabled = true;
        eventLight.intensity = originalIntensity + flashIntensity;
        flashRemaining = flashSeconds;
    }

    private void Update()
    {
        if (flashRemaining <= 0f) return;
        flashRemaining = Mathf.Max(0f, flashRemaining - Time.unscaledDeltaTime);
        float progress = flashRemaining / flashSeconds;
        eventLight.intensity = originalIntensity + flashIntensity * progress * progress;
        if (flashRemaining <= 0f) RestoreLight();
    }

    private void RestoreLight()
    {
        eventLight.intensity = originalIntensity;
        eventLight.enabled = originalEnabled;
        eventLight.transform.position = originalLightPosition;
    }

    private void ClearEffects(bool preserveDoorFeedback = false)
    {
        if (!initialized) return;
        if (!preserveDoorFeedback)
        {
            doorFeedbackActive = false;
            flashRemaining = 0f;
        }
        if (!preserveDoorFeedback && successBurst != null)
        {
            successBurst.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            successBurst.transform.position = originalSuccessPosition;
        }
        if (errorBurst != null)
        {
            errorBurst.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            errorBurst.transform.position = originalErrorPosition;
        }
        if (!preserveDoorFeedback && eventLight != null) RestoreLight();
    }

    private void OnDisable()
    {
        if (subscribed && presentation != null) presentation.Cue -= OnCue;
        subscribed = false;
        ClearEffects();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) ClearEffects();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) ClearEffects();
    }
}
