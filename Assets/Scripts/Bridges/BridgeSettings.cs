using UnityEngine;

[CreateAssetMenu(fileName = "BridgeSettings", menuName = "Ludum Dare/Bridge Settings")]
public class BridgeSettings : ScriptableObject
{
    [Header("Ink")]
    [Tooltip("Total length available across all strokes, in world units.")]
    [SerializeField] [Min(0.001f)] private float inkLength = 24f;
    [Tooltip("Point spacing in world units, except the final ink-limited segment. Must not exceed ink length.")]
    [SerializeField] [Min(0.001f)] private float pointSpacing = 0.08f;
    [Tooltip("Line width and collider diameter in stroke local space.")]
    [SerializeField] [Min(0.001f)] private float thickness = 0.14f;

    [Space]
    [Header("Ball")]
    [Tooltip("Automatic horizontal speed in world units per second.")]
    [SerializeField] [Min(0.001f)] private float ballSpeed = 2.5f;

    [Space]
    [Header("Drawing Feedback")]
    [SerializeField] private Color tipColor = new Color(0.949f, 0.776f, 0.427f, 1f);
    [SerializeField] private Color completionColor = new Color(0.941f, 0.945f, 0.914f, 0.8f);
    [SerializeField] private Color exhaustionColor = new Color(0.871f, 0.506f, 0.494f, 0.9f);
    [SerializeField] [Min(0.001f)] private float tipSize = 0.2f;
    [SerializeField] [Min(0.001f)] private float pulseSize = 0.36f;
    [SerializeField] [Min(0.001f)] private float completionDuration = 0.18f;
    [SerializeField] [Min(0.001f)] private float exhaustionDuration = 0.2f;

    public Color TipColor => tipColor;
    public Color CompletionColor => completionColor;
    public Color ExhaustionColor => exhaustionColor;
    public float TipSize => tipSize;
    public float PulseSize => pulseSize;
    public float CompletionDuration => completionDuration;
    public float ExhaustionDuration => exhaustionDuration;
    public float InkLength => inkLength;
    public float PointSpacing => pointSpacing;
    public float Thickness => thickness;
    public float BallSpeed => ballSpeed;

    public bool Validate()
    {
        if (!Positive(inkLength, nameof(inkLength)) || !Positive(pointSpacing, nameof(pointSpacing)) ||
            !Positive(thickness, nameof(thickness)) || !Positive(ballSpeed, nameof(ballSpeed)) ||
            !Positive(tipSize, nameof(tipSize)) || !Positive(pulseSize, nameof(pulseSize)) ||
            !Positive(completionDuration, nameof(completionDuration)) || !Positive(exhaustionDuration, nameof(exhaustionDuration))) return false;
        if (pointSpacing > inkLength)
        {
            Debug.LogError($"Настройки моста {name}: pointSpacing={pointSpacing} больше inkLength={inkLength}", this);
            return false;
        }

        return true;
    }

    private bool Positive(float value, string field)
    {
        if (!float.IsNaN(value) && !float.IsInfinity(value) && value > 0) return true;

        Debug.LogError($"Настройки моста {name}: {field}={value}, нужно конечное положительное значение", this);

        return false;
    }
}
