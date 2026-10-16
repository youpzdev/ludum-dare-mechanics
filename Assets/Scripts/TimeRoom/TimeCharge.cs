using System;
using UnityEngine;

public static class TimeCharge
{
    public static double Advance(double charge, double seconds, double multiplier, bool charging,
        double capacity, double chargeSpeed, double dischargeSpeed)
    {
        if (!RequireNonNegative(seconds, nameof(seconds)) || !RequireNonNegative(multiplier, nameof(multiplier)) ||
            !RequireNonNegative(capacity, nameof(capacity)) || !RequireNonNegative(chargeSpeed, nameof(chargeSpeed)) ||
            !RequireNonNegative(dischargeSpeed, nameof(dischargeSpeed)) || !RequireNonNegative(charge, nameof(charge))) return charge;
        double rate = charging ? chargeSpeed : -dischargeSpeed;

        return Math.Max(0, Math.Min(capacity, charge + rate * seconds * multiplier));
    }

    public static double AdvanceAngle(double angle, double degreesPerSecond, double seconds, double multiplier)
    {
        if (!RequireNonNegative(seconds, nameof(seconds)) || !RequireNonNegative(multiplier, nameof(multiplier)) ||
            !RequireFinite(angle, nameof(angle)) || !RequireFinite(degreesPerSecond, nameof(degreesPerSecond))) return angle;

        return ((angle + degreesPerSecond * seconds * multiplier) % 360 + 360) % 360;
    }

    public static double AdvanceHold(double hold, double startCharge, double seconds,
        double multiplier, double dischargeSpeed, bool docked, double threshold, double requiredHold)
    {
        if (!RequireNonNegative(hold, nameof(hold)) || !RequireNonNegative(startCharge, nameof(startCharge)) ||
            !RequireNonNegative(multiplier, nameof(multiplier)) || !RequireNonNegative(dischargeSpeed, nameof(dischargeSpeed)) ||
            !RequireNonNegative(seconds, nameof(seconds)) || !RequireNonNegative(threshold, nameof(threshold)) ||
            !RequireNonNegative(requiredHold, nameof(requiredHold))) return hold;
        if (requiredHold == 0)
        {
            Debug.LogError("Удержание: requiredHold=0, нужно положительное время");
            return hold;
        }
        if (!docked || startCharge <= threshold) return 0;
        double drain = dischargeSpeed * multiplier;
        double lifetime = drain == 0 ? double.PositiveInfinity : (startCharge - threshold) / drain;
        double remainingHold = Math.Max(0, requiredHold - hold);
        if (remainingHold <= seconds + 1e-12 * requiredHold && remainingHold < lifetime) return requiredHold;

        return seconds >= lifetime ? 0 : hold + seconds;
    }

    private static bool RequireNonNegative(double value, string field)
    {
        if (!RequireFinite(value, field)) return false;
        if (value < 0)
        {
            Debug.LogError($"Время: {field}={value}, нужно неотрицательное значение");
            return false;
        }

        return true;
    }

    private static bool RequireFinite(double value, string field)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            Debug.LogError($"Время: {field}={value}, нужно конечное значение");
            return false;
        }

        return true;
    }
}
