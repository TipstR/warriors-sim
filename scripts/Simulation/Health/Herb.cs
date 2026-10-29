using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>The herbs a medicine cat gathers. Each one treats one condition, see <see cref="Tuning.HerbFor" />.</summary>
public enum Herb {
	/// <summary>For whitecough.</summary>
	Tansy,

	/// <summary>The only cure for greencough. Grows near Twolegplace and is very rare in leaf-bare.</summary>
	Catmint,

	/// <summary>Stops the bleeding of wounds and keeps them from getting infected. Found all year.</summary>
	Cobweb,

	/// <summary>For infected wounds.</summary>
	Marigold
}

/// <summary>The medicine cat's store of herbs, like the fresh-kill pile. Amounts are doses, one dose treats one cat for a day.</summary>
public class HerbStore {
	private readonly Dictionary<Herb, float> _doses = Enum.GetValues<Herb>().ToDictionary(h => h, _ => 0f);

	/// <summary>How many doses of the herb are left. Can be a fraction, because herbs slowly lose their freshness.</summary>
	public float Doses(Herb herb) {
		return _doses[herb];
	}

	public void Add(Herb herb, float doses) {
		_doses[herb] += Math.Max(0f, doses);
	}

	/// <summary>Takes one dose. Returns false if there isn't a whole dose left.</summary>
	public bool TryUse(Herb herb) {
		if (_doses[herb] < 1f)
			return false;

		_doses[herb]--;
		return true;
	}

	/// <summary>Every herb loses this share of its doses, e.g. 0.02 = 2%.</summary>
	public void Spoil(float share) {
		foreach (var herb in _doses.Keys.ToList())
			_doses[herb] *= 1f - share;
	}

	public override string ToString() {
		return string.Join(", ", _doses.Select(d => $"{d.Key.ToString().ToLowerInvariant()} {d.Value:0}"));
	}
}
