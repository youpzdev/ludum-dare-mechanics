using System;
using System.Collections.Generic;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

public class InkPath
{
    private readonly List<Vector2> points = new List<Vector2>();
    private readonly float inkLength;
    private readonly float pointSpacing;
    private readonly bool ready;

    private double remainingInk; // остаток лучше лишний раз не округлять
    private Vector2 startPoint;

    private bool drawing;

    public InkPath(float inkLength, float pointSpacing)
    {
        if (!RequirePositive(inkLength, nameof(inkLength)) || !RequirePositive(pointSpacing, nameof(pointSpacing))) return;
        if (pointSpacing > inkLength)
        {
            Debug.LogError($"Чернила: pointSpacing={pointSpacing} больше inkLength={inkLength}");
            return;
        }

        this.inkLength = inkLength;
        this.pointSpacing = pointSpacing;
        remainingInk = inkLength;
        ready = true;
    }

    public IReadOnlyList<Vector2> Points => points;
    public float RemainingInk => (float)remainingInk;
    public bool IsDrawing => drawing;

    public bool Begin(Vector2 point)
    {
        if (!ready || !RequireFinite(point)) return false;
        End();
        points.Clear();
        if (remainingInk < pointSpacing) return false;

        startPoint = point;
        drawing = true;

        return true;
    }

    public bool Append(Vector2 point)
    {
        if (!ready || !RequireFinite(point)) return false;
        if (!drawing) return false;

        Vector2 previous = points.Count == 0 ? startPoint : points[points.Count - 1];
        double dx = (double)point.X - previous.X;
        double dy = (double)point.Y - previous.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance == 0 || distance < pointSpacing && (points.Count == 0 || distance < remainingInk)) return false;

        bool exhausted = distance >= remainingInk;
        double acceptedLength = Math.Min(distance, remainingInk);
        Vector2 accepted = exhausted
            ? new Vector2((float)(previous.X + dx * acceptedLength / distance), (float)(previous.Y + dy * acceptedLength / distance))
            : point;
        if (points.Count == 0) points.Add(startPoint);

        points.Add(accepted);
        remainingInk = exhausted ? 0 : remainingInk - acceptedLength;
        if (exhausted) drawing = false;

        return true;
    }

    public void End() { drawing = false; }

    public void Reset()
    {
        End();
        points.Clear();
        remainingInk = inkLength;
    }

    private static bool RequireFinite(Vector2 point)
    {
        if (float.IsNaN(point.X) || float.IsInfinity(point.X) || float.IsNaN(point.Y) || float.IsInfinity(point.Y))
        {
            Debug.LogError($"Чернила: некорректная точка point={point}");
            return false;
        }

        return true;
    }

    private static bool RequirePositive(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
        {
            Debug.LogError($"Чернила: {name}={value}, нужно конечное положительное значение");
            return false;
        }

        return true;
    }
}
