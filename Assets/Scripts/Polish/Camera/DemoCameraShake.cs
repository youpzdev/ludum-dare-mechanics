using System;
using UnityEngine;

[DefaultExecutionOrder(300)]
public class DemoCameraShake : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private DemoFlow flow;
    [SerializeField] [Min(0f)] private float maximumOffset = 0.12f;
    [SerializeField] [Min(0f)] private float maximumRoll = 0.5f;
    [SerializeField] [Min(0.01f)] private float duration = 0.24f;
    [SerializeField] [Min(0.01f)] private float frequency = 28f;
    [SerializeField] [Range(0f, 1f)] private float intensity = 1f;

    private Transform cameraTransform;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float energy;
    private float phase;
    private bool initialized;
    private bool focused = true;
    private bool paused;

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (initialized)
        {
            originalPosition = cameraTransform.localPosition;
            originalRotation = cameraTransform.localRotation;
        }
        focused = Application.isFocused;
    }

    public void Impulse(float strength)
    {
        if (float.IsNaN(strength) || float.IsInfinity(strength) || strength < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(strength), strength, "DemoCameraShake impulse requires a finite nonnegative strength on " + name);
        }
        if (!isActiveAndEnabled) return;
        Initialize();
        if (!focused || paused || flow.Finished)
        {
            Clear();
            return;
        }
        energy = Mathf.Min(1f, energy + Mathf.Clamp01(strength));
    }

    private void LateUpdate()
    {
        if (!focused || paused || flow.Finished)
        {
            Clear();
            return;
        }
        energy = Mathf.Max(0f, energy - Time.unscaledDeltaTime / duration);
        if (energy <= 0f)
        {
            RestorePose();
            phase = 0f;
            return;
        }
        phase = Mathf.Repeat(phase + Time.unscaledDeltaTime * frequency, Mathf.PI * 200f);
        float envelope = energy * energy * intensity;
        Vector2 wave = new Vector2(Mathf.Sin(phase), Mathf.Sin(phase * 1.37f + 0.7f));
        wave = Vector2.ClampMagnitude(wave, 1f);
        cameraTransform.localPosition = originalPosition +
            new Vector3(wave.x, wave.y, 0f) * (maximumOffset * envelope);
        cameraTransform.localRotation = originalRotation *
            Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.83f) * maximumRoll * envelope);
    }

    public void Clear()
    {
        energy = 0f;
        phase = 0f;
        RestorePose();
    }

    private void RestorePose()
    {
        if (initialized && cameraTransform != null)
        {
            cameraTransform.localPosition = originalPosition;
            cameraTransform.localRotation = originalRotation;
        }
    }

    private void Initialize()
    {
        if (initialized) return;
        if (targetCamera == null || flow == null)
        {
            throw new InvalidOperationException($"DemoCameraShake '{name}' requires targetCamera and flow: targetCamera={targetCamera}, flow={flow}.");
        }
        if (!FiniteNonnegative(maximumOffset) || !FiniteNonnegative(maximumRoll) ||
            !FiniteNonnegative(duration) || duration <= 0f ||
            !FiniteNonnegative(frequency) || frequency <= 0f ||
            !FiniteNonnegative(intensity) || intensity > 1f)
        {
            throw new InvalidOperationException($"DemoCameraShake '{name}' has invalid parameters: maximumOffset={maximumOffset}, maximumRoll={maximumRoll}, duration={duration}, frequency={frequency}, intensity={intensity}.");
        }
        cameraTransform = targetCamera.transform;
        originalPosition = cameraTransform.localPosition;
        originalRotation = cameraTransform.localRotation;
        initialized = true;
    }

    private static bool FiniteNonnegative(float value) { return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f; }

    private void OnDisable()
    {
        Clear();
    }

    private void OnApplicationPause(bool isPaused)
    {
        paused = isPaused;
        if (isPaused) Clear();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        focused = hasFocus;
        if (!hasFocus) Clear();
    }
}
