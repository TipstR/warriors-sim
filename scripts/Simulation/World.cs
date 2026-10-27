using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>
///     Runs the simulation one tick at a time and reports what happens to the log.
/// </summary>
public class World {
    private static int DaysPerSeason => Tuning.DaysPerMoon * Tuning.MoonsPerSeason;
    private readonly Action<LogLevel, string> _log;
    private readonly Random _random;
    private List<Cat> _lastHunters = new();
    private HashSet<Cat> _sleeping = new();
    private readonly HashSet<Cat> _ateThisTick = new();
    private int _mealsMissedThisMoon;
    private int _miceCaughtToday;
    private int _birthsThisMoon;
    private int _ticksWithoutFamineHunger;

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

    /// <summary>The current moon, starting at 1.</summary>
    public int Moon => (Day - 1) / Tuning.DaysPerMoon + 1;

    /// <summary>Whether the cat slept during the last tick.</summary>
    public bool IsSleeping(Cat cat) {
        return _sleeping.Contains(cat);
    }

    /// <summary>Advances the world by one tick (6 hours).</summary>
    public void Tick() {
        if (TimeOfDay == TimeOfDay.Dawn && (Day - 1) % DaysPerSeason == 0)
            Log($"{Season} begins.", LogLevel.Summary);

        _ateThisTick.Clear();
        UpdateNursery();
        LoseSatiation();
        DecideWhoSleeps();

        if (TimeOfDay != TimeOfDay.Night) {
            SendHuntingPatrolsIfNeeded();
            FeedHungryCats();
        }

        UpdateEnergy();
        UpdateStarvation();

        if (TimeOfDay == TimeOfDay.Night) {
            AdvancePregnancies();
            LogDaySummary();

            if (Day % Tuning.DaysPerMoon == 0) {
                AgeOneMoon();
                FindMates();
                ConceiveKits();
                LogMoonSummary();
            }
        }

        EnsureLeadership();
        AdvanceTime();
    }

    /// <summary>
    ///     The queens in the nursery share their milk: all nursing kits are fed by all nursing queens together,
    ///     orphans included. A starving queen has no milk, and one queen can feed at most
    ///     <see cref="Tuning.MaxKitsPerQueen" /> kits. Kits without milk go hungry.
    /// </summary>
    private void UpdateNursery() {
        var nursingKits = Clan.Cats
            .Where(c => c.Rank == Rank.Kit && c.AgeMoons < Tuning.WeanedAgeMoons)
            .ToList();
        var queens = nursingKits
            .Select(k => k.Mother)
            .Where(m => m != null && Clan.Cats.Contains(m))
            .Distinct()
            .ToList();

        foreach (var cat in Clan.Cats) {
            cat.IsNursing = queens.Contains(cat);
            cat.MilkLoad = 0f;
        }

        if (nursingKits.Count == 0)
            return;

        var queensWithMilk = queens.Where(q => !q.IsStarving).ToList();
        // The youngest kits are fed first
        var fedKits = nursingKits
            .OrderBy(k => k.AgeMoons)
            .Take(queensWithMilk.Count * Tuning.MaxKitsPerQueen)
            .ToHashSet();

        foreach (var queen in queensWithMilk)
            queen.MilkLoad = (float)fedKits.Count / queensWithMilk.Count;

        var withoutMilk = new List<Cat>();
        foreach (var kit in nursingKits) {
            if (fedKits.Contains(kit)) {
                kit.Eat(Tuning.SatiationLossPerTick);
                _ateThisTick.Add(kit);
            }
            else {
                kit.LoseSatiation(Tuning.SatiationLossPerTick);
                withoutMilk.Add(kit);
            }
        }

        if (withoutMilk.Count > 0)
            Log($"There is no milk for {Names(withoutMilk)}.");
    }

    /// <summary>Counts down every pregnancy by one day and lets the kits be born.</summary>
    private void AdvancePregnancies() {
        foreach (var queen in Clan.Cats.Where(c => c.IsPregnant).ToList()) {
            var father = queen.FatherOfUnbornKits;
            if (queen.AdvancePregnancy())
                GiveBirth(queen, father);
        }
    }

