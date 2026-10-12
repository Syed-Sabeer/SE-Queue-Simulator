namespace Assignment;

public static class MG2
{
    // Report's variability-adjusted M/M/2 approximation; Ca² = 1.
    internal static double WaitingTime(double mm2WaitingTime, double csSquared) => mm2WaitingTime * (1 + csSquared) / 2;
}
