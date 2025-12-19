using OrchardCore.ABTesting.Models;

namespace OrchardCore.ABTesting.Services;

/// <summary>
/// Implements statistical analysis for A/B test results using the Z-test for two proportions
/// and Bayesian Monte Carlo simulation for "Probability to Be Best" calculations.
/// </summary>
public sealed class StatisticalAnalysisService : IStatisticalAnalysisService
{
    /// <summary>
    /// Z-score thresholds for different confidence levels (two-tailed test).
    /// </summary>
    private const double ZScore90 = 1.645;  // 90% confidence (p < 0.10)
    private const double ZScore95 = 1.96;   // 95% confidence (p < 0.05)
    private const double ZScore99 = 2.576;  // 99% confidence (p < 0.01)

    /// <summary>
    /// Number of Monte Carlo simulation iterations for Bayesian probability calculation.
    /// 10,000 iterations provides ~1% precision with ~1-2ms execution time.
    /// </summary>
    private const int MonteCarloIterations = 10000;

    public ABTestStatisticsResult Analyze(
        long impressionsA,
        long impressionsB,
        long conversionsA,
        long conversionsB,
        int minimumSampleSize,
        int confidenceThreshold)
    {
        var result = new ABTestStatisticsResult();

        // Calculate Bayesian probabilities (always calculate, even with insufficient data for Z-test)
        CalculateBayesianProbabilities(result, impressionsA, impressionsB, conversionsA, conversionsB);

        // Calculate uplift (relative improvement of B vs A)
        CalculateUplift(result, impressionsA, impressionsB, conversionsA, conversionsB);

        // Check for sufficient sample size
        if (impressionsA < minimumSampleSize || impressionsB < minimumSampleSize)
        {
            result.HasSufficientData = false;
            result.IsSignificant = false;
            result.SummaryText = $"Not enough data yet. Need at least {minimumSampleSize} impressions per variant for statistical analysis.";
            return result;
        }

        result.HasSufficientData = true;

        // Calculate conversion rates
        var rateA = (double)conversionsA / impressionsA;
        var rateB = (double)conversionsB / impressionsB;

        // Handle edge case where both rates are 0 or both are 1
        if ((rateA == 0 && rateB == 0) || (rateA == 1 && rateB == 1))
        {
            result.IsSignificant = false;
            result.SummaryText = "No clear winner yet. Both variants have identical conversion rates.";
            return result;
        }

        // Calculate pooled proportion
        var totalConversions = conversionsA + conversionsB;
        var totalImpressions = impressionsA + impressionsB;
        var pooledProportion = (double)totalConversions / totalImpressions;

        // Handle edge case where pooled proportion is 0 or 1
        if (pooledProportion == 0 || pooledProportion == 1)
        {
            result.IsSignificant = false;
            result.SummaryText = "No clear winner yet. Results are not statistically significant.";
            return result;
        }

        // Calculate standard error
        var standardError = Math.Sqrt(
            pooledProportion * (1 - pooledProportion) * (1.0 / impressionsA + 1.0 / impressionsB)
        );

        // Handle edge case where standard error is 0
        if (standardError == 0)
        {
            result.IsSignificant = false;
            result.SummaryText = "No clear winner yet. Results are not statistically significant.";
            return result;
        }

        // Calculate Z-score
        var zScore = (rateA - rateB) / standardError;
        var absZScore = Math.Abs(zScore);

        // Get the required Z-score threshold based on confidence threshold setting
        var requiredZScore = GetZScoreForConfidence(confidenceThreshold);

        // Determine actual achieved confidence level
        int achievedConfidence;
        if (absZScore >= ZScore99)
        {
            achievedConfidence = 99;
        }
        else if (absZScore >= ZScore95)
        {
            achievedConfidence = 95;
        }
        else if (absZScore >= ZScore90)
        {
            achievedConfidence = 90;
        }
        else
        {
            achievedConfidence = 0;
        }

        // Check if we meet the required confidence threshold
        if (absZScore < requiredZScore)
        {
            result.IsSignificant = false;
            result.SummaryText = "No clear winner yet. Results are not statistically significant.";
            return result;
        }

        result.IsSignificant = true;
        result.ConfidenceLevel = achievedConfidence;

        // Determine winner and calculate lift
        if (rateA > rateB)
        {
            result.WinningVariant = ABVariant.A;
            result.Lift = rateB > 0 ? Math.Round((rateA - rateB) / rateB * 100, 1) : 0;
            result.SummaryText = FormatWinnerSummary("A", result.ConfidenceLevel, result.Lift);
        }
        else
        {
            result.WinningVariant = ABVariant.B;
            result.Lift = rateA > 0 ? Math.Round((rateB - rateA) / rateA * 100, 1) : 0;
            result.SummaryText = FormatWinnerSummary("B", result.ConfidenceLevel, result.Lift);
        }

        return result;
    }

    private static double GetZScoreForConfidence(int confidence)
    {
        return confidence switch
        {
            99 => ZScore99,
            95 => ZScore95,
            _ => ZScore90,
        };
    }