    private void GiveBirth(Cat mother, Cat father) {
        var litterSize = RollLitterSize();
        var kits = new List<Cat>();

        for (var i = 0; i < litterSize; i++) {
            var prefix = CatNames.RandomKitPrefix(Clan.Cats.Select(c => c.Prefix), _random);
            var sex = _random.Next(2) == 0 ? Sex.SheCat : Sex.Tom;
            var diligence = _random.Next(Tuning.SkillMin + 20, Tuning.SkillMax - 19);
            var kit = new Cat(prefix + CatNames.KitSuffix, Rank.Kit, sex, 0, 0, diligence, mother, father);
            Clan.AddCat(kit);
            kits.Add(kit);
        }

        _birthsThisMoon += litterSize;
        var fatherText = father != null ? $" Their father is {father.Name}." : "";
        Log($"{mother.Name} gave birth to {litterSize} kits: {Names(kits)}.{fatherText}", LogLevel.Summary);
    }

    private int RollLitterSize() {
        var weights = Tuning.LitterSizeWeights;
        var roll = _random.Next(weights.Sum());
        for (var size = 0; size < weights.Length; size++) {
            roll -= weights[size];
            if (roll < 0)
                return size;
        }

        return weights.Length - 1;
    }

    /// <summary>
    ///     Placeholder until relationships exist: single she-cat warriors sometimes become mates with a
    ///     single tom of a similar age who isn't close kin.
    /// </summary>
    private void FindMates() {
        var singles = Clan.Cats
            .Where(c => c.Mate == null && c.Rank is Rank.Warrior or Rank.Deputy)
            .ToList();

        foreach (var sheCat in singles.Where(c => c.Sex == Sex.SheCat)) {
            if (sheCat.Mate != null || _random.NextDouble() >= Tuning.MateChancePerMoon)
                continue;

            var candidates = singles
                .Where(t => t.Sex == Sex.Tom && t.Mate == null && !t.IsCloseKinOf(sheCat))
                .Where(t => Math.Abs(t.AgeMoons - sheCat.AgeMoons) <= Tuning.MaxMateAgeDifferenceMoons)
                .ToList();
            if (candidates.Count == 0)
                continue;

            var tom = candidates[_random.Next(candidates.Count)];
            sheCat.BecomeMates(tom);
            Log($"{sheCat.Name} and {tom.Name} have become mates.", LogLevel.Summary);
        }
    }

    /// <summary>
    ///     She-cats with a mate may expect kits. Less likely outside newleaf and greenleaf,
    ///     and less likely when the Clan went hungry this moon.
    /// </summary>
    private void ConceiveKits() {
        // Share of meals the Clan missed this moon (a cat can eat up to 3 times a day)
        var possibleMeals = Math.Max(1, Clan.Cats.Count * Tuning.DaysPerMoon * 3);
        var missedShare = (float)_mealsMissedThisMoon / possibleMeals;
        var foodFactor = 1f / (1f + missedShare * Tuning.HungerConceptionPenalty);
        var chance = Tuning.ConceptionChancePerMoon * Tuning.SeasonConceptionMultiplier(Season)
                     * foodFactor * CrowdingFactor();

        var mothers = Clan.Cats.Where(c =>
            c.Sex == Sex.SheCat && c.Mate != null && !c.IsQueen
            && c.Rank is Rank.Warrior or Rank.Deputy
            && c.AgeMoons <= Tuning.FertileUntilMoons);

        foreach (var sheCat in mothers.ToList()) {
            if (_random.NextDouble() >= chance)
                continue;

            sheCat.BecomePregnant(sheCat.Mate);
            Log($"{sheCat.Name} is expecting {sheCat.Mate.Name}'s kits.", LogLevel.Summary);
        }
    }

    /// <summary>
    ///     From 1 (plenty of room) to 0 (the territory is full): how many cats the territory could feed in
    ///     <see cref="Tuning.CrowdingSeason" /> compared to how many the Clan has. Births get rarer long before
    ///     the limit is reached.
    /// </summary>
    private float CrowdingFactor() {
        var micePerAdultPerDay = Tuning.SatiationLossPerTick * Tuning.TicksPerDay / Tuning.MouseSatiation;
        var prey = Tuning.TerritoryPreyPerDay * Tuning.SeasonCatchMultiplier(Tuning.CrowdingSeason);
        var catsTheTerritoryFeeds = prey / micePerAdultPerDay;

        var fullness = Clan.Cats.Count / catsTheTerritoryFeeds;
        return Math.Max(0f, 1f - fullness * fullness);
    }

