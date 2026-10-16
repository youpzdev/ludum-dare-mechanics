using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(EdgeCollider2D))]
public class BridgeStroke : MonoBehaviour
{
    [Header("Stroke Components")]
    [Tooltip("Must belong to the same GameObject as this stroke.")]
    [SerializeField] private LineRenderer line;
    [Tooltip("Must belong to the same GameObject as this stroke.")]
    [SerializeField] private EdgeCollider2D edge;

    private readonly List<Vector2> colliderPoints = new List<Vector2>();
    private readonly AnimationCurve widthCurve = AnimationCurve.Constant(0, 1, 1);

    private bool failed;

    public void Configure(LineRenderer lineRenderer, EdgeCollider2D edgeCollider)
    {
        if (lineRenderer == null || edgeCollider == null)
        {
            Fail($"Линия моста {name}: lineRenderer={lineRenderer}, edgeCollider={edgeCollider}");
            return;
        }

        if (lineRenderer.gameObject != gameObject || edgeCollider.gameObject != gameObject)
        {
            Fail($"Линия моста {name}: lineRenderer={lineRenderer.name} и edgeCollider={edgeCollider.name} должны быть на том же объекте");
            return;
        }

        line = lineRenderer;
        edge = edgeCollider;
        if (failed) { failed = false; enabled = true; }
    }

    public bool SetPoints(IReadOnlyList<System.Numerics.Vector2> points, float thickness)
    {
        if (failed) return false;
        if (line == null || edge == null) return Fail($"Линия моста {name}: line={line}, edge={edge}");

        if (points == null || points.Count < 2)
            return Fail($"Линия моста {name}: points.Count={points?.Count ?? 0}, нужно хотя бы 2 точки");

        if (float.IsNaN(thickness) || float.IsInfinity(thickness) || thickness <= 0)
            return Fail($"Линия моста {name}: thickness={thickness}, нужна конечная положительная толщина");

        for (int i = 0; i < points.Count; i++)
            if (float.IsNaN(points[i].X) || float.IsInfinity(points[i].X) || float.IsNaN(points[i].Y) || float.IsInfinity(points[i].Y))
                return Fail($"Линия моста {name}: points[{i}]={points[i]}");

        line.useWorldSpace = false;
        line.widthMultiplier = thickness;
        line.widthCurve = widthCurve;
        line.positionCount = points.Count;
        colliderPoints.Clear();
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 position = transform.InverseTransformPoint(new Vector3(points[i].X, points[i].Y, 0));
            line.SetPosition(i, position);
            colliderPoints.Add(position);
        }

        edge.offset = Vector2.zero;
        edge.edgeRadius = thickness * 0.5f;
        if (!edge.SetPoints(colliderPoints))
            return Fail($"Линия моста {name}: коллайдер отклонил points.Count={colliderPoints.Count}");
        edge.isTrigger = false;
        edge.enabled = true;

        return true;
    }

    private bool Fail(string error)
    {
        if (!failed) Debug.LogError(error, this);
        failed = true;
        enabled = false;
        if (edge != null && edge.gameObject == gameObject) edge.enabled = false;

        return false;
    }
}
