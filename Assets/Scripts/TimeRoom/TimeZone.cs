using UnityEngine;

public class TimeZone : MonoBehaviour
{
    private bool failed;
    private bool disabledByFailure;

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
        failed = false;
        settings = balance;
        area = region;
        fast = accelerated;
        priority = order;
        if (!Validate()) return;
        if (disabledByFailure) { disabledByFailure = false; enabled = true; }
    }

    public bool Contains(Vector2 position) { return !failed && isActiveAndEnabled && area != null && area.enabled && area.OverlapPoint(position); }

    private void Start()
    {
        if (!Validate()) return;
    }

    private bool Validate()
    {
        if (failed) return false;
        if (settings == null || area == null)
        {
            Debug.LogError($"Зона времени {name}: нужны settings и area", this);
            disabledByFailure |= enabled;
            failed = true;
            enabled = false;
            return false;
        }
        if (!settings.Validate())
        {
            disabledByFailure |= enabled;
            failed = true;
            enabled = false;
            return false;
        }

        return true;
    }
}
