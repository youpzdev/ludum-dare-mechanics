using System;
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

    public float RemainingInk => path == null ? GetSettings().InkLength : path.RemainingInk;
    public int StrokeCount => strokes.Count;

    public void Configure(Camera camera, BridgeSettings bridgeSettings, BridgeStroke prefab, Transform root)
    {
        if (camera == null || bridgeSettings == null || prefab == null || root == null)
        {
            throw new ArgumentException("BridgeDrawing requires camera, settings, stroke prefab and strokes root on " + name);
        }

        ResetDrawing();
        drawingCamera = camera;
        settings = bridgeSettings;
        strokePrefab = prefab;
        strokesRoot = root;
        path = CreatePath();
    }

    public void ConfigureFeedback(BridgeFeedback bridgeFeedback)
    {
        if (bridgeFeedback == null) throw new ArgumentNullException(nameof(bridgeFeedback), "BridgeDrawing feedback is unassigned on " + name);

        CancelStroke();
        feedback = bridgeFeedback;
        feedback.ResetFeedback();
    }

    public bool BeginStroke(Vector2 point)
    {
        ValidateReferences();
        EndStroke();
        if (path == null) path = CreatePath();

        bool began = path.Begin(new System.Numerics.Vector2(point.x, point.y));
        if (began && feedback != null && feedback.isActiveAndEnabled) feedback.BeginStroke();

        return began;
    }

    public bool AppendPoint(Vector2 point)
    {
        if (path == null || !path.Append(new System.Numerics.Vector2(point.x, point.y))) return false;

        if (activeStroke == null)
        {
            activeStroke = Instantiate(strokePrefab, strokesRoot);
            strokes.Add(activeStroke);
        }

        activeStroke.SetPoints(path.Points, settings.Thickness);
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

        if (mouse.leftButton.wasPressedThisFrame) BeginStroke(ScreenToWorld(mouse.position.ReadValue()));

        if (mouse.leftButton.isPressed) AppendPoint(ScreenToWorld(mouse.position.ReadValue()));

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (path != null && path.IsDrawing) AppendPoint(ScreenToWorld(mouse.position.ReadValue()));

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

    private Vector2 ScreenToWorld(Vector2 screenPoint)
    {
        ValidateReferences();
        Ray ray = drawingCamera.ScreenPointToRay(screenPoint);
        if (Mathf.Abs(ray.direction.z) < 0.000001f)
        {
            throw new InvalidOperationException("BridgeDrawing camera ray cannot intersect z=0 on " + name);
        }

        float distance = -ray.origin.z / ray.direction.z;
        if (distance < 0) throw new InvalidOperationException("BridgeDrawing z=0 plane is behind camera " + drawingCamera.name);

        Vector3 point = ray.GetPoint(distance);

        return new Vector2(point.x, point.y);
    }

    private BridgeSettings GetSettings()
    {
        if (settings == null) throw new InvalidOperationException("BridgeDrawing settings are unassigned on " + name);

        return settings;
    }

    private void ValidateReferences()
    {
        GetSettings();
        if (drawingCamera == null || strokePrefab == null || strokesRoot == null)
        {
            throw new InvalidOperationException("BridgeDrawing camera, stroke prefab or strokes root is unassigned on " + name);
        }
    }
}
