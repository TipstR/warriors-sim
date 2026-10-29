using System;
using System.Collections.Generic;
using System.Linq;
using WarriorsSim.Simulation;

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

        Console.WriteLine();
        Console.WriteLine("Training:");
        var ages = results.SelectMany(r => r.CeremonyAge).OrderBy(a => a).ToList();
        if (ages.Count > 0) {
            var late = ages.Count(a => a > Tuning.WarriorAgeMoons) * 100f / ages.Count;
            Console.WriteLine($"  age at the ceremony: mean {ages.Average():0.0}, 90% {Percentile(ages, 0.9f):0}, "
                              + $"99% {Percentile(ages, 0.99f):0}, max {ages[^1]:0}; {late:0}% were late");
            Console.WriteLine($"  oldest apprentice per run: mean {results.Average(r => r.OldestApprentice):0.0}, "
                              + $"max {results.Max(r => r.OldestApprentice)}; "
                              + $"{results.Sum(r => r.DiedAsLateApprentice) / (float)seeds:0.0} per run died as late apprentices");
        }

        var lessons = results.Sum(r => r.Lessons);
        var apprenticeMoons = results.Sum(r => r.ApprenticeMoons);
        if (apprenticeMoons > 0)
            Console.WriteLine($"  {lessons / (float)apprenticeMoons:0.0} lessons per apprentice and moon, "
                              + $"{results.Sum(r => r.LessonsByHelpers) * 100f / Math.Max(1, lessons):0}% given by a helper");
        PrintSpread("  mentors' mentoring", results.SelectMany(r => r.MentorsMentoring).ToList());
        PrintSpread("  warriors' mentoring", results.SelectMany(r => r.WarriorsMentoring).ToList());

        Console.WriteLine();
        Console.WriteLine("Skill faded below the peak (living cats, checked yearly):");
        var groups = results.SelectMany(r => r.FadedBelowPeak).GroupBy(g => g.Key);
        foreach (var group in groups.OrderBy(g => g.Key)) {
            var faded = group.SelectMany(g => g.Value).ToList();
            var share = faded.Count(f => f > 0.5f) * 100f / faded.Count;
            Console.WriteLine($"  {group.Key + ":",-25} mean {faded.Average():0.0}, max {faded.Max():0}, "
                              + $"{share:0}% faded noticeably");
        }

        var rusty = results.SelectMany(r => r.RustyTimes).ToList();
        if (rusty.Count > 0) {
            var moons = rusty.Select(r => (float)r.Moons).OrderBy(m => m).ToList();
            Console.WriteLine($"  hunters who got rusty: {rusty.Count / (float)seeds:0} times per run, "
                              + $"lost {rusty.Average(r => r.Lost):0.0} points (max {rusty.Max(r => r.Lost):0}), "
                              + $"back at their peak after {moons.Average():0.0} moons (90%: {Percentile(moons, 0.9f):0}, "
                              + $"max {moons[^1]:0})");
        }
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
