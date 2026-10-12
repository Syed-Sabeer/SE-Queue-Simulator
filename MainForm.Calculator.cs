using System.Text;

namespace Assignment;

public partial class MainForm
{
    private static readonly string[] ModelNames = { "M/M/1", "M/G/1", "G/G/1", "M/G/2", "M/M/S" };
    private static readonly string[] ModelDescriptions =
    {
        "Poisson arrivals · one server", "General service · one server", "General arrivals and service",
        "General service · two servers", "Poisson arrivals · S servers"
    };

    private static ComboBox SelectBox(string name, params string[] items)
    {
        var box = new ComboBox { Name = name, DropDownStyle = ComboBoxStyle.DropDownList, Width = 165 };
        box.Items.AddRange(items); box.SelectedIndex = 0;
        return box;
    }

    private static Control LabeledField(string label, Control control)
    {
        var panel = new Panel { Width = Math.Max(180, control.Width + 8), Height = 64, Margin = new Padding(4) };
        panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Top, Height = 25, AutoEllipsis = true });
        control.Location = new Point(0, 27); panel.Controls.Add(control);
        return panel;
    }

    private sealed class ParameterFields
    {
        public GroupBox Group { get; }
        public ComboBox Unit { get; }
        public ComboBox Mode { get; }
        public TextBox Value { get; }
        public ParameterFields(string name, string title)
        {
            Unit = SelectBox(name + "Unit", "Minutes", "Hours", "Seconds");
            Mode = SelectBox(name + "Mode", "Rate per selected unit", "Mean time");
            Value = new TextBox { Name = name + "Value", Width = 155, PlaceholderText = "e.g. 30 or 1/2" };
            Group = new GroupBox { Text = title, Dock = DockStyle.Top, Height = 108, Padding = new Padding(8) };
            var row = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = true };
            row.Controls.AddRange(new[] { LabeledField("Unit", Unit), LabeledField("Input method", Mode), LabeledField("Rate / mean time", Value) });
            Group.Controls.Add(row);
        }
        public double Read(string label) => UnitConverter.ToRate(ParsePositive(Value.Text, label), (ParameterInput)Mode.SelectedIndex, (TimeUnit)Unit.SelectedIndex);
    }

    private sealed class VarianceFields
    {
        public GroupBox Group { get; }
        public ComboBox Unit { get; }
        public ComboBox Mode { get; }
        public TextBox Value { get; }
        public TextBox Maximum { get; }
        public VarianceFields(string name, string title)
        {
            Unit = SelectBox(name + "Unit", "Minutes", "Hours", "Seconds");
            Mode = SelectBox(name + "Mode", "Variance directly", "Standard deviation", "Min & max (uniform)");
            Value = new TextBox { Name = name + "Value", Width = 150, PlaceholderText = "Required" };
            Maximum = new TextBox { Name = name + "Maximum", Width = 150, PlaceholderText = "Maximum time" };
            Group = new GroupBox { Text = title, Dock = DockStyle.Top, Height = 108, Padding = new Padding(8) };
            var row = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = true };
            var valueField = LabeledField("Variance (unit squared)", Value);
            var valueLabel = valueField.Controls.OfType<Label>().Single();
            var maxField = LabeledField("Maximum (selected unit)", Maximum);
            maxField.Visible = false;
            Mode.SelectedIndexChanged += (_, _) =>
            {
                valueLabel.Text = Mode.SelectedIndex switch { 0 => "Variance (unit squared)", 1 => "Deviation (selected unit)", _ => "Minimum (selected unit)" };
                maxField.Visible = Mode.SelectedIndex == 2;
                Group.Height = Mode.SelectedIndex == 2 ? 180 : 108;
            };
            row.Controls.AddRange(new[] { LabeledField("Method", Mode), LabeledField("Unit", Unit), valueField, maxField });
            Group.Controls.Add(row);
        }
        public double Read(string label) => UnitConverter.ToVariance(ParseNonNegative(Value.Text, label),
            Mode.SelectedIndex == 2 ? ParseNonNegative(Maximum.Text, label + " maximum") : 0,
            (VarianceInput)Mode.SelectedIndex, (TimeUnit)Unit.SelectedIndex);
    }

    private TabPage BuildCalculatorTab()
    {
        var page = new TabPage("Models Calculator") { BackColor = AppBackground };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(12) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var modelList = new ListBox
        {
            Name = "CalculatorModels", Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 76, BorderStyle = BorderStyle.None, BackColor = Color.White, IntegralHeight = false
        };
        modelList.Items.AddRange(ModelNames);
        modelList.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using var brush = new SolidBrush(selected ? Accent : Color.White);
            e.Graphics.FillRectangle(brush, e.Bounds);
            using var titleFont = new Font("Segoe UI", 11, FontStyle.Bold);
            using var detailFont = new Font("Segoe UI", 8);
            TextRenderer.DrawText(e.Graphics, ModelNames[e.Index], titleFont, new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 12, 195, 25), selected ? Color.White : HeaderBackground);
            TextRenderer.DrawText(e.Graphics, ModelDescriptions[e.Index], detailFont, new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 40, 203, 24), selected ? Color.White : Color.DimGray, TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
            e.DrawFocusRectangle();
        };
        layout.Controls.Add(modelList, 0, 0);
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12, 0, 0, 0) };
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 57)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 43));
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var stack = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(0, 0, 16, 0) };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        void Add(Control control) { control.Margin = new Padding(0, 0, 0, 8); stack.Controls.Add(control, 0, stack.RowCount++); }
        var arrival = new ParameterFields("CalculatorArrival", "Arrival (lambda)");
        var service = new ParameterFields("CalculatorService", "Service per server (mu)");
        var arrivalVariance = new VarianceFields("CalculatorArrivalVariance", "Inter-arrival time variance");
        var serviceVariance = new VarianceFields("CalculatorServiceVariance", "Service time variance");
        var servers = new NumericUpDown { Name = "CalculatorServers", Minimum = 1, Maximum = 100000, Value = 3, Width = 155 };
        var serverPanel = LabeledField("Number of servers (S)", servers);
        Add(arrival.Group); Add(service.Group); Add(arrivalVariance.Group); Add(serviceVariance.Group); Add(serverPanel);
        var output = new TextBox
        {
            Name = "CalculatorResults", Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.FromArgb(225, 235, 250),
            Font = new Font("Consolas", 10), ScrollBars = ScrollBars.Both, WordWrap = false, BorderStyle = BorderStyle.FixedSingle
        };
        var status = new Label { Name = "CalculatorStatus", AutoSize = true, MaximumSize = new Size(640, 0), ForeColor = Accent, Text = "Enter parameters and click Calculate." };
        var actions = BuildButtonRow(); actions.Dock = DockStyle.Top; actions.WrapContents = true;
        actions.Controls.Add(CreatePrimaryButton("Calculate", (_, _) =>
        {
            try
            {
                var model = (CalculatorModel)modelList.SelectedIndex;
                var input = new QueueInputs(model, arrival.Read("Arrival"), service.Read("Service"),
                    model == CalculatorModel.GG1 ? arrivalVariance.Read("Arrival variance") : 0,
                    model is CalculatorModel.MG1 or CalculatorModel.GG1 or CalculatorModel.MG2 ? serviceVariance.Read("Service variance") : 0,
                    (int)servers.Value);
                output.Text = FormatCalculator(input, QueueCalculator.Calculate(input));
                status.ForeColor = Color.DarkGreen; status.Text = "Calculation complete.";
            }
            catch (ArgumentException ex)
            {
                output.Clear(); status.ForeColor = Color.Firebrick; status.Text = ex.Message;
            }
        }));
        actions.Controls.Add(CreateSecondaryButton("Clear", (_, _) =>
        {
            arrival.Value.Clear(); service.Value.Clear(); arrivalVariance.Value.Clear(); arrivalVariance.Maximum.Clear();
            serviceVariance.Value.Clear(); serviceVariance.Maximum.Clear(); output.Clear();
            status.Text = "Enter parameters and click Calculate."; status.ForeColor = Accent;
        }));
        Add(actions); Add(status);
        var examples = SelectBox("CalculatorExamples", SampleScenarios.All.Select(x => x.Name).ToArray());
        examples.Width = 270;
        var samples = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
        samples.Controls.Add(examples);
        samples.Controls.Add(CreateSecondaryButton("Load example", (_, _) =>
        {
            var example = SampleScenarios.All[examples.SelectedIndex];
            modelList.SelectedIndex = (int)example.Model;
            arrival.Unit.SelectedIndex = service.Unit.SelectedIndex = 1;
            arrival.Mode.SelectedIndex = service.Mode.SelectedIndex = 0;
            arrival.Value.Text = example.ArrivalPerHour.ToString(); service.Value.Text = example.ServicePerHour.ToString();
            servers.Value = example.Servers;
            arrivalVariance.Value.Clear(); serviceVariance.Value.Clear(); arrivalVariance.Maximum.Clear(); serviceVariance.Maximum.Clear();
            arrivalVariance.Unit.SelectedIndex = serviceVariance.Unit.SelectedIndex = 1;
            arrivalVariance.Mode.SelectedIndex = serviceVariance.Mode.SelectedIndex = 0;
            output.Clear(); status.ForeColor = Accent;
            status.Text = example.Model is CalculatorModel.MG1 or CalculatorModel.GG1 or CalculatorModel.MG2
                ? "Report rates loaded. The report does not supply variances for this scenario; enter them before calculating."
                : "Report scenario loaded. Click Calculate (the peak bank scenario is deliberately unstable).";
        }));
        Add(samples);
        modelList.SelectedIndexChanged += (_, _) =>
        {
            var model = (CalculatorModel)modelList.SelectedIndex;
            arrivalVariance.Group.Visible = model == CalculatorModel.GG1;
            serviceVariance.Group.Visible = model is CalculatorModel.MG1 or CalculatorModel.GG1 or CalculatorModel.MG2;
            serverPanel.Visible = model == CalculatorModel.MMS;
            output.Text = $"Model: {ModelNames[modelList.SelectedIndex]}\r\n{ModelDescriptions[modelList.SelectedIndex]}\r\n\r\nEnter parameters and click Calculate.";
            status.Text = model == CalculatorModel.MG2 ? "M/G/2 uses two servers and approximate waiting-time results." : "Enter parameters and click Calculate.";
            status.ForeColor = Accent;
        };
        modelList.SelectedIndex = 0;
        scroll.Controls.Add(stack); right.Controls.Add(scroll, 0, 0); right.Controls.Add(output, 0, 1);
        layout.Controls.Add(right, 1, 0); page.Controls.Add(layout);
        return page;
    }

    private static string FormatCalculator(QueueInputs input, QueueResults result)
    {
        int servers = input.Model == CalculatorModel.MG2 ? 2 : input.Model == CalculatorModel.MMS ? input.Servers : 1;
        var text = new StringBuilder();
        text.AppendLine($"===== {ModelNames[(int)input.Model]} RESULTS =====");
        text.AppendLine(result.Method);
        text.AppendLine($"Servers = {servers}; base unit = minutes");
        text.AppendLine($"lambda = {input.Lambda:G8}/min = {input.Lambda * 60:G8}/hr = {input.Lambda / 60:G8}/sec");
        text.AppendLine($"mu     = {input.Mu:G8}/min per server = {input.Mu * 60:G8}/hr = {input.Mu / 60:G8}/sec");
        text.AppendLine($"Ca² = {result.CaSquared:G8}    Cs² = {result.CsSquared:G8}");
        text.AppendLine($"\r\nTraffic intensity (rho) = {result.Rho:F6}");
        text.AppendLine($"Lq (average in queue)   = {result.Lq:F6} customers");
        text.AppendLine($"L  (average in system)  = {result.L:F6} customers");
        text.AppendLine($"Wq (queue wait)         = {result.Wq:F6} min | {result.Wq / 60:F6} hr | {result.Wq * 60:F6} sec");
        text.AppendLine($"W  (system time)        = {result.W:F6} min | {result.W / 60:F6} hr | {result.W * 60:F6} sec");
        text.AppendLine($"P0 (system empty)      = {result.P0?.ToString("F6") ?? "N/A — requires more distribution information"}");
        text.AppendLine($"Server idle fraction   = {result.IdleFraction:F6} ({result.IdleFraction:P2})");
        if (servers > 1) text.AppendLine("Idle fraction is the average idle capacity per server, not the probability that all servers are idle.");
        text.AppendLine("\r\nStable: lambda < servers × mu. Infinite capacity; FCFS queue.");
        return text.ToString();
    }
}
