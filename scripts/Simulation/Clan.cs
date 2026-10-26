using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

public class Clan
{
	private readonly List<Cat> _cats = new();

	public string Name { get; }
	public IReadOnlyList<Cat> Cats => _cats;
	public FreshKillPile FreshKillPile { get; } = new();

	public Clan(string name)
	{
		Name = name;
	}

	/// <summary>The Clan's leader, or null if the Clan has none.</summary>
	public Cat Leader => _cats.FirstOrDefault(c => c.Rank == Rank.Leader);

	public void AddCat(Cat cat)
	{
		_cats.Add(cat);
	}

	/// <summary>
	/// Cats in the order they get to eat when there isn't enough prey for everyone.
	/// Within the same rank, the hungriest cat eats first.
	/// </summary>
	public IEnumerable<Cat> CatsInEatingOrder()
	{
		return _cats.OrderBy(c => EatingPriority(c.Rank)).ThenBy(c => c.Satiation);
	}

	private static int EatingPriority(Rank rank) => rank switch
	{
		Rank.Kit => 0,
		Rank.Elder => 1,
		// Sick cats will go here (2) once sickness exists.
		Rank.Leader => 3,
		Rank.Deputy => 4,
		Rank.Warrior => 5,
		Rank.Apprentice => 6,
		_ => 7,
	};
}
