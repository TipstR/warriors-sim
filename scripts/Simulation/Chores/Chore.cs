namespace WarriorsSim.Simulation;

/// <summary>The kinds of chores. For now only bringing prey, later e.g. gathering moss or ticks for the elders.</summary>
public enum ChoreKind {
	/// <summary>Carrying a cat's piece from the fresh-kill pile to a cat who stays in its den.</summary>
	BringPrey
}

/// <summary>How much of a cat's tick a chore takes.</summary>
public enum ChoreSize {
	/// <summary>Done in passing, next to the cat's other work in the same tick.</summary>
	Small,

	/// <summary>Takes the cat's whole tick. None yet.</summary>
	Big
}

/// <summary>
///     A small task in camp that some cat has to do. It waits on the Clan's chores (<see cref="Clan.Chores" />, the
///     "blackboard") until a cat responsible for it takes it.
/// </summary>
public class Chore {
	public Chore(ChoreKind kind, Cat forCat, int openedAtTick) {
		Kind = kind;
		ForCat = forCat;
		OpenedAtTick = openedAtTick;
	}

	public ChoreKind Kind { get; }

	/// <summary>The cat the chore is done for, e.g. the elder who gets the prey. Null if it is for the whole Clan.</summary>
	public Cat ForCat { get; }

	/// <summary>The tick the chore was put up in (see <see cref="World.TickNumber" />), to know how long it waited.</summary>
	public int OpenedAtTick { get; }

	/// <summary>
	///     Small chores are a crutch for 6-hour ticks: carrying a mouse takes minutes. With shorter ticks they could
	///     become real short activities. All chores are small until big chores come.
	/// </summary>
	public ChoreSize Size => ChoreSize.Small;
}
