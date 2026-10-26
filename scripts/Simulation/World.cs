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
	private List<Cat> _lastPatrol = new();

	// Statistics for the moon summary
	private int _miceCaughtThisMoon;
	private int _mealsMissedThisMoon;

	public Clan Clan { get; }
	public int Day { get; private set; } = 1;
	public TimeOfDay TimeOfDay { get; private set; } = TimeOfDay.Dawn;

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
		LoseSatiation();

		if (TimeOfDay == TimeOfDay.Night)
		{
			LogDaySummary();

			if (Day % Tuning.DaysPerMoon == 0)
				LogMoonSummary();
		}
		else
		{
			SendHuntingPatrolIfNeeded();
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
				Log($"{cat} is starving!");
		}
	}

	private void SendHuntingPatrolIfNeeded()
	{
		if (Clan.FreshKillPile.Mice >= Clan.Cats.Count)
			return;

		var patrol = ChooseHuntingPatrol();
		if (patrol.Count == 0)
		{
			Log("The pile is low, but no cat can hunt.");
			return;
		}

		var catches = new List<string>();
		int total = 0;
		foreach (var cat in patrol)
		{
			int caught = Hunt(cat);
			total += caught;
			catches.Add($"{cat.Name} {caught}");
		}

		Clan.FreshKillPile.Add(total);
		_lastPatrol = patrol;
		_miceCaughtThisMoon += total;
		Log($"Hunting patrol caught {total} mice ({string.Join(", ", catches)}). Pile: {Clan.FreshKillPile.Mice}");
	}

	/// <summary>
	/// Picks the best hunters who were not on the previous patrol.
	/// The leader and deputy only go if there aren't enough other cats.
	/// </summary>
	private List<Cat> ChooseHuntingPatrol()
	{
		return Clan.Cats
			.Where(c => c.CanHunt && !_lastPatrol.Contains(c))
			.OrderBy(c => c.Rank is Rank.Leader or Rank.Deputy)
			.ThenByDescending(c => c.CatchChance)
			.Take(Tuning.PatrolSize)
			.ToList();
	}

	/// <summary>Rolls every hunting attempt of one cat and returns how many mice it caught.</summary>
	private int Hunt(Cat cat)
	{
		int caught = 0;
		for (int i = 0; i < Tuning.AttemptsPerCat; i++)
		{
			if (_random.NextDouble() < cat.CatchChance)
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

		Log($"Moon {Day / Tuning.DaysPerMoon} over. Mice caught: {_miceCaughtThisMoon}, "
			+ $"meals missed: {_mealsMissedThisMoon}, pile: {Clan.FreshKillPile.Mice}, "
			+ $"average satiation: {averageSatiation:0}, starving: {starving}", LogLevel.Summary);

		_miceCaughtThisMoon = 0;
		_mealsMissedThisMoon = 0;
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
