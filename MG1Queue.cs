using System;

namespace Assignment;

public class MG1Queue
{
    public double Lambda { get; }
    public double MeanServiceTime { get; }
    public double VarianceServiceTime { get; }
    
    // Derived: μ = 1 / E[S]
    // Utilization: ρ = λ * E[S]
    public double Rho => Lambda * MeanServiceTime;

    public MG1Queue(double lambda, double meanServiceTime, double varianceServiceTime)
    {
        if (lambda < 0 || meanServiceTime <= 0 || varianceServiceTime < 0)
            throw new ArgumentException("Inputs must be positive.");
            
        Lambda = lambda;
        MeanServiceTime = meanServiceTime;
        VarianceServiceTime = varianceServiceTime;

        if (Rho >= 1)
            throw new ArgumentException("System is unstable: Utilization (ρ) must be less than 1.");
    }

    // Average number in queue: Lq = (λ² * Var(S) + ρ²) / (2 * (1 - ρ))
    public double AverageNumberInQueue()
    {
        return ((Lambda * Lambda) * VarianceServiceTime + (Rho * Rho)) / (2 * (1 - Rho));
    }

    // Average number in system: L = Lq + ρ
    public double AverageNumberInSystem() => AverageNumberInQueue() + Rho;

    // Average waiting time: Wq = Lq / λ
    public double AverageWaitingTime() => AverageNumberInQueue() / Lambda;

    // Average time in system: W = Wq + E[S]
    public double AverageTimeInSystem() => AverageWaitingTime() + MeanServiceTime;
}
