using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>How good a piece of prey still is. It gets worse with every tick since it was caught.</summary>
public enum Freshness {
	Fresh,

	/// <summary>Still edible, but gives less food (<see cref="Tuning.StalePreyFoodShare" />).</summary>
	Stale,

	/// <summary>Rotten. Nobody eats it, it gets thrown out of the pile.</summary>
	Crowfood
}

/// <summary>The kinds of prey in the forest. What each kind is like is in <see cref="Tuning.PreyKinds" />.</summary>
public enum PreyKind {
	Shrew,
	Mouse,
	Vole,
	Bird,
	Squirrel,
	Rabbit
}

/// <summary>
///     What a kind of prey is like.
///     <para><c>Portions</c>: how much food it is. One portion is a mouse (<see cref="Tuning.PortionSatiation" />).</para>
///     <para>
///         <c>Hardness</c>: how hard it is to catch. The cat's catch chance is raised to this power, so hard prey
///         (above 1) is much harder for weak hunters than for strong ones, and easy prey (below 1) is caught by
///         almost anyone. A mouse is 1.
///     </para>
///     <para><c>SeasonWeights</c>: how often a cat finds this kind, in newleaf, greenleaf, leaf-fall and leaf-bare.</para>
/// </summary>
public record PreyKindStats(float Portions, float Hardness, float[] SeasonWeights);

/// <summary>
///     One piece of prey on the fresh-kill pile. Big prey (a rabbit, a squirrel) has several portions and is
///     shared: a cat eats at most one portion per tick.
/// </summary>
public class Prey {
	public Prey(PreyKind kind, int caughtAtTick) {
		Kind = kind;
		CaughtAtTick = caughtAtTick;
		PortionsLeft = Stats.Portions;
	}

	public PreyKind Kind { get; }
	public PreyKindStats Stats => Tuning.PreyKinds[Kind];

	/// <summary>The world tick it was caught in (see <see cref="World.TickNumber" />).</summary>
	public int CaughtAtTick { get; }

	public float PortionsLeft { get; private set; }

	/// <summary>Some cat has already eaten from it.</summary>
	public bool IsStarted => PortionsLeft < Stats.Portions;

	public bool IsEaten => PortionsLeft <= 0.001f;

	/// <summary>"mouse", "rabbit", ...</summary>
	public string Name => Kind.ToString().ToLowerInvariant();

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

	/// <summary>How much satiation one portion gives when eaten now. Crowfood gives nothing.</summary>
	public float FoodPerPortionAt(int tick) {
		return FreshnessAt(tick) switch {
			Freshness.Fresh => Tuning.PortionSatiation,
			Freshness.Stale => Tuning.PortionSatiation * Tuning.StalePreyFoodShare,
			_ => 0f
		};
	}

	/// <summary>A cat eats one portion (or what is left, if less) and gets the satiation for it.</summary>
	public float EatPortion(int tick) {
		var eaten = Math.Min(1f, PortionsLeft);
		PortionsLeft -= eaten;
		return eaten * FoodPerPortionAt(tick);
	}

	/// <summary>"a mouse", "2 mice", "nothing": the prey caught, by kind, e.g. "2 mice, a vole and a rabbit".</summary>
	public static string Describe(IEnumerable<Prey> prey) {
		var parts = prey
			.GroupBy(p => p.Kind)
			.OrderBy(g => g.Key)
			.Select(g => g.Count() == 1 ? $"a {g.First().Name}" : $"{g.Count()} {Plural(g.Key)}")
			.ToList();
		return parts.Count switch {
			0 => "nothing",
			1 => parts[0],
			_ => string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[^1]
		};
	}

	/// <summary>Portions written the same on every computer, e.g. "1.5" (never "1,5"), so logs can be read back.</summary>
	public static string FormatPortions(float portions) {
		return portions.ToString("0.#", CultureInfo.InvariantCulture);
	}

	private static string Plural(PreyKind kind) {
		return kind == PreyKind.Mouse ? "mice" : kind.ToString().ToLowerInvariant() + "s";
	}
}
