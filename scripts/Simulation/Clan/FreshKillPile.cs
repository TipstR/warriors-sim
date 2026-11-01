using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>
///     The Clan's shared food store: real pieces of prey that go bad one by one. Whoever eats first takes the best
///     piece, so the cats at the end of the eating order get what is left: stale prey or a shrew.
/// </summary>
public class FreshKillPile {
	private readonly List<Prey> _prey = new();

	/// <summary>Every piece on the pile, oldest first.</summary>
	public IReadOnlyList<Prey> Prey => _prey;

	/// <summary>How many pieces are on the pile (a half-eaten rabbit is one piece).</summary>
	public int Pieces => _prey.Count;

	/// <summary>How much food is on the pile, in portions (a mouse is one).</summary>
	public float Portions => _prey.Sum(p => p.PortionsLeft);

	public void Add(Prey prey) {
		_prey.Add(prey);
	}

	public void Add(IEnumerable<Prey> prey) {
		_prey.AddRange(prey);
	}

	/// <summary>How many pieces on the pile are this fresh right now.</summary>
	public int Count(Freshness freshness, int tick) {
		return _prey.Count(p => p.FreshnessAt(tick) == freshness);
	}

	/// <summary>
	///     The piece the next cat eats from. A fresh piece that was started is finished first (that is how a
	///     rabbit gets shared). Otherwise the freshest piece, and of those the biggest. Null if the pile is empty.
	/// </summary>
	public Prey ChooseNext(int tick) {
		var started = _prey
			.Where(p => p.IsStarted && p.FreshnessAt(tick) == Freshness.Fresh)
			.MaxBy(p => p.CaughtAtTick);
		if (started != null)
			return started;

		return _prey
			.OrderByDescending(p => p.CaughtAtTick)
			.ThenByDescending(p => p.PortionsLeft)
			.FirstOrDefault();
	}

	/// <summary>
	///     Eats one portion of the next piece (see <see cref="ChooseNext" />) and removes the piece once it is
	///     eaten up. Returns the piece, or null if the pile is empty; <paramref name="food" /> is the satiation.
	/// </summary>
	public Prey EatPortion(int tick, out float food) {
		var piece = ChooseNext(tick);
		if (piece == null) {
			food = 0f;
			return null;
		}

		food = piece.EatPortion(tick);
		if (piece.IsEaten)
			_prey.Remove(piece);
		return piece;
	}

	/// <summary>Removes every piece that has become crowfood and returns them.</summary>
	public List<Prey> ThrowOutCrowfood(int tick) {
		var rotten = _prey.Where(p => p.FreshnessAt(tick) == Freshness.Crowfood).ToList();
		_prey.RemoveAll(rotten.Contains);
		return rotten;
	}
}
