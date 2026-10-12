using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Assignment;

public partial class MainForm : Form
{
    private const int SimulationCustomerCount = 50000;

    private static readonly Color AppBackground = Color.FromArgb(245, 248, 252);
    private static readonly Color CardBackground = Color.White;
    private static readonly Color Accent = Color.FromArgb(49, 94, 173);
    private static readonly Color HeaderBackground = Color.FromArgb(33, 56, 99);

    private readonly TextBox _mm1InterArrivalTextBox = new() { PlaceholderText = "e.g. 10 or 1/10" };
    private readonly TextBox _mm1ServiceTextBox = new() { PlaceholderText = "e.g. 8 or 1:8" };
    private readonly ComboBox _mm1TimeUnitComboBox = CreateUnitComboBox();
    private readonly TextBox _mm1ResultsTextBox = new();

    private readonly TextBox _mg1MeanInterArrivalTextBox = new() { PlaceholderText = "e.g. 10" };
    private readonly TextBox _mg1MeanServiceTextBox = new() { PlaceholderText = "e.g. 8" };
    private readonly TextBox _mg1VarianceServiceTextBox = new() { PlaceholderText = "e.g. 25" };
    private readonly ComboBox _mg1TimeUnitComboBox = CreateUnitComboBox();
    private readonly TextBox _mg1ResultsTextBox = new();

