namespace WarriorsSim.Simulation;

/// <summary>
///     All balancing numbers of the simulation in one place. They can be changed before a simulation is started,
///     e.g. from a settings screen. <see cref="TuningSettings" /> lists them all, grouped, and can reset them.
///     Changing them while a simulation is running works too, but can give strange results.
///     See docs/Milestone1.md for what they mean.
/// </summary>
public static class Tuning {
    // Fixed values: the code depends on them (e.g. TimeOfDay has exactly 4 values), so they can't be tuned
    public const int TicksPerDay = 4;
    public const int SkillMin = 0;
    public const int SkillMax = 100;

    // Time
    [TuningGroup("Time")] public static int DaysPerMoon = 30;
    [TuningGroup("Time")] public static int MoonsPerSeason = 3;
    [TuningGroup("Time")] public static int DaysToSimulate = 720;

    // Skills and traits
    [TuningGroup("Skills")] public static int AverageDiligence = 50;

    // Aging and ranks
    [TuningGroup("Aging")] public static int ApprenticeAgeMoons = 6;
    [TuningGroup("Aging")] public static int WarriorAgeMoons = 12;

    /// <summary>Warriors and deputies retire to the elders' den at this age. Leaders never retire.</summary>
    [TuningGroup("Aging")] public static int RetireAgeMoons = 120;

    /// <summary>How much an apprentice's hunting skill improves every moon of training.</summary>
    [TuningGroup("Aging")] public static int ApprenticeHuntingGainPerMoon = 8;

    /// <summary>From this age on, a cat can die of old age (96 moons = 8 years).</summary>
    [TuningGroup("Aging")] public static int OldAgeStartMoons = 96;

    /// <summary>Chance per moon to die of old age at <see cref="OldAgeStartMoons" />.</summary>
    [TuningGroup("Aging")] public static float OldAgeDeathChanceAtStart = 0.005f;

    /// <summary>The chance to die of old age doubles every this many moons.</summary>
    [TuningGroup("Aging")] public static int OldAgeDeathChanceDoublingMoons = 18;

    // Mates, queens and kits
    /// <summary>Chance per moon that a single she-cat warrior finds a mate (placeholder until relationships exist).</summary>
    [TuningGroup("Family")] public static float MateChancePerMoon = 0.15f;

    [TuningGroup("Family")] public static int MaxMateAgeDifferenceMoons = 36;

    /// <summary>She-cats can have kits from <see cref="WarriorAgeMoons" /> until this age.</summary>
    [TuningGroup("Family")] public static int FertileUntilMoons = 96;

    /// <summary>Chance per moon that a she-cat with a mate expects kits, before season and food are applied.</summary>
    [TuningGroup("Family")] public static float ConceptionChancePerMoon = 0.3f;

    /// <summary>
    ///     How much hunger lowers the chance of kits. With 5: if the Clan missed 20% of its meals this moon,
    ///     the chance is halved.
    /// </summary>
    [TuningGroup("Family")] public static float HungerConceptionPenalty = 5f;

    /// <summary>
    ///     Births get rarer the closer the Clan gets to the number of cats the territory feeds in this season.
    ///     With a good season, the Clan can grow beyond what leaf-bare can feed, and leaf-bare thins it out.
    /// </summary>
    [TuningGroup("Family")] public static Season CrowdingSeason = Season.Newleaf;

    /// <summary>Real cats are pregnant for 63–65 days.</summary>
    [TuningGroup("Family")] public static int PregnancyDays = 63;

    /// <summary>A pregnant queen moves to the nursery and stops hunting this many days before the birth.</summary>
    [TuningGroup("Family")] public static int NurseryDaysBeforeBirth = 21;

    [TuningGroup("Family")] public static float PregnancyExtraFoodNeed = 0.25f;

    /// <summary>Kits drink milk until this age.</summary>
    [TuningGroup("Family")] public static int WeanedAgeMoons = 2;

    /// <summary>Kits younger than this can't eat prey at all, only milk.</summary>
    [TuningGroup("Family")] public static int FirstPreyAgeMoons = 1;

