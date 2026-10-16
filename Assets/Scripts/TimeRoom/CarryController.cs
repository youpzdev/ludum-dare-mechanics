using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-100)]
public class CarryController : MonoBehaviour
{
    private bool failed;
    private bool disabledByFailure;

    [Header("Input")]
    [SerializeField] private Camera sceneCamera;

    [Header("Carried object")]
    [SerializeField] private TimeObject battery;
    [Tooltip("Collider on the battery root, not its rotating part.")]
    [SerializeField] private Collider2D batteryCollider;

    [Header("Movement limits")]
    [Tooltip("Axis-aligned bounds that contain the whole battery.")]
    [SerializeField] private BoxCollider2D roomBounds;

    private Vector2 dragOffset;
    private Vector2 halfSize;

    public bool IsDragging { get; private set; }

    public void Configure(Camera camera, TimeObject item, Collider2D itemCollider, BoxCollider2D bounds)
    {
        failed = false;
        sceneCamera = camera;
        battery = item;
        batteryCollider = itemCollider;
        roomBounds = bounds;
        if (!Validate()) return;
        if (disabledByFailure) { disabledByFailure = false; enabled = true; }
    }

    private void Start()
    {
        if (!Validate()) return;
    }

    private void Update()
    {
        if (!Validate()) return;
        Mouse mouse = Mouse.current;
        if (mouse == null || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            CancelDrag();
            return;
        }
        Ray ray = sceneCamera.ScreenPointToRay(mouse.position.ReadValue());
        Plane plane = new Plane(Vector3.forward, battery.transform.position);
        if (!plane.Raycast(ray, out float distance))
        {
            CancelDrag();
            return;
        }
        Vector2 pointer = ray.GetPoint(distance);
        if (mouse.leftButton.wasPressedThisFrame && batteryCollider.OverlapPoint(pointer))
        {
            battery.Undock();
            IsDragging = true;
            dragOffset = (Vector2)battery.transform.position - pointer;
        }
        if (!mouse.leftButton.isPressed && IsDragging)
        {
            MoveTo(pointer + dragOffset);
            Release();
        }
        if (IsDragging) MoveTo(pointer + dragOffset);
    }

    public void MoveTo(Vector2 position)
    {
        if (!Validate()) return;
        Bounds bounds = roomBounds.bounds;
        battery.MoveTo(new Vector2(
            Mathf.Clamp(position.x, bounds.min.x + halfSize.x, bounds.max.x - halfSize.x),
            Mathf.Clamp(position.y, bounds.min.y + halfSize.y, bounds.max.y - halfSize.y)));
    }

    public void Release()
    {
        if (!IsDragging) return;
        if (!Validate()) return;
        IsDragging = false;
        battery.TryDock();
    }

    public void CancelDrag() { IsDragging = false; }

    private void OnDisable()
    {
        CancelDrag();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) CancelDrag();
    }

    private bool Validate()
    {
        if (failed) return false;
        if (sceneCamera == null || battery == null || batteryCollider == null || roomBounds == null)
            return Invalid($"Перенос {name}: нужны sceneCamera, battery, batteryCollider, roomBounds");
        if (batteryCollider.transform != battery.transform)
            return Invalid($"Перенос {name}: batteryCollider={batteryCollider.name} должен быть на battery={battery.name}");
        if (Mathf.Abs(Mathf.DeltaAngle(roomBounds.transform.eulerAngles.z, 0)) > 0.01f)
            return Invalid($"Перенос {name}: roomBounds={roomBounds.name}, угол={roomBounds.transform.eulerAngles.z}, нужен 0");
        halfSize = batteryCollider.bounds.extents;
        if (roomBounds.bounds.size.x <= halfSize.x * 2 || roomBounds.bounds.size.y <= halfSize.y * 2)
            return Invalid($"Перенос {name}: roomBounds={roomBounds.bounds.size} меньше battery={batteryCollider.bounds.size}");

        return true;
    }

    private bool Invalid(string message)
    {
        if (failed) return false;
        Debug.LogError(message, this);
        CancelDrag();
        disabledByFailure |= enabled;
        failed = true;
        enabled = false;

        return false;
    }
}
