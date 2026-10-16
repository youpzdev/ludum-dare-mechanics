using System;
using UnityEngine;

public class TimeZone : MonoBehaviour
{
    [Header("Region")]
    [SerializeField] private TimeRoomSettings settings;
    [SerializeField] private Collider2D area;

    [Header("Local time")]
    [SerializeField] private bool fast;
    [Tooltip("Higher values win when zones overlap. Negative priorities are valid.")]
    [SerializeField] private int priority;

    public int Priority => priority;
    public float Multiplier => fast ? settings.FastMultiplier : settings.SlowMultiplier;

    public void Configure(TimeRoomSettings balance, Collider2D region, bool accelerated, int order)
    {
        settings = balance;
        area = region;
        fast = accelerated;
        priority = order;
        Validate();
    }

    public bool Contains(Vector2 position) { return isActiveAndEnabled && area.enabled && area.OverlapPoint(position); }

    private void Start()
    {
        Validate();
    }

    private void Validate()
    {
        if (settings == null || area == null)
            throw new InvalidOperationException($"Time zone '{name}' needs settings and area collider.");
        settings.Validate();
    }
}
