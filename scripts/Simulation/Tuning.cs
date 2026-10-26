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
	public const int DaysToSimulate = 360;

	// Skills and traits
	public const int SkillMin = 0;
	public const int SkillMax = 100;
	public const int AverageDiligence = 50;

	// Satiation
	public const float SatiationMax = 100f;
	public const int TicksUntilEmpty = 6;
	public const float SatiationLossPerTick = SatiationMax / TicksUntilEmpty;
	public const float EatBelowSatiation = 50f;
	public const float MouseSatiation = 25f;

	// Hunting
	public const int PatrolSize = 4;
	public const int AttemptsPerCat = 6;
	public const float BaseCatchChance = 0.20f;
	public const float CatchChancePerHuntingPoint = 0.005f;
	public const float CatchChancePerDiligencePoint = 0.002f;

	// Randomness
	public const int RandomSeed = 12345;
}
