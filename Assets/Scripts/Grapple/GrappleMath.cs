using System;

public static class GrappleMath
{
    public static bool IsWithinRange(float distance, float range)
    {
        return IsFinite(distance) && IsFinite(range) && distance > 0f && range > 0f && distance <= range;
    }

    public static bool IsWithinTether(float distance, float maxTetherLength)
    {
        return IsFinite(distance) && IsFinite(maxTetherLength) && distance >= 0f && maxTetherLength > 0f && distance <= maxTetherLength;
    }

    public static void PullVelocity(float velocityX, float velocityY, float offsetX, float offsetY,
        float acceleration, float maxSpeed, float arrivalDistance, float deltaTime,
        out float resultX, out float resultY)
    {
        if (!IsFinite(velocityX) || !IsFinite(velocityY) || !IsFinite(offsetX) || !IsFinite(offsetY))
        {
            throw new ArgumentException($"Grapple velocity ({velocityX}, {velocityY}) and offset ({offsetX}, {offsetY}) must be finite.");
        }
        if (!IsFinite(acceleration) || acceleration < 0f || !IsFinite(maxSpeed) || maxSpeed <= 0f ||
            !IsFinite(arrivalDistance) || arrivalDistance < 0f || !IsFinite(deltaTime) || deltaTime < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(acceleration),
                $"Grapple acceleration={acceleration}, maxSpeed={maxSpeed}, arrivalDistance={arrivalDistance}, deltaTime={deltaTime} are not valid.");
        }

        resultX = velocityX;
        resultY = velocityY;
        double distance = Math.Sqrt((double)offsetX * offsetX + (double)offsetY * offsetY);
        if (distance <= arrivalDistance || deltaTime == 0f) return;

        double step = (double)acceleration * deltaTime / distance;
        double nextX = velocityX + offsetX * step;
        double nextY = velocityY + offsetY * step;
        double speed = Math.Sqrt(nextX * nextX + nextY * nextY);
        double scale = speed > maxSpeed ? maxSpeed / speed : 1d;
        resultX = (float)(nextX * scale);
        resultY = (float)(nextY * scale);
    }

    public static bool IsFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
}
