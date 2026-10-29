using System.Collections.Generic;

namespace WarriorsSim.Simulation;

/// <summary>A group of cats the leader sent out together, and what they brought back.</summary>
public class Patrol {
	public Patrol(IReadOnlyList<Cat> cats, bool isTraining, int miceCaught) {
		Cats = cats;
		IsTraining = isTraining;
		MiceCaught = miceCaught;
	}

	public IReadOnlyList<Cat> Cats { get; }

	/// <summary>A training patrol: apprentices with their teachers. They hunt too.</summary>
	public bool IsTraining { get; }

	public int MiceCaught { get; }
}
