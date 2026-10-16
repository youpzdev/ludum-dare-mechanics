using System;
using UnityEngine;

public class TimePuzzle : MonoBehaviour
{
    [Header("Battery")]
    [SerializeField] private TimeRoomSettings settings;
    [SerializeField] private TimeObject battery;

    [Header("Interaction points")]
    [SerializeField] private Transform chargeStation;
    [SerializeField] private Transform dock;

    [Header("Puzzle controls")]
    [Tooltip("Disabled when the required dock hold is complete.")]
    [SerializeField] private Collider2D exitDoor;
    [SerializeField] private CarryController carry;

    private double hold;
    private uint dockInterruptionRevision;

    public float HoldProgress => (float)hold;
    public bool IsComplete { get; private set; }

    public void Configure(TimeRoomSettings balance, TimeObject item, Transform station, Transform socket,
        Collider2D door, CarryController controller)
    {
        settings = balance;
        battery = item;
        chargeStation = station;
        dock = socket;
        exitDoor = door;
        carry = controller;
        Validate();
    }

    private void Start()
    {
        Validate();
    }

    private void LateUpdate()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float seconds)
    {
        if (IsComplete)
            return;
        if (dockInterruptionRevision != battery.DockInterruptionRevision)
        {
            hold = 0;
            dockInterruptionRevision = battery.DockInterruptionRevision;
        }
        hold = TimeCharge.AdvanceHold(hold, battery.PreviousCharge, seconds,
            battery.LocalMultiplier, settings.DischargeSpeed, battery.IsDocked,
            settings.ChargeThreshold, settings.HoldSeconds);
        if (hold >= settings.HoldSeconds)
        {
            IsComplete = true;
            exitDoor.enabled = false;
        }
    }

    public void ResetPuzzle()
    {
        carry.CancelDrag();
        battery.ResetObject();
        dockInterruptionRevision = battery.DockInterruptionRevision;
        hold = 0;
        IsComplete = false;
        exitDoor.enabled = true;
    }

    private void Validate()
    {
        if (settings == null || battery == null || chargeStation == null || dock == null || exitDoor == null || carry == null)
            throw new InvalidOperationException($"Time puzzle '{name}' needs settings, battery, chargeStation, dock, exitDoor and carry.");
        settings.Validate();
        if (battery.Settings != settings || battery.Dock != dock || battery.ChargeStation != chargeStation)
            throw new InvalidOperationException($"Time puzzle '{name}' and battery '{battery.name}' must share settings, chargeStation and dock.");
    }
}
