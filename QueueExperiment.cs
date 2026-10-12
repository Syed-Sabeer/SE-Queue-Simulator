namespace Assignment;

public enum QueueModel { MM1, MG1, MMC }

public sealed record ExperimentOptions(QueueModel Model, double ArrivalRate, double ServiceRate,
    double ServiceVariance, int Servers, double Duration, int? Seed)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Model)) throw new ArgumentException("Select a supported queue model.");
        if (!double.IsFinite(ArrivalRate) || ArrivalRate <= 0 ||
            !double.IsFinite(ServiceRate) || ServiceRate <= 0 ||
            !double.IsFinite(Duration) || Duration <= 0 ||
            !double.IsFinite(ServiceVariance) || ServiceVariance < 0)
            throw new ArgumentException("Rates and duration must be finite and positive; variance must be finite and non-negative.");
        if (Servers < 1 || Servers > 100 || (Model != QueueModel.MMC && Servers != 1))
            throw new ArgumentException("Use one server for M/M/1 or M/G/1, or 1–100 servers for M/M/c.");
        if (ArrivalRate / Servers >= ServiceRate)
            throw new ArgumentException("The system is unstable: arrival rate must be below total service capacity.");
        if (!double.IsFinite(ArrivalRate * Duration) || ArrivalRate * Duration > 5_000_000)
            throw new ArgumentException("Reduce arrival rate or duration to at most 5,000,000 expected arrivals.");
        double mean = 1 / ServiceRate;
        if (!double.IsFinite(mean) || mean <= 0 || !double.IsFinite(1 / ArrivalRate))
            throw new ArgumentException("Rates are outside the supported numeric range.");
        if (Model == QueueModel.MG1 && ServiceVariance > 0 &&
            (!double.IsFinite(mean * mean / ServiceVariance) || mean * mean / ServiceVariance <= 0 ||
             !double.IsFinite(ServiceVariance / mean)))
            throw new ArgumentException("Service moments are outside the supported numeric range.");
    }
}

public sealed record QueueTheory(double L, double Lq, double W, double Wq, double Utilization, double P0, double ProbabilityWait);
public sealed record ExperimentResult(long Arrived, long Served, int Waiting, int Busy, int MaxQueue,
    double L, double Lq, double W, double Wq, double Utilization, double P0);

public static class QueueExperiment
{
    public static QueueTheory Calculate(ExperimentOptions options)
    {
        options.Validate();
        double a = options.ArrivalRate / options.ServiceRate;
        double rho = a / options.Servers;
        double p0, pw, wq;
        if (options.Model == QueueModel.MG1)
        {
            p0 = 1 - rho;
            pw = rho;
            wq = options.ArrivalRate * (options.ServiceVariance + 1 / options.ServiceRate / options.ServiceRate) / (2 * (1 - rho));
        }
        else
        {
            // Erlang B recursion avoids factorial overflow; convert to Erlang C.
            double b = 1;
            for (int k = 1; k <= options.Servers; k++) b = a * b / (k + a * b);
            pw = b / (1 - rho + rho * b);
            double term = 1, sum = 1;
            for (int k = 1; k < options.Servers; k++) { term *= a / k; sum += term; }
            term *= a / options.Servers;
            p0 = 1 / (sum + term / (1 - rho));
            wq = pw / (options.Servers * options.ServiceRate - options.ArrivalRate);
        }
        double w = wq + 1 / options.ServiceRate;
        var result = new QueueTheory(options.ArrivalRate * w, options.ArrivalRate * wq, w, wq, rho, p0, pw);
        if (!double.IsFinite(result.L) || !double.IsFinite(result.W))
            throw new ArgumentException("Inputs produce results outside the supported numeric range.");
        return result;
    }

    public static ExperimentResult Run(ExperimentOptions options, CancellationToken cancellationToken = default)
    {
        options.Validate();
        var random = options.Seed is int seed ? new Random(seed) : new Random();
        var waiting = new Queue<double>();
        var departures = new PriorityQueue<(double Arrival, double Start), double>();
        double clock = 0, nextArrival = Exponential(random, options.ArrivalRate);
        double queueArea = 0, systemArea = 0, busyArea = 0, emptyTime = 0, sumW = 0, sumWq = 0;
        long arrived = 0, served = 0, events = 0;
        int maxQueue = 0;

        void StartService(double arrival)
        {
            double service = options.Model == QueueModel.MG1
                ? QueueSimulator.SampleGammaByMoments(random, 1 / options.ServiceRate, options.ServiceVariance)
                : Exponential(random, options.ServiceRate);
            if (!double.IsFinite(service) || service < 0 || !double.IsFinite(clock + service))
                throw new ArgumentException("Service time exceeded the supported numeric range.");
            departures.Enqueue((arrival, clock), clock + service);
        }

        while (clock < options.Duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (++events > 12_000_000) throw new ArgumentException("Simulation event limit reached. Reduce duration or arrival rate.");
            double departure = departures.TryPeek(out _, out double time) ? time : double.PositiveInfinity;
            double next = Math.Min(options.Duration, Math.Min(nextArrival, departure));
            double elapsed = next - clock;
            queueArea += waiting.Count * elapsed;
            systemArea += (waiting.Count + departures.Count) * elapsed;
            busyArea += departures.Count * elapsed;
            if (waiting.Count + departures.Count == 0) emptyTime += elapsed;
            clock = next;
            if (clock >= options.Duration) break;
            if (departure <= nextArrival)
            {
                var customer = departures.Dequeue();
                served++;
                sumW += clock - customer.Arrival;
                sumWq += customer.Start - customer.Arrival;
                if (waiting.Count > 0) StartService(waiting.Dequeue());
            }
            else
            {
                arrived++;
                if (departures.Count < options.Servers) StartService(clock);
                else { waiting.Enqueue(clock); maxQueue = Math.Max(maxQueue, waiting.Count); }
                nextArrival = clock + Exponential(random, options.ArrivalRate);
                if (nextArrival <= clock) throw new ArgumentException("Time increments are too small for this simulation duration.");
            }
        }
        return new(arrived, served, waiting.Count, departures.Count, maxQueue,
            systemArea / options.Duration, queueArea / options.Duration,
            served == 0 ? 0 : sumW / served, served == 0 ? 0 : sumWq / served,
            busyArea / options.Duration / options.Servers, emptyTime / options.Duration);
    }

    private static double Exponential(Random random, double rate) => -Math.Log(1 - random.NextDouble()) / rate;
}
