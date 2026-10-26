namespace WarriorsSim.Simulation;

/// <summary>
///     All balancing numbers of the simulation in one place.
///     See docs/Milestone1.md for what they mean.
/// </summary>
public static class Tuning {
    // Time
    public const int TicksPerDay = 4;
    public const int DaysPerMoon = 30;
    public const int MoonsPerSeason = 3;
    public const int DaysToSimulate = 720;

    // Skills and traits
    public const int SkillMin = 0;
    public const int SkillMax = 100;
    public const int AverageDiligence = 50;

    // Aging and ranks
    public const int ApprenticeAgeMoons = 6;
    public const int WarriorAgeMoons = 12;

    /// <summary>Warriors and deputies retire to the elders' den at this age. Leaders never retire.</summary>
    public const int RetireAgeMoons = 120;

    /// <summary>How much an apprentice's hunting skill improves every moon of training.</summary>
    public const int ApprenticeHuntingGainPerMoon = 8;

    /// <summary>From this age on, a cat can die of old age (96 moons = 8 years).</summary>
    public const int OldAgeStartMoons = 96;

    /// <summary>Chance per moon to die of old age at <see cref="OldAgeStartMoons" />.</summary>
    public const float OldAgeDeathChanceAtStart = 0.005f;

    /// <summary>The chance to die of old age doubles every this many moons.</summary>
    public const int OldAgeDeathChanceDoublingMoons = 18;

    // Mates, queens and kits
    /// <summary>Chance per moon that a single she-cat warrior finds a mate (placeholder until relationships exist).</summary>
    public const float MateChancePerMoon = 0.15f;

    public const int MaxMateAgeDifferenceMoons = 36;

    /// <summary>She-cats can have kits from <see cref="WarriorAgeMoons" /> until this age.</summary>
    public const int FertileUntilMoons = 96;

    /// <summary>Chance per moon that a she-cat with a mate expects kits, before season and food are applied.</summary>
    public const float ConceptionChancePerMoon = 0.3f;

    /// <summary>
    ///     How much hunger lowers the chance of kits. With 5: if the Clan missed 20% of its meals this moon,
    ///     the chance is halved.
    /// </summary>
    public const float HungerConceptionPenalty = 5f;

    /// <summary>
    ///     Births get rarer the closer the Clan gets to the number of cats the territory feeds in this season.
    ///     With a good season, the Clan can grow beyond what leaf-bare can feed, and leaf-bare thins it out.
    /// </summary>
    public const Season CrowdingSeason = Season.Newleaf;

    /// <summary>Real cats are pregnant for 63–65 days.</summary>
    public const int PregnancyDays = 63;

    /// <summary>A pregnant queen moves to the nursery and stops hunting this many days before the birth.</summary>
    public const int NurseryDaysBeforeBirth = 21;

    public const float PregnancyExtraFoodNeed = 0.25f;

    /// <summary>Kits drink milk until this age.</summary>
    public const int WeanedAgeMoons = 2;

    /// <summary>Kits younger than this can't eat prey at all, only milk.</summary>
    public const int FirstPreyAgeMoons = 1;

    /// <summary>How many kits one queen's milk can feed. Queens share all kits in the nursery.</summary>
    public const int MaxKitsPerQueen = 6;

    /// <summary>Extra food a queen needs for every kit she nurses (real nursing queens need 2–3× as much).</summary>
    public const float MilkFoodNeedPerKit = 0.3f;

    /// <summary>How likely each litter size is. Index = number of kits, value = weight. Real litters: usually 3–4.</summary>
    public static readonly int[] LitterSizeWeights = { 0, 5, 15, 30, 30, 15, 5 };

    // Satiation
    public const float SatiationMax = 100f;
    public const int TicksUntilEmpty = 6;
    public const float SatiationLossPerTick = SatiationMax / TicksUntilEmpty;
    public const float EatBelowSatiation = 75f;
    public const float MouseSatiation = 25f;

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

