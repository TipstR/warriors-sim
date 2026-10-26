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
    ///     At this many stacks the cat dies (20 ticks = 5 days without food).
    /// </summary>
    public const int StarvationStacksToDie = 20;

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
}