    /// <summary>How many kits one queen's milk can feed. Queens share all kits in the nursery.</summary>
    [TuningGroup("Family")] public static int MaxKitsPerQueen = 6;

    /// <summary>Extra food a queen needs for every kit she nurses (real nursing queens need 2–3× as much).</summary>
    [TuningGroup("Family")] public static float MilkFoodNeedPerKit = 0.3f;

    /// <summary>How likely each litter size is. Index = number of kits, value = weight. Real litters: usually 3–4.</summary>
    public static readonly int[] LitterSizeWeights = { 0, 5, 15, 30, 30, 15, 5 };

    // Satiation
    [TuningGroup("Satiation")] public static float SatiationMax = 100f;
    [TuningGroup("Satiation")] public static int TicksUntilEmpty = 6;
    [TuningGroup("Satiation")] public static float EatBelowSatiation = 75f;
    [TuningGroup("Satiation")] public static float MouseSatiation = 25f;

    public static float SatiationLossPerTick => SatiationMax / TicksUntilEmpty;

    /// <summary>
    ///     How much food a cat needs compared to an adult, by age in moons. Ages in between are interpolated.
    ///     Based on real cats: kittens only nurse until about 1 moon and are weaned by 2 moons. Because they need
    ///     so much more food per kg, they eat about as much as an adult from 3 moons on, a little more while
    ///     growing fast, and are fully grown at about 12 moons.
    /// </summary>
    public static readonly (int AgeMoons, float FoodNeed)[] FoodNeedByAge = {
        (0, 0f),
        (1, 0f),
        (2, 0.8f),
        (3, 0.95f),
        (4, 1.05f),
        (6, 1.1f),
        (9, 1.15f),
        (12, 1f)
    };

    // Starvation
    /// <summary>
    ///     Every tick a starving cat doesn't eat adds a stack, every meal removes one.
    ///     At this many stacks the cat always dies (20 ticks = 5 days without food).
    /// </summary>
    [TuningGroup("Starvation")] public static int StarvationStacksToDie = 20;

    /// <summary>Up to this many stacks, starving can't kill a cat yet.</summary>
    [TuningGroup("Starvation")] public static int StarvationSafeStacks = 8;

    /// <summary>
    ///     Above <see cref="StarvationSafeStacks" />, every further stack adds this much chance per tick to die.
    ///     So not every starving cat dies at the same moment: e.g. at 14 stacks 6% per tick.
    /// </summary>
    [TuningGroup("Starvation")] public static float StarvationDeathChancePerStack = 0.01f;

    /// <summary>Kits and elders are frailer: their chance to die of starvation is multiplied by this.</summary>
    [TuningGroup("Starvation")] public static float FrailStarvationMultiplier = 2f;

    /// <summary>
    ///     The Clan is in a famine when at least this many cats carry starvation stacks.
    ///     Then elders and queens help hunting, and most leaders feed the hunters first.
    /// </summary>
    [TuningGroup("Starvation")] public static int FamineStarvingCats = 3;

    /// <summary>
    ///     A cat only counts as starving for a famine with at least this many stacks.
    ///     One stack after a hungry night is normal, it's not a famine yet.
    /// </summary>
    [TuningGroup("Starvation")] public static int FamineMinStacks = 2;

    /// <summary>
    ///     A famine only ends after fewer than <see cref="FamineStarvingCats" /> cats were starving for this many
    ///     ticks in a row (12 = three days). Keeps the famine from starting and ending every day.
    /// </summary>
    [TuningGroup("Starvation")] public static int FamineEndTicks = 12;

    /// <summary>Elders who help hunting in a famine are slower than they used to be.</summary>
    [TuningGroup("Starvation")] public static float ElderHuntingMultiplier = 0.6f;

    /// <summary>Each starvation stack lowers the cat's max energy by this much.</summary>
    [TuningGroup("Starvation")] public static float MaxEnergyLossPerStarvationStack = 4f;

    /// <summary>
    ///     Who eats first when there isn't enough prey. Pick one of <see cref="FeedingPolicy.Presets" />
    ///     or make your own with new FeedingPolicy(...).
    /// </summary>
    public static FeedingPolicy DefaultFeedingPolicy = FeedingPolicy.Balanced;

