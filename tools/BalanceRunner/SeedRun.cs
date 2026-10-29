using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WarriorsSim.Simulation;

namespace WarriorsSim.BalanceRunner;

/// <summary>What happened in one seed's run. Skill numbers only count cats born during the run, no medicine cats.</summary>
public class SeedResult {
    public bool DiedOut;
    public int Starved;

    /// <summary>Deaths from sickness or injury (not old age, not starvation).</summary>
    public int SicknessDeaths;

    public readonly List<float> CeremonyHunting = new();
    public readonly List<float> HuntingAt48Moons = new();
    public readonly List<float> HuntingAt96Moons = new();

    /// <summary>Once per year: the hunting skill of all living cats aged 12 moons or more, lowest first.</summary>
    public readonly List<List<float>> LivingHuntersByYear = new();

    /// <summary>Once per year: the share of all mice caught by the best 20% of that year's hunters.</summary>
    public readonly List<float> Top20CatchShare = new();

    /// <summary>
    ///     Once per year: how far below its peak each living cat's skill is, by group, e.g. "elders" → [3.5, 0, 12].
    ///     Hunting for everyone except medicine cats, healing for medicine cats.
    /// </summary>
    public readonly Dictionary<string, List<float>> FadedBelowPeak = new();

    /// <summary>
    ///     Every time a hunter (not an elder) got rusty: how many points it lost at most and how many moons it took
    ///     until it was back at its peak. Times that hadn't ended when the run ended (or the cat died) don't count.
    /// </summary>
    public readonly List<(float Lost, int Moons)> RustyTimes = new();

    public int Adults;
    public int Reached90;
    public int Reached95;
}

/// <summary>Runs ThunderClan with one seed and collects statistics from the log and from the cats.</summary>
public static class SeedRun {
    private static readonly Regex PatrolLine = new(@"Hunting patrol caught \d+ mice \((.*)\)\. Pile");
    private static readonly Regex CeremonyLine = new(@"has become a warrior: .*\(hunting (\d+)");
    private static int TicksPerMoon => Tuning.TicksPerDay * Tuning.DaysPerMoon;
    private static int TicksPerYear => TicksPerMoon * 12;

    public static SeedResult Run(int seed, int years) {
        var result = new SeedResult();
        var catchesThisYear = new Dictionary<string, int>();
        var clan = StartingClans.CreateThunderClan();
        // The starting cats got their skills by hand, so they don't say anything about learning
        var startingCats = clan.Cats.ToHashSet();
        var seenAt48 = new HashSet<Cat>();
        var seenAt96 = new HashSet<Cat>();
        var rusty = new Dictionary<Cat, (float Lost, int Moons)>();

        var world = new World(clan, seed, (_, line) => ReadLogLine(line, result, catchesThisYear));

        for (var tick = 1; tick <= TicksPerYear * years; tick++) {
            world.Tick();
            if (clan.Cats.Count == 0) {
                result.DiedOut = true;
                break;
            }

            if (tick % TicksPerMoon == 0) {
                foreach (var cat in clan.Cats.Where(c => !startingCats.Contains(c) && !c.IsMedicineCat)) {
                    var hunting = cat.Skills[SkillType.Hunting].Level;
                    if (cat.AgeMoons >= 48 && seenAt48.Add(cat))
                        result.HuntingAt48Moons.Add(hunting);
                    if (cat.AgeMoons >= 96 && seenAt96.Add(cat))
                        result.HuntingAt96Moons.Add(hunting);
                }

                TrackRustyHunters(clan, rusty, result);
            }

            if (tick % TicksPerYear == 0) {
                EndYear(clan, result, catchesThisYear);
                catchesThisYear.Clear();
            }
        }

        // The best level each cat ever had, because skills can fade
        foreach (var cat in clan.Dead.Concat(clan.Cats)) {
            if (startingCats.Contains(cat) || cat.IsMedicineCat || cat.AgeMoons < 12)
                continue;
            var hunting = cat.Skills[SkillType.Hunting].Peak;
            result.Adults++;
            if (hunting >= 90)
                result.Reached90++;
            if (hunting >= 95)
                result.Reached95++;
        }

        return result;
    }

    private static void ReadLogLine(string line, SeedResult result, Dictionary<string, int> catchesThisYear) {
        if (line.Contains("cats are left.")) {
            if (line.Contains("starved to death"))
                result.Starved++;
            else if (line.Contains("died of") && !line.Contains("died of old age"))
                result.SicknessDeaths++;
            return;
        }

        var ceremony = CeremonyLine.Match(line);
        if (ceremony.Success) {
            result.CeremonyHunting.Add(int.Parse(ceremony.Groups[1].Value));
            return;
        }

        // "(Graypaw 2, Sandstorm 3)". Names change at ceremonies, so a cat may count as two hunters that year.
        var patrol = PatrolLine.Match(line);
        if (!patrol.Success)
            return;
        foreach (var part in patrol.Groups[1].Value.Split(", ")) {
            var space = part.LastIndexOf(' ');
            var name = part[..space];
            catchesThisYear[name] = catchesThisYear.GetValueOrDefault(name) + int.Parse(part[(space + 1)..]);
        }
    }

    private static void EndYear(Clan clan, SeedResult result, Dictionary<string, int> catchesThisYear) {
        if (catchesThisYear.Count >= 5) {
            var catches = catchesThisYear.Values.OrderByDescending(n => n).ToList();
            var best = Math.Max(1, catches.Count / 5);
            result.Top20CatchShare.Add((float)catches.Take(best).Sum() / catches.Sum());
        }

        var hunters = clan.Cats
            .Where(c => !c.IsMedicineCat && c.AgeMoons >= 12)
            .Select(c => c.Skills[SkillType.Hunting].Level)
            .OrderBy(level => level)
            .ToList();
        if (hunters.Count >= 5)
            result.LivingHuntersByYear.Add(hunters);

        foreach (var cat in clan.Cats.Where(c => c.Rank != Rank.Kit)) {
            var skill = cat.Skills[cat.IsMedicineCat ? SkillType.Healing : SkillType.Hunting];
            var group = FadeGroup(cat);
            if (!result.FadedBelowPeak.TryGetValue(group, out var list))
                result.FadedBelowPeak[group] = list = new List<float>();
            list.Add(skill.Peak - skill.Level);
        }
    }

    private static void TrackRustyHunters(Clan clan, Dictionary<Cat, (float Lost, int Moons)> rusty, SeedResult result) {
        foreach (var cat in clan.Cats.Where(c => !c.IsMedicineCat && c.Rank != Rank.Elder)) {
            var hunting = cat.Skills[SkillType.Hunting];
            var lost = hunting.Peak - hunting.Level;
            var wasRusty = rusty.TryGetValue(cat, out var time);

            if (lost >= 0.5f)
                rusty[cat] = (Math.Max(time.Lost, lost), time.Moons + 1);
            else if (wasRusty && lost < 0.1f) {
                result.RustyTimes.Add(time);
                rusty.Remove(cat);
            }
        }
    }

    private static string FadeGroup(Cat cat) {
        if (cat.IsMedicineCat)
            return "medicine cats (healing)";
        if (cat.IsNursing)
            return "nursing queens";
        return cat.Rank switch {
            Rank.Leader or Rank.Deputy => "leader and deputy",
            Rank.Elder => "elders",
            Rank.Apprentice => "apprentices",
            _ => "warriors"
        };
    }
}
