using System;

public static class TimeCharge
{
    public static double Advance(double charge, double seconds, double multiplier, bool charging,
        double capacity, double chargeSpeed, double dischargeSpeed)
    {
        RequireNonNegative(seconds, nameof(seconds));
        RequireNonNegative(multiplier, nameof(multiplier));
        RequireNonNegative(capacity, nameof(capacity));
        RequireNonNegative(chargeSpeed, nameof(chargeSpeed));
        RequireNonNegative(dischargeSpeed, nameof(dischargeSpeed));
        RequireNonNegative(charge, nameof(charge));
        double rate = charging ? chargeSpeed : -dischargeSpeed;

        return Math.Max(0, Math.Min(capacity, charge + rate * seconds * multiplier));
    }

    public static double AdvanceAngle(double angle, double degreesPerSecond, double seconds, double multiplier)
    {
        RequireNonNegative(seconds, nameof(seconds));
        RequireNonNegative(multiplier, nameof(multiplier));
        if (double.IsNaN(angle) || double.IsInfinity(angle))
            throw new ArgumentOutOfRangeException(nameof(angle), angle, "Angle must be finite.");
        if (double.IsNaN(degreesPerSecond) || double.IsInfinity(degreesPerSecond))
            throw new ArgumentOutOfRangeException(nameof(degreesPerSecond), degreesPerSecond, "Rotation speed must be finite.");

        return ((angle + degreesPerSecond * seconds * multiplier) % 360 + 360) % 360;
    }

    public static double AdvanceHold(double hold, double startCharge, double seconds,
        double multiplier, double dischargeSpeed, bool docked, double threshold, double requiredHold)
    {
        RequireNonNegative(hold, nameof(hold));
        RequireNonNegative(startCharge, nameof(startCharge));
        RequireNonNegative(multiplier, nameof(multiplier));
        RequireNonNegative(dischargeSpeed, nameof(dischargeSpeed));
        RequireNonNegative(seconds, nameof(seconds));
        RequireNonNegative(threshold, nameof(threshold));
        RequireNonNegative(requiredHold, nameof(requiredHold));
        if (requiredHold == 0)
            throw new ArgumentOutOfRangeException(nameof(requiredHold), requiredHold, "Required hold must be positive.");
        if (!docked || startCharge <= threshold)
            return 0;
        double drain = dischargeSpeed * multiplier;
        double lifetime = drain == 0 ? double.PositiveInfinity : (startCharge - threshold) / drain;
        double remainingHold = Math.Max(0, requiredHold - hold);
        if (remainingHold <= seconds + 1e-12 * requiredHold && remainingHold < lifetime)
            return requiredHold;

        return seconds >= lifetime ? 0 : hold + seconds;
    }

    private static void RequireNonNegative(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            throw new ArgumentOutOfRangeException(name, value, "Value must be finite and non-negative.");
    }
}
