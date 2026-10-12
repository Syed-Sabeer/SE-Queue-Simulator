namespace Assignment;

public static class GG1
{
    // Marchal (1976); returns approximate mean waiting customers.
    // See README for the formula reference and limitations.
    internal static double QueueLength(double rho, double caSquared, double csSquared)
    {
        double r2cs = rho * rho * csSquared;
        return rho * rho * (1 + csSquared) * (caSquared + r2cs) /
            (2 * (1 - rho) * (1 + r2cs));
    }
}
