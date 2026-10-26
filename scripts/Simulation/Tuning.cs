namespace WarriorsSim.Simulation;

/// <summary>
/// All balancing numbers of the simulation in one place.
/// See docs/Milestone1.md for what they mean.
/// </summary>
public static class Tuning
{
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

	// Hunting
	public const int PatrolSize = 4;
	/// <summary>The leader sends patrols until they are expected to catch this much more than needed.</summary>
	public const float PatrolPlanningMargin = 1.2f;
	public const int AttemptsPerCat = 6;
	public const float BaseCatchChance = 0.1f;
	public const float CatchChancePerHuntingPoint = 0.006f;
	public const float CatchChancePerDiligencePoint = 0.002f;

	/// <summary>How much easier or harder hunting is in each season. 1 = normal.</summary>
	public static float SeasonCatchMultiplier(Season season) => season switch
	{
		Season.Newleaf => 1.0f,
		Season.Greenleaf => 1.3f,
		Season.LeafFall => 1.0f,
		Season.LeafBare => 0.93f,
		_ => 1.0f,
	};

	// Randomness
	public const int RandomSeed = 12345;
}
