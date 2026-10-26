using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>
///     Runs the simulation one tick at a time and reports what happens to the log.
/// </summary>
public class World {
    private const int DaysPerSeason = Tuning.DaysPerMoon * Tuning.MoonsPerSeason;
    private readonly Action<LogLevel, string> _log;
    private readonly Random _random;
    private List<Cat> _lastHunters = new();
    private HashSet<Cat> _sleeping = new();
    private readonly HashSet<Cat> _ateThisTick = new();
    private int _mealsMissedThisMoon;

    // Statistics for the moon summary
    private int _miceCaughtThisMoon;
    private int _timesStarvedThisMoon;
    private int _timesExhaustedThisMoon;
    private int _deathsThisMoon;
    private int _starvingPassedOverThisMoon;
    private int _peakStarvationStacksThisMoon;
    private string _peakStarvationCatThisMoon = "";
    private long _starvationStackSumThisMoon;
    private long _catTicksThisMoon;

    /// <param name="log">Called with every line of the log and its level, e.g. to print it.</param>
    public World(Clan clan, int seed, Action<LogLevel, string> log) {
        Clan = clan;
        _random = new Random(seed);
        _log = log;
    }

    public Clan Clan { get; }
    public int Day { get; private set; } = 1;
    public TimeOfDay TimeOfDay { get; private set; } = TimeOfDay.Dawn;

    /// <summary>The year starts with newleaf, and every season lasts the same number of moons.</summary>
    public Season Season => (Season)((Day - 1) / DaysPerSeason % 4);

    /// <summary>Advances the world by one tick (6 hours).</summary>
    public void Tick() {
        if (TimeOfDay == TimeOfDay.Dawn && (Day - 1) % DaysPerSeason == 0)
            Log($"{Season} begins.", LogLevel.Summary);

        LoseSatiation();
        DecideWhoSleeps();
        _ateThisTick.Clear();

        if (TimeOfDay != TimeOfDay.Night) {
            SendHuntingPatrolsIfNeeded();
            FeedHungryCats();
        }

        UpdateEnergy();
        UpdateStarvation();

        if (TimeOfDay == TimeOfDay.Night) {
            LogDaySummary();

            if (Day % Tuning.DaysPerMoon == 0)
                LogMoonSummary();
        }

        AdvanceTime();
    }

    private void LoseSatiation() {
        foreach (var cat in Clan.Cats) {
            var wasStarving = cat.IsStarving;
            // Young cats need a different amount of food than adults, nursing kits none at all
            cat.LoseSatiation(Tuning.SatiationLossPerTick * cat.FoodNeed);

            if (cat.IsStarving && !wasStarving) {
                Log($"{cat} is starving!");
                _timesStarvedThisMoon++;
            }
        }
    }

    /// <summary>At night every cat sleeps. During the day, tired cats sleep and can't go on patrol.</summary>
    private void DecideWhoSleeps() {
        _sleeping = Clan.Cats
            .Where(c => TimeOfDay == TimeOfDay.Night || c.IsTired)
            .ToHashSet();
    }

    /// <summary>Sleeping cats regain energy, awake cats lose it. Happens at the end of a tick.</summary>
    private void UpdateEnergy() {
        foreach (var cat in Clan.Cats) {
            if (_sleeping.Contains(cat)) {
                cat.Sleep(Tuning.SleepEnergy);
                continue;
            }

            var wasExhausted = cat.IsExhausted;
            cat.LoseEnergy(Tuning.EnergyLossPerTick);

            if (cat.IsExhausted && !wasExhausted) {
                Log($"{cat} is exhausted!");
                _timesExhaustedThisMoon++;
            }
        }
    }

