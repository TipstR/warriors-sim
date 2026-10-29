namespace WarriorsSim.Simulation;

/// <summary>Where a cat sleeps. Sickness spreads most easily between cats in the same den.</summary>
public enum Den {
	Warriors,
	Apprentices,
	Nursery,
	Elders,
	Leader,

	/// <summary>The medicine cat's den. Medicine cats and their apprentices sleep here.</summary>
	Medicine
}
