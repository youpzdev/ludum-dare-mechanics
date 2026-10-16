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
        if (!Initialize())
        {
            enabled = false;
            return;
        }
        presentation.Cue += OnCue;
        subscribed = true;
    }

    private bool Initialize()
    {
        if (initialized) return true;
        if (presentation == null || successBurst == null || errorBurst == null ||
            eventLight == null || successBurst == errorBurst)
        {
            Debug.LogError($"Эффекты: presentation={presentation}, success={successBurst}, error={errorBurst}, light={eventLight}", this);
            return false;
        }
        if (!float.IsFinite(flashIntensity) || flashIntensity < 0f ||
            !float.IsFinite(flashSeconds) || flashSeconds <= 0f || successCount < 1 || errorCount < 1)
        {
            Debug.LogError($"Эффекты: intensity={flashIntensity}, seconds={flashSeconds}, count={successCount}/{errorCount}", this);
            return false;
        }
        if (!ValidateParticles(successBurst) || !ValidateParticles(errorBurst) || !ValidateVisual(eventLight.transform)) return false;
        if (eventLight.transform == successBurst.transform || eventLight.transform == errorBurst.transform)
        {
            Debug.LogError($"Эффекты: eventLight={eventLight.name} использует transform частиц", this);
            return false;
        }
        originalIntensity = eventLight.intensity;
        originalEnabled = eventLight.enabled;
        originalLightPosition = eventLight.transform.position;
        originalSuccessPosition = successBurst.transform.position;
        originalErrorPosition = errorBurst.transform.position;
        initialized = true;
        ClearEffects();

        return true;
    }

    private bool ValidateParticles(ParticleSystem particles)
    {
        if (!ValidateVisual(particles.transform)) return false;
        ParticleSystem.MainModule main = particles.main;
        if (main.loop || main.playOnAwake || main.simulationSpace != ParticleSystemSimulationSpace.World ||
            main.stopAction != ParticleSystemStopAction.None || particles.subEmitters.enabled)
        {
            Debug.LogError($"Эффект {particles.name}: loop={main.loop}, awake={main.playOnAwake}, space={main.simulationSpace}, stop={main.stopAction}, subEmitters={particles.subEmitters.enabled}", this);
            return false;
        }

        return true;
    }

    private bool ValidateVisual(Transform target)
    {
        bool hasPhysics = target.GetComponent<Rigidbody2D>() != null || target.GetComponent<Collider2D>() != null ||
            target.GetComponent<Rigidbody>() != null || target.GetComponent<Collider>() != null;
        if (target == transform || target.childCount != 0 || hasPhysics)
        {
            Debug.LogError($"Эффект {target.name}: children={target.childCount}, physics={hasPhysics}; нужен отдельный визуал", this);
            return false;
        }

        return true;
    }

    private void OnCue(MechanicCue cue, Vector3 point)
    {
        if (!isActiveAndEnabled || !initialized) return;
        if (presentation == null || successBurst == null || errorBurst == null || eventLight == null)
        {
            Debug.LogError($"Эффекты: потеряны presentation={presentation}, success={successBurst}, error={errorBurst}, light={eventLight}", this);
            enabled = false;
            return;
        }
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
        if (eventLight == null)
        {
            Debug.LogError("Эффекты: потерян eventLight", this);
            enabled = false;
            return;
        }
        flashRemaining = Mathf.Max(0f, flashRemaining - Time.unscaledDeltaTime);
        float progress = flashRemaining / flashSeconds;
        eventLight.intensity = originalIntensity + flashIntensity * progress * progress;
        if (flashRemaining <= 0f) RestoreLight();
    }

    private void RestoreLight()
    {
        if (eventLight == null) return;
        eventLight.intensity = originalIntensity;
        eventLight.enabled = originalEnabled;
        eventLight.transform.position = originalLightPosition;
    }

    private void ClearEffects(bool preserveDoorFeedback = false)
    {
        if (!initialized)
        {
            if (successBurst != null) successBurst.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (errorBurst != null) errorBurst.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (eventLight != null) eventLight.enabled = false;
            return;
        }
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
