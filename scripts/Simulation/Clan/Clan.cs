using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

public class Clan {
	private readonly List<Cat> _cats = new();
	private readonly List<Cat> _dead = new();

	public Clan(string name) {
		Name = name;
	}

	public string Name { get; }
	/// <summary>The living cats of the Clan.</summary>
	public IReadOnlyList<Cat> Cats => _cats;

	/// <summary>Cats that have died, in the order they died.</summary>
	public IReadOnlyList<Cat> Dead => _dead;
	public FreshKillPile FreshKillPile { get; } = new();
	public HerbStore HerbStore { get; } = new();

	/// <summary>The Clan's leader, or null if the Clan has none.</summary>
	public Cat Leader => _cats.FirstOrDefault(c => c.Rank == Rank.Leader);

	/// <summary>The Clan's deputy, or null if the Clan has none.</summary>
	public Cat Deputy => _cats.FirstOrDefault(c => c.Rank == Rank.Deputy);

	/// <summary>The apprentice this cat is mentoring right now, or null.</summary>
	public Cat ApprenticeOf(Cat mentor) {
		return _cats.FirstOrDefault(c => c.Rank == Rank.Apprentice && c.Mentor == mentor);
	}

	/// <summary>
	///     Called when a cat joins the Clan, e.g. a newborn kit. The starting cats joined before anyone could listen,
	///     so read them from <see cref="Cats" />.
	/// </summary>
	public event Action<Cat> CatAdded;

	/// <summary>Called when a cat of the Clan has died.</summary>
	public event Action<Cat> CatDied;

	public void AddCat(Cat cat) {
		_cats.Add(cat);
		CatAdded?.Invoke(cat);
	}

	/// <summary>Moves a cat from the living to the dead. Its mate becomes free to find a new one.</summary>
	public void RecordDeath(Cat cat) {
		if (!_cats.Remove(cat))
			return;

		_dead.Add(cat);
		cat.Mate?.LoseMate();
		CatDied?.Invoke(cat);
	}

	/// <summary>How the Clan decides who eats first. Later this will come from the leader's personality.</summary>
	public FeedingPolicy FeedingPolicy { get; set; } = Tuning.DefaultFeedingPolicy;

	/// <summary>
	///     Cats in the order they get to eat when there isn't enough prey for everyone, decided by the
	///     <see cref="FeedingPolicy" />. With the same score, the hungriest cat eats first.
	/// </summary>
	public IEnumerable<Cat> CatsInEatingOrder() {
		var famine = IsInFamine;
		return _cats.OrderByDescending(c => FeedingPolicy.Score(c, famine)).ThenBy(c => c.Satiation);
	}

	/// <summary>
	///     Whether the Clan is in a famine. Elders and queens then help with hunting, and most leaders feed the
	///     hunters first. Set by the world, see World.UpdateFamine.
	/// </summary>
	public bool IsInFamine { get; set; }

	/// <summary>How many cats are starving badly enough to count for a famine, see <see cref="Tuning.FamineMinStacks" />.</summary>
	public int StarvingCatCount => _cats.Count(c => c.StarvationStacks >= Tuning.FamineMinStacks);
}