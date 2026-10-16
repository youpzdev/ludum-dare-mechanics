using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Ludum Dare/Time Room Settings")]
public class TimeRoomSettings : ScriptableObject
{
    [Header("Local time")]
    [SerializeField] private float fastMultiplier = 3f;
    [SerializeField] private float slowMultiplier = 0.2f;

    [Header("Charge")]
    [SerializeField] private float capacity = 100f;
    [SerializeField] private float initialCharge = 0f;
    [Tooltip("Charge gained per second of local time.")]
    [SerializeField] private float chargeSpeed = 40f;
    [Tooltip("Charge lost per second of local time.")]
    [SerializeField] private float dischargeSpeed = 15f;

    [Header("Dock hold")]
    [Tooltip("Charge must stay strictly above this value during the hold.")]
    [SerializeField] private float chargeThreshold = 20f;
    [Tooltip("Required hold duration in real seconds, not local time.")]
    [SerializeField] private float holdSeconds = 8f;

    [Header("Rotation")]
    [Tooltip("Degrees per second of local time. Negative values reverse rotation.")]
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Interaction distances")]
    [SerializeField] private float stationRadius = 0.9f;
    [SerializeField] private float dockRadius = 0.7f;

    [Header("Feedback")]
    [SerializeField] private Color normalColor = new Color(0.949f, 0.776f, 0.427f, 1f);
    [SerializeField] private Color fastColor = new Color(0.922f, 0.643f, 0.467f, 1f);
    [SerializeField] private Color slowColor = new Color(0.467f, 0.725f, 0.875f, 1f);
    [SerializeField] private Color chargeColor = new Color(0.949f, 0.776f, 0.427f, 1f);
    [SerializeField] private Color holdColor = new Color(0.651f, 0.831f, 0.553f, 1f);
    [SerializeField] private Color socketColor = new Color(0.698f, 0.741f, 0.776f, 1f);
    [SerializeField] private Color dockReadyColor = new Color(0.455f, 0.839f, 0.804f, 1f);
    [SerializeField] private Color dockedColor = new Color(0.651f, 0.831f, 0.553f, 1f);
    [SerializeField] [Min(0.001f)] private float feedbackDuration = 0.18f;

    public Color NormalColor => normalColor;
    public Color FastColor => fastColor;
    public Color SlowColor => slowColor;
    public Color ChargeColor => chargeColor;
    public Color HoldColor => holdColor;
    public Color SocketColor => socketColor;
    public Color DockReadyColor => dockReadyColor;
    public Color DockedColor => dockedColor;
    public float FeedbackDuration => feedbackDuration;
    public float FastMultiplier => fastMultiplier;
    public float SlowMultiplier => slowMultiplier;
    public float Capacity => capacity;
    public float InitialCharge => initialCharge;
    public float ChargeSpeed => chargeSpeed;
    public float DischargeSpeed => dischargeSpeed;
    public float ChargeThreshold => chargeThreshold;
    public float HoldSeconds => holdSeconds;
    public float RotationSpeed => rotationSpeed;
    public float StationRadius => stationRadius;
    public float DockRadius => dockRadius;

    public void Validate()
    {
        float[] positive = { fastMultiplier, slowMultiplier, capacity, chargeSpeed, dischargeSpeed,
            holdSeconds, stationRadius, dockRadius };
        foreach (float value in positive)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                throw new InvalidOperationException($"Time room settings '{name}' contain invalid positive value {value}.");
        }
        if (float.IsNaN(initialCharge) || initialCharge < 0 || initialCharge > capacity)
            throw new InvalidOperationException($"Initial charge {initialCharge} must be within 0..{capacity} in '{name}'.");
        if (float.IsNaN(chargeThreshold) || chargeThreshold < 0 || chargeThreshold >= capacity)
            throw new InvalidOperationException($"Charge threshold {chargeThreshold} must be within 0..{capacity} in '{name}'.");
        if (float.IsNaN(rotationSpeed) || float.IsInfinity(rotationSpeed))
            throw new InvalidOperationException($"Rotation speed {rotationSpeed} must be finite in '{name}'.");
    }
}
