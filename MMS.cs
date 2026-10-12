namespace Assignment;

public static class MMS
{
    public static (double Wq, double P0) Calculate(double lambda, double mu, int servers)
    {
        if (!double.IsFinite(lambda) || lambda <= 0 || !double.IsFinite(mu) || mu <= 0 || servers < 1)
            throw new ArgumentException("Rates and server count must be positive.");
        double a = lambda / mu, rho = a / servers;
        if (rho >= 1) throw new ArgumentException("System is unstable.");
        // Sum the normalizer in log space to avoid powers and factorial overflow.
        double logTerm = 0, logSum = 0, b = 1;
        for (int k = 1; k <= servers; k++)
        {
            logTerm += Math.Log(a) - Math.Log(k);
            b = a * b / (k + a * b);
            if (k < servers) logSum = LogAdd(logSum, logTerm);
        }
        logSum = LogAdd(logSum, logTerm - Math.Log(1 - rho));
        double pw = b / (1 - rho + rho * b);
        return (pw / (servers * mu - lambda), Math.Exp(-logSum));
    }
    private static double LogAdd(double a, double b)
    {
        double max = Math.Max(a, b);
        return max + Math.Log(Math.Exp(a - max) + Math.Exp(b - max));
    }
}
