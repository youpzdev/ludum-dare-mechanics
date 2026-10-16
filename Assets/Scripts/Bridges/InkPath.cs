using System;
using System.Collections.Generic;
using System.Numerics;

public class InkPath
{
    private readonly List<Vector2> points = new List<Vector2>();
    private readonly float inkLength;
    private readonly float pointSpacing;

    private double remainingInk; // остаток лучше лишний раз не округлять
    private Vector2 startPoint;

    private bool drawing;

    public InkPath(float inkLength, float pointSpacing)
    {
        RequirePositive(inkLength, nameof(inkLength));
        RequirePositive(pointSpacing, nameof(pointSpacing));
        if (pointSpacing > inkLength)
        {
            throw new ArgumentOutOfRangeException(nameof(pointSpacing), pointSpacing, "Point spacing must not exceed ink length " + inkLength);
        }

        this.inkLength = inkLength;
        this.pointSpacing = pointSpacing;
        remainingInk = inkLength;
    }

    public IReadOnlyList<Vector2> Points => points;
    public float RemainingInk => (float)remainingInk;
    public bool IsDrawing => drawing;

    public bool Begin(Vector2 point)
    {
        RequireFinite(point);
        End();
        points.Clear();
        if (remainingInk < pointSpacing) return false;

        startPoint = point;
        drawing = true;

        return true;
    }

    public bool Append(Vector2 point)
    {
        RequireFinite(point);
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

    private static void RequireFinite(Vector2 point)
    {
        if (float.IsNaN(point.X) || float.IsInfinity(point.X) || float.IsNaN(point.Y) || float.IsInfinity(point.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(point), point, "Ink point coordinates must be finite");
        }
    }

    private static void RequirePositive(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, value, "Ink setting must be finite and positive");
        }
    }
}
