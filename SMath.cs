namespace SilkyUIFramework;

public static class SMath
{
    public static bool NearlyEqual(this float a, float b, float tolerance = 1e-5f)
    {
        return MathF.Abs(a - b) < tolerance;
    }
}
