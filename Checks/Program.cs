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
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
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
        if (args.Contains("--preview")) { form.Size = new Size(1250, 950); form.Show(); Application.DoEvents(); }
        Check(form.Controls.Count > 0, "Form construction");
        var tabs = form.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<TabControl>().Single();
        Check(tabs.TabPages.Count == 4, "Calculator and existing screens");
        Near(UnitConverter.ToRate(30, ParameterInput.Rate, TimeUnit.Hours), 0.5, 1e-12, "Hourly arrival rate");
        Near(UnitConverter.ToRate(120, ParameterInput.MeanTime, TimeUnit.Seconds), 0.5, 1e-12, "Seconds mean time");
        Near(UnitConverter.ToVariance(2, 0, VarianceInput.StandardDeviation, TimeUnit.Hours), 14400, 1e-9, "Deviation squared and converted");
        Near(UnitConverter.ToVariance(0.05, 0, VarianceInput.Variance, TimeUnit.Hours), 180, 1e-9, "Variance squared-unit conversion");
        Near(UnitConverter.ToVariance(60, 180, VarianceInput.UniformBounds, TimeUnit.Seconds), 1.0 / 3, 1e-12, "Uniform variance");
        Reject(() => UnitConverter.ToVariance(2, 1, VarianceInput.UniformBounds, TimeUnit.Minutes));
        Reject(() => UnitConverter.ToRate(0, ParameterInput.MeanTime, TimeUnit.Minutes));
        var baseInput = new QueueInputs(CalculatorModel.MM1, 2, 3);
        var baseResult = QueueCalculator.Calculate(baseInput);
        Near(baseResult.L, 2, 1e-12, "Calculator M/M/1");
        var gg = QueueCalculator.Calculate(baseInput with { Model = CalculatorModel.GG1, ArrivalVariance = 0.25, ServiceVariance = 1.0 / 9 });
        Near(gg.Lq, baseResult.Lq, 1e-12, "Marchal exponential reduction");
        var general = QueueCalculator.Calculate(baseInput with { Model = CalculatorModel.GG1, ArrivalVariance = 0.125, ServiceVariance = 2.0 / 9 });
        Near(general.Lq, 25.0 / 17, 1e-12, "Marchal non-exponential known value");
        Near(QueueCalculator.Calculate(baseInput with { Model = CalculatorModel.GG1 }).Wq, 0, 1e-12, "D/D/1 stable no wait");
        var mg2 = QueueCalculator.Calculate(baseInput with { Model = CalculatorModel.MG2, ServiceVariance = 1.0 / 9 });
        Near(mg2.Wq, mt.Wq, 1e-12, "M/G/2 exponential reduction");
        Check(mg2.P0 == null, "No invented exact M/G/2 empty probability");
        Near(QueueCalculator.Calculate(baseInput with { Model = CalculatorModel.MG2 }).Wq, mt.Wq / 2, 1e-12, "M/D/2 approximation");
        Near(QueueCalculator.Calculate(new(CalculatorModel.MMS, 45.0 / 60, 25.0 / 60, Servers: 3)).Rho, 0.6, 1e-12, "Report M/M/S screenshot example");
        var large = QueueCalculator.Calculate(new(CalculatorModel.MMS, 900, 1, Servers: 1000));
        Check(double.IsFinite(large.Lq) && large.P0 >= 0, "Large server-count numerical stability");
        Reject(() => QueueCalculator.Calculate(baseInput with { Lambda = 3 }));
        Reject(() => QueueCalculator.Calculate(baseInput with { ServiceVariance = double.NaN }));
        Check(SampleScenarios.All.Count == 11, "All report scenarios");
        var models = (ListBox)form.Controls.Find("CalculatorModels", true).Single();
        var arrivalBox = (TextBox)form.Controls.Find("CalculatorArrivalValue", true).Single();
        var serviceBox = (TextBox)form.Controls.Find("CalculatorServiceValue", true).Single();
        var calcOutput = (TextBox)form.Controls.Find("CalculatorResults", true).Single();
        var calcButton = Descendants(tabs.TabPages[0]).OfType<Button>().Single(x => x.Text == "Calculate");
        void CalculateClick() => typeof(Button).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(calcButton, new object[] { EventArgs.Empty });
        arrivalBox.Text = "2"; serviceBox.Text = "3";
        CalculateClick();
        Check(calcOutput.Text.Contains("2.000000 customers"), "Calculator event displays calculated results");
        arrivalBox.Text = "4"; CalculateClick();
        Check(calcOutput.Text == "" && form.Controls.Find("CalculatorStatus", true).Single().Text.Contains("Unstable"), "Unstable calculation clears stale results");
        arrivalBox.Text = "2";
        form.Size = new Size(1250, 950);
        form.CreateControl();
        Directory.CreateDirectory("tmp/previews");
        if (args.Contains("--preview")) using (var bitmap = new Bitmap(form.Width, form.Height))
        {
            CalculateClick(); form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            bitmap.Save("tmp/previews/calculator-mm1.png");
        }
        models.SelectedIndex = 2;
        ((TextBox)form.Controls.Find("CalculatorArrivalVarianceValue", true).Single()).Text = "0.125";
        ((TextBox)form.Controls.Find("CalculatorServiceVarianceValue", true).Single()).Text = "0.2222222222222222";
        CalculateClick();
        Check(calcOutput.Text.Contains("Marchal approximation"), "G/G/1 dispatched from UI");
        models.SelectedIndex = 3; CalculateClick();
        Check(calcOutput.Text.Contains("M/M/2 waiting time") && calcOutput.Text.Contains("N/A"), "M/G/2 UI and P0 limitation");
        models.SelectedIndex = 4; CalculateClick();
        Check(calcOutput.Text.Contains("Servers = 3"), "M/M/S server control");
        models.SelectedIndex = 1; CalculateClick();
        Check(calcOutput.Text.Contains("Pollaczek"), "M/G/1 UI");
        models.SelectedIndex = 2; CalculateClick();
        if (args.Contains("--preview")) using (var bitmap = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size)); bitmap.Save("tmp/previews/calculator-gg1.png");
        }
        Console.WriteLine($"Passed {checks} checks.");
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
