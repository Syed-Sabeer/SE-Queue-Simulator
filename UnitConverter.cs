namespace Assignment;

public enum TimeUnit { Minutes, Hours, Seconds }
public enum ParameterInput { Rate, MeanTime }
public enum VarianceInput { Variance, StandardDeviation, UniformBounds }
public enum CalculatorModel { MM1, MG1, GG1, MG2, MMS }

public static class UnitConverter
{
    public static double MinutesPerUnit(TimeUnit unit) => unit switch
    {
        TimeUnit.Minutes => 1, TimeUnit.Hours => 60, TimeUnit.Seconds => 1.0 / 60,
        _ => throw new ArgumentException("Unknown time unit.")
    };
    public static double ToRate(double value, ParameterInput input, TimeUnit unit)
    {
        if (!double.IsFinite(value) || value <= 0) throw new ArgumentException("Rate or mean time must be finite and greater than zero.");
        double rate = input == ParameterInput.Rate ? value / MinutesPerUnit(unit) : 1 / (value * MinutesPerUnit(unit));
        if (!double.IsFinite(rate) || rate <= 0) throw new ArgumentException("Input is outside the supported numeric range.");
        return rate;
    }
    public static double ToVariance(double value, double maximum, VarianceInput input, TimeUnit unit)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentException("Variance, deviation or minimum must be finite and non-negative.");
        if (input == VarianceInput.UniformBounds && (!double.IsFinite(maximum) || maximum < value))
            throw new ArgumentException("Uniform maximum must be finite and at least the minimum.");
        double factor = MinutesPerUnit(unit);
        double converted = input switch
        {
            VarianceInput.Variance => value * factor * factor,
            VarianceInput.StandardDeviation => Math.Pow(value * factor, 2),
            VarianceInput.UniformBounds => Math.Pow((maximum - value) * factor, 2) / 12,
            _ => throw new ArgumentException("Unknown variance method.")
        };
        if (!double.IsFinite(converted)) throw new ArgumentException("Variance is outside the supported numeric range.");
        return converted;
    }
}

public sealed record QueueInputs(CalculatorModel Model, double Lambda, double Mu,
    double ArrivalVariance = 0, double ServiceVariance = 0, int Servers = 1);

public sealed record QueueResults(double Rho, double L, double Lq, double W, double Wq,
    double? P0, double CaSquared, double CsSquared, string Method)
{
    public double IdleFraction => 1 - Rho;
}

public static class QueueCalculator
{
    public static QueueResults Calculate(QueueInputs input)
    {
        if (!Enum.IsDefined(input.Model)) throw new ArgumentException("Select a supported model.");
        int servers = input.Model == CalculatorModel.MG2 ? 2 : input.Model == CalculatorModel.MMS ? input.Servers : 1;
        if (servers < 1) throw new ArgumentException("Server count must be at least one.");
        if (!double.IsFinite(input.Lambda) || input.Lambda <= 0 || !double.IsFinite(input.Mu) || input.Mu <= 0 ||
            !double.IsFinite(input.ArrivalVariance) || input.ArrivalVariance < 0 || !double.IsFinite(input.ServiceVariance) || input.ServiceVariance < 0)
            throw new ArgumentException("Rates must be finite and positive; variances must be finite and non-negative.");
        double rho = input.Lambda / input.Mu / servers;
        if (rho >= 1) throw new ArgumentException("Unstable system: lambda must be less than servers × mu (rho < 1).");
        double ca = input.Model == CalculatorModel.GG1 ? input.ArrivalVariance * input.Lambda * input.Lambda : 1;
        double cs = input.Model is CalculatorModel.MM1 or CalculatorModel.MMS ? 1 : input.ServiceVariance * input.Mu * input.Mu;
        double wq;
        double? p0;
        string method;
        if (input.Model == CalculatorModel.GG1)
        {
            double lq = GG1.QueueLength(rho, ca, cs);
            wq = lq / input.Lambda; p0 = 1 - rho;
            method = "Marchal approximation (L, Lq, W and Wq are approximate).";
        }
        else if (input.Model == CalculatorModel.MG1)
        {
            wq = rho * (1 + cs) / (2 * input.Mu * (1 - rho));
            p0 = 1 - rho; method = "Pollaczek–Khinchine (exact steady-state moments).";
        }
        else
        {
            var erlang = MMS.Calculate(input.Lambda, input.Mu, servers);
            wq = erlang.Wq; p0 = erlang.P0;
            method = "Erlang C (exact steady-state M/M/S).";
            if (input.Model == CalculatorModel.MG2)
            {
                wq = MG2.WaitingTime(wq, cs);
                p0 = null;
                method = "M/M/2 waiting time × (1 + Cs²) / 2 (approximation). P0 is not determined by these moments.";
            }
        }
        double w = wq + 1 / input.Mu;
        var result = new QueueResults(rho, input.Lambda * w, input.Lambda * wq, w, wq, p0, ca, cs, method);
        if (new[] { result.Rho, result.L, result.Lq, result.W, result.Wq, ca, cs }.Any(x => !double.IsFinite(x)))
            throw new ArgumentException("Inputs produce results outside the supported numeric range.");
        return result;
    }
}