    // Energy
    [TuningGroup("Energy")] public static float EnergyMax = 100f;
    [TuningGroup("Energy")] public static int TicksUntilExhausted = 5;
    [TuningGroup("Energy")] public static float SleepBelowEnergy = 33f;
    [TuningGroup("Energy")] public static float SleepEnergy = 70f;

    /// <summary>Above this energy, tiredness has (almost) no effect.</summary>
    [TuningGroup("Energy")] public static float EnergyPenaltyStart = 80f;

    /// <summary>Below this energy, a cat can't do anything useful anymore.</summary>
    [TuningGroup("Energy")] public static float EnergyPenaltyFull = 5f;

    public static float EnergyLossPerTick => EnergyMax / TicksUntilExhausted;

    // Hunting
    [TuningGroup("Hunting")] public static int PatrolSize = 4;

    /// <summary>The leader sends patrols until they are expected to catch this much more than needed.</summary>
    [TuningGroup("Hunting")] public static float PatrolPlanningMargin = 1.2f;

    [TuningGroup("Hunting")] public static int AttemptsPerCat = 6;
    [TuningGroup("Hunting")] public static float BaseCatchChance = 0.1f;
    [TuningGroup("Hunting")] public static float CatchChancePerHuntingPoint = 0.006f;
    [TuningGroup("Hunting")] public static float CatchChancePerDiligencePoint = 0.002f;

    /// <summary>
    ///     How much prey the territory has per day, multiplied by <see cref="SeasonCatchMultiplier" />.
    ///     The more is caught in a day, the harder the next catch, until none is left.
    ///     This limits how many cats the territory can feed (an adult eats about 2.7 mice a day).
    /// </summary>
    [TuningGroup("Hunting")] public static float TerritoryPreyPerDay = 100f;

    /// <summary>
    ///     How suddenly prey gets scarce. With 4, hunting is almost normal until half of the day's prey is caught
    ///     (94% chance), then drops quickly (75% caught: 68%, 90% caught: 34%, all caught: 0%).
    /// </summary>
    [TuningGroup("Hunting")] public static float PreyDepletionSharpness = 4f;

    // Seasons
    /// <summary>How much easier or harder hunting is in each season. 1 = normal.</summary>
    [TuningGroup("Seasons")] public static float NewleafCatchMultiplier = 1.0f;

    [TuningGroup("Seasons")] public static float GreenleafCatchMultiplier = 1.3f;
    [TuningGroup("Seasons")] public static float LeafFallCatchMultiplier = 1.0f;
    [TuningGroup("Seasons")] public static float LeafBareCatchMultiplier = 0.8f;

    /// <summary>Cats mostly have kits in newleaf and greenleaf. Leaf-bare kits are rare.</summary>
    [TuningGroup("Seasons")] public static float NewleafConceptionMultiplier = 1.0f;

    [TuningGroup("Seasons")] public static float GreenleafConceptionMultiplier = 1.0f;
    [TuningGroup("Seasons")] public static float LeafFallConceptionMultiplier = 0.3f;
    [TuningGroup("Seasons")] public static float LeafBareConceptionMultiplier = 0.1f;

    // Randomness
    [TuningGroup("Randomness")] public static int RandomSeed = 12345;

    public static float SeasonCatchMultiplier(Season season) {
        return season switch {
            Season.Newleaf => NewleafCatchMultiplier,
            Season.Greenleaf => GreenleafCatchMultiplier,
            Season.LeafFall => LeafFallCatchMultiplier,
            Season.LeafBare => LeafBareCatchMultiplier,
            _ => 1.0f
        };
    }

    public static float SeasonConceptionMultiplier(Season season) {
        return season switch {
            Season.Newleaf => NewleafConceptionMultiplier,
            Season.Greenleaf => GreenleafConceptionMultiplier,
            Season.LeafFall => LeafFallConceptionMultiplier,
            Season.LeafBare => LeafBareConceptionMultiplier,
            _ => 1.0f
        };
    }
}
