using UnityEngine;

public class TimeObject : MonoBehaviour
{
    private bool failed;
    private bool disabledByFailure;

    [Header("Local time")]
    [SerializeField] private TimeRoomSettings settings;
    [Tooltip("Overlapping zones use the highest priority; ties keep the first zone.")]
    [SerializeField] private TimeZone[] zones;

    [Header("Interaction points")]
    [SerializeField] private Transform chargeStation;
    [SerializeField] private Transform dock;

    [Header("Visuals")]
    [SerializeField] private Transform rotatingPart;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Quaternion initialPartRotation;

    private double charge;
    private double angle;

    private bool initialized;

    public float Charge => (float)charge;
    public double PreviousCharge { get; private set; }
    public float LocalMultiplier { get; private set; } = 1f;
    public float LocalAngle => (float)angle;
    public bool IsCharging { get; private set; }
    public bool IsDocked { get; private set; }
    public uint DockInterruptionRevision { get; private set; }
    public bool CanDock => !failed && settings != null && dock != null && Vector2.Distance(transform.position, dock.position) <= settings.DockRadius;
    public TimeRoomSettings Settings => settings;
    public Transform Dock => dock;
    public Transform ChargeStation => chargeStation;

    public void Configure(TimeRoomSettings balance, TimeZone[] regions, Transform station,
        Transform socket, Transform rotor)
    {
        failed = false;
        settings = balance;
        zones = regions;
        chargeStation = station;
        dock = socket;
        rotatingPart = rotor;
        if (!Initialize()) return;
        if (disabledByFailure) { disabledByFailure = false; enabled = true; }
    }

    private void Start()
    {
        if (!initialized && !Initialize()) return;
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float seconds)
    {
        if (failed) return;
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0)
        {
            Debug.LogError($"Аккумулятор {name}: seconds={seconds}, нужно неотрицательное конечное время", this);
            disabledByFailure |= enabled;
            failed = true;
            enabled = false;
            return;
        }
        if (!initialized && !Initialize()) return;
        RefreshLocation();
        PreviousCharge = charge;
        charge = TimeCharge.Advance(charge, seconds, LocalMultiplier, IsCharging,
            settings.Capacity, settings.ChargeSpeed, settings.DischargeSpeed);
        angle = TimeCharge.AdvanceAngle(angle, settings.RotationSpeed, seconds, LocalMultiplier);
        rotatingPart.localRotation = initialPartRotation * Quaternion.Euler(0, 0, (float)angle);
    }

    public void MoveTo(Vector2 position)
    {
        if (failed) return;
        if (!initialized && !Initialize()) return;
        BreakDock();
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        RefreshLocation();
    }

    public bool TryDock()
    {
        if (failed) return false;
        if (!initialized && !Initialize()) return false;
        if (!CanDock) return false;
        transform.position = new Vector3(dock.position.x, dock.position.y, transform.position.z);
        IsDocked = true;
        RefreshLocation();

        return true;
    }

    public void Undock()
    {
        if (failed) return;
        if (!initialized && !Initialize()) return;
        BreakDock();
        RefreshLocation();
    }

    public void ResetObject()
    {
        if (failed) return;
        if (!initialized && !Initialize()) return;
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        rotatingPart.localRotation = initialPartRotation;
        charge = settings.InitialCharge;
        PreviousCharge = charge;
        angle = 0;
        BreakDock();
        RefreshLocation();
    }

    private void BreakDock()
    {
        if (IsDocked)
        {
            DockInterruptionRevision = unchecked(DockInterruptionRevision + 1);
            IsDocked = false;
        }
    }

    private bool Initialize()
    {
        if (failed) return false;
        initialized = false;
        if (settings == null || zones == null || chargeStation == null || dock == null || rotatingPart == null)
        {
            Debug.LogError($"Аккумулятор {name}: нужны settings, zones, chargeStation, dock, rotatingPart", this);
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
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] == null)
            {
                Debug.LogError($"Аккумулятор {name}: zones[{i}]=null", this);
                disabledByFailure |= enabled;
                failed = true;
                enabled = false;
                return false;
            }
        }
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialPartRotation = rotatingPart.localRotation;
        initialized = true;
        ResetObject();

        return true;
    }

    private void RefreshLocation()
    {
        LocalMultiplier = 1f;
        int bestPriority = int.MinValue;
        bool found = false;
        foreach (TimeZone zone in zones)
        {
            if (!zone.Contains(transform.position) || (found && zone.Priority <= bestPriority)) continue;
            LocalMultiplier = zone.Multiplier;
            bestPriority = zone.Priority;
            found = true;
        }
        IsCharging = !IsDocked && Vector2.Distance(transform.position, chargeStation.position) <= settings.StationRadius;
    }
}