    /// <summary>
    ///     Starving cats that didn't eat this tick get a starvation stack (eating removes one, see <see cref="Cat.Eat" />).
    ///     A cat with too many stacks dies.
    /// </summary>
    private void UpdateStarvation() {
        // ToList() makes a copy, because dead cats are removed from the Clan while looping
        foreach (var cat in Clan.Cats.ToList()) {
            if (!cat.IsStarving || _ateThisTick.Contains(cat))
                continue;

            cat.AddStarvationStack();

            if (cat.StarvationStacks > _peakStarvationStacksThisMoon) {
                _peakStarvationStacksThisMoon = cat.StarvationStacks;
                _peakStarvationCatThisMoon = cat.Name;
            }

            if (cat.HasStarvedToDeath) {
                Clan.RecordDeath(cat);
                _deathsThisMoon++;
                Log($"{cat} starved to death. {Clan.Cats.Count} cats are left.", LogLevel.Summary);
            }
        }

        // For the mean in the moon summary: every living cat counts once per tick
        _starvationStackSumThisMoon += Clan.Cats.Sum(c => c.StarvationStacks);
        _catTicksThisMoon += Clan.Cats.Count;
    }

    /// <summary>
    ///     If the pile has fewer mice than there are cats, the leader sends patrols
    ///     until they are expected to catch enough to make up the difference.
    ///     The leader plans with normal hunting chances and doesn't account for the
    ///     season, so patrols bring back more than expected in greenleaf and less in leaf-bare.
    /// </summary>
    private void SendHuntingPatrolsIfNeeded() {
        var miceNeeded = Clan.Cats.Count - Clan.FreshKillPile.Mice;
        if (miceNeeded <= 0)
            return;

        var available = AvailableHunters();
        if (available.Count == 0) {
            Log("The pile is low, but no cat can hunt.");
            return;
        }

        var hunters = new List<Cat>();
        var expectedCatch = 0f;
        while (expectedCatch < miceNeeded * Tuning.PatrolPlanningMargin && available.Count > 0) {
            var patrol = available.Take(Tuning.PatrolSize).ToList();
            available.RemoveRange(0, patrol.Count);

            expectedCatch += patrol.Sum(c => c.CatchChance * Tuning.AttemptsPerCat);
            SendHuntingPatrol(patrol);
            hunters.AddRange(patrol);
        }

        _lastHunters = hunters;
    }

    /// <summary>
    ///     Cats that can hunt, are awake and were not out hunting last time, best hunters first.
    ///     Tired cats have a lower catch chance, so they are picked later.
    ///     The leader and deputy come last, so they only go if there aren't enough other cats.
    /// </summary>
    private List<Cat> AvailableHunters() {
        return Clan.Cats
            .Where(c => c.CanHunt && !_sleeping.Contains(c) && !_lastHunters.Contains(c))
            .OrderBy(c => c.Rank is Rank.Leader or Rank.Deputy)
            .ThenByDescending(c => c.CatchChance)
            .ToList();
    }

    private void SendHuntingPatrol(List<Cat> patrol) {
        var catches = new List<string>();
        var total = 0;
        foreach (var cat in patrol) {
            var caught = Hunt(cat);
            total += caught;
            catches.Add($"{cat.Name} {caught}");
        }

        Clan.FreshKillPile.Add(total);
        _miceCaughtThisMoon += total;
        Log($"Hunting patrol caught {total} mice ({string.Join(", ", catches)}). Pile: {Clan.FreshKillPile.Mice}");
    }

    /// <summary>Rolls every hunting attempt of one cat and returns how many mice it caught.</summary>
    private int Hunt(Cat cat) {
        var chance = Math.Clamp(cat.CatchChance * Tuning.SeasonCatchMultiplier(Season), 0f, 1f);

        var caught = 0;
        for (var i = 0; i < Tuning.AttemptsPerCat; i++)
            if (_random.NextDouble() < chance)
                caught++;
        return caught;
    }

