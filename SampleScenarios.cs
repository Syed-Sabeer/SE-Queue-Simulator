namespace Assignment;

public sealed record SampleScenario(string Name, CalculatorModel Model, double ArrivalPerHour, double ServicePerHour, int Servers = 1);

public static class SampleScenarios
{
    // Report page 8 supplies these rates but no general-model variances.
    public static IReadOnlyList<SampleScenario> All { get; } = new[]
    {
        new SampleScenario("Fast-food counter", CalculatorModel.MM1, 20, 30),
        new SampleScenario("Bank teller (peak)", CalculatorModel.MM1, 15, 12),
        new SampleScenario("Bank teller (normal)", CalculatorModel.MM1, 8, 12),
        new SampleScenario("Hospital ER triage", CalculatorModel.MG1, 5, 8),
        new SampleScenario("Call center", CalculatorModel.MG1, 40, 50),
        new SampleScenario("Post office (2)", CalculatorModel.MMS, 30, 20, 2),
        new SampleScenario("Grocery (2 lanes)", CalculatorModel.MMS, 50, 30, 2),
        new SampleScenario("Airport check-in", CalculatorModel.GG1, 60, 80),
        new SampleScenario("Coffee shop (3)", CalculatorModel.MMS, 45, 20, 3),
        new SampleScenario("Help desk (5)", CalculatorModel.MMS, 100, 25, 5),
        new SampleScenario("Supermarket", CalculatorModel.MG2, 40, 25, 2)
    };
}
