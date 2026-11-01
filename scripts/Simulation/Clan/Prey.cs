namespace WarriorsSim.Simulation;

/// <summary>How good a piece of prey still is. It gets worse with every tick since it was caught.</summary>
public enum Freshness {
	Fresh,

	/// <summary>Still edible, but gives less food (<see cref="Tuning.StalePreyFoodShare" />).</summary>
	Stale,

	/// <summary>Rotten. Nobody eats it, it gets thrown out of the pile.</summary>
	Crowfood
}

/// <summary>One piece of prey on the fresh-kill pile. For now every piece is a mouse.</summary>
public class Prey {
	public Prey(int caughtAtTick) {
		CaughtAtTick = caughtAtTick;
	}

	/// <summary>The world tick it was caught in (see <see cref="World.TickNumber" />).</summary>
	public int CaughtAtTick { get; }

	/// <summary>
	///     Fresh for <see cref="Tuning.PreyFreshDays" />, then stale for <see cref="Tuning.PreyStaleDays" />, then
	///     crowfood.
	/// </summary>
	public Freshness FreshnessAt(int tick) {
		var ageDays = (tick - CaughtAtTick) / (float)Tuning.TicksPerDay;
		if (ageDays < Tuning.PreyFreshDays)
			return Freshness.Fresh;
		return ageDays < Tuning.PreyFreshDays + Tuning.PreyStaleDays ? Freshness.Stale : Freshness.Crowfood;
	}

	/// <summary>How much satiation it gives when eaten now. Crowfood gives nothing.</summary>
	public float FoodAt(int tick) {
		return FreshnessAt(tick) switch {
			Freshness.Fresh => Tuning.MouseSatiation,
			Freshness.Stale => Tuning.MouseSatiation * Tuning.StalePreyFoodShare,
			_ => 0f
		};
	}
}
