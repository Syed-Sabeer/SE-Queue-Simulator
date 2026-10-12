using System;

namespace Assignment;

public class MM1Queue
{
    public double Lambda { get; }
    public double Mu { get; }
    
    // Utilization: ρ = λ / μ
    public double Rho => Lambda / Mu;

    public MM1Queue(double lambda, double mu)
    {
        if (lambda < 0 || mu <= 0)
            throw new ArgumentException("Rates must be positive.");
            
        if (lambda >= mu)
            throw new ArgumentException("System is unstable: Arrival rate (λ) must be less than Service rate (μ).");

        Lambda = lambda;
        Mu = mu;
    }

    // Probability of zero customers: P0 = 1 - ρ
    public double ProbabilityZeroCustomers() => 1 - Rho;

    // Average number in system: L = λ / (μ - λ)
    public double AverageNumberInSystem() => Lambda / (Mu - Lambda);

    // Average number in queue: Lq = λ^2 / (μ(μ - λ))
    public double AverageNumberInQueue() => (Lambda * Lambda) / (Mu * (Mu - Lambda));

    // Average time in system: W = 1 / (μ - λ)
    public double AverageTimeInSystem() => 1 / (Mu - Lambda);

    // Average waiting time: Wq = λ / (μ(μ - λ))
    public double AverageWaitingTime() => Lambda / (Mu * (Mu - Lambda));

    // Probability of exactly n customers: P(n) = (1 - ρ) * ρ^n
    public double ProbabilityOfNCustomers(int n) => (1 - Rho) * Math.Pow(Rho, n);

    // Probability of more than k customers: P(N > k) = ρ^(k + 1)
    public double ProbabilityMoreThanKCustomers(int k) => Math.Pow(Rho, k + 1);

    // Probability of less than or equal to k customers: P(N <= k) = 1 - ρ^(k + 1)
    public double ProbabilityLessThanOrEqualKCustomers(int k) => 1 - Math.Pow(Rho, k + 1);
}