    /// <summary>
    ///     Every cat gets a moon older: apprentices train, some old cats die,
    ///     and cats that are old enough move on to their next rank.
    /// </summary>
    private void AgeOneMoon() {
        foreach (var cat in Clan.Cats.ToList()) {
            cat.AgeOneMoon();

            if (cat.Rank == Rank.Apprentice)
                cat.ImproveHunting(Tuning.ApprenticeHuntingGainPerMoon);

            if (DiesOfOldAge(cat)) {
                Clan.RecordDeath(cat);
                _deathsThisMoon++;
                Log($"{cat} died of old age at {cat.AgeMoons} moons. {Clan.Cats.Count} cats are left.",
                    LogLevel.Summary);
                continue;
            }

            if (cat.Rank == Rank.Kit && cat.AgeMoons >= Tuning.ApprenticeAgeMoons) {
                var kitName = cat.Name;
                cat.ChangeRank(Rank.Apprentice);
                Log($"{kitName} has become an apprentice: {cat.Name}.", LogLevel.Summary);
            }
            else if (cat.Rank == Rank.Apprentice && cat.AgeMoons >= Tuning.WarriorAgeMoons) {
                var apprenticeName = cat.Name;
                var otherNames = Clan.Cats.Where(c => c != cat).Select(c => c.Name);
                cat.ChangeRank(Rank.Warrior, CatNames.RandomWarriorSuffix(cat.Prefix, otherNames, _random));
                Log($"{apprenticeName} has become a warrior: {cat.Name}.", LogLevel.Summary);
            }
            else if (cat.Rank is Rank.Warrior or Rank.Deputy && cat.AgeMoons >= Tuning.RetireAgeMoons) {
                cat.ChangeRank(Rank.Elder);
                Log($"{cat.Name} has retired to the elders' den.", LogLevel.Summary);
            }
        }
    }

    /// <summary>The chance starts at <see cref="Tuning.OldAgeStartMoons" /> and doubles regularly after that.</summary>
    private bool DiesOfOldAge(Cat cat) {
        if (cat.AgeMoons < Tuning.OldAgeStartMoons)
            return false;

        var doublings = (float)(cat.AgeMoons - Tuning.OldAgeStartMoons) / Tuning.OldAgeDeathChanceDoublingMoons;
        var chance = Tuning.OldAgeDeathChanceAtStart * MathF.Pow(2f, doublings);
        return _random.NextDouble() < chance;
    }

    /// <summary>
    ///     If the leader has died, the deputy becomes leader. If there is no deputy,
    ///     the best hunter among the warriors is chosen.
    /// </summary>
    private void EnsureLeadership() {
        if (Clan.Leader == null) {
            var successor = Clan.Deputy ?? ChooseDeputy();
            if (successor != null) {
                var oldName = successor.Name;
                successor.ChangeRank(Rank.Leader);
                Log($"{oldName} is the new leader of {Clan.Name}: {successor.Name}.", LogLevel.Summary);
            }
        }

        if (Clan.Deputy == null && Clan.Leader != null) {
            var deputy = ChooseDeputy();
            if (deputy != null) {
                deputy.ChangeRank(Rank.Deputy);
                Log($"{deputy.Name} is the new deputy of {Clan.Name}.", LogLevel.Summary);
            }
        }
    }

    /// <summary>For now the best hunter among the warriors. Later the leader's personality will decide.</summary>
    private Cat ChooseDeputy() {
        return Clan.Cats
            .Where(c => c.Rank == Rank.Warrior)
            .OrderByDescending(c => c.Hunting)
            .ThenByDescending(c => c.AgeMoons)
            .FirstOrDefault();
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

            if (_random.NextDouble() < cat.StarvationDeathChance) {
                Clan.RecordDeath(cat);
                _deathsThisMoon++;
                Log($"{cat} starved to death. {Clan.Cats.Count} cats are left.", LogLevel.Summary);
            }
        }

