using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.BalanceRunner;

/// <summary>Prints the statistics of all seeds and checks them against the targets.</summary>
public static class Report {
    // Targets: a balance change should keep these. Provisional, like all balance.
    private const int MaxDiedOutPer100 = 15;
    private const float MaxStarvedPerYear = 8f;

    public static void Print(List<SeedResult> results, int seeds, int years, TimeSpan elapsed) {
        var diedOut = results.Count(r => r.DiedOut);
        var starvedPerYear = results.Sum(r => r.Starved) / (float)(seeds * years);
        var sicknessDeathsPerRun = results.Sum(r => r.SicknessDeaths) / (float)seeds;

        Console.WriteLine($"{seeds} seeds × {years} years ({elapsed.TotalSeconds:0} s)");
        Console.WriteLine();
        Console.WriteLine($"Clans died out:        {diedOut} of {seeds}  {Check(diedOut * 100f / seeds <= MaxDiedOutPer100)}");
        Console.WriteLine($"Starved per year:      {starvedPerYear:0.0}  {Check(starvedPerYear <= MaxStarvedPerYear)}");
        Console.WriteLine($"Sickness deaths / run: {sicknessDeathsPerRun:0.0}");
        Console.WriteLine();

        Console.WriteLine("Hunting skill (cats born in the run, no medicine cats):");
        PrintSpread("  at the ceremony", results.SelectMany(r => r.CeremonyHunting).ToList());
        PrintSpread("  at 48 moons", results.SelectMany(r => r.HuntingAt48Moons).ToList());
        PrintSpread("  at 96 moons", results.SelectMany(r => r.HuntingAt96Moons).ToList());

        var livingByYear = results.SelectMany(r => r.LivingHuntersByYear).ToList();
        if (livingByYear.Count > 0) {
            Console.WriteLine($"  living hunters, yearly average: mean {livingByYear.Average(h => h.Average()):0.0}, "
                              + $"10% {livingByYear.Average(h => Percentile(h, 0.1f)):0.0}, "
                              + $"90% {livingByYear.Average(h => Percentile(h, 0.9f)):0.0}, "
                              + $"best {livingByYear.Average(h => h[^1]):0.0}");
        }

        var adults = results.Sum(r => r.Adults);
        var reached90 = results.Sum(r => r.Reached90);
        var oneIn = reached90 > 0 ? $" (1 in {adults / (float)reached90:0})" : "";
        Console.WriteLine($"  {adults} adults, {reached90} reached 90+{oneIn}, {results.Sum(r => r.Reached95)} reached 95+");

        var shares = results.SelectMany(r => r.Top20CatchShare).ToList();
        if (shares.Count > 0)
            Console.WriteLine($"  the best 20% of hunters catch {shares.Average() * 100:0}% of the mice");
    }

    private static string Check(bool ok) {
        return ok ? "ok" : "ABOVE TARGET";
    }

    private static void PrintSpread(string label, List<float> values) {
        if (values.Count == 0)
            return;
        var sorted = values.OrderBy(v => v).ToList();
        Console.WriteLine($"{label + ":",-18} mean {sorted.Average():0.0}, 10% {Percentile(sorted, 0.1f):0}, "
                          + $"90% {Percentile(sorted, 0.9f):0}, max {sorted[^1]:0}  ({sorted.Count} cats)");
    }

    /// <summary>The value below which this share of the (sorted) values lies.</summary>
    private static float Percentile(List<float> sorted, float share) {
        return sorted[Math.Min(sorted.Count - 1, (int)(share * sorted.Count))];
    }
}