    /// <summary>
    ///     Every tick a starving cat doesn't eat adds a stack, every meal removes one.
    ///     At this many stacks the cat always dies (20 ticks = 5 days without food).
    /// </summary>
    public const int StarvationStacksToDie = 20;

    /// <summary>Up to this many stacks, starving can't kill a cat yet.</summary>
    public const int StarvationSafeStacks = 8;

    /// <summary>
    ///     Above <see cref="StarvationSafeStacks" />, every further stack adds this much chance per tick to die.
    ///     So not every starving cat dies at the same moment: e.g. at 14 stacks 6% per tick.
    /// </summary>
    public const float StarvationDeathChancePerStack = 0.01f;

    /// <summary>Kits and elders are frailer: their chance to die of starvation is multiplied by this.</summary>
    public const float FrailStarvationMultiplier = 2f;

    /// <summary>
    ///     The Clan is in a famine when at least this many cats carry starvation stacks.
    ///     Then elders and queens help hunting, and most leaders feed the hunters first.
    /// </summary>
    public const int FamineStarvingCats = 3;

    /// <summary>Elders who help hunting in a famine are slower than they used to be.</summary>
    public const float ElderHuntingMultiplier = 0.6f;

    /// <summary>Each starvation stack lowers the cat's max energy by this much.</summary>
    public const float MaxEnergyLossPerStarvationStack = 4f;

    /// <summary>
    ///     Who eats first when there isn't enough prey. Try Balanced, Compassionate, Ruthless or ByTheCode,
    ///     or make your own with new FeedingPolicy(...).
    /// </summary>
    public static readonly FeedingPolicy DefaultFeedingPolicy = FeedingPolicy.Balanced;

    // Energy
    public const float EnergyMax = 100f;
    public const int TicksUntilExhausted = 5;
    public const float EnergyLossPerTick = EnergyMax / TicksUntilExhausted;
    public const float SleepBelowEnergy = 33f;
    public const float SleepEnergy = 70f;

    /// <summary>Above this energy, tiredness has (almost) no effect.</summary>
    public const float EnergyPenaltyStart = 80f;

    /// <summary>Below this energy, a cat can't do anything useful anymore.</summary>
    public const float EnergyPenaltyFull = 5f;

    // Hunting
    public const int PatrolSize = 4;

    /// <summary>The leader sends patrols until they are expected to catch this much more than needed.</summary>
    public const float PatrolPlanningMargin = 1.2f;

    public const int AttemptsPerCat = 6;
    public const float BaseCatchChance = 0.1f;
    public const float CatchChancePerHuntingPoint = 0.006f;
    public const float CatchChancePerDiligencePoint = 0.002f;

    /// <summary>
    ///     How much prey the territory has per day, multiplied by <see cref="SeasonCatchMultiplier" />.
    ///     The more is caught in a day, the harder the next catch, until none is left.
    ///     This limits how many cats the territory can feed (an adult eats about 2.7 mice a day).
    /// </summary>
    public const float TerritoryPreyPerDay = 100f;

    /// <summary>
    ///     How suddenly prey gets scarce. With 4, hunting is almost normal until half of the day's prey is caught
    ///     (94% chance), then drops quickly (75% caught: 68%, 90% caught: 34%, all caught: 0%).
    /// </summary>
    public const float PreyDepletionSharpness = 4f;

    // Randomness
    public const int RandomSeed = 12345;

    /// <summary>How much easier or harder hunting is in each season. 1 = normal.</summary>
    public static float SeasonCatchMultiplier(Season season) {
        return season switch {
            Season.Newleaf => 1.0f,
            Season.Greenleaf => 1.3f,
            Season.LeafFall => 1.0f,
            Season.LeafBare => 0.8f,
            _ => 1.0f
        };
    }

    /// <summary>Cats mostly have kits in newleaf and greenleaf. Leaf-bare kits are rare.</summary>
    public static float SeasonConceptionMultiplier(Season season) {
        return season switch {
            Season.Newleaf => 1.0f,
            Season.Greenleaf => 1.0f,
            Season.LeafFall => 0.3f,
            Season.LeafBare => 0.1f,
            _ => 1.0f
        };
    }
}