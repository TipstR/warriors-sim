using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>
///     Runs the simulation one tick at a time and reports what happens to the log.
///     Split into several files by topic (partial class), e.g. World.Health.cs for sickness.
/// </summary>
public partial class World {
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

    /// <summary>Cats whose main skill (hunting, or healing for medicine cats) has faded and not come back yet.</summary>
    private readonly HashSet<Cat> _feelsRusty = new();

    /// <summary>A cat feels rusty (in the log) once its main skill is this many points below its best.</summary>
    private const float RustyPoints = 2f;

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
        UpdateHealth();

        if (TimeOfDay == TimeOfDay.Night) {
            AdvancePregnancies();
            LogDaySummary();

            if (Day % Tuning.DaysPerMoon == 0) {
                AgeOneMoon();
                FadeUnusedSkills();
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
            var huntingGift = RollGift(mother.Skills[SkillType.Hunting].Gift, FathersGift(father, SkillType.Hunting));
            var mentoringGift = RollGift(mother.Skills[SkillType.Mentoring].Gift, FathersGift(father, SkillType.Mentoring));
            var kit = new Cat(prefix + CatNames.KitSuffix, Rank.Kit, sex, 0, 0, diligence,
                huntingGift, mentoringGift, mother, father);
            var healingGift = RollHealingGift(mother.Skills[SkillType.Healing].Gift,
                FathersGift(father, SkillType.Healing));
            kit.SetSkill(SkillType.Healing, 0, healingGift);
            Clan.AddCat(kit);
            kits.Add(kit);
        }

        _birthsThisMoon += litterSize;
        var fatherText = father != null ? $" Their father is {father.Name}." : "";
        Log($"{mother.Name} gave birth to {litterSize} kits: {Names(kits)}.{fatherText}", LogLevel.Summary);
    }

    /// <summary>The father's gift for the skill, or null if the father is unknown.</summary>
    private static int? FathersGift(Cat father, SkillType skill) {
        return father?.Skills[skill].Gift;
    }

