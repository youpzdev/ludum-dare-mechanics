using UnityEngine;

public class MechanicAudioFeedback : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private MechanicPresentation presentation;
    [SerializeField] private DemoPlayerVisuals playerPresentation;

    [Header("Scene sources")]
    [SerializeField] private AudioSource movementSource;
    [SerializeField] private AudioSource mechanicSource;

    [Header("Movement")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landingClip;
    [SerializeField] [Range(0f, 1f)] private float movementVolume = 0.45f;
    [SerializeField] [Range(0f, 1f)] private float minimumLandingStrength = 0.3f;
    [SerializeField] [Min(0f)] private float landingCooldown = 0.25f;

    [Header("Mechanics")]
    [SerializeField] private AudioClip bridgeCompletedClip;
    [SerializeField] private AudioClip bridgeDestroyedClip;
    [SerializeField] private AudioClip hookAttachedClip;
    [SerializeField] private AudioClip hookStrongPullClip;
    [SerializeField] private AudioClip hookReleasedClip;
    [SerializeField] private AudioClip batteryDockedClip;
    [SerializeField] private AudioClip doorOpenedClip;
    [SerializeField] [Range(0f, 1f)] private float mechanicVolume = 0.55f;

    [Header("Rejection")]
    [SerializeField] private AudioClip rejectedClip;
    [SerializeField] [Range(0f, 1f)] private float rejectionVolume = 0.3f;
    [SerializeField] [Min(0f)] private float rejectionCooldown = 0.4f;

    private double nextLandingTime;
    private double nextRejectionTime;

    private bool focused = true;
    private bool paused;

    private void OnEnable()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }
        focused = Application.isFocused;
        paused = false;
        Clear();
        presentation.Cue += OnCue;
        if (playerPresentation != null)
        {
            playerPresentation.Jumped += OnJumped;
            playerPresentation.Landed += OnLanded;
        }
    }

    private void OnCue(MechanicCue cue, Vector3 point)
    {
        if (cue == MechanicCue.Cancelled)
        {
            Clear(CanPlay() && presentation != null && presentation.PreserveDoorFeedback &&
                mechanicSource != null && doorOpenedClip != null && mechanicSource.clip == doorOpenedClip);
            return;
        }
        if (!CanPlay()) return;

        switch (cue)
        {
            case MechanicCue.BridgeCompleted:
                Play(mechanicSource, bridgeCompletedClip, mechanicVolume, nameof(bridgeCompletedClip));
                break;
            case MechanicCue.BridgeDestroyed:
                Play(mechanicSource, bridgeDestroyedClip, mechanicVolume, nameof(bridgeDestroyedClip));
                break;
            case MechanicCue.HookAttached:
                Play(mechanicSource, hookAttachedClip, mechanicVolume, nameof(hookAttachedClip));
                break;
            case MechanicCue.HookStrongPull:
                Play(mechanicSource, hookStrongPullClip, mechanicVolume, nameof(hookStrongPullClip));
                break;
            case MechanicCue.HookReleased:
                Play(mechanicSource, hookReleasedClip, mechanicVolume, nameof(hookReleasedClip));
                break;
            case MechanicCue.BatteryDocked:
                Play(mechanicSource, batteryDockedClip, mechanicVolume, nameof(batteryDockedClip));
                break;
            case MechanicCue.DoorOpened:
                Play(mechanicSource, doorOpenedClip, mechanicVolume, nameof(doorOpenedClip));
                break;
            case MechanicCue.InkRejected:
            case MechanicCue.HookRejected:
            case MechanicCue.BatteryRejected:
                PlayRejection();
                break;
            default:
                Debug.LogError($"Звук: неизвестный cue={cue}", this);
                return;
        }
    }

    private void OnJumped(float strength)
    {
        if (CanPlay()) Play(movementSource, jumpClip, movementVolume, nameof(jumpClip));
    }

    private void OnLanded(float strength)
    {
        if (!CanPlay() || strength < minimumLandingStrength) return;
        double now = Time.unscaledTimeAsDouble;
        if (now < nextLandingTime) return;
        if (!Play(movementSource, landingClip, movementVolume * Mathf.Lerp(0.4f, 1f,
            Mathf.Clamp01(strength)), nameof(landingClip))) return;
        nextLandingTime = now + landingCooldown;
    }

    private void PlayRejection()
    {
        double now = Time.unscaledTimeAsDouble;
        if (now < nextRejectionTime) return;
        if (!Play(mechanicSource, rejectedClip, rejectionVolume, nameof(rejectedClip))) return;
        nextRejectionTime = now + rejectionCooldown;
    }

    private bool CanPlay() { return isActiveAndEnabled && focused && !paused; }

    private bool Play(AudioSource source, AudioClip clip, float volume, string field)
    {
        if (source == null || clip == null)
        {
            Debug.LogError($"Звук: source={source}, {field}={clip}", this);
            enabled = false;
            return false;
        }
        source.Stop();
        source.clip = clip;
        source.volume = volume;
        source.Play();

        return true;
    }

    private bool ValidateReferences()
    {
        if (presentation == null || movementSource == null || mechanicSource == null ||
            movementSource == mechanicSource)
        {
            Debug.LogError($"Звук: presentation={presentation}, movement={movementSource}, mechanic={mechanicSource}; нужны разные источники", this);
            return false;
        }
        if (!ValidateSource(movementSource, nameof(movementSource)) || !ValidateSource(mechanicSource, nameof(mechanicSource))) return false;
        if (!InRange(movementVolume, 0f, 1f) || !InRange(mechanicVolume, 0f, 1f) ||
            !InRange(rejectionVolume, 0f, 1f) || !InRange(minimumLandingStrength, 0f, 1f) ||
            !InRange(landingCooldown, 0f, float.MaxValue) ||
            !InRange(rejectionCooldown, 0f, float.MaxValue))
        {
            Debug.LogError($"Звук: volume={movementVolume}/{mechanicVolume}/{rejectionVolume}, landing={minimumLandingStrength}, cooldown={landingCooldown}/{rejectionCooldown}", this);
            return false;
        }

        return true;
    }

    private bool ValidateSource(AudioSource source, string field)
    {
        if (source.gameObject.scene != gameObject.scene || !source.enabled ||
            !source.gameObject.activeInHierarchy || source.loop || source.playOnAwake ||
            source.spatialBlend != 0f)
        {
            Debug.LogError($"Звук: {field}={source.name}, scene={source.gameObject.scene.path}, enabled={source.enabled}, active={source.gameObject.activeInHierarchy}, loop={source.loop}, awake={source.playOnAwake}, spatial={source.spatialBlend}", this);
            return false;
        }

        return true;
    }

    private static bool InRange(float value, float minimum, float maximum) { return !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum && value <= maximum; }

    private void Clear(bool preserveDoorFeedback = false)
    {
        if (movementSource != null) movementSource.Stop();
        if (!preserveDoorFeedback && mechanicSource != null) mechanicSource.Stop();
        nextLandingTime = double.NegativeInfinity;
        nextRejectionTime = double.NegativeInfinity;
    }

    private void OnDisable()
    {
        if (presentation != null) presentation.Cue -= OnCue;
        if (playerPresentation != null)
        {
            playerPresentation.Jumped -= OnJumped;
            playerPresentation.Landed -= OnLanded;
        }
        Clear();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        focused = hasFocus;
        if (!hasFocus) Clear();
    }

    private void OnApplicationPause(bool isPaused)
    {
        paused = isPaused;
        if (isPaused) Clear();
    }
}
