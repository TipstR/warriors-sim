using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>A group of cats that went hunting together, and what they brought back. A cat hunting alone is a patrol of one.</summary>
public class Patrol {
	public Patrol(IReadOnlyList<Cat> cats, bool isTraining, IReadOnlyList<Prey> caught) {
		Cats = cats;
		IsTraining = isTraining;
		Caught = caught;
	}

	public IReadOnlyList<Cat> Cats { get; }

	/// <summary>A training patrol: apprentices with their teachers. They hunt too.</summary>
	public bool IsTraining { get; }

	/// <summary>Every piece of prey the patrol brought back.</summary>
	public IReadOnlyList<Prey> Caught { get; }

	/// <summary>How much food the patrol brought back, in portions.</summary>
	public float PortionsCaught => Caught.Sum(p => p.Stats.Portions);
}
