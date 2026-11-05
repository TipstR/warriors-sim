using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

public class Clan {
	private readonly List<Cat> _cats = new();
	private readonly List<Cat> _dead = new();
	private readonly List<Chore> _chores = new();

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
		_chores.RemoveAll(c => c.ForCat == cat);
		CatDied?.Invoke(cat);
	}

	/// <summary>
	///     The Clan's chores: every chore that is open right now, oldest first. In programming terms the
	///     "blackboard": cats responsible for a chore take it from here. Later the UI can show it and the player can
	///     take chores from it.
	/// </summary>
	public IReadOnlyList<Chore> Chores => _chores;

	public void PostChore(Chore chore) {
		_chores.Add(chore);
	}

	/// <summary>Takes a chore off the list, because it is done or not needed anymore.</summary>
	public void RemoveChore(Chore chore) {
		_chores.Remove(chore);
	}

	/// <summary>The open chore of this kind for this cat, or null.</summary>
	public Chore FindChore(ChoreKind kind, Cat forCat) {
		return _chores.FirstOrDefault(c => c.Kind == kind && c.ForCat == forCat);
	}

	/// <summary>How the Clan decides who eats first. Later this will come from the leader's personality.</summary>
	public FeedingPolicy FeedingPolicy { get; set; } = Tuning.DefaultFeedingPolicy;

	/// <summary>
	///     Cats in the order they get to eat when there isn't enough prey for everyone, decided by the
	///     <see cref="FeedingPolicy" />.
	/// </summary>
	public IEnumerable<Cat> CatsInEatingOrder() {
		return FeedingPolicy.EatingOrder(_cats, IsInFamine);
	}

	/// <summary>
	///     Whether the Clan is in a famine. Elders and queens then help with hunting, and most leaders feed the
	///     hunters first. Set by the world, see World.UpdateFamine.
	/// </summary>
	public bool IsInFamine { get; set; }

	/// <summary>How many cats are starving badly enough to count for a famine, see <see cref="Tuning.FamineMinStacks" />.</summary>
	public int StarvingCatCount => _cats.Count(c => c.StarvationStacks >= Tuning.FamineMinStacks);
}