using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

/// <summary>
/// Kelly Criterion Optimizer — transcribed from Python/SciPy.
/// Uses Differential Evolution to maximize logarithmic bankroll growth.
/// </summary>
namespace Solver
{
    public class KellyOptimizer
    {
        // --- INPUT DATA ---
        static double F28 = 3000.0;                          // Bankroll
        static double[] G;// = { -103.0, 107.0, -18.0 };       // Current Exposure (H, A, D)
        static double[] J;// = { 0.344588, 0.57788, 0.077532 }; // Probabilities
        static double[] K;// = { 3.2, 1.6, 14.0 };             // Odds

        // --- KELLY TOGGLE ---
        // Change to 0.5, 0.25, or 0.1 as needed
        static double KELLY_FRACTION = 1.0;

        // ---------------------------------------------------------------
        // Objective: minimise negative fractional log growth (i.e. maximise growth)
        // ---------------------------------------------------------------
        static double Objective(double[] L)
        {
            double[] M = { L[0] * (K[0] - 1), L[1] * (K[1] - 1), L[2] * (K[2] - 1) };

            double w29 = F28 + G[0] + M[0] - L[1] - L[2];
            double w30 = F28 + G[1] + M[1] - L[0] - L[2];
            double w31 = F28 + G[2] + M[2] - L[0] - L[1];

            // Penalise if any scenario wealth falls below 1 % of bankroll
            double floor = F28 * 0.01;
            if (w29 < floor || w30 < floor || w31 < floor)
                return 1e10;

            double logGrowth = J[0] * Math.Log(w29 / F28)
                             + J[1] * Math.Log(w30 / F28)
                             + J[2] * Math.Log(w31 / F28);

            return -(logGrowth * KELLY_FRACTION);
        }

        // ---------------------------------------------------------------
        // Differential Evolution
        // ---------------------------------------------------------------
        static double[] DifferentialEvolution( Func<double[], double> objective, double[] lowerBounds, double[] upperBounds, int populationSize = 150, int maxGenerations = 1000, double mutationFactor = 0.8, double crossoverProbability = 0.7, double tol = 1e-7, int seed = 42)
        {
            int dimensions = lowerBounds.Length;
            var rng = new Random(seed);

            // Initialise population uniformly within bounds
            double[][] population = new double[populationSize][];
            for (int i = 0; i < populationSize; i++)
            {
                population[i] = new double[dimensions];
                for (int d = 0; d < dimensions; d++)
                    population[i][d] = lowerBounds[d] + rng.NextDouble() * (upperBounds[d] - lowerBounds[d]);
            }

            double[] fitness = new double[populationSize];
            for (int i = 0; i < populationSize; i++)
                fitness[i] = objective(population[i]);

            double[] bestSolution = (double[])population[0].Clone();
            double bestFitness = fitness[0];
            for (int i = 1; i < populationSize; i++)
            {
                if (fitness[i] < bestFitness)
                {
                    bestFitness = fitness[i];
                    bestSolution = (double[])population[i].Clone();
                }
            }

            for (int gen = 0; gen < maxGenerations; gen++)
            {
                double prevBest = bestFitness;

                for (int i = 0; i < populationSize; i++)
                {
                    // Pick three distinct indices ≠ i
                    int a, b, c;
                    do { a = rng.Next(populationSize); } while (a == i);
                    do { b = rng.Next(populationSize); } while (b == i || b == a);
                    do { c = rng.Next(populationSize); } while (c == i || c == a || c == b);

                    // Mutation + crossover → trial vector
                    double[] trial = new double[dimensions];
                    int forceDim = rng.Next(dimensions);
                    for (int d = 0; d < dimensions; d++)
                    {
                        if (d == forceDim || rng.NextDouble() < crossoverProbability)
                        {
                            trial[d] = population[a][d]
                                     + mutationFactor * (population[b][d] - population[c][d]);
                            // Clamp to bounds
                            trial[d] = Math.Max(lowerBounds[d], Math.Min(upperBounds[d], trial[d]));
                        }
                        else
                        {
                            trial[d] = population[i][d];
                        }
                    }

                    // Greedy selection
                    double trialFitness = objective(trial);
                    if (trialFitness < fitness[i])
                    {
                        population[i] = trial;
                        fitness[i] = trialFitness;

                        if (trialFitness < bestFitness)
                        {
                            bestFitness = trialFitness;
                            bestSolution = (double[])trial.Clone();
                        }
                    }
                }

                // Convergence check
                if (Math.Abs(prevBest - bestFitness) < tol && gen > 100)
                    break;
            }

            return bestSolution;
        }

        // ---------------------------------------------------------------
        // Entry point
        // ---------------------------------------------------------------
        public static void Solve(double[] _G, double[] _J, double[] _K)
        {
			G = _G;
            J = _J;
			K = _K;

            double[] lower = { 0, 0, 0 };
            double[] upper = { F28, F28, F28 };

            double[] result = DifferentialEvolution(Objective, lower, upper, tol: 1e-7);

            // Scale by Kelly fraction (standard fractional-Kelly practice)
            double[] optimalStakes = { result[0] * KELLY_FRACTION, result[1] * KELLY_FRACTION, result[2] * KELLY_FRACTION };

            // Compute D27 equivalent
            double[] mOpt = { optimalStakes[0] * (K[0] - 1), optimalStakes[1] * (K[1] - 1), optimalStakes[2] * (K[2] - 1) };

            double[] wOpt = { F28 + G[0] + mOpt[0] - optimalStakes[1] - optimalStakes[2], F28 + G[1] + mOpt[1] - optimalStakes[0] - optimalStakes[2], F28 + G[2] + mOpt[2] - optimalStakes[0] - optimalStakes[1] };

            double d27 = Math.Pow(wOpt[0] / F28, J[0]) * Math.Pow(wOpt[1] / F28, J[1]) * Math.Pow(wOpt[2] / F28, J[2]);

            Debug.WriteLine($"--- Results (Fraction: {KELLY_FRACTION}) ---");
            Debug.WriteLine($"L29 (Home): {optimalStakes[0]:F2}");
            Debug.WriteLine($"L30 (Away): {optimalStakes[1]:F2}");
            Debug.WriteLine($"L31 (Draw): {optimalStakes[2]:F2}");
            Debug.WriteLine($"Equivalent D27: {d27:F6}");
        }
    }
}
