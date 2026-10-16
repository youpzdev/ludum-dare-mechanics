using UnityEngine;

public class TimePuzzle : MonoBehaviour
{
    private bool failed;
    private bool disabledByFailure;

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
        failed = false;
        settings = balance;
        battery = item;
        chargeStation = station;
        dock = socket;
        exitDoor = door;
        carry = controller;
        if (!Validate()) return;
        if (disabledByFailure) { disabledByFailure = false; enabled = true; }
    }

    private void Start()
    {
        if (!Validate()) return;
    }

    private void LateUpdate()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float seconds)
    {
        if (!Validate()) return;
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0)
        {
            Debug.LogError($"Приемник {name}: seconds={seconds}, нужно неотрицательное конечное время", this);
            disabledByFailure |= enabled;
            failed = true;
            enabled = false;
            return;
        }
        if (IsComplete) return;
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
        if (!Validate()) return;
        carry.CancelDrag();
        battery.ResetObject();
        dockInterruptionRevision = battery.DockInterruptionRevision;
        hold = 0;
        IsComplete = false;
        exitDoor.enabled = true;
    }

    private bool Validate()
    {
        if (failed) return false;
        if (settings == null || battery == null || chargeStation == null || dock == null || exitDoor == null || carry == null)
        {
            Debug.LogError($"Приемник {name}: нужны settings, battery, chargeStation, dock, exitDoor, carry", this);
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
        if (battery.Settings != settings || battery.Dock != dock || battery.ChargeStation != chargeStation)
        {
            Debug.LogError($"Приемник {name}: у battery={battery.name} другие settings, chargeStation или dock", this);
            disabledByFailure |= enabled;
            failed = true;
            enabled = false;
            return false;
        }

        return true;
    }
}