    public MainForm()
    {
        Text = "Queueing Models Calculator";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 700);
        BackColor = AppBackground;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        ConfigureResultTextBox(_mm1ResultsTextBox);
        ConfigureResultTextBox(_mg1ResultsTextBox);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildTabs(), 0, 1);

        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = HeaderBackground,
            Padding = new Padding(24, 18, 24, 14)
        };

        var title = new Label
        {
            Text = "Queueing Models Calculator",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point),
            Dock = DockStyle.Top,
            Height = 42
        };

        var subtitle = new Label
        {
            Text = "Input supports decimal (0.1), fraction (1/10), and ratio (1:10)",
            ForeColor = Color.FromArgb(223, 233, 250),
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            Dock = DockStyle.Fill
        };

        panel.Controls.Add(subtitle);
        panel.Controls.Add(title);
        return panel;
    }

    private Control BuildTabs()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(16, 8),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point)
        };

        tabs.TabPages.Add(BuildCalculatorTab());
        tabs.TabPages.Add(BuildMM1Tab());
        tabs.TabPages.Add(BuildMG1Tab());
        tabs.TabPages.Add(BuildSimulationTab());
        return tabs;
    }

    private TabPage BuildMM1Tab()
    {
        var page = new TabPage("M/M/1") { BackColor = AppBackground };

        var split = BuildSplitLayout();

        var inputCard = CreateCard("M/M/1 Inputs", out var inputBody);
        var inputLayout = BuildInputLayout();
        AddInputRow(inputLayout, "Mean inter-arrival time", _mm1InterArrivalTextBox, 0);
        AddInputRow(inputLayout, "Mean service time", _mm1ServiceTextBox, 1);
        AddInputRow(inputLayout, "Time unit", _mm1TimeUnitComboBox, 2);

        var buttonRow = BuildButtonRow();
        var computeButton = CreatePrimaryButton("Compute M/M/1", (_, _) => ComputeMM1());
        var clearButton = CreateSecondaryButton("Clear", (_, _) =>
        {
            _mm1InterArrivalTextBox.Clear();
            _mm1ServiceTextBox.Clear();
            _mm1ResultsTextBox.Clear();
            _mm1TimeUnitComboBox.SelectedItem = "Minutes";
        });
        buttonRow.Controls.Add(computeButton);
        buttonRow.Controls.Add(clearButton);

        inputBody.Controls.Add(buttonRow);
        inputBody.Controls.Add(inputLayout);

        var resultCard = CreateCard("M/M/1 Results", out var resultBody);
        resultBody.Controls.Add(_mm1ResultsTextBox);

        split.Panel1.Controls.Add(inputCard);
        split.Panel2.Controls.Add(resultCard);

        page.Controls.Add(split);
        return page;
    }

    private TabPage BuildMG1Tab()
    {
        var page = new TabPage("M/G/1") { BackColor = AppBackground };

        var split = BuildSplitLayout();

        var inputCard = CreateCard("M/G/1 Inputs", out var inputBody);
        var inputLayout = BuildInputLayout();
        AddInputRow(inputLayout, "Mean inter-arrival time", _mg1MeanInterArrivalTextBox, 0);
        AddInputRow(inputLayout, "Mean service time E[S]", _mg1MeanServiceTextBox, 1);
        AddInputRow(inputLayout, "Variance of service time Var(S)", _mg1VarianceServiceTextBox, 2);
        AddInputRow(inputLayout, "Time unit", _mg1TimeUnitComboBox, 3);

        var buttonRow = BuildButtonRow();
        var computeButton = CreatePrimaryButton("Compute M/G/1", (_, _) => ComputeMG1());
        var clearButton = CreateSecondaryButton("Clear", (_, _) =>
        {
            _mg1MeanInterArrivalTextBox.Clear();
            _mg1MeanServiceTextBox.Clear();
            _mg1VarianceServiceTextBox.Clear();
            _mg1ResultsTextBox.Clear();
            _mg1TimeUnitComboBox.SelectedItem = "Minutes";
        });
        buttonRow.Controls.Add(computeButton);
        buttonRow.Controls.Add(clearButton);

        inputBody.Controls.Add(buttonRow);
        inputBody.Controls.Add(inputLayout);

        var resultCard = CreateCard("M/G/1 Results", out var resultBody);
        resultBody.Controls.Add(_mg1ResultsTextBox);

        split.Panel1.Controls.Add(inputCard);
        split.Panel2.Controls.Add(resultCard);

        page.Controls.Add(split);
        return page;
    }

    private static SplitContainer BuildSplitLayout()
    {
        var split = new SplitContainer();
        ((ISupportInitialize)split).BeginInit();
        
        split.Size = new Size(1000, 600);
        split.SplitterDistance = 500;
        
        split.Dock = DockStyle.Fill;
        split.Orientation = Orientation.Vertical;
        split.Panel1MinSize = 350;
        split.Panel2MinSize = 350;
        split.IsSplitterFixed = false;
        split.Padding = new Padding(14);
    
        ((ISupportInitialize)split).EndInit();
        return split;
    }

    private static TableLayoutPanel BuildInputLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(0, 0, 0, 8)
        };

        return layout;
    }

    private static FlowLayoutPanel BuildButtonRow()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };
    }

    private Panel CreateCard(string title, out Panel body)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBackground,
            Padding = new Padding(16),
            Margin = new Padding(0)
        };

        var titleLabel = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Accent,
            Dock = DockStyle.Top,
            Height = 30
        };

        body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 4, 0, 0)
        };

        card.Controls.Add(body);
        card.Controls.Add(titleLabel);

        return card;
    }

    private static void AddInputRow(Control container, string labelText, Control inputControl, int row)
    {
        var rowPanel = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            Dock = DockStyle.Top,
            Height = 74,
            Padding = new Padding(0, 0, 0, 10)
        };

        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(52, 63, 82)
        };

        inputControl.Dock = DockStyle.Top;
        inputControl.Height = 34;
        inputControl.Margin = new Padding(0);

        if (inputControl is TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        }
        else if (inputControl is ComboBox comboBox)
        {
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        }

        rowPanel.Controls.Add(inputControl, 0, 1);
        rowPanel.Controls.Add(label, 0, 0);

        container.Controls.Add(rowPanel);
        rowPanel.BringToFront();
    }

    private static Button CreatePrimaryButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            BackColor = Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Padding = new Padding(16, 8, 16, 8),
            Margin = new Padding(0, 0, 8, 0)
        };

        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;
        return button;
    }

    private static Button CreateSecondaryButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            BackColor = Color.FromArgb(233, 238, 247),
            ForeColor = Color.FromArgb(42, 55, 88),
            FlatStyle = FlatStyle.Flat,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(0)
        };

        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;
        return button;
    }

    private static void ConfigureResultTextBox(TextBox textBox)
    {
        textBox.Multiline = true;
        textBox.ReadOnly = true;
        textBox.ScrollBars = ScrollBars.Vertical;
        textBox.Dock = DockStyle.Fill;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Font = new Font("Consolas", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        textBox.BackColor = Color.FromArgb(251, 252, 255);
    }

    private void ComputeMM1()
    {
        try
        {
            string unit = GetSelectedUnit(_mm1TimeUnitComboBox);
            double interArrival = ConvertTimeToMinutes(ParsePositive(_mm1InterArrivalTextBox.Text, "Mean inter-arrival time"), unit);
            double service = ConvertTimeToMinutes(ParsePositive(_mm1ServiceTextBox.Text, "Mean service time"), unit);

            double lambda = 1.0 / interArrival;
            double mu = 1.0 / service;

            if (lambda >= mu)
            {
                MessageBox.Show(
                    this,
                    "System is unstable. Mean inter-arrival time must be greater than mean service time (inter-arrival > service).",
                    "Stability Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var mm1 = new MM1Queue(lambda, mu);
            var simulation = QueueSimulator.SimulateMM1(lambda, mu, SimulationCustomerCount);

            var sb = new StringBuilder();
            sb.AppendLine("===== M/M/1 RESULTS =====");
            sb.AppendLine($"Input unit = {unit} (converted to minutes)");
            sb.AppendLine($"λ = {mm1.Lambda:F4} per minute");
            sb.AppendLine($"μ = {mm1.Mu:F4} per minute");
            sb.AppendLine($"ρ = {mm1.Rho:F4}");
            sb.AppendLine($"P0 = {mm1.ProbabilityZeroCustomers():F4}");
            sb.AppendLine($"L = {mm1.AverageNumberInSystem():F4}");
            sb.AppendLine($"Lq = {mm1.AverageNumberInQueue():F4}");
            sb.AppendLine($"W = {mm1.AverageTimeInSystem():F4} minutes");
            sb.AppendLine($"Wq = {mm1.AverageWaitingTime():F4} minutes");

            sb.AppendLine();
            sb.AppendLine($"----- Simulation ({SimulationCustomerCount:N0} customers) -----");
            sb.AppendLine($"Utilization ≈ {simulation.Utilization:F4}");
            sb.AppendLine($"L  ≈ {simulation.L:F4}");
            sb.AppendLine($"Lq ≈ {simulation.Lq:F4}");
            sb.AppendLine($"W  ≈ {simulation.W:F4} minutes");
            sb.AppendLine($"Wq ≈ {simulation.Wq:F4} minutes");

            _mm1ResultsTextBox.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ComputeMG1()
    {
        try
        {
            string unit = GetSelectedUnit(_mg1TimeUnitComboBox);
            double factor = GetUnitToMinutesFactor(unit);

            double interArrivalMean = ConvertTimeToMinutes(ParsePositive(_mg1MeanInterArrivalTextBox.Text, "Mean inter-arrival time"), unit);
            double serviceMean = ConvertTimeToMinutes(ParsePositive(_mg1MeanServiceTextBox.Text, "Mean service time E[S]"), unit);
            double serviceVariance = ConvertVarianceToMinutesSquared(ParseNonNegative(_mg1VarianceServiceTextBox.Text, "Variance of service time Var(S)"), factor);

            double lambda = 1.0 / interArrivalMean;
            double rho = lambda * serviceMean;

            if (rho >= 1)
            {
                MessageBox.Show(
                    this,
                    "System is unstable. Mean inter-arrival time must be greater than mean service time (inter-arrival > service).",
                    "Stability Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var mg1 = new MG1Queue(lambda, serviceMean, serviceVariance);
            double mu = 1.0 / serviceMean;
            double cs2 = serviceVariance / (serviceMean * serviceMean);

            double lq = mg1.AverageNumberInQueue();
            double l = mg1.AverageNumberInSystem();
            double wq = mg1.AverageWaitingTime();
            double w = mg1.AverageTimeInSystem();
            double proportionIdle = 1.0 - mg1.Rho;

            var simulation = QueueSimulator.SimulateMG1(interArrivalMean, serviceMean, serviceVariance, SimulationCustomerCount);

            var sb = new StringBuilder();
            sb.AppendLine("===== M/G/1 RESULTS =====");
            sb.AppendLine($"Input unit = {unit} (converted to minutes)");
            sb.AppendLine($"λ = {lambda:F4} per minute");
            sb.AppendLine($"μ = {mu:F4} per minute");
            sb.AppendLine($"ρ = {mg1.Rho:F4}");
            sb.AppendLine($"Cs² = {cs2:F4}");
            sb.AppendLine();
            sb.AppendLine($"L  = {l:F4}");
            sb.AppendLine($"Lq = {lq:F4}");
            sb.AppendLine($"W  = {w:F4} minutes");
            sb.AppendLine($"Wq = {wq:F4} minutes");
            sb.AppendLine($"Proportion Idle = {proportionIdle:F4}");
            sb.AppendLine();
            sb.AppendLine($"----- Simulation ({SimulationCustomerCount:N0} customers) -----");
            sb.AppendLine($"Utilization ≈ {simulation.Utilization:F4}");
            sb.AppendLine($"L  ≈ {simulation.L:F4}");
            sb.AppendLine($"Lq ≈ {simulation.Lq:F4}");
            sb.AppendLine($"W  ≈ {simulation.W:F4} minutes");
            sb.AppendLine($"Wq ≈ {simulation.Wq:F4} minutes");

            _mg1ResultsTextBox.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static ComboBox CreateUnitComboBox()
    {
        var comboBox = new ComboBox();
        comboBox.Items.AddRange(new[] { "Seconds", "Minutes", "Hours" });
        comboBox.SelectedItem = "Minutes";
        return comboBox;
    }

    private static string GetSelectedUnit(ComboBox comboBox)
    {
        return comboBox.SelectedItem?.ToString() ?? "Minutes";
    }

    private static double ConvertTimeToMinutes(double value, string unit)
    {
        return value * GetUnitToMinutesFactor(unit);
    }

    private static double ConvertVarianceToMinutesSquared(double variance, double factorToMinutes)
    {
        return variance * factorToMinutes * factorToMinutes;
    }

    private static double GetUnitToMinutesFactor(string unit)
    {
        return unit switch
        {
            "Seconds" => 1.0 / 60.0,
            "Hours" => 60.0,
            _ => 1.0
        };
    }

    private static double ParsePositive(string value, string fieldName)
    {
        if (!InputHelper.TryParseFlexibleDouble(value, out double parsed) || parsed <= 0)
        {
            throw new ArgumentException($"{fieldName} must be a valid number greater than 0.");
        }

        return parsed;
    }

    private static double ParseNonNegative(string value, string fieldName)
    {
        if (!InputHelper.TryParseFlexibleDouble(value, out double parsed) || parsed < 0)
        {
            throw new ArgumentException($"{fieldName} must be a valid number 0 or greater.");
        }

        return parsed;
    }

    private static int? ParseOptionalNonNegativeInt(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value.Trim(), out int parsed) || parsed < 0)
        {
            throw new ArgumentException($"{fieldName} must be a whole number 0 or greater.");
        }

        return parsed;
    }
}
