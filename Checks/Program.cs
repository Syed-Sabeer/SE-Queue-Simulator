using Assignment;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
    private static void Near(double actual, double expected, double tolerance, string message) => Check(Math.Abs(actual - expected) <= tolerance, $"{message}: {actual} vs {expected}");
    private static void Reject(Action action)
    {
        try { action(); } catch (ArgumentException) { checks++; return; }
        throw new Exception("Expected invalid input to be rejected.");
    }
    [STAThread]
    private static void Main()
    {
        var mm1 = new ExperimentOptions(QueueModel.MM1, 2, 3, 0, 1, 200000, 42);
        var theory = QueueExperiment.Calculate(mm1);
        Near(theory.L, 2, 1e-12, "M/M/1 L");
        Near(theory.Lq, 4.0 / 3, 1e-12, "M/M/1 Lq");
        Near(theory.W, 1, 1e-12, "M/M/1 W");
        var multi = mm1 with { Model = QueueModel.MMC, Servers = 2 };
        var mt = QueueExperiment.Calculate(multi);
        Near(mt.P0, 0.5, 1e-12, "M/M/2 empty probability");
        Near(mt.ProbabilityWait, 1.0 / 6, 1e-12, "M/M/2 waiting probability");
        Near(mt.Lq, 1.0 / 12, 1e-12, "M/M/2 Lq");
        var deterministic = mm1 with { Model = QueueModel.MG1, ServiceVariance = 0 };
        Near(QueueExperiment.Calculate(deterministic).Wq, 1.0 / 3, 1e-12, "M/D/1 Wq");
        Near(QueueExperiment.Calculate(deterministic with { ServiceVariance = 1.0 / 9 }).Lq, theory.Lq, 1e-12, "M/G/1 exponential moments");
        foreach (var options in new[] { mm1, multi, deterministic, deterministic with { ServiceVariance = 0.05 } })
        {
            var result = QueueExperiment.Run(options);
            var expected = QueueExperiment.Calculate(options);
            Check(result.Arrived == result.Served + result.Waiting + result.Busy, "Customer conservation");
            Check(result.Utilization >= 0 && result.Utilization <= 1, "Utilization bounds");
            Check(result.MaxQueue >= result.Waiting, "Maximum queue bounds");
            Near(result.Lq, expected.Lq, Math.Max(0.025, expected.Lq * 0.06), "Simulated Lq");
            Near(result.Wq, expected.Wq, Math.Max(0.015, expected.Wq * 0.06), "Simulated Wq");
            Check(result == QueueExperiment.Run(options), "Seed reproducibility");
        }
        var empty = QueueExperiment.Run(mm1 with { Duration = 1e-12 });
        Check(empty.Arrived == 0 && empty.Served == 0 && empty.L == 0 && empty.P0 == 1, "Empty observation interval");
        var scaled = QueueExperiment.Run(mm1 with { ArrivalRate = 120, ServiceRate = 180, Duration = mm1.Duration / 60 });
        var original = QueueExperiment.Run(mm1);
        Check(scaled.Arrived == original.Arrived && scaled.Served == original.Served, "Unit scaling counts");
        Near(scaled.W * 60, original.W, 1e-6, "Unit scaling wait");
        Reject(() => QueueExperiment.Calculate(mm1 with { ArrivalRate = 3 }));
        Reject(() => QueueExperiment.Run(mm1 with { Duration = double.NaN }));
        Reject(() => QueueExperiment.Run(mm1 with { ServiceRate = double.PositiveInfinity }));
        Reject(() => QueueExperiment.Run(multi with { Servers = 0 }));
        Reject(() => QueueExperiment.Run(deterministic with { ServiceVariance = -1 }));
        Reject(() => QueueExperiment.Run(mm1 with { Duration = 10_000_000 }));
        Check(!InputHelper.TryParseFlexibleDouble("NaN", out _), "Reject NaN");
        Check(!InputHelper.TryParseFlexibleDouble("Infinity", out _), "Reject infinity");
        Check(!InputHelper.TryParseFlexibleDouble("1/0", out _), "Reject zero denominator");
        Check(InputHelper.TryParseFlexibleDouble("1:8", out double fraction) && fraction == 0.125, "Ratio support");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        try { QueueExperiment.Run(mm1, cancellation.Token); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { checks++; }
        using var form = new MainForm();
        Check(form.Controls.Count > 0, "Form construction");
        var tabs = form.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<TabControl>().Single();
        Check(tabs.TabPages.Count == 3, "Existing screens and new simulation tab");
        Console.WriteLine($"Passed {checks} checks.");
    }
}
