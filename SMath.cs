namespace SilkyUIFramework;

public static class SMath
{
    public static bool NearlyEqual(this float a, float b, float tolerance = 1e-5f)
    {
        return Math.Abs(a - b) < tolerance;
    }

    public static bool NearlyEqual(this double a, double b, double tolerance = 1e-5f)
    {
        return Math.Abs(a - b) < tolerance;
    }
}
