using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum GrappleTargetState
{
    None,
    Blocked,
    Available
}

public readonly struct GrappleTarget
{
    public GrappleTargetState State { get; }
    public Vector2 Point { get; }
    public RaycastHit2D Hit { get; }

    public GrappleTarget(GrappleTargetState state, Vector2 point, RaycastHit2D hit)
    {
        State = state;
        Point = point;
        Hit = hit;
    }
}

[DefaultExecutionOrder(100)]
public class GrappleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private GrappleSettings settings;
    [SerializeField] private LineRenderer rope;
    [SerializeField] private GrappleFeedback feedback;

    [Header("Surface Query")]
    [Tooltip("Layers checked for hits, including blockers and all allowed grapple surfaces.")]
    [SerializeField] private LayerMask queryMask;
    [Tooltip("Layers that allow attachment when they are the nearest non-player hit.")]
    [SerializeField] private LayerMask grappleSurfaceMask;

    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    private Collider2D anchor;
    private Vector3 localPoint;

    private bool invalid;
    private bool focused = true;
    private bool paused;

    public bool IsAttached
    {
        get
        {
            ValidateAttachment();

            return anchor != null;
        }
    }

    public Vector2 AttachmentPoint => IsAttached ? (Vector2)anchor.transform.TransformPoint(localPoint) : body == null ? (Vector2)transform.position : body.position;
    public float CurrentDistance => IsAttached ? Vector2.Distance(body.position, anchor.transform.TransformPoint(localPoint)) : 0f;

    public void ConfigureFeedback(GrappleFeedback value)
    {
        if (feedback != null) feedback.Clear();
        feedback = value;
    }

    private void Awake()
    {
        if (!Validate()) return;

        rope.useWorldSpace = true;
        rope.positionCount = 2;
        Release();
    }

    private bool Validate()
    {
        if (invalid) return false;

        if (body == null || aimCamera == null || settings == null || rope == null)
            return StopInvalid($"Крюк {name}: body={body}, camera={aimCamera}, settings={settings}, rope={rope}");

        if (!settings.IsValid)
            return StopInvalid($"Крюк {name}: range={settings.Range}, acceleration={settings.Acceleration}, maxSpeed={settings.MaxSpeed}, arrival={settings.ArrivalDistance}, tether={settings.MaxTetherLength}");

        if (grappleSurfaceMask.value == 0 || (queryMask.value & grappleSurfaceMask.value) != grappleSurfaceMask.value)
            return StopInvalid($"Крюк {name}: queryMask={queryMask.value}, surfaceMask={grappleSurfaceMask.value}");

        return true;
    }

    private bool StopInvalid(string message)
    {
        if (!invalid) Debug.LogError(message, this);
        invalid = true;
        Release();
        enabled = false;

        return false;
    }

    private void Update()
    {
        if (!Validate()) return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !body.simulated || !focused || paused)
        {
            Release();
            return;
        }

        EventSystem events = EventSystem.current;
        if (events != null && (events.IsPointerOverGameObject() || events.IsPointerOverGameObject(mouse.deviceId)))
        {
            Release();
            return;
        }

        if (!mouse.leftButton.isPressed) Release();

        Ray pointerRay = aimCamera.ScreenPointToRay(mouse.position.ReadValue());
        Plane bodyPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, body.transform.position.z));
        if (!bodyPlane.Raycast(pointerRay, out float distance))
        {
            Release();
            return;
        }

        GrappleTarget target = QueryTarget(pointerRay.GetPoint(distance));
        if (mouse.leftButton.isPressed && mouse.leftButton.wasPressedThisFrame)
        {
            Release();
            Attach(target);
        }

        if (feedback != null && !IsAttached) feedback.ShowTarget(target, body.transform.position.z);
    }

    public GrappleTarget QueryTarget(Vector2 target)
    {
        if (!isActiveAndEnabled || !Validate())
            return new GrappleTarget(GrappleTargetState.None, body == null ? (Vector2)transform.position : body.position, default);

        if (!body.simulated || !GrappleMath.IsFinite(target.x) || !GrappleMath.IsFinite(target.y))
            return new GrappleTarget(GrappleTargetState.None, body.position, default);

        Vector2 offset = target - body.position;
        if (offset.sqrMagnitude <= Mathf.Epsilon) return new GrappleTarget(GrappleTargetState.None, body.position, default);

        Vector2 direction = offset.normalized;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(queryMask);
        filter.useTriggers = false;
        hits.Clear();
        Physics2D.Raycast(body.position, direction, filter, hits, settings.Range);

        RaycastHit2D first = FindNearestHit();
        if (first.collider == null) return new GrappleTarget(GrappleTargetState.None, body.position + direction * settings.Range, default);

        bool available = (grappleSurfaceMask.value & (1 << first.collider.gameObject.layer)) != 0 &&
            GrappleMath.IsWithinRange(first.distance, settings.Range) &&
            GrappleMath.IsWithinTether(first.distance, settings.MaxTetherLength);

        return new GrappleTarget(available ? GrappleTargetState.Available : GrappleTargetState.Blocked, first.point, first);
    }

    public bool TryAttach(Vector2 target)
    {
        Release();
        if (!isActiveAndEnabled || !Validate() || !focused || paused) return false;

        return Attach(QueryTarget(target));
    }

    private bool Attach(GrappleTarget target)
    {
        if (target.State != GrappleTargetState.Available) return false;

        anchor = target.Hit.collider;
        localPoint = anchor.transform.InverseTransformPoint(target.Point);
        SyncRope();
        if (feedback != null) feedback.ShowAttachment(new Vector3(target.Point.x, target.Point.y, body.transform.position.z));

        return true;
    }

    private RaycastHit2D FindNearestHit()
    {
        RaycastHit2D first = default;
        float nearestDistance = float.PositiveInfinity;
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger || hit.collider.attachedRigidbody == body ||
                hit.collider.transform == body.transform || hit.collider.transform.IsChildOf(body.transform))
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                first = hit;
                nearestDistance = hit.distance;
            }
        }

        return first;
    }

    public void Release()
    {
        anchor = null;
        localPoint = Vector3.zero;
        if (rope != null) rope.enabled = false;
        if (feedback != null) feedback.Clear();
    }

    private void FixedUpdate()
    {
        if (!Validate() || !IsAttached) return;

        Vector2 offset = (Vector2)anchor.transform.TransformPoint(localPoint) - body.position;
        Vector2 velocity = body.linearVelocity;
        if (!GrappleMath.IsFinite(velocity.x) || !GrappleMath.IsFinite(velocity.y) ||
            !GrappleMath.IsFinite(offset.x) || !GrappleMath.IsFinite(offset.y))
        {
            StopInvalid($"Крюк {name}: velocity={velocity}, offset={offset}");
            return;
        }

        GrappleMath.PullVelocity(velocity.x, velocity.y, offset.x, offset.y,
            settings.Acceleration, settings.MaxSpeed, settings.ArrivalDistance, Time.fixedDeltaTime,
            out float nextX, out float nextY);
        body.linearVelocity = new Vector2(nextX, nextY);
    }

    private void LateUpdate()
    {
        if (!Validate()) return;

        if (!body.simulated || !focused || paused)
        {
            Release();
            return;
        }
        SyncRope();
        if (anchor != null && feedback != null)
        {
            Vector2 point = anchor.transform.TransformPoint(localPoint);
            feedback.FollowAttachment(new Vector3(point.x, point.y, body.transform.position.z));
        }
    }

    private void SyncRope()
    {
        bool attached = IsAttached;
        if (rope == null) return;
        rope.enabled = attached;
        if (!attached) return;

        rope.SetPosition(0, body.transform.position);
        Vector2 point = anchor.transform.TransformPoint(localPoint);
        rope.SetPosition(1, new Vector3(point.x, point.y, body.transform.position.z));
    }

    private void ValidateAttachment()
    {
        if (anchor == null)
        {
            if (rope != null && rope.enabled) Release();
            return;
        }

        if (body == null || settings == null || !anchor.enabled || !anchor.gameObject.activeInHierarchy || !body.simulated ||
            (anchor.attachedRigidbody != null && !anchor.attachedRigidbody.simulated) ||
            !GrappleMath.IsWithinTether(Vector2.Distance(body.position, anchor.transform.TransformPoint(localPoint)), settings.MaxTetherLength))
        {
            Release();
        }
    }

    private void OnApplicationFocus(bool value)
    {
        focused = value;
        if (!value) Release();
    }

    private void OnApplicationPause(bool value)
    {
        paused = value;
        if (value) Release();
    }

    private void OnDisable()
    {
        Release();
    }
}
