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
        if (!Initialize()) enabled = false;
    }

    private void OnEnable()
    {
        if (!Initialize())
        {
            enabled = false;
            return;
        }
        originalPosition = cameraTransform.localPosition;
        originalRotation = cameraTransform.localRotation;
        focused = Application.isFocused;
    }

    public void Impulse(float strength)
    {
        if (float.IsNaN(strength) || float.IsInfinity(strength) || strength < 0f)
        {
            Debug.LogError($"Шейк: неверная strength={strength}", this);
            return;
        }
        if (!isActiveAndEnabled) return;
        if (!Initialize()) return;
        if (cameraTransform == null || flow == null)
        {
            Debug.LogError($"Шейк: потеряны camera={cameraTransform}, flow={flow}", this);
            enabled = false;
            return;
        }
        if (!focused || paused || flow.Finished)
        {
            Clear();
            return;
        }
        energy = Mathf.Min(1f, energy + Mathf.Clamp01(strength));
    }

    private void LateUpdate()
    {
        if (!initialized || !isActiveAndEnabled) return;
        if (cameraTransform == null || flow == null)
        {
            Debug.LogError($"Шейк: потеряны camera={cameraTransform}, flow={flow}", this);
            enabled = false;
            return;
        }
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

    private bool Initialize()
    {
        if (initialized) return true;
        if (targetCamera == null || flow == null)
        {
            Debug.LogError($"Шейк: targetCamera={targetCamera}, flow={flow}", this);
            return false;
        }
        if (!FiniteNonnegative(maximumOffset) || !FiniteNonnegative(maximumRoll) ||
            !FiniteNonnegative(duration) || duration <= 0f ||
            !FiniteNonnegative(frequency) || frequency <= 0f ||
            !FiniteNonnegative(intensity) || intensity > 1f)
        {
            Debug.LogError($"Шейк: offset={maximumOffset}, roll={maximumRoll}, duration={duration}, frequency={frequency}, intensity={intensity}", this);
            return false;
        }
        cameraTransform = targetCamera.transform;
        originalPosition = cameraTransform.localPosition;
        originalRotation = cameraTransform.localRotation;
        initialized = true;

        return true;
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
