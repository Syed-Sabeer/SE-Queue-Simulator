using System;

namespace Assignment;

public readonly record struct SimulationResult(double L, double Lq, double W, double Wq, double Utilization);

public static class QueueSimulator
{
    public static SimulationResult SimulateMM1(double lambda, double mu, int customerCount, int warmupCount = 1000, int? seed = null)
    {

        ValidateInputs(lambda, mu, customerCount, warmupCount);

        return SimulateSingleServer(
            rng => SampleExponential(rng, lambda),
            rng => SampleExponential(rng, mu),
            customerCount,
            warmupCount,
            seed);
    }

    public static SimulationResult SimulateMG1(double interArrivalMean, double serviceMean, double serviceVariance, int customerCount, int warmupCount = 1000, int? seed = null)
    {
        if (interArrivalMean <= 0 || serviceMean <= 0)
            throw new ArgumentException("Means must be greater than 0.");

        if (serviceVariance < 0)
            throw new ArgumentException("Service variance must be 0 or greater.");

        double lambda = 1.0 / interArrivalMean;
        double mu = 1.0 / serviceMean;

        ValidateInputs(lambda, mu, customerCount, warmupCount);

        return SimulateSingleServer(
            rng => SampleExponential(rng, lambda),
            rng => SampleGammaByMoments(rng, serviceMean, serviceVariance),
            customerCount,
            warmupCount,
            seed);
    }

    public static SimulationResult SimulateGG1(double interArrivalMean, double interArrivalVariance, double serviceMean, double serviceVariance, int customerCount, int warmupCount = 1000, int? seed = null)
    {
        if (interArrivalMean <= 0 || serviceMean <= 0)
            throw new ArgumentException("Means must be greater than 0.");

        if (interArrivalVariance < 0 || serviceVariance < 0)
            throw new ArgumentException("Variances must be 0 or greater.");

        ValidateInputs(1.0 / interArrivalMean, 1.0 / serviceMean, customerCount, warmupCount);

        return SimulateSingleServer(
            rng => SampleGammaByMoments(rng, interArrivalMean, interArrivalVariance),
            rng => SampleGammaByMoments(rng, serviceMean, serviceVariance),
            customerCount,
            warmupCount,
            seed);
    }

    private static void ValidateInputs(double lambda, double mu, int customerCount, int warmupCount)
    {
        if (lambda <= 0 || mu <= 0)
            throw new ArgumentException("Rates must be greater than 0.");

        if (lambda >= mu)
            throw new ArgumentException("System is unstable for simulation (arrival rate must be less than service rate).");

        if (customerCount <= 0)
            throw new ArgumentException("Customer count must be greater than 0.");

        if (warmupCount < 0)
            throw new ArgumentException("Warmup count cannot be negative.");
    }

    private static SimulationResult SimulateSingleServer(Func<Random, double> interArrivalSampler, Func<Random, double> serviceSampler, int customerCount, int warmupCount, int? seed)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        int totalCustomers = customerCount + warmupCount;
        double arrivalTime = 0;
        double serverFreeTime = 0;

        double firstKeptArrival = 0;
        double lastKeptDeparture = 0;
        bool started = false;

        double sumWq = 0;
        double sumW = 0;
        double sumService = 0;

        for (int i = 0; i < totalCustomers; i++)
        {
            arrivalTime += interArrivalSampler(rng);
            double serviceTime = serviceSampler(rng);

            double serviceStart = Math.Max(arrivalTime, serverFreeTime);
            double waitInQueue = serviceStart - arrivalTime;
            double departureTime = serviceStart + serviceTime;

            serverFreeTime = departureTime;

            if (i < warmupCount)
                continue;

            if (!started)
            {
                firstKeptArrival = arrivalTime;
                started = true;
            }

            lastKeptDeparture = departureTime;
            sumWq += waitInQueue;
            sumW += (departureTime - arrivalTime);
            sumService += serviceTime;
        }

        double observationWindow = Math.Max(lastKeptDeparture - firstKeptArrival, double.Epsilon);
        double lambdaEffective = customerCount / observationWindow;

        double wq = sumWq / customerCount;
        double w = sumW / customerCount;
        double lq = lambdaEffective * wq;
        double l = lambdaEffective * w;
        double utilization = sumService / observationWindow;

        return new SimulationResult(l, lq, w, wq, utilization);
    }

    private static double SampleExponential(Random rng, double rate)
    {
        double u = 1.0 - rng.NextDouble();
        return -Math.Log(u) / rate;
    }

    internal static double SampleGammaByMoments(Random rng, double mean, double variance)
    {
        if (variance == 0)
            return mean;

        double shape = (mean * mean) / variance;
        double scale = variance / mean;
        return SampleGamma(rng, shape) * scale;
    }

    private static double SampleGamma(Random rng, double shape)
    {
        if (shape < 1)
        {
            double u = rng.NextDouble();
            return SampleGamma(rng, shape + 1) * Math.Pow(u, 1.0 / shape);
        }

        double d = shape - (1.0 / 3.0);
        double c = 1.0 / Math.Sqrt(9.0 * d);

        while (true)
        {
            double x = SampleStandardNormal(rng);
            double v = 1.0 + c * x;

            if (v <= 0)
                continue;

            v = v * v * v;
            double u = rng.NextDouble();

            if (u < 1.0 - 0.0331 * x * x * x * x)
                return d * v;

            if (Math.Log(u) < 0.5 * x * x + d * (1.0 - v + Math.Log(v)))
                return d * v;
        }
    }

    private static double SampleStandardNormal(Random rng)
    {
        double u1 = Math.Max(rng.NextDouble(), double.Epsilon);
        double u2 = rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
