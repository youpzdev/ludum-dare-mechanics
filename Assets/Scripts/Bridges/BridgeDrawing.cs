using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BridgeDrawing : MonoBehaviour
{
    [Header("Drawing")]
    [Tooltip("Projects the mouse position onto the world z=0 plane.")]
    [SerializeField] private Camera drawingCamera;
    [SerializeField] private BridgeSettings settings;

    [Space]
    [Header("Strokes")]
    [SerializeField] private BridgeStroke strokePrefab;
    [SerializeField] private Transform strokesRoot;
    [SerializeField] private BridgeFeedback feedback;

    private readonly List<BridgeStroke> strokes = new List<BridgeStroke>();

    private InkPath path;
    private BridgeStroke activeStroke;
    private bool failed;

    public float RemainingInk
    {
        get
        {
            if (path != null) return path.RemainingInk;
            if (failed) return 0f;
            if (settings != null)
            {
                if (settings.Validate()) return settings.InkLength;
                Fail(null);
                return 0f;
            }
            Fail($"Рисование моста {name}: settings не назначены");

            return 0f;
        }
    }

    public int StrokeCount => strokes.Count;

    public void Configure(Camera camera, BridgeSettings bridgeSettings, BridgeStroke prefab, Transform root)
    {
        if (camera == null || bridgeSettings == null || prefab == null || root == null)
        {
            Fail($"Рисование моста {name}: camera={camera}, settings={bridgeSettings}, prefab={prefab}, root={root}");
            return;
        }
        if (!bridgeSettings.Validate()) { Fail(null); return; }

        ResetDrawing();
        drawingCamera = camera;
        settings = bridgeSettings;
        strokePrefab = prefab;
        strokesRoot = root;
        if (failed) { failed = false; enabled = true; }
        path = CreatePath();
    }

    public void ConfigureFeedback(BridgeFeedback bridgeFeedback)
    {
        if (bridgeFeedback == null) { Fail($"Рисование моста {name}: bridgeFeedback не назначен"); return; }

        CancelStroke();
        feedback = bridgeFeedback;
        feedback.ResetFeedback();
    }

    public bool BeginStroke(Vector2 point)
    {
        if (!ValidateReferences()) return false;
        EndStroke();
        if (path == null) path = CreatePath();

        bool began = path.Begin(new System.Numerics.Vector2(point.x, point.y));
        if (began && feedback != null && feedback.isActiveAndEnabled) feedback.BeginStroke();

        return began;
    }

    public bool AppendPoint(Vector2 point)
    {
        if (!ValidateReferences()) return false;
        if (path == null || !path.Append(new System.Numerics.Vector2(point.x, point.y))) return false;

        if (activeStroke == null)
        {
            activeStroke = Instantiate(strokePrefab, strokesRoot);
            strokes.Add(activeStroke);
        }

        if (!activeStroke.SetPoints(path.Points, settings.Thickness)) { Fail(null); return false; }
        if (feedback != null && feedback.isActiveAndEnabled)
        {
            System.Numerics.Vector2 accepted = path.Points[path.Points.Count - 1];
            feedback.AcceptPoint(new Vector2(accepted.X, accepted.Y));
            if (!path.IsDrawing) feedback.ExhaustInk();
        }

        if (!path.IsDrawing) activeStroke = null;

        return true;
    }

    public void EndStroke()
    {
        if (activeStroke != null && feedback != null && feedback.isActiveAndEnabled) feedback.CompleteStroke();

        if (path != null) path.End();

        activeStroke = null;
    }

    public void ResetDrawing()
    {
        CancelStroke();
        if (feedback != null) feedback.ResetFeedback();
        foreach (BridgeStroke stroke in strokes)
        {
            if (stroke != null)
            {
                stroke.gameObject.SetActive(false);
                Destroy(stroke.gameObject);
            }
        }

        strokes.Clear();
        if (path != null) path.Reset();
    }

    private void Update()
    {
        if (!ValidateReferences()) return;
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            CancelStroke();
            return;
        }

        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (overUi)
        {
            CancelStroke();
            return;
        }

        Vector2 point = default;
        if ((mouse.leftButton.wasPressedThisFrame || mouse.leftButton.isPressed ||
            mouse.leftButton.wasReleasedThisFrame) && !TryScreenToWorld(mouse.position.ReadValue(), out point)) return;

        if (mouse.leftButton.wasPressedThisFrame) BeginStroke(point);

        if (mouse.leftButton.isPressed) AppendPoint(point);

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (path != null && path.IsDrawing) AppendPoint(point);

            EndStroke();
        }
    }

    private void CancelStroke()
    {
        if (path != null) path.End();

        activeStroke = null;
        if (feedback != null) feedback.ClearFeedback();
    }

    private void OnDisable()
    {
        CancelStroke();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) CancelStroke();
    }

    private InkPath CreatePath() { return new InkPath(settings.InkLength, settings.PointSpacing); }

    private bool TryScreenToWorld(Vector2 screenPoint, out Vector2 point)
    {
        point = default;
        if (!ValidateReferences()) return false;
        Ray ray = drawingCamera.ScreenPointToRay(screenPoint);
        if (Mathf.Abs(ray.direction.z) < 0.000001f)
            return Fail($"Рисование моста {name}: луч camera={drawingCamera.name} параллелен плоскости z=0");

        float distance = -ray.origin.z / ray.direction.z;
        if (distance < 0) return Fail($"Рисование моста {name}: плоскость z=0 позади camera={drawingCamera.name}, distance={distance}");

        Vector3 intersection = ray.GetPoint(distance);
        point = new Vector2(intersection.x, intersection.y);

        return true;
    }

    private bool ValidateReferences()
    {
        if (failed) return false;
        if (settings == null || drawingCamera == null || strokePrefab == null || strokesRoot == null)
            return Fail($"Рисование моста {name}: settings={settings}, camera={drawingCamera}, strokePrefab={strokePrefab}, strokesRoot={strokesRoot}");
        if (!settings.Validate()) return Fail(null);

        return true;
    }

    private bool Fail(string error)
    {
        if (!failed && error != null) Debug.LogError(error, this);
        failed = true;
        enabled = false;
        CancelStroke();

        return false;
    }
}