    private static string FormatWinnerSummary(string variant, int confidence, double lift)
    {
        var liftText = lift > 0 ? $" (+{lift}% lift)" : "";
        return $"Variant {variant} is winning with {confidence}% confidence{liftText}";
    }

    /// <summary>
    /// Calculates Bayesian "Probability to Be Best" using Monte Carlo simulation.
    /// Models each variant's conversion rate as Beta(conversions + 1, impressions - conversions + 1).
    /// </summary>
    private static void CalculateBayesianProbabilities(
        ABTestStatisticsResult result,
        long impressionsA,
        long impressionsB,
        long conversionsA,
        long conversionsB)
    {
        // Handle edge case: no impressions for either variant
        if (impressionsA == 0 && impressionsB == 0)
        {
            result.ProbabilityToBeBestA = 50.0;
            result.ProbabilityToBeBestB = 50.0;
            return;
        }

        // Handle edge case: no impressions for one variant
        if (impressionsA == 0)
        {
            result.ProbabilityToBeBestA = 0.0;
            result.ProbabilityToBeBestB = 100.0;
            return;
        }

        if (impressionsB == 0)
        {
            result.ProbabilityToBeBestA = 100.0;
            result.ProbabilityToBeBestB = 0.0;
            return;
        }

        // Beta distribution parameters using uninformative prior (Jeffreys prior)
        // Beta(alpha, beta) where alpha = successes + 1, beta = failures + 1
        var alphaA = conversionsA + 1;
        var betaA = impressionsA - conversionsA + 1;
        var alphaB = conversionsB + 1;
        var betaB = impressionsB - conversionsB + 1;

        var aWins = 0;

        // Monte Carlo simulation: sample from both distributions and count wins
        for (var i = 0; i < MonteCarloIterations; i++)
        {
            var sampleA = SampleBetaDistribution(alphaA, betaA);
            var sampleB = SampleBetaDistribution(alphaB, betaB);

            if (sampleA > sampleB)
            {
                aWins++;
            }
        }

        result.ProbabilityToBeBestA = Math.Round((double)aWins / MonteCarloIterations * 100, 2);
        result.ProbabilityToBeBestB = Math.Round(100.0 - result.ProbabilityToBeBestA, 2);
    }

    /// <summary>
    /// Calculates the uplift (relative improvement) of Variant B vs Variant A (control).
    /// Uplift = (RateB - RateA) / RateA * 100
    /// </summary>
    private static void CalculateUplift(
        ABTestStatisticsResult result,
        long impressionsA,
        long impressionsB,
        long conversionsA,
        long conversionsB)
    {
        // Calculate conversion rates
        var rateA = impressionsA > 0 ? (double)conversionsA / impressionsA : 0;
        var rateB = impressionsB > 0 ? (double)conversionsB / impressionsB : 0;

        // Calculate relative uplift: (RateB - RateA) / RateA * 100
        if (rateA > 0)
        {
            result.UpliftB = Math.Round((rateB - rateA) / rateA * 100, 1);
        }
        else if (rateB > 0)
        {
            // A has 0% rate, B has conversions - show as positive uplift
            result.UpliftB = 100.0;
        }
        else
        {
            // Both have 0% conversion rate
            result.UpliftB = 0.0;
        }
    }

    /// <summary>
    /// Samples from a Beta distribution using the Gamma distribution relationship:
    /// Beta(a,b) = Gamma(a) / (Gamma(a) + Gamma(b))
    /// </summary>
    private static double SampleBetaDistribution(long alpha, long beta)
    {
        var gammaA = SampleGammaDistribution(alpha);
        var gammaB = SampleGammaDistribution(beta);

        return gammaA / (gammaA + gammaB);
    }

    /// <summary>
    /// Samples from a Gamma distribution using Marsaglia and Tsang's method.
    /// This is efficient for shape >= 1.
    /// </summary>
    private static double SampleGammaDistribution(long shape)
    {
        if (shape < 1)
        {
            // For shape < 1, use the transformation: Gamma(a) = Gamma(a+1) * U^(1/a)
            return SampleGammaDistribution(shape + 1) * Math.Pow(Random.Shared.NextDouble(), 1.0 / shape);
        }

        // Marsaglia and Tsang's method for shape >= 1
        var d = shape - 1.0 / 3.0;
        var c = 1.0 / Math.Sqrt(9.0 * d);

        while (true)
        {
            double x, v;
            do
            {
                x = SampleStandardNormal();
                v = 1.0 + c * x;
            }
            while (v <= 0);

            v = v * v * v;
            var u = Random.Shared.NextDouble();

            if (u < 1.0 - 0.0331 * (x * x) * (x * x))
            {
                return d * v;
            }

            if (Math.Log(u) < 0.5 * x * x + d * (1.0 - v + Math.Log(v)))
            {
                return d * v;
            }
        }
    }

    /// <summary>
    /// Samples from a standard normal distribution using the Box-Muller transform.
    /// </summary>
    private static double SampleStandardNormal()
    {
        var u1 = 1.0 - Random.Shared.NextDouble(); // Subtract from 1 to avoid log(0)
        var u2 = Random.Shared.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
