using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum MechanicCue
{
    BridgeCompleted,
    BridgeDestroyed,
    InkRejected,
    HookAttached,
    HookStrongPull,
    HookReleased,
    HookRejected,
    BatteryDocked,
    BatteryRejected,
    DoorOpened,
    Cancelled
}

[DefaultExecutionOrder(250)]
public class MechanicPresentation : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private DemoFlow flow;

    [Header("Bridge")]
    [SerializeField] private BridgeDrawing bridge;
    [SerializeField] private Transform strokesRoot;
    [SerializeField] private SpriteRenderer bridgeTip;
    [SerializeField] private SpriteRenderer bridgePulse;

    [Header("Hook")]
    [SerializeField] private GrappleController grapple;
    [SerializeField] private Rigidbody2D grappleBody;
    [SerializeField] [Min(0.01f)] private float strongPullSpeed = 7f;

    [Header("Time room")]
    [SerializeField] private CarryController carry;
    [SerializeField] private TimeObject battery;
    [SerializeField] private TimePuzzle puzzle;
    [SerializeField] private Transform doorPoint;

    private bool hasState;
    private bool wasTipVisible;
    private bool wasPulseVisible;

    private int previousStrokeCount;
    private float previousInk;
    private Vector3 bridgePoint;

    private bool wasAttached;
    private bool strongPullShown;
    private Vector3 hookPoint;

    private bool wasDragging;
    private bool wasDocked;
    private bool wasComplete;

    private bool focused = true;
    private bool paused;
    private bool wasFinished;
    private bool doorFeedbackPending;

    public event Action<MechanicCue, Vector3> Cue;
    public string ErrorText { get; private set; } = string.Empty;
    public string HintText { get; private set; } = string.Empty;
    public bool PreserveDoorFeedback { get; private set; }

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }
        CaptureState();
    }

    private void OnEnable()
    {
        hasState = false;
        if (!ValidateReferences()) enabled = false;
    }

    private void LateUpdate()
    {
        if (!isActiveAndEnabled || !focused || paused) return;
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }
        if (flow != null && flow.Finished)
        {
            if (!wasFinished)
            {
                CancelFeedback(flow.Won && doorFeedbackPending);
                wasFinished = true;
            }
            return;
        }
        wasFinished = false;
        if (!hasState)
        {
            CaptureState();
            return;
        }

        Mouse mouse = Mouse.current;
        bool pressed = mouse != null && mouse.leftButton.wasPressedThisFrame && !OverUi(mouse);
        if (bridge != null) ObserveBridge(pressed, mouse);
        if (!isActiveAndEnabled) return;
        if (grapple != null) ObserveHook(pressed, mouse);
        if (!isActiveAndEnabled) return;
        if (battery != null) ObserveBattery(mouse);
    }

    private void ObserveBridge(bool pressed, Mouse mouse)
    {
        bool tipVisible = bridgeTip.enabled && bridgeTip.gameObject.activeInHierarchy;
        bool pulseVisible = bridgePulse.enabled && bridgePulse.gameObject.activeInHierarchy;
        float ink = bridge.RemainingInk;
        if (tipVisible) bridgePoint = bridgeTip.transform.position;
        bool completed = pulseVisible && (!wasPulseVisible || wasTipVisible && !tipVisible);
        if (completed && bridge.isActiveAndEnabled)
        {
            bridgePoint = bridgePulse.transform.position;
            Publish(MechanicCue.BridgeCompleted, bridgePoint);
        }
        int count = strokesRoot.childCount;
        if (count < previousStrokeCount) Publish(MechanicCue.BridgeDestroyed, bridgePoint);
        if (bridge.isActiveAndEnabled && (previousInk > 0f && ink <= 0f || pressed && ink <= 0f))
        {
            Vector3 point = bridgePoint;
            if (pressed && !PointerWorld(mouse, 0f, out point)) return;
            Reject(MechanicCue.InkRejected, point, "Чернила закончились", "Нажмите R, чтобы нарисовать мост заново");
        }
        previousStrokeCount = count;
        previousInk = ink;
        wasTipVisible = tipVisible;
        wasPulseVisible = pulseVisible;
    }

    private void ObserveHook(bool pressed, Mouse mouse)
    {
        bool attached = grapple.IsAttached;
        if (attached)
        {
            hookPoint = grapple.AttachmentPoint;
            hookPoint.z = grappleBody.transform.position.z;
            if (!wasAttached)
            {
                strongPullShown = false;
                Publish(MechanicCue.HookAttached, hookPoint);
            }
            Vector2 direction = ((Vector2)hookPoint - grappleBody.position).normalized;
            if (!strongPullShown && Vector2.Dot(grappleBody.linearVelocity, direction) >= strongPullSpeed)
            {
                strongPullShown = true;
                Publish(MechanicCue.HookStrongPull, grappleBody.transform.position);
            }
        }
        else if (wasAttached)
        {
            strongPullShown = false;
            Publish(MechanicCue.HookReleased, hookPoint);
        }
        if (pressed && !attached && grapple.isActiveAndEnabled && grappleBody.simulated)
        {
            if (!PointerWorld(mouse, grappleBody.transform.position.z, out Vector3 pointer)) return;
            GrappleTarget target = grapple.QueryTarget(pointer);
            if (target.State != GrappleTargetState.Available)
            {
                Vector3 point = new Vector3(target.Point.x, target.Point.y, grappleBody.transform.position.z);
                string error = target.State == GrappleTargetState.Blocked ? "Зацеп закрыт препятствием" : "Крюк не достает до зацепа";
                Reject(MechanicCue.HookRejected, point, error, "Выберите бирюзовую поверхность ближе к игроку");
            }
        }
        wasAttached = attached;
    }

    private void ObserveBattery(Mouse mouse)
    {
        bool docked = battery.IsDocked;
        bool complete = puzzle.IsComplete;
        if (docked && !wasDocked)
        {
            Publish(MechanicCue.BatteryDocked, battery.Dock.position);
            if (!complete && battery.Charge <= battery.Settings.ChargeThreshold)
                Reject(MechanicCue.BatteryRejected, battery.Dock.position, "Недостаточно заряда", "Зарядите аккумулятор слева и перенесите в приемник");
        }
        else if (wasDragging && !carry.IsDragging && !docked && mouse != null &&
            mouse.leftButton.wasReleasedThisFrame && !OverUi(mouse) && carry.isActiveAndEnabled)
        {
            float distance = Vector2.Distance(battery.transform.position, battery.Dock.position);
            if (distance <= battery.Settings.DockRadius * 2f && !battery.IsCharging)
                Reject(MechanicCue.BatteryRejected, battery.transform.position, "Аккумулятор не попал в приемник", "Подведите аккумулятор к центру приемника и отпустите ЛКМ");
        }
        if (complete && !wasComplete) Publish(MechanicCue.DoorOpened, doorPoint.position);
        wasDragging = carry.IsDragging;
        wasDocked = docked;
        wasComplete = complete;
    }

    private void Reject(MechanicCue cue, Vector3 point, string error, string hint)
    {
        doorFeedbackPending = false;
        ErrorText = error;
        HintText = hint;
        Cue?.Invoke(cue, point);
    }

    private void Publish(MechanicCue cue, Vector3 point)
    {
        doorFeedbackPending = cue == MechanicCue.DoorOpened;
        ErrorText = string.Empty;
        HintText = string.Empty;
        Cue?.Invoke(cue, point);
    }

    private bool PointerWorld(Mouse mouse, float depth, out Vector3 point)
    {
        point = default;
        Ray ray = sceneCamera.ScreenPointToRay(mouse.position.ReadValue());
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, depth));
        if (!plane.Raycast(ray, out float distance))
        {
            Debug.LogError($"Отклик: camera={sceneCamera.name}, нет пересечения с z={depth}, pointer={mouse.position.ReadValue()}", this);
            enabled = false;
            return false;
        }
        point = ray.GetPoint(distance);

        return true;
    }

    private static bool OverUi(Mouse mouse)
    {
        EventSystem events = EventSystem.current;

        return events != null && (events.IsPointerOverGameObject() || events.IsPointerOverGameObject(mouse.deviceId));
    }

    private void CaptureState()
    {
        if (bridge != null)
        {
            previousStrokeCount = strokesRoot.childCount;
            previousInk = bridge.RemainingInk;
            wasTipVisible = bridgeTip.enabled && bridgeTip.gameObject.activeInHierarchy;
            wasPulseVisible = bridgePulse.enabled && bridgePulse.gameObject.activeInHierarchy;
            bridgePoint = bridgeTip.transform.position;
        }
        if (grapple != null)
        {
            wasAttached = grapple.IsAttached;
            hookPoint = grapple.AttachmentPoint;
            strongPullShown = false;
        }
        if (battery != null)
        {
            wasDragging = carry.IsDragging;
            wasDocked = battery.IsDocked;
            wasComplete = puzzle.IsComplete;
        }
        hasState = true;
    }

    private bool ValidateReferences()
    {
        if (sceneCamera == null || bridge == null && grapple == null && battery == null)
        {
            Debug.LogError($"Отклик: camera={sceneCamera}, bridge={bridge}, grapple={grapple}, battery={battery}", this);
            return false;
        }
        if (bridge != null && (strokesRoot == null || bridgeTip == null || bridgePulse == null))
        {
            Debug.LogError($"Отклик мостов: root={strokesRoot}, tip={bridgeTip}, pulse={bridgePulse}", this);
            return false;
        }
        if (grapple != null && (grappleBody == null || !float.IsFinite(strongPullSpeed) || strongPullSpeed <= 0f))
        {
            Debug.LogError($"Отклик крюка: body={grappleBody}, strongPullSpeed={strongPullSpeed}", this);
            return false;
        }
        if (battery != null && (carry == null || puzzle == null || doorPoint == null))
        {
            Debug.LogError($"Отклик батареи: carry={carry}, puzzle={puzzle}, doorPoint={doorPoint}", this);
            return false;
        }
        if (battery != null && (battery.Settings == null || battery.Dock == null))
        {
            Debug.LogError($"Отклик батареи: settings={battery.Settings}, dock={battery.Dock}", this);
            return false;
        }

        return true;
    }

    private void Cancel() { CancelFeedback(false); }

    private void CancelFeedback(bool preserveDoorFeedback)
    {
        hasState = false;
        strongPullShown = false;
        PreserveDoorFeedback = preserveDoorFeedback;
        try
        {
            Publish(MechanicCue.Cancelled, transform.position);
        }
        finally
        {
            PreserveDoorFeedback = false;
        }
    }

    private void OnApplicationFocus(bool value)
    {
        focused = value;
        if (!value) Cancel();
    }

    private void OnApplicationPause(bool value)
    {
        paused = value;
        if (value) Cancel();
    }

    private void OnDisable()
    {
        Cancel();
    }
}
