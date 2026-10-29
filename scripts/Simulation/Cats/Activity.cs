namespace WarriorsSim.Simulation;

/// <summary>
///     What a cat did during the last tick, so the game can show it. Where in camp a cat stays comes from its
///     <see cref="Den" />.
/// </summary>
public enum Activity {
	/// <summary>Awake in camp, with nothing special to do.</summary>
	InCamp,
	Sleeping,

	/// <summary>Lying in its nest, because it is too sick to get up or is a patient in the medicine den.</summary>
	Resting,

	/// <summary>Out on a hunting patrol.</summary>
	Hunting,

	/// <summary>Out on a training patrol, as an apprentice or as its teacher.</summary>
	Training,

	/// <summary>A medicine cat out gathering herbs.</summary>
	GatheringHerbs,

	/// <summary>Caring for the sick and injured in camp.</summary>
	Caring
}