        // For the mean in the moon summary: every living cat counts once per tick
        _starvationStackSumThisMoon += Clan.Cats.Sum(c => c.StarvationStacks);
        _catTicksThisMoon += Clan.Cats.Count;

        UpdateFamine();
    }

    /// <summary>
    ///     A famine starts when at least <see cref="Tuning.FamineStarvingCats" /> cats carry starvation stacks.
    ///     It only ends after there were fewer than that for <see cref="Tuning.FamineEndTicks" /> ticks in a row,
    ///     so the usual rise of hunger at night doesn't start and end a famine every day.
    /// </summary>
    private void UpdateFamine() {
        var manyStarving = Clan.StarvingCatCount >= Tuning.FamineStarvingCats;

        if (!Clan.IsInFamine) {
            if (manyStarving) {
                Clan.IsInFamine = true;
                _ticksWithoutFamineHunger = 0;
                Log($"Famine in {Clan.Name}! Elders and queens join the hunt.", LogLevel.Summary);
            }

            return;
        }

        _ticksWithoutFamineHunger = manyStarving ? 0 : _ticksWithoutFamineHunger + 1;
        if (_ticksWithoutFamineHunger >= Tuning.FamineEndTicks) {
            Clan.IsInFamine = false;
            Log($"The famine in {Clan.Name} is over.", LogLevel.Summary);
        }
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
    ///     The leader and deputy come after the others, and in a famine elders and queens help as a last reserve.
    /// </summary>
    private List<Cat> AvailableHunters() {
        var famine = Clan.IsInFamine;
        return Clan.Cats
            .Where(c => (famine ? c.CanHuntInFamine : c.CanHunt) && !_sleeping.Contains(c) && !_lastHunters.Contains(c))
            .OrderBy(HuntingReserveOrder)
            .ThenByDescending(c => c.CatchChance)
            .ToList();
    }

    /// <summary>0 = warriors and apprentices, 1 = leader and deputy, 2 = famine helpers (elders and queens).</summary>
    private static int HuntingReserveOrder(Cat cat) {
        if (!cat.CanHunt)
            return 2;
        return cat.Rank is Rank.Leader or Rank.Deputy ? 1 : 0;
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
        var caught = 0;
        for (var i = 0; i < Tuning.AttemptsPerCat; i++) {
            // The territory only has so much prey per day, and less in bad seasons.
            // The more was already caught today, the harder it gets to find more, until there is none left.
            var seasonMultiplier = Tuning.SeasonCatchMultiplier(Season);
            var preyToday = Tuning.TerritoryPreyPerDay * seasonMultiplier;
            var used = Math.Min(1f, _miceCaughtToday / preyToday);
            var preyLeft = 1f - MathF.Pow(used, Tuning.PreyDepletionSharpness);
            var chance = Math.Clamp(cat.CatchChance * seasonMultiplier * preyLeft, 0f, 1f);

            if (_random.NextDouble() < chance) {
                caught++;
                _miceCaughtToday++;
            }
        }

        return caught;
    }

    private void FeedHungryCats() {
        var ate = new List<Cat>();
        var wentHungry = new List<Cat>();
        // Remembered before eating, because eating ends starvation
        var wasStarving = Clan.Cats.Where(c => c.IsStarving).ToHashSet();

        foreach (var cat in Clan.CatsInEatingOrder().Where(c => c.IsHungry && c.CanEatPrey))
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
            + $"born: {_birthsThisMoon}, died: {_deathsThisMoon}, "
            + $"kits: {Clan.Cats.Count(c => c.Rank == Rank.Kit)}, queens: {Clan.Cats.Count(c => c.IsQueen)}, "
            + $"starving cats passed over: {_starvingPassedOverThisMoon}, "
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
        _birthsThisMoon = 0;
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
            _miceCaughtToday = 0;
        }
        else {
            TimeOfDay++;
        }
    }

    private void Log(string message, LogLevel level = LogLevel.Detail) {
        _log(level, $"Day {Day,3} {TimeOfDay,-7} | {message}");
    }
}