    /// <summary>
    ///     Part of a kit's gift comes from its parents (<see cref="Tuning.GiftInheritance" />), the rest is luck.
    ///     The luck is the average of three rolls, so very gifted and very ungifted kits are rare.
    /// </summary>
    private int RollGift(int motherGift, int? fatherGift) {
        var parents = fatherGift.HasValue ? (motherGift + fatherGift.Value) / 2f : motherGift;
        var roll = (_random.Next(Tuning.SkillMax + 1) + _random.Next(Tuning.SkillMax + 1)
                    + _random.Next(Tuning.SkillMax + 1)) / 3f;
        var luck = roll - Tuning.AverageGift;
        var gift = Tuning.AverageGift + Tuning.GiftInheritance * (parents - Tuning.AverageGift) + luck;
        return Math.Clamp((int)MathF.Round(gift), Tuning.SkillMin, Tuning.SkillMax);
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
    ///     From 1 (plenty of room) to <see cref="Tuning.MinCrowdingFactor" /> (the territory is full): how many cats the
    ///     territory could feed in <see cref="Tuning.CrowdingSeason" /> compared to how many the Clan has. Births get
    ///     rarer long before the limit is reached, but never stop: in a full Clan, hunger decides who survives.
    ///     A brake that stops all births lets the Clan grow old together and collapse.
    /// </summary>
    private float CrowdingFactor() {
        var micePerAdultPerDay = Tuning.SatiationLossPerTick * Tuning.TicksPerDay / Tuning.MouseSatiation;
        var prey = Tuning.TerritoryPreyPerDay * Tuning.SeasonCatchMultiplier(Tuning.CrowdingSeason);
        var catsTheTerritoryFeeds = prey / micePerAdultPerDay;

        var fullness = Clan.Cats.Count / catsTheTerritoryFeeds;
        return Math.Max(Tuning.MinCrowdingFactor, 1f - fullness * fullness);
    }

    /// <summary>
    ///     Every cat gets a moon older: apprentices train, some old cats die,
    ///     and cats that are old enough move on to their next rank.
    /// </summary>
    private void AgeOneMoon() {
        ReplaceLostMentors();
        var medicineApprentice = ChooseMedicineApprentice();

        foreach (var cat in Clan.Cats.ToList()) {
            cat.AgeOneMoon();

            if (cat.Rank == Rank.Apprentice)
                Train(cat);
            else if (cat.Rank == Rank.MedicineCatApprentice)
                TrainMedicineApprentice(cat);

            if (DiesOfOldAge(cat)) {
                Clan.RecordDeath(cat);
                _deathsThisMoon++;
                Log($"{cat} died of old age at {cat.AgeMoons} moons. {Clan.Cats.Count} cats are left.",
                    LogLevel.Summary);
                continue;
            }

            if (cat == medicineApprentice) {
                MakeMedicineApprentice(cat);
            }
            else if (cat.Rank == Rank.Kit && cat.AgeMoons >= Tuning.ApprenticeAgeMoons) {
                var kitName = cat.Name;
                cat.ChangeRank(Rank.Apprentice);
                var mentor = FindMentor(cat);
                var mentorText = mentor != null
                    ? $"{mentor.Name} is the mentor."
                    : "No warrior is free to mentor them.";
                Log($"{kitName} has become an apprentice: {cat.Name}. {mentorText}", LogLevel.Summary);
            }
            else if (cat.Rank == Rank.Apprentice && cat.AgeMoons >= Tuning.WarriorAgeMoons) {
                var apprenticeName = cat.Name;
                var otherNames = Clan.Cats.Where(c => c != cat).Select(c => c.Name);
                cat.ChangeRank(Rank.Warrior, CatNames.RandomWarriorSuffix(cat.Prefix, otherNames, _random));
                var mentorText = cat.Mentor != null ? $", trained by {cat.Mentor.Name}" : "";
                Log($"{apprenticeName} has become a warrior: {cat.Name} (hunting {cat.Skills[SkillType.Hunting].Level:0}{mentorText}).",
                    LogLevel.Summary);
            }
            else if (cat.Rank == Rank.MedicineCatApprentice && cat.AgeMoons >= Tuning.MedicineCatAgeMoons) {
                MakeMedicineCat(cat);
            }
            else if (cat.Rank is Rank.Warrior or Rank.Deputy && cat.AgeMoons >= Tuning.RetireAgeMoons) {
                cat.ChangeRank(Rank.Elder);
                Log($"{cat.Name} has retired to the elders' den.", LogLevel.Summary);
            }
        }
    }

    /// <summary>
    ///     An apprentice learns in camp every moon. How much depends on its hunting gift and on its mentor.
    ///     Without a mentor it only learns a little on its own. The other half of its training are the lessons on
    ///     training patrols (see <see cref="SendTrainingPatrols" />), where the mentor gets better at mentoring.
    /// </summary>
    private void Train(Cat apprentice) {
        var mentor = IsAbleToMentor(apprentice.Mentor) ? apprentice.Mentor : null;
        var mentorFactor = mentor?.MentorFactor ?? Tuning.NoMentorFactor;
        apprentice.Skills[SkillType.Hunting].Improve(Tuning.ApprenticeHuntingGainPerMoon * mentorFactor);
    }

    /// <summary>
    ///     Use it or lose it: every skill a cat didn't practice this moon fades a little (see <see cref="Skill.FadeIfUnused" />).
    ///     Apprentices don't lose anything, because their training counts as practice. Mentoring never fades: a cat
    ///     can only practice it with an apprentice, so a warrior who waits long for one would be punished for it.
    ///     Tells the story when a cat feels rusty and when it is back to its old self.
    /// </summary>
    private void FadeUnusedSkills() {
        foreach (var cat in Clan.Cats) {
            foreach (var (type, skill) in cat.Skills)
                if (type != SkillType.Mentoring)
                    skill.FadeIfUnused();

            // Elders stop hunting for good, that's no story
            if (cat.Rank is Rank.Kit or Rank.Elder)
                continue;

            var mainSkill = cat.IsMedicineCat ? SkillType.Healing : SkillType.Hunting;
            var level = cat.Skills[mainSkill];
            if (level.Peak - level.Level >= RustyPoints && _feelsRusty.Add(cat))
                Log(mainSkill == SkillType.Healing
                    ? $"{cat.Name} hasn't cared for anyone in a while and feels rusty."
                    : $"{cat.Name} hasn't hunted in a while and feels rusty.", LogLevel.Summary);
            else if (level.Level >= level.Peak && _feelsRusty.Remove(cat))
                Log($"{cat.Name} is back to {(mainSkill == SkillType.Healing ? "healing" : "hunting")} as well as ever.",
                    LogLevel.Summary);
        }

        // Dead cats don't need to be remembered
        _feelsRusty.RemoveWhere(c => !Clan.Cats.Contains(c));
    }

    /// <summary>
    ///     Apprentices whose mentor has died, retired or moved to the nursery get a new one.
    ///     If none is free, they keep their old mentor, who takes them back when able again.
    /// </summary>
    private void ReplaceLostMentors() {
        var withoutMentor = Clan.Cats
            .Where(c => c.Rank == Rank.Apprentice && !IsAbleToMentor(c.Mentor))
            .ToList();

        foreach (var apprentice in withoutMentor) {
            var mentor = FindMentor(apprentice);
            if (mentor != null)
                Log($"{mentor.Name} is {apprentice.Name}'s new mentor.", LogLevel.Summary);
        }
    }

    private bool IsAbleToMentor(Cat mentor) {
        return mentor != null && mentor.CanMentor && Clan.Cats.Contains(mentor);
    }

    /// <summary>
    ///     The leader chooses a mentor among the cats who can mentor and don't have an apprentice yet.
    ///     The leader can't see who has a gift for mentoring, but sees who hunts well and who taught well before,
    ///     so good hunters and good mentors are more likely to be chosen, but every free cat has a chance.
    ///     Later the leader's personality will decide.
    ///     Returns the new mentor, or null (and changes nothing) if no cat is free.
    /// </summary>
    private Cat FindMentor(Cat apprentice) {
        var mentor = Clan.Cats
            .Where(c => c.CanMentor && Clan.ApprenticeOf(c) == null)
            .OrderByDescending(c => (c.Skills[SkillType.Hunting].Level + c.Skills[SkillType.Mentoring].Level) / Tuning.SkillMax
                                    + (float)_random.NextDouble())
            .FirstOrDefault();

        if (mentor != null)
            apprentice.SetMentor(mentor);
        return mentor;
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
            .OrderByDescending(c => c.Skills[SkillType.Hunting].Level)
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
    ///     Outside a famine, the leader first sends the apprentices out on training patrols, even if the pile is full
    ///     (see <see cref="SendTrainingPatrols" />). Then, if the pile has fewer mice than there are cats, the leader
    ///     sends patrols until they are expected to catch enough to make up the difference, the training patrols'
    ///     catch included. The leader plans with normal hunting chances and doesn't account for the
    ///     season, so patrols bring back more than expected in greenleaf and less in leaf-bare.
    /// </summary>
    private void SendHuntingPatrolsIfNeeded() {
        var available = AvailableHunters();
        var hunters = Clan.IsInFamine ? new List<Cat>() : SendTrainingPatrols(available);
        var expectedCatch = hunters.Sum(c => c.CatchChance * Tuning.AttemptsPerCat);

        var miceNeeded = Clan.Cats.Count - Clan.FreshKillPile.Mice;
        if (miceNeeded > 0 && hunters.Count == 0 && available.Count == 0)
            Log("The pile is low, but no cat can hunt.");

        while (expectedCatch < miceNeeded * Tuning.PatrolPlanningMargin && available.Count > 0) {
            var patrol = available.Take(Tuning.PatrolSize).ToList();
            available.RemoveRange(0, patrol.Count);

            expectedCatch += patrol.Sum(c => c.CatchChance * Tuning.AttemptsPerCat);
            SendHuntingPatrol(patrol);
            hunters.AddRange(patrol);
        }

        if (hunters.Count > 0)
            _lastHunters = hunters;
    }

    /// <summary>
    ///     Every apprentice who can hunt goes out with a teacher, two pairs per patrol. The leader pairs each
    ///     apprentice with its mentor. If the mentor can't go (asleep, sick, hunted last time, ...), another warrior
    ///     helps out, preferably one without an apprentice of its own. Apprentices without a teacher stay with the
    ///     normal patrols. Removes everyone it sends from <paramref name="available" /> and returns them.
    /// </summary>
    private List<Cat> SendTrainingPatrols(List<Cat> available) {
        var apprentices = available.Where(c => c.Rank == Rank.Apprentice).ToList();
        var pairs = new List<(Cat Apprentice, Cat Teacher)>();

        // First every apprentice whose mentor can come, so no helper takes a mentor away from its own apprentice
        foreach (var apprentice in apprentices.Where(a => available.Contains(a.Mentor)).ToList()) {
            pairs.Add((apprentice, apprentice.Mentor));
            available.Remove(apprentice);
            available.Remove(apprentice.Mentor);
            apprentices.Remove(apprentice);
        }

        foreach (var apprentice in apprentices) {
            var helper = ChooseHelper(available);
            if (helper == null)
                break;
            pairs.Add((apprentice, helper));
            available.Remove(apprentice);
            available.Remove(helper);
        }

        var sent = new List<Cat>();
        foreach (var patrolPairs in pairs.Chunk(Tuning.PatrolSize / 2)) {
            var lessons = patrolPairs.Select(p => p.Teacher == p.Apprentice.Mentor
                ? $"{p.Apprentice.Name} with {p.Teacher.Name}"
                : $"{p.Apprentice.Name} with {p.Teacher.Name} (helping out)");
            Log($"Training patrol: {string.Join(", ", lessons)}.");

            var patrol = patrolPairs.SelectMany(p => new[] { p.Apprentice, p.Teacher }).ToList();
            SendHuntingPatrol(patrol);
            foreach (var (apprentice, teacher) in patrolPairs)
                GiveLesson(apprentice, teacher);
            sent.AddRange(patrol);
        }

        return sent;
    }

    /// <summary>A warrior who can teach, preferably one without an apprentice of its own. Null if nobody is free.</summary>
    private Cat ChooseHelper(List<Cat> available) {
        var candidates = available.Where(c => c.CanMentor).ToList();
        if (candidates.Count == 0)
            return null;

        var withoutApprentice = candidates.Where(c => Clan.ApprenticeOf(c) == null).ToList();
        var choices = withoutApprentice.Count > 0 ? withoutApprentice : candidates;
        return choices[_random.Next(choices.Count)];
    }

    /// <summary>
    ///     On a training patrol the apprentice learns hunting (the better the teacher, the more), and the teacher gets
    ///     better at mentoring. A helper learns less than the real mentor (<see cref="Tuning.HelperMentoringShare" />).
    /// </summary>
    private static void GiveLesson(Cat apprentice, Cat teacher) {
        apprentice.Skills[SkillType.Hunting].Improve(Tuning.HuntingLessonGain * teacher.MentorFactor);
        var share = teacher == apprentice.Mentor ? 1f : Tuning.HelperMentoringShare;
        teacher.Skills[SkillType.Mentoring].Improve(Tuning.MentoringPracticePerLesson * share);
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
            RollHuntingInjury(cat);
        }

        Clan.FreshKillPile.Add(total);
        _miceCaughtThisMoon += total;
        Log($"Hunting patrol caught {total} mice ({string.Join(", ", catches)}). Pile: {Clan.FreshKillPile.Mice}");
    }

    /// <summary>
    ///     Rolls every hunting attempt of one cat and returns how many mice it caught.
    ///     Every mouse it catches teaches it a little (<see cref="Tuning.HuntingPracticePerMouse" />).
    ///     Going hunting is practice even without a catch, so the skill doesn't fade.
    /// </summary>
    private int Hunt(Cat cat) {
        cat.Skills[SkillType.Hunting].Practice();
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
                cat.Skills[SkillType.Hunting].Improve(Tuning.HuntingPracticePerMouse);
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
        var sick = Clan.Cats.Count(c => c.IsSick);
        var confined = Clan.Cats.Count(c => c.IsConfined);

        Log($"End of day. Pile: {Clan.FreshKillPile.Mice}, average satiation: {averageSatiation:0}, "
            + $"hungry: {hungry}, starving: {starving}, average energy: {averageEnergy:0}, "
            + $"starvation stacks: most {mostStacks}, mean {meanStacks:0.00}, sick: {sick} ({confined} confined)");
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
            + $"starving now: {starving}, {HealthSummary()}", LogLevel.Summary);

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
        ResetHealthStatistics();
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