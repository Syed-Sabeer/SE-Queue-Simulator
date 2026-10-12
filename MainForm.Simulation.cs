using System.Text;

namespace Assignment;

public partial class MainForm
{
    private Control BuildSimulationInputs(out ComboBox model, out ComboBox unit, out TextBox arrival,
        out TextBox service, out TextBox variance, out NumericUpDown servers, out TextBox duration, out TextBox seed)
    {
        var layout = BuildInputLayout();
        model = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        model.Items.AddRange(new[] { "M/M/1", "M/G/1", "M/M/c (multi-server)" });
        model.SelectedIndex = 0;
        unit = CreateUnitComboBox();
        arrival = new TextBox { Text = "2" };
        service = new TextBox { Text = "3" };
        variance = new TextBox { Text = "0.111111", Enabled = false };
        servers = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 1, Enabled = false };
        duration = new TextBox { Text = "10000" };
        seed = new TextBox { PlaceholderText = "Optional integer for repeatable results" };
        AddInputRow(layout, "Queue model", model, 0);
        AddInputRow(layout, "Time unit (rates are per selected unit)", unit, 1);
        AddInputRow(layout, "Arrival rate (lambda)", arrival, 2);
        AddInputRow(layout, "Service rate per server (mu)", service, 3);
        AddInputRow(layout, "Service variance (selected unit squared; M/G/1)", variance, 4);
        AddInputRow(layout, "Number of servers (c)", servers, 5);
        AddInputRow(layout, "Simulation duration (selected unit)", duration, 6);
        AddInputRow(layout, "Random seed", seed, 7);
        return layout;
    }

    private TabPage BuildSimulationTab()
    {
        var page = new TabPage("Rates & Simulation") { BackColor = AppBackground };
        var split = BuildSplitLayout();
        var inputCard = CreateCard("Configure queue experiment", out var inputBody);
        inputBody.AutoScroll = true;
        var inputs = BuildSimulationInputs(out var model, out var unit, out var arrival,
            out var service, out var variance, out var servers, out var duration, out var seed);
        var resultsCard = CreateCard("Theory and simulation comparison", out var resultBody);
        var output = new TextBox();
        ConfigureResultTextBox(output);
        output.WordWrap = false;
        output.ScrollBars = ScrollBars.Both;
        output.Text = "Choose a model, calculate theory, or run a timed simulation.\r\n\r\nM/M/1: exponential arrivals and service.\r\nM/G/1: exponential arrivals; gamma service matching the entered mean and variance (constant when variance is zero).\r\nM/M/c: exponential arrivals and service, with one shared FIFO queue.\r\n\r\nRates use the selected time unit. Service rate is per server.";
        resultBody.Controls.Add(output);
        model.SelectedIndexChanged += (_, _) =>
        {
            variance.Enabled = model.SelectedIndex == 1;
            servers.Enabled = model.SelectedIndex == 2;
            if (!servers.Enabled) servers.Value = 1;
        };

        ExperimentOptions ReadOptions()
        {
            int? randomSeed = null;
            if (!string.IsNullOrWhiteSpace(seed.Text))
            {
                if (!int.TryParse(seed.Text.Trim(), out int parsed)) throw new ArgumentException("Seed must be a whole number in the 32-bit integer range.");
                randomSeed = parsed;
            }
            return new((QueueModel)model.SelectedIndex, ParsePositive(arrival.Text, "Arrival rate"),
                ParsePositive(service.Text, "Service rate"), model.SelectedIndex == 1 ? ParseNonNegative(variance.Text, "Service variance") : 0,
                (int)servers.Value, ParsePositive(duration.Text, "Duration"), randomSeed);
        }

        var buttons = BuildButtonRow();
        buttons.WrapContents = true;
        var theoryButton = CreateSecondaryButton("Calculate theory", (_, _) =>
        {
            try { var options = ReadOptions(); output.Text = FormatExperiment(options, QueueExperiment.Calculate(options), null, GetSelectedUnit(unit)); }
            catch (ArgumentException ex) { MessageBox.Show(this, ex.Message, "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        });
        var run = CreatePrimaryButton("Run simulation", (_, _) => { });
        var cancel = CreateSecondaryButton("Cancel", (_, _) => { });
        cancel.Enabled = false;
        CancellationTokenSource? cancellation = null;
        cancel.Click += (_, _) => cancellation?.Cancel();
        FormClosing += (_, _) => cancellation?.Cancel();
        var clear = CreateSecondaryButton("Reset", (_, _) =>
        {
            model.SelectedIndex = 0; unit.SelectedItem = "Minutes"; arrival.Text = "2"; service.Text = "3";
            variance.Text = "0.111111"; servers.Value = 1; duration.Text = "10000"; seed.Clear(); output.Clear();
        });
        run.Click += async (_, _) =>
        {
            try
            {
                var options = ReadOptions();
                var theory = QueueExperiment.Calculate(options);
                string selectedUnit = GetSelectedUnit(unit);
                cancellation = new CancellationTokenSource();
                var token = cancellation.Token;
                inputs.Enabled = run.Enabled = theoryButton.Enabled = clear.Enabled = false;
                cancel.Enabled = true;
                output.Text = "Simulation running...";
                var result = await Task.Run(() => QueueExperiment.Run(options, token), token);
                if (!IsDisposed) output.Text = FormatExperiment(options, theory, result, selectedUnit);
            }
            catch (OperationCanceledException) { if (!IsDisposed) output.Text = "Simulation cancelled."; }
            catch (Exception ex) { if (!IsDisposed) MessageBox.Show(this, ex.Message, "Simulation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally
            {
                cancellation?.Dispose(); cancellation = null;
                if (!IsDisposed) { inputs.Enabled = run.Enabled = theoryButton.Enabled = clear.Enabled = true; cancel.Enabled = false; }
            }
        };
        buttons.Controls.AddRange(new Control[] { theoryButton, run, cancel, clear });
        inputBody.Controls.Add(buttons);
        inputBody.Controls.Add(inputs);
        split.Panel1.Controls.Add(inputCard);
        split.Panel2.Controls.Add(resultsCard);
        page.Controls.Add(split);
        return page;
    }

    private static string FormatExperiment(ExperimentOptions options, QueueTheory theory, ExperimentResult? result, string unit)
    {
        var text = new StringBuilder();
        string name = options.Model switch { QueueModel.MM1 => "M/M/1", QueueModel.MG1 => "M/G/1", _ => $"M/M/{options.Servers}" };
        text.AppendLine($"{name} — stable (lambda < c × mu)");
        text.AppendLine($"Arrival rate: {options.ArrivalRate:G6} per {unit.ToLowerInvariant()}");
        text.AppendLine($"Service rate: {options.ServiceRate:G6} per server per {unit.ToLowerInvariant()}");
        text.AppendLine($"Servers: {options.Servers} | Duration: {options.Duration:G6} {unit.ToLowerInvariant()}");
        text.AppendLine($"Seed: {options.Seed?.ToString() ?? "random"}");
        if (options.Model == QueueModel.MG1) text.AppendLine($"Service variance: {options.ServiceVariance:G6} {unit.ToLowerInvariant()} squared (gamma service)");
        text.AppendLine();
        text.AppendLine($"{"Measure",-17} {"Theory",12} {"Simulation",12} {"Error %",10}");
        void Row(string label, double expected, double? observed)
        {
            string error = observed is null ? "—" : expected == 0 ? "N/A" : (100 * Math.Abs(observed.Value - expected) / Math.Abs(expected)).ToString("F2");
            text.AppendLine($"{label,-17} {expected,12:F6} {(observed?.ToString("F6") ?? "—"),12} {error,10}");
        }
        Row("Utilization", theory.Utilization, result?.Utilization);
        Row("P0 (empty)", theory.P0, result?.P0);
        Row("L (system size)", theory.L, result?.L);
        Row("Lq (queue size)", theory.Lq, result?.Lq);
        Row("W (system time)", theory.W, result is { Served: > 0 } ? result.W : null);
        Row("Wq (queue wait)", theory.Wq, result is { Served: > 0 } ? result.Wq : null);
        text.AppendLine($"\r\nTime measures are in {unit.ToLowerInvariant()}.");
        text.AppendLine($"Theoretical probability of waiting: {theory.ProbabilityWait:F6}");
        if (result != null)
        {
            text.AppendLine($"\r\nCustomers arrived: {result.Arrived:N0}\r\nCustomers served: {result.Served:N0}");
            text.AppendLine($"Waiting at end: {result.Waiting:N0}\r\nIn service at end: {result.Busy:N0}\r\nMaximum queue length: {result.MaxQueue:N0}");
            text.AppendLine("\r\nSimulation starts empty and stops at the chosen duration. L, Lq, P0 and utilization are time averages. W and Wq use completed customers only; unfinished customers remain at the cutoff. No warm-up is discarded, so short runs can differ substantially from steady-state theory.");
        }
        return text.ToString();
    }
}
