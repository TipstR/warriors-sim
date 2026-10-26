using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>
/// Runs the simulation one tick at a time and reports what happens to the log.
/// </summary>
public class World
{
	private readonly Random _random;
	private readonly Action<LogLevel, string> _log;
	private List<Cat> _lastHunters = new();

	// Statistics for the moon summary
	private int _miceCaughtThisMoon;
	private int _mealsMissedThisMoon;
	private int _timesStarvedThisMoon;

	public Clan Clan { get; }
	public int Day { get; private set; } = 1;
	public TimeOfDay TimeOfDay { get; private set; } = TimeOfDay.Dawn;

	/// <summary>The year starts with newleaf, and every season lasts the same number of moons.</summary>
	public Season Season => (Season)((Day - 1) / DaysPerSeason % 4);

	private const int DaysPerSeason = Tuning.DaysPerMoon * Tuning.MoonsPerSeason;

	/// <param name="log">Called with every line of the log and its level, e.g. to print it.</param>
	public World(Clan clan, int seed, Action<LogLevel, string> log)
	{
		Clan = clan;
		_random = new Random(seed);
		_log = log;
	}

	/// <summary>Advances the world by one tick (6 hours).</summary>
	public void Tick()
	{
		if (TimeOfDay == TimeOfDay.Dawn && (Day - 1) % DaysPerSeason == 0)
			Log($"{Season} begins.", LogLevel.Summary);

		LoseSatiation();

		if (TimeOfDay == TimeOfDay.Night)
		{
			LogDaySummary();

			if (Day % Tuning.DaysPerMoon == 0)
				LogMoonSummary();
		}
		else
		{
			SendHuntingPatrolsIfNeeded();
			FeedHungryCats();
		}

		AdvanceTime();
	}

	private void LoseSatiation()
	{
		foreach (var cat in Clan.Cats)
		{
			bool wasStarving = cat.IsStarving;
			cat.LoseSatiation(Tuning.SatiationLossPerTick);

			if (cat.IsStarving && !wasStarving)
			{
				Log($"{cat} is starving!");
				_timesStarvedThisMoon++;
			}
		}
	}

	/// <summary>
	/// If the pile has fewer mice than there are cats, the leader sends patrols
	/// until they are expected to catch enough to make up the difference.
	/// The leader plans with normal hunting chances and doesn't account for the
	/// season, so patrols bring back more than expected in greenleaf and less in leaf-bare.
	/// </summary>
	private void SendHuntingPatrolsIfNeeded()
	{
		int miceNeeded = Clan.Cats.Count - Clan.FreshKillPile.Mice;
		if (miceNeeded <= 0)
			return;

		var available = AvailableHunters();
		if (available.Count == 0)
		{
			Log("The pile is low, but no cat can hunt.");
			return;
		}

		var hunters = new List<Cat>();
		float expectedCatch = 0f;
		while (expectedCatch < miceNeeded * Tuning.PatrolPlanningMargin && available.Count > 0)
		{
			var patrol = available.Take(Tuning.PatrolSize).ToList();
			available.RemoveRange(0, patrol.Count);

			expectedCatch += patrol.Sum(c => c.CatchChance * Tuning.AttemptsPerCat);
			SendHuntingPatrol(patrol);
			hunters.AddRange(patrol);
		}

		_lastHunters = hunters;
	}

	/// <summary>
	/// Cats that can hunt and were not out hunting last time, best hunters first.
	/// The leader and deputy come last, so they only go if there aren't enough other cats.
	/// </summary>
	private List<Cat> AvailableHunters()
	{
		return Clan.Cats
			.Where(c => c.CanHunt && !_lastHunters.Contains(c))
			.OrderBy(c => c.Rank is Rank.Leader or Rank.Deputy)
			.ThenByDescending(c => c.CatchChance)
			.ToList();
	}

	private void SendHuntingPatrol(List<Cat> patrol)
	{
		var catches = new List<string>();
		int total = 0;
		foreach (var cat in patrol)
		{
			int caught = Hunt(cat);
			total += caught;
			catches.Add($"{cat.Name} {caught}");
		}

		Clan.FreshKillPile.Add(total);
		_miceCaughtThisMoon += total;
		Log($"Hunting patrol caught {total} mice ({string.Join(", ", catches)}). Pile: {Clan.FreshKillPile.Mice}");
	}

	/// <summary>Rolls every hunting attempt of one cat and returns how many mice it caught.</summary>
	private int Hunt(Cat cat)
	{
		float chance = Math.Clamp(cat.CatchChance * Tuning.SeasonCatchMultiplier(Season), 0f, 1f);

		int caught = 0;
		for (int i = 0; i < Tuning.AttemptsPerCat; i++)
		{
			if (_random.NextDouble() < chance)
				caught++;
		}
		return caught;
	}

	private void FeedHungryCats()
	{
		var ate = new List<string>();
		var wentHungry = new List<string>();

		foreach (var cat in Clan.CatsInEatingOrder().Where(c => c.IsHungry))
		{
			if (Clan.FreshKillPile.TryTakeMouse())
			{
				cat.Eat(Tuning.MouseSatiation);
				ate.Add(cat.Name);
			}
			else
			{
				wentHungry.Add(cat.Name);
			}
		}

		if (ate.Count > 0)
			Log($"{ate.Count} cats ate ({string.Join(", ", ate)}). Pile: {Clan.FreshKillPile.Mice}");

		if (wentHungry.Count > 0)
			Log($"No prey left for {string.Join(", ", wentHungry)}.");

		_mealsMissedThisMoon += wentHungry.Count;
	}

	private void LogDaySummary()
	{
		float averageSatiation = Clan.Cats.Average(c => c.Satiation);
		int hungry = Clan.Cats.Count(c => c.IsHungry);
		int starving = Clan.Cats.Count(c => c.IsStarving);

		Log($"End of day. Pile: {Clan.FreshKillPile.Mice}, average satiation: {averageSatiation:0}, "
			+ $"hungry: {hungry}, starving: {starving}");
	}

	private void LogMoonSummary()
	{
		float averageSatiation = Clan.Cats.Average(c => c.Satiation);
		int starving = Clan.Cats.Count(c => c.IsStarving);

		Log($"Moon {Day / Tuning.DaysPerMoon} ({Season}) over. Mice caught: {_miceCaughtThisMoon}, "
			+ $"meals missed: {_mealsMissedThisMoon}, times a cat starved: {_timesStarvedThisMoon}, "
			+ $"pile: {Clan.FreshKillPile.Mice}, average satiation: {averageSatiation:0}, "
			+ $"starving now: {starving}", LogLevel.Summary);

		_miceCaughtThisMoon = 0;
		_mealsMissedThisMoon = 0;
		_timesStarvedThisMoon = 0;
	}

	private void AdvanceTime()
	{
		if (TimeOfDay == TimeOfDay.Night)
		{
			Day++;
			TimeOfDay = TimeOfDay.Dawn;
		}
		else
		{
			TimeOfDay++;
		}
	}

	private void Log(string message, LogLevel level = LogLevel.Detail)
	{
		_log(level, $"Day {Day,3} {TimeOfDay,-7} | {message}");
	}
}
