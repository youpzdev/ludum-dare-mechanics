using UnityEngine;

[CreateAssetMenu(fileName = "GrappleSettings", menuName = "Ludum Dare/Grapple Settings")]
public class GrappleSettings : ScriptableObject
{
    [Header("Attachment")]
    [Tooltip("Maximum distance checked when attaching the grapple.")]
    [SerializeField] [Min(0.01f)] private float range = 9f;

    [Header("Pull")]
    [SerializeField] [Min(0f)] private float acceleration = 32f;
    [SerializeField] [Min(0.01f)] private float maxSpeed = 13f;
    [Tooltip("Pull stops accelerating inside this distance, preserving momentum.")]
    [SerializeField] [Min(0f)] private float arrivalDistance = 0.35f;

    [Space]
    [Tooltip("Attachment releases beyond this distance. Must be at least the attachment range.")]
    [SerializeField] [Min(0.01f)] private float maxTetherLength = 14.4f;

    [Header("Feedback")]
    [SerializeField] private Color availableColor = new Color(0.455f, 0.839f, 0.804f, 1f);
    [SerializeField] private Color blockedColor = new Color(0.871f, 0.506f, 0.494f, 0.85f);
    [SerializeField] private Color noTargetColor = new Color(0.698f, 0.741f, 0.776f, 0.45f);
    [SerializeField] private Color contactColor = new Color(0.941f, 0.945f, 0.914f, 1f);
    [SerializeField] [Min(0.01f)] private float availableMarkerSize = 0.28f;
    [SerializeField] [Min(0.01f)] private float blockedMarkerSize = 0.22f;
    [SerializeField] [Min(0.01f)] private float noTargetMarkerSize = 0.12f;
    [SerializeField] [Min(0.01f)] private float contactSize = 0.2f;
    [SerializeField] [Min(0.01f)] private float contactEndSize = 0.48f;
    [SerializeField] [Min(0.01f)] private float contactDuration = 0.2f;
    [SerializeField] [Min(0f)] private float ropeAccentDuration = 0.16f;

    [SerializeField] [Min(0.01f)] private float contactGlowSize = 0.5f;
    [SerializeField] [Min(0.01f)] private float contactGlowEndSize = 0.9f;
    [SerializeField] [Range(0.01f, 1f)] private float contactGlowOpacity = 0.55f;

    public float ContactGlowSize => contactGlowSize;
    public float ContactGlowEndSize => contactGlowEndSize;
    public float ContactGlowOpacity => contactGlowOpacity;

    public Color AvailableColor => availableColor;
    public Color BlockedColor => blockedColor;
    public Color NoTargetColor => noTargetColor;
    public Color ContactColor => contactColor;
    public float AvailableMarkerSize => availableMarkerSize;
    public float BlockedMarkerSize => blockedMarkerSize;
    public float NoTargetMarkerSize => noTargetMarkerSize;
    public float ContactSize => contactSize;
    public float ContactEndSize => contactEndSize;
    public float ContactDuration => contactDuration;
    public float RopeAccentDuration => ropeAccentDuration;

    public bool IsFeedbackValid =>
        GrappleMath.IsFinite(availableMarkerSize) && availableMarkerSize > 0f &&
        GrappleMath.IsFinite(blockedMarkerSize) && blockedMarkerSize > 0f &&
        GrappleMath.IsFinite(noTargetMarkerSize) && noTargetMarkerSize > 0f &&
        GrappleMath.IsFinite(contactSize) && contactSize > 0f &&
        GrappleMath.IsFinite(contactEndSize) && contactEndSize > 0f &&
        GrappleMath.IsFinite(contactDuration) && contactDuration > 0f &&
        GrappleMath.IsFinite(ropeAccentDuration) && ropeAccentDuration >= 0f &&
        GrappleMath.IsFinite(contactGlowSize) && contactGlowSize > 0f &&
        GrappleMath.IsFinite(contactGlowEndSize) && contactGlowEndSize > 0f &&
        GrappleMath.IsFinite(contactGlowOpacity) && contactGlowOpacity > 0f && contactGlowOpacity <= 1f;

    public float Range => range;
    public float Acceleration => acceleration;
    public float MaxSpeed => maxSpeed;
    public float ArrivalDistance => arrivalDistance;
    public float MaxTetherLength => maxTetherLength;

    public bool IsValid => GrappleMath.IsFinite(range) && range > 0f &&
        GrappleMath.IsFinite(acceleration) && acceleration >= 0f &&
        GrappleMath.IsFinite(maxSpeed) && maxSpeed > 0f &&
        GrappleMath.IsFinite(arrivalDistance) && arrivalDistance >= 0f && arrivalDistance < range &&
        GrappleMath.IsFinite(maxTetherLength) && maxTetherLength >= range;
}
