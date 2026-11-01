using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>
///     The Clan's shared food store: real pieces of prey that go bad one by one. Whoever eats first takes the best
///     piece, so the cats at the end of the eating order get what is left: the stale prey.
/// </summary>
public class FreshKillPile {
	private readonly List<Prey> _prey = new();

	/// <summary>Every piece on the pile, oldest first.</summary>
	public IReadOnlyList<Prey> Prey => _prey;

	/// <summary>How many pieces are on the pile. For now every piece is a mouse.</summary>
	public int Mice => _prey.Count;

	public void Add(Prey prey) {
		_prey.Add(prey);
	}

	/// <summary>How many pieces on the pile are this fresh right now.</summary>
	public int Count(Freshness freshness, int tick) {
		return _prey.Count(p => p.FreshnessAt(tick) == freshness);
	}

	/// <summary>Takes the freshest piece from the pile. Returns null if the pile is empty.</summary>
	public Prey TakeBest() {
		if (_prey.Count == 0)
			return null;

		var best = _prey.MaxBy(p => p.CaughtAtTick);
		_prey.Remove(best);
		return best;
	}

	/// <summary>Removes every piece that has become crowfood and returns how many there were.</summary>
	public int ThrowOutCrowfood(int tick) {
		return _prey.RemoveAll(p => p.FreshnessAt(tick) == Freshness.Crowfood);
	}
}