    private void FeedHungryCats() {
        var ate = new List<Cat>();
        var wentHungry = new List<Cat>();
        // Remembered before eating, because eating ends starvation
        var wasStarving = Clan.Cats.Where(c => c.IsStarving).ToHashSet();

        foreach (var cat in Clan.CatsInEatingOrder().Where(c => c.IsHungry))
            if (Clan.FreshKillPile.TryTakeMouse()) {
                cat.Eat(Tuning.MouseSatiation);
                _ateThisTick.Add(cat);
                ate.Add(cat);
            }
            else {
                wentHungry.Add(cat);
            }

        if (ate.Count > 0)
            Log($"{ate.Count} cats ate ({Names(ate)}). Pile: {Clan.FreshKillPile.Mice}");

        if (wentHungry.Count > 0)
            Log($"No prey left for {Names(wentHungry)}.");

        // The leader's feeding policy decided who got food. Tell the story of that decision.
        var fedStarving = ate.Where(wasStarving.Contains).ToList();
        var passedOverStarving = wentHungry.Where(wasStarving.Contains).ToList();
        var fedInstead = ate.Where(c => !wasStarving.Contains(c)).ToList();

        if (passedOverStarving.Count > 0 && fedInstead.Count > 0) {
            Log($"{Names(fedInstead)} ate while the starving {Names(passedOverStarving)} got nothing.");
            _starvingPassedOverThisMoon += passedOverStarving.Count;
        }
        else if (fedStarving.Count > 0 && wentHungry.Count > 0) {
            Log($"{Names(wentHungry)} went hungry so that the starving {Names(fedStarving)} could eat.");
        }

        _mealsMissedThisMoon += wentHungry.Count;
    }

    private static string Names(IEnumerable<Cat> cats) {
        return string.Join(", ", cats.Select(c => c.Name));
    }

    private void LogDaySummary() {
        // DefaultIfEmpty() avoids a crash when every cat has died
        var averageSatiation = Clan.Cats.Select(c => c.Satiation).DefaultIfEmpty().Average();
        var averageEnergy = Clan.Cats.Select(c => c.Energy).DefaultIfEmpty().Average();
        var hungry = Clan.Cats.Count(c => c.IsHungry);
        var starving = Clan.Cats.Count(c => c.IsStarving);
        var mostStacks = Clan.Cats.Select(c => c.StarvationStacks).DefaultIfEmpty().Max();
        var meanStacks = Clan.Cats.Select(c => (float)c.StarvationStacks).DefaultIfEmpty().Average();

        Log($"End of day. Pile: {Clan.FreshKillPile.Mice}, average satiation: {averageSatiation:0}, "
            + $"hungry: {hungry}, starving: {starving}, average energy: {averageEnergy:0}, "
            + $"starvation stacks: most {mostStacks}, mean {meanStacks:0.00}");
    }

    private void LogMoonSummary() {
        var averageSatiation = Clan.Cats.Select(c => c.Satiation).DefaultIfEmpty().Average();
        var starving = Clan.Cats.Count(c => c.IsStarving);
        var meanStacks = _catTicksThisMoon > 0 ? (float)_starvationStackSumThisMoon / _catTicksThisMoon : 0f;
        var peak = _peakStarvationStacksThisMoon > 0
            ? $"{_peakStarvationStacksThisMoon} ({_peakStarvationCatThisMoon})"
            : "0";

        Log($"Moon {Day / Tuning.DaysPerMoon} ({Season}) over. Cats alive: {Clan.Cats.Count}, "
            + $"starvation stacks: most {peak}, mean {meanStacks:0.00}, "
            + $"died this moon: {_deathsThisMoon}, starving cats passed over: {_starvingPassedOverThisMoon}, "
            + $"mice caught: {_miceCaughtThisMoon}, "
            + $"meals missed: {_mealsMissedThisMoon}, times a cat starved: {_timesStarvedThisMoon}, "
            + $"times a cat was exhausted: {_timesExhaustedThisMoon}, "
            + $"pile: {Clan.FreshKillPile.Mice}, average satiation: {averageSatiation:0}, "
            + $"starving now: {starving}", LogLevel.Summary);

        _miceCaughtThisMoon = 0;
        _mealsMissedThisMoon = 0;
        _timesStarvedThisMoon = 0;
        _timesExhaustedThisMoon = 0;
        _deathsThisMoon = 0;
        _starvingPassedOverThisMoon = 0;
        _peakStarvationStacksThisMoon = 0;
        _peakStarvationCatThisMoon = "";
        _starvationStackSumThisMoon = 0;
        _catTicksThisMoon = 0;
    }

    private void AdvanceTime() {
        if (TimeOfDay == TimeOfDay.Night) {
            Day++;
            TimeOfDay = TimeOfDay.Dawn;
        }
        else {
            TimeOfDay++;
        }
    }

    private void Log(string message, LogLevel level = LogLevel.Detail) {
        _log(level, $"Day {Day,3} {TimeOfDay,-7} | {message}");
    }
}