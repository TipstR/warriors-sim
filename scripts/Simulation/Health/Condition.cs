using System;

namespace WarriorsSim.Simulation;

/// <summary>The kinds of sickness (and later injury) a cat can have.</summary>
public enum ConditionType {
	/// <summary>A mild cough. Spreads easily and can turn into greencough in weak cats.</summary>
	Whitecough,

	/// <summary>A deadly cough, especially for kits and elders. Spreads easily.</summary>
	Greencough,

	/// <summary>An injury, e.g. from hunting. Doesn't get worse on its own, but can get infected.</summary>
	Wound,

	/// <summary>A wound that got infected. Gets worse and can kill.</summary>
	InfectedWound
}

/// <summary>
///     One sickness or injury of one cat. Two values race each other: <see cref="Severity" /> (how bad it is) grows
///     every tick, and <see cref="Recovery" /> (the cat's body fighting it) grows too. If recovery reaches 100 first,
///     the cat is over the worst and the severity goes down until the cat is healthy. If the severity reaches 100
///     first, the cat dies. A plain wound doesn't grow, it only heals (or gets infected).
/// </summary>
public class Condition {
	public const float Max = 100f;

	/// <param name="caseSpeed">How fast this case gets worse compared to usual, e.g. 1.2 = a bad case.</param>
	/// <param name="recovery">How far the cat has already fought it, e.g. when whitecough turns into greencough.</param>
	/// <param name="description">What exactly it is, for the log, e.g. "a sprained paw". Null for the usual name.</param>
	public Condition(ConditionType type, float severity, float caseSpeed, float recovery = 0f,
		string description = null) {
		Type = type;
		Severity = Math.Clamp(severity, 0f, Max);
		CaseSpeed = caseSpeed;
		Recovery = Math.Clamp(recovery, 0f, Max);
		Name = description ?? NameOf(type);
	}

	public ConditionType Type { get; }

	/// <summary>For the log, e.g. "greencough" or "a sprained paw".</summary>
	public string Name { get; }

	/// <summary>How bad it is, from 0 (healthy) to <see cref="Max" /> (the cat dies).</summary>
	public float Severity { get; private set; }

	/// <summary>
	///     How far the cat's body has fought the condition, from 0 to <see cref="Max" />.
	///     At <see cref="Max" /> the cat is getting better.
	/// </summary>
	public float Recovery { get; private set; }

	/// <summary>How fast this case gets worse compared to usual. Some cases are worse than others.</summary>
	public float CaseSpeed { get; }

	/// <summary>The day the cat last got the right herb for this, or null. A dose works for the rest of that day.</summary>
	public int? HerbGivenOnDay { get; set; }

	/// <summary>The cat is over the worst, and the severity goes down.</summary>
	public bool IsHealing => Recovery >= Max;

	public bool IsCured => IsHealing && Severity <= 0f;

	/// <summary>Whether it can spread right now. Once the cat is over the worst, it doesn't infect others anymore.</summary>
	public bool IsContagious => Tuning.IsContagious(Type) && !IsHealing;
	public bool IsDeadly => Severity >= Max;

	/// <summary>The usual name for the log, e.g. "an infected wound".</summary>
	public static string NameOf(ConditionType type) {
		return type switch {
			ConditionType.Wound => "a wound",
			ConditionType.InfectedWound => "an infected wound",
			_ => type.ToString().ToLowerInvariant()
		};
	}

	/// <summary>Lets the condition run for one tick. Before the cat is healing, the severity grows and the cat recovers.</summary>
	public void Progress(float severityGain, float recoveryGain, float healing) {
		if (IsHealing) {
			Severity = Math.Max(0f, Severity - healing);
			return;
		}

		Severity = Math.Min(Max, Severity + severityGain * CaseSpeed);
		Recovery = Math.Min(Max, Recovery + recoveryGain);
	}
}
