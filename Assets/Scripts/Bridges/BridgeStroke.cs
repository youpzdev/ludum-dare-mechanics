using System;
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

    public void Configure(LineRenderer lineRenderer, EdgeCollider2D edgeCollider)
    {
        if (lineRenderer == null || edgeCollider == null)
        {
            throw new ArgumentException("BridgeStroke requires LineRenderer and EdgeCollider2D references on " + name);
        }

        if (lineRenderer.gameObject != gameObject || edgeCollider.gameObject != gameObject)
        {
            throw new ArgumentException("BridgeStroke renderer and collider must belong to " + name);
        }

        line = lineRenderer;
        edge = edgeCollider;
    }

    public void SetPoints(IReadOnlyList<System.Numerics.Vector2> points, float thickness)
    {
        if (line == null || edge == null) throw new InvalidOperationException("BridgeStroke references are unassigned on " + name);

        if (points == null || points.Count < 2)
        {
            throw new ArgumentException("BridgeStroke requires at least 2 points, received " + (points == null ? 0 : points.Count), nameof(points));
        }

        if (float.IsNaN(thickness) || float.IsInfinity(thickness) || thickness <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thickness), thickness, "Bridge thickness must be finite and positive");
        }

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
        {
            throw new InvalidOperationException("BridgeStroke failed to set collider points on " + name + "; count=" + colliderPoints.Count);
        }
        edge.isTrigger = false;
        edge.enabled = true;
    }
}
