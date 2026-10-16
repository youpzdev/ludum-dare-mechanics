using System;
using UnityEngine;

public class TimeObject : MonoBehaviour
{
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
    public bool CanDock => Vector2.Distance(transform.position, dock.position) <= settings.DockRadius;
    public TimeRoomSettings Settings => settings;
    public Transform Dock => dock;
    public Transform ChargeStation => chargeStation;

    public void Configure(TimeRoomSettings balance, TimeZone[] regions, Transform station,
        Transform socket, Transform rotor)
    {
        settings = balance;
        zones = regions;
        chargeStation = station;
        dock = socket;
        rotatingPart = rotor;
        Initialize();
    }

    private void Start()
    {
        if (!initialized)
            Initialize();
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float seconds)
    {
        if (!initialized)
            Initialize();
        RefreshLocation();
        PreviousCharge = charge;
        charge = TimeCharge.Advance(charge, seconds, LocalMultiplier, IsCharging,
            settings.Capacity, settings.ChargeSpeed, settings.DischargeSpeed);
        angle = TimeCharge.AdvanceAngle(angle, settings.RotationSpeed, seconds, LocalMultiplier);
        rotatingPart.localRotation = initialPartRotation * Quaternion.Euler(0, 0, (float)angle);
    }

    public void MoveTo(Vector2 position)
    {
        BreakDock();
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        RefreshLocation();
    }

    public bool TryDock()
    {
        if (!CanDock)
            return false;
        transform.position = new Vector3(dock.position.x, dock.position.y, transform.position.z);
        IsDocked = true;
        RefreshLocation();

        return true;
    }

    public void Undock()
    {
        BreakDock();
        RefreshLocation();
    }

    public void ResetObject()
    {
        if (!initialized)
            Initialize();
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

    private void Initialize()
    {
        if (settings == null || zones == null || chargeStation == null || dock == null || rotatingPart == null)
            throw new InvalidOperationException($"Time object '{name}' needs settings, zones, chargeStation, dock and rotatingPart.");
        settings.Validate();
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] == null)
                throw new InvalidOperationException($"Time object '{name}' has null zones[{i}].");
        }
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialPartRotation = rotatingPart.localRotation;
        initialized = true;
        ResetObject();
    }

    private void RefreshLocation()
    {
        LocalMultiplier = 1f;
        int bestPriority = int.MinValue;
        bool found = false;
        foreach (TimeZone zone in zones)
        {
            if (!zone.Contains(transform.position) || (found && zone.Priority <= bestPriority))
                continue;
            LocalMultiplier = zone.Multiplier;
            bestPriority = zone.Priority;
            found = true;
        }
        IsCharging = !IsDocked && Vector2.Distance(transform.position, chargeStation.position) <= settings.StationRadius;
    }
